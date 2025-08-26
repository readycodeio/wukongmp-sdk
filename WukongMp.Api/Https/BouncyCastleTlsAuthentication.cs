using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Tls;
using Org.BouncyCastle.Tls.Crypto;
using Org.BouncyCastle.X509;
using WukongMp.Api.WukongUtils;
using X509Certificate = Org.BouncyCastle.X509.X509Certificate;

namespace WukongMp.Api.Https;

internal class BouncyCastleTlsAuthentication : TlsAuthentication
{
    private static X509Certificate[] _trustedRoots = null!;
    private readonly string _host;

    public BouncyCastleTlsAuthentication(string host)
    {
        _host = host;

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (_trustedRoots is not null)
            return;

        // Load root CAs from a PEM file called "cacert.pem" where the DLL is
        var path = Path.Combine(GameSaveUtils.GetModDirectory(), "cacert.pem");
        using var stream = File.OpenRead(path);

        using var reader = new StreamReader(stream);
        var pemReader = new PemReader(reader);

        var roots = new List<X509Certificate>();
        while (pemReader.ReadObject() is { } obj)
        {
            if (obj is X509Certificate cert)
                roots.Add(cert);
        }

        _trustedRoots = [.. roots];
    }

    public void NotifyServerCertificate(TlsServerCertificate serverCertificate)
    {
        ValidateServerCertificate(serverCertificate.Certificate.GetCertificateList());
    }

    public TlsCredentials? GetClientCredentials(CertificateRequest certificateRequest)
    {
        // return client certificate
        return null;
    }

    private void ValidateServerCertificate(TlsCertificate[] chain)
    {
        if (chain.Length == 0)
            throw new TlsFatalAlert(AlertDescription.bad_certificate);

        var parser = new X509CertificateParser();
        var certs = chain.Select(c => parser.ReadCertificate(c.GetEncoded())).ToList();

        // Verify chain in any order

        // 1. Find all permutations of indices 0 .. certs.Count-1
        var indices = Enumerable.Range(0, certs.Count).ToArray();
        var permutations = GetPermutations(indices);

        // 2. Try each permutation until one works
        var validChain = false;
        X509Certificate? root = null;
        X509Certificate? leaf = null;
        foreach (var perm in permutations)
        {
            try
            {
                for (var i = 0; i < perm.Length - 1; i++)
                {
                    certs[perm[i]].Verify(certs[perm[i + 1]].GetPublicKey());
                }

                validChain = true;
                leaf = certs[perm[0]];
                root = certs[perm[^1]];
                break;
            }
            catch
            {
                // ignored
            }
        }

        if (!validChain || root is null || leaf is null)
            throw new TlsFatalAlert(AlertDescription.bad_certificate);

        // Last certificate must be signed by a trusted root
        var trusted = false;
        foreach (var trustedRoot in _trustedRoots)
        {
            try
            {
                root.Verify(trustedRoot.GetPublicKey());
                trusted = true;
                break;
            }
            catch
            {
                // ignored
            }
        }

        if (!trusted)
            throw new TlsFatalAlert(AlertDescription.bad_certificate);

        // Hostname validation
        if (!VerifyHostname(leaf))
            throw new TlsFatalAlert(AlertDescription.bad_certificate);
    }

    private static IEnumerable<int[]> GetPermutations(int[] list)
    {
        if (list.Length == 1)
        {
            yield return list;
            yield break;
        }

        for (var i = 0; i < list.Length; i++)
        {
            var current = list[i];
            var remaining = list.Where((_, index) => index != i).ToArray();
            foreach (var perm in GetPermutations(remaining))
            {
                yield return new[] { current }.Concat(perm).ToArray();
            }
        }
    }

    private bool VerifyHostname(X509Certificate cert)
    {
        // Check SAN
        var sanExt = cert.GetExtensionValue(X509Extensions.SubjectAlternativeName);
        if (sanExt != null)
        {
            var asn1 = Asn1Object.FromByteArray(sanExt.GetOctets());
            var seq = Asn1Sequence.GetInstance(asn1);
            foreach (var entry in seq)
            {
                var genName = GeneralName.GetInstance(entry);
                if (genName.TagNo == GeneralName.DnsName)
                {
                    var withoutWildcard = genName.Name.ToString().Replace("*", "");
                    if (_host.EndsWith(withoutWildcard))
                        return true;
                }
            }
        }

        // Fallback to CN
        var cn = cert.SubjectDN.GetValueList(X509Name.CN);
        if (cn != null)
        {
            foreach (var name in cn)
            {
                if (string.Equals(_host, name, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }

        return false;
    }
}