using System.Collections.Generic;
using System.Text;
using Org.BouncyCastle.Tls;
using Org.BouncyCastle.Tls.Crypto;

namespace WukongMp.Api.Https;

internal class BouncyCastleTlsClient(string host, TlsCrypto crypto) : DefaultTlsClient(crypto)
{
    public override TlsAuthentication GetAuthentication()
    {
        return new BouncyCastleTlsAuthentication(host);
    }

    public override int[] GetCipherSuites()
    {
        return
        [
            CipherSuite.TLS_ECDHE_RSA_WITH_AES_128_GCM_SHA256,
            CipherSuite.TLS_ECDHE_RSA_WITH_AES_256_GCM_SHA384,
            CipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_GCM_SHA256,
            CipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_GCM_SHA384
        ];
    }

    protected override ProtocolVersion[] GetSupportedVersions()
    {
        return [ProtocolVersion.TLSv12];
    }

    public override IDictionary<int, byte[]> GetClientExtensions()
    {
        var extensions = base.GetClientExtensions() ?? new Dictionary<int, byte[]>();
        TlsExtensionsUtilities.AddServerNameExtensionClient(extensions, [new ServerName(NameType.host_name, Encoding.UTF8.GetBytes(host))]);
        return extensions;
    }

    public override void NotifyConnectionClosed() { }
}