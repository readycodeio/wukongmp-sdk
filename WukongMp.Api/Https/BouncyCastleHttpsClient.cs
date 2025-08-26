using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
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
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<T?> GetAsync<T>(Uri url, Dictionary<string, string>? headers = null, CancellationToken ct = default)
    {
        var response = await GetRaw(url, headers);
        return await JsonSerializer.DeserializeAsync<T>(response.Body, JsonOptions, cancellationToken: ct);
    }

    public async Task<byte[]?> GetBytesAsync(Uri url, Dictionary<string, string>? headers = null)
    {
        var response = await GetRaw(url, headers);

        using var ms = new MemoryStream();
        await response.Body.CopyToAsync(ms);
        return ms.ToArray();
    }

    private async Task<HttpRequestResponse> GetRaw(Uri url, Dictionary<string, string>? headers = null)
    {
        using var tcp = new TcpClient(url.Host, url.Port);
        using var stream = tcp.GetStream();

        // Setup TLS connection using BouncyCastle
        var crypto = new BcTlsCrypto();
        var protocol = new MyTlsClient(url.Host, crypto);
        var tls = new TlsClientProtocol(stream);
        tls.Connect(protocol);

        using var tlsStream = tls.Stream;
        using var writer = new StreamWriter(tlsStream);

        // Write HTTP request
        await writer.WriteLineAsync($"GET {url.PathAndQuery} HTTP/1.1");
        await writer.WriteLineAsync($"Host: {url.Host}");
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
            await tlsStream.CopyToAsync(memoryStream);
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