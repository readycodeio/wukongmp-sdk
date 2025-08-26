using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HttpMachine;
using IHttpMachine.Model;
using Org.BouncyCastle.Tls;
using Org.BouncyCastle.Tls.Crypto.Impl.BC;

namespace WukongMp.Api.Https;

public class BouncyCastleHttpsClient
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<T?> GetAsync<T>(Uri url, Dictionary<string, string>? headers = null, CancellationToken ct = default)
    {
        await _semaphore.WaitAsync(ct);
        try
        {
            var response = await GetRaw(url, headers);
            return await JsonSerializer.DeserializeAsync<T>(response.Body, JsonOptions, cancellationToken: ct);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<byte[]?> GetBytesAsync(Uri url, Dictionary<string, string>? headers = null, CancellationToken ct = default)
    {
        await _semaphore.WaitAsync(ct);
        try
        {
            var response = await GetRaw(url, headers);

            using var ms = new MemoryStream();
            await response.Body.CopyToAsync(ms);
            return ms.ToArray();
        }
        finally
        {
            _semaphore.Release();
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
                "Content-Type: application/octet-stream\r\n\r\n"
            );
            var endBoundary = Encoding.UTF8.GetBytes($"\r\n--{boundary}--\r\n");

            await bodyStream.WriteAsync(fileHeader, 0, fileHeader.Length, ct);
            await bodyStream.WriteAsync(fileBytes, 0, fileBytes.Length, ct);
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
        }
    }

    private async Task<HttpRequestResponse> GetRaw(Uri url, Dictionary<string, string>? headers = null)
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

        var memoryStream = new MemoryStream();
        try
        {
            await requestStream.CopyToAsync(memoryStream);
        }
        catch (TlsNoCloseNotifyException)
        {
            // ignored
        }

        parser.Execute(memoryStream);
        handler.HttpRequestResponse.Body.Position = 0;
        return handler.HttpRequestResponse;
    }
}