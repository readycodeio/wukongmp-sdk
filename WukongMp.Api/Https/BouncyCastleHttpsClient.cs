using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HttpMachine;
using IHttpMachine.Model;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Tls;
using Org.BouncyCastle.Tls.Crypto.Impl.BC;

namespace WukongMp.Api.Https;

public class BouncyCastleHttpsClient(ILogger logger)
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<T?> GetAsync<T>(Uri url, Dictionary<string, string>? headers = null, CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();
        await _semaphore.WaitAsync(ct);
        try
        {
            var response = await GetRawAsync(url, headers, ct);

            if (response is null || response.StatusCode is < 200 or >= 300)
                return default;

            using var body = GetResponseBody(response);
            return await JsonSerializer.DeserializeAsync<T>(body, JsonOptions, cancellationToken: ct);
        }
        finally
        {
            _semaphore.Release();
            logger.LogInformation("GET {Url} completed in {ElapsedMilliseconds} ms", url.ToString(), stopwatch.ElapsedMilliseconds);
        }
    }

    public async Task<byte[]?> GetBytesAsync(Uri url, Dictionary<string, string>? headers = null, CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();
        await _semaphore.WaitAsync(ct);
        try
        {
            var response = await GetRawAsync(url, headers, ct);

            if (response is null || response.StatusCode is < 200 or >= 300)
                return null;

            using var body = GetResponseBody(response);

            using var ms = new MemoryStream();
            await body.CopyToAsync(ms);
            return ms.ToArray();
        }
        finally
        {
            _semaphore.Release();
            logger.LogInformation("GET bytes from {Url} completed in {ElapsedMilliseconds} ms", url.ToString(), stopwatch.ElapsedMilliseconds);
        }
    }

    /// <summary>
    /// Performs a PUT request to upload raw byte data, suitable for Azure Blob Storage with a SAS URL.
    /// </summary>
    /// <param name="url">The request URL, including SAS token if applicable.</param>
    /// <param name="headers">Required headers, e.g., "x-ms-blob-type: BlockBlob".</param>
    /// <param name="fileBytes">The raw file content to upload.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The HTTP status code of the response.</returns>
    public async Task<HttpStatusCode> PutBytesAsync(
        Uri url,
        Dictionary<string, string> headers,
        byte[] fileBytes,
        CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();
        await _semaphore.WaitAsync(ct);

        try
        {
            using var tcp = new TcpClient(url.Host, url.Port);
            using var stream = tcp.GetStream();

            Stream requestStream = stream;

            if (url.Scheme == "https")
            {
                // Setup TLS connection using BouncyCastle
                var crypto = new BcTlsCrypto();
                var protocol = new BouncyCastleTlsClient(url.Host, crypto);
                var tls = new TlsClientProtocol(stream);
                tls.Connect(protocol);
                requestStream = tls.Stream; // Use TLS stream for https
            }

            // Use ASCII encoding for headers and leave the stream open for the body
            using var writer = new StreamWriter(requestStream);
            writer.NewLine = "\r\n";

            // Write HTTP request line and headers
            await writer.WriteLineAsync($"PUT {url.PathAndQuery} HTTP/1.1");
            if (url is { Scheme: "http", Port: 80 } or { Scheme: "https", Port: 443 })
                await writer.WriteLineAsync($"Host: {url.Host}");
            else
                await writer.WriteLineAsync($"Host: {url.Host}:{url.Port}");
            await writer.WriteLineAsync("Connection: close");
            await writer.WriteLineAsync("Accept: application/xml, application/json, text/plain, */*");
            await writer.WriteLineAsync($"Content-Length: {fileBytes.Length}");
            await writer.WriteLineAsync("Content-Type: application/octet-stream");

            foreach (var header in headers)
                await writer.WriteLineAsync($"{header.Key}: {header.Value}");

            await writer.WriteLineAsync(); // End of headers
            await writer.FlushAsync();

            // Write body (the file bytes)
            await requestStream.WriteAsync(fileBytes, 0, fileBytes.Length, ct);
            await requestStream.FlushAsync(ct);

            // Read response
            using var handler = new HttpParserDelegate();
            using var parser = new HttpCombinedParser(handler);

            var buffer = new byte[8192];
            while (!ct.IsCancellationRequested)
            {
                int bytesRead;
                try
                {
                    // Read the next chunk of data from the network
                    bytesRead = await requestStream.ReadAsync(buffer, 0, buffer.Length, ct);
                }
                catch (TlsNoCloseNotifyException)
                {
                    // Treat a missing close_notify as the end of the stream
                    break;
                }
                catch (IOException) when (tcp.Client?.Connected == false)
                {
                    // The remote side closed the connection
                    break;
                }

                if (bytesRead == 0)
                {
                    // End of stream
                    break;
                }

                // Feed the chunk to the parser immediately.
                // The parser will build the response internally.
                using var chunkStream = new MemoryStream(buffer, 0, bytesRead, writable: false);
                parser.Execute(chunkStream);

                if (!parser.ShouldKeepAlive)
                {
                    if (handler.HttpRequestResponse is null)
                    {
                        handler.OnMessageEnd(parser);
                    }

                    break;
                }

                if (handler.HttpRequestResponse is not null)
                {
                    break;
                }
            }

            // Now, handler.HttpRequestResponse is populated correctly, even for errors.
            if (handler.HttpRequestResponse is null)
            {
                // This might happen if the server closes the connection without sending a valid response.
                logger.LogWarning("No valid HTTP response received for PUT to {Url}", url.ToString());
                return HttpStatusCode.ServiceUnavailable; // Or another appropriate status
            }

            return (HttpStatusCode)handler.HttpRequestResponse.StatusCode;
        }
        finally
        {
            _semaphore.Release();
            logger.LogInformation("PUT bytes to {Url} completed in {ElapsedMilliseconds} ms", url.ToString(), stopwatch.ElapsedMilliseconds);
        }
    }

    public async Task<HttpStatusCode> PutMultipartAsync(
        Uri url,
        Dictionary<string, object>? fields,
        string fileFieldName,
        string fileName,
        byte[] fileBytes,
        Dictionary<string, string>? headers = null,
        CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();
        await _semaphore.WaitAsync(ct);

        try
        {
            using var tcp = new TcpClient(url.Host, url.Port);
            using var stream = tcp.GetStream();

            Stream requestStream = stream;

            if (url.Scheme == "https")
            {
                // Setup TLS connection using BouncyCastle
                var crypto = new BcTlsCrypto();
                var protocol = new BouncyCastleTlsClient(url.Host, crypto);
                var tls = new TlsClientProtocol(stream);
                tls.Connect(protocol);
                requestStream = tls.Stream; // Use TLS stream for https
            }

            using var writer = new StreamWriter(requestStream);
            writer.NewLine = "\r\n";

            // Generate boundary
            var boundary = $"----BOUNDARY{DateTime.UtcNow.Ticks}";

            // Build multipart body
            using var bodyStream = new MemoryStream();
            var newline = "\r\n"u8.ToArray();

            if (fields != null)
            {
                foreach (var kv in fields)
                {
                    var fieldHeader = Encoding.UTF8.GetBytes(
                        $"--{boundary}\r\n" +
                        $"Content-Disposition: form-data; name=\"{kv.Key}\"\r\n\r\n"
                    );
                    var valueBytes = Encoding.UTF8.GetBytes(kv.Value.ToString() ?? "");
                    await bodyStream.WriteAsync(fieldHeader, 0, fieldHeader.Length, ct);
                    await bodyStream.WriteAsync(valueBytes, 0, valueBytes.Length, ct);
                    await bodyStream.WriteAsync(newline, 0, newline.Length, ct);
                }
            }

            // File part
            var fileHeader = Encoding.UTF8.GetBytes(
                $"--{boundary}\r\n" +
                $"Content-Disposition: form-data; name=\"{fileFieldName}\"; filename=\"{fileName}\"\r\n" +
                "Content-Type: application/octet-stream\r\n" +
                "Content-Encoding: gzip\r\n\r\n"
            );
            var endBoundary = Encoding.UTF8.GetBytes($"\r\n--{boundary}--\r\n");

            await bodyStream.WriteAsync(fileHeader, 0, fileHeader.Length, ct);

            // Compress file bytes with GZip
            using (var gzip = new GZipStream(bodyStream, CompressionLevel.Optimal, leaveOpen: true))
            {
                await gzip.WriteAsync(fileBytes, 0, fileBytes.Length, ct);
            }

            await bodyStream.WriteAsync(endBoundary, 0, endBoundary.Length, ct);

            var contentLength = bodyStream.Length;
            bodyStream.Position = 0;

            // Write HTTP request
            await writer.WriteLineAsync($"PUT {url.PathAndQuery} HTTP/1.1");
            await writer.WriteLineAsync($"Host: {url.Host}:{url.Port}");
            await writer.WriteLineAsync("Connection: close");
            await writer.WriteLineAsync($"Content-Type: multipart/form-data; boundary={boundary}");
            await writer.WriteLineAsync($"Content-Length: {contentLength}");

            if (headers != null)
            {
                foreach (var header in headers)
                    await writer.WriteLineAsync($"{header.Key}: {header.Value}");
            }

            await writer.WriteLineAsync(); // End of headers

            // Write body
            await writer.FlushAsync();
            await bodyStream.CopyToAsync(requestStream);
            await requestStream.FlushAsync(ct);

            // Read response
            using var handler = new HttpParserDelegate();
            using var parser = new HttpCombinedParser(handler);

            var responseStream = new MemoryStream();
            try
            {
                await requestStream.CopyToAsync(responseStream);
            }
            catch (TlsNoCloseNotifyException)
            {
                // ignore missing close_notify
            }

            parser.Execute(responseStream);

            if (handler.HttpRequestResponse is null)
            {
                return HttpStatusCode.NoContent;
            }

            return (HttpStatusCode)handler.HttpRequestResponse.StatusCode;
        }
        finally
        {
            _semaphore.Release();
            logger.LogInformation("PUT multipart to {Url} completed in {ElapsedMilliseconds} ms", url.ToString(), stopwatch.ElapsedMilliseconds);
        }
    }

    private async Task<HttpRequestResponse?> GetRawAsync(Uri url, Dictionary<string, string>? headers = null, CancellationToken ct = default)
    {
        using var tcp = new TcpClient(url.Host, url.Port);
        using var stream = tcp.GetStream();

        Stream requestStream = stream;

        if (url.Scheme == "https")
        {
            // Setup TLS connection using BouncyCastle
            var crypto = new BcTlsCrypto();
            var protocol = new BouncyCastleTlsClient(url.Host, crypto);
            var tls = new TlsClientProtocol(stream);
            tls.Connect(protocol);
            requestStream = tls.Stream; // Use TLS stream for https
        }

        using var writer = new StreamWriter(requestStream);
        writer.NewLine = "\r\n";

        // Write HTTP request
        await writer.WriteLineAsync($"GET {url.PathAndQuery} HTTP/1.1");
        await writer.WriteLineAsync($"Host: {url.Host}:{url.Port}");
        await writer.WriteLineAsync("Connection: close");

        if (headers != null)
        {
            foreach (var header in headers)
            {
                await writer.WriteLineAsync($"{header.Key}: {header.Value}");
            }
        }

        await writer.WriteLineAsync();
        await writer.FlushAsync();

        // Read response
        using var handler = new HttpParserDelegate();
        using var parser = new HttpCombinedParser(handler);

        var buffer = new byte[8192];
        // We will collect any leftover bytes into a MemoryStream only if the parser requires it.
        // But the parser.Execute can be called on small chunks.
        while (!ct.IsCancellationRequested)
        {
            int bytesRead;
            try
            {
                bytesRead = await requestStream.ReadAsync(buffer, 0, buffer.Length, ct);
            }
            catch (IOException) when (tcp.Client?.Connected == false)
            {
                // remote closed; stop reading
                break;
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (TlsNoCloseNotifyException)
            {
                // Some TLS implementations throw on missing close_notify; treat as EOF
                break;
            }

            if (bytesRead == 0)
                break; // remote closed connection

            // Feed the parser with the chunk we just read
            using var chunkStream = new MemoryStream(buffer, 0, bytesRead, writable: false);
            parser.Execute(chunkStream);

            // If we have a response and Content-Length header, stop when we've read enough
            if (handler.HttpRequestResponse is not null)
            {
                if (handler.HttpRequestResponse.Headers.TryGetValue("CONTENT-LENGTH", out var vals) &&
                    long.TryParse(vals.FirstOrDefault(), out var expectedLength))
                {
                    if (handler.HttpRequestResponse.Body.Length >= expectedLength)
                        break;
                }
                else
                {
                    // No content-length but the parser set the response once complete.
                    break;
                }
            }
        }

        if (handler.HttpRequestResponse is null)
        {
            return null;
        }

        handler.HttpRequestResponse.Body.Position = 0;
        return handler.HttpRequestResponse;
    }

    private static Stream GetResponseBody(HttpRequestResponse response)
    {
        if (!response.Headers.TryGetValue("CONTENT-ENCODING", out var encodings))
            return response.Body;

        var encoding = encodings.First().ToLowerInvariant();
        return encoding switch
        {
            "gzip" => new GZipStream(response.Body, CompressionMode.Decompress, leaveOpen: false),
            "deflate" => new DeflateStream(response.Body, CompressionMode.Decompress, leaveOpen: false),
            _ => throw new NotSupportedException($"Unsupported content encoding: {encoding}")
        };
    }
}