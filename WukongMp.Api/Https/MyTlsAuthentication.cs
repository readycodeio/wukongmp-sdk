using Org.BouncyCastle.Tls;

namespace WukongMp.Api.Https;

internal class MyTlsAuthentication : TlsAuthentication
{
    public void NotifyServerCertificate(TlsServerCertificate serverCertificate) { }

    public TlsCredentials? GetClientCredentials(CertificateRequest certificateRequest)
    {
        // return client certificate
        return null;
    }
}