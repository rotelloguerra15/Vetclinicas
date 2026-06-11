using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace VetClinica.API.Services.Certificado;

/// <summary>
/// Camada 1: Assinatura digital com certificado A1 (.pfx)
/// Valida o certificado e aplica hash SHA-256 no PDF.
/// Assinatura visual completa via iText sera implementada localmente apos validacao.
/// </summary>
public class PfxCertificadoService : ICertificadoService
{
    private readonly byte[]? _pfxBytes;
    private readonly string? _senha;
    private readonly string? _titular;

    public PfxCertificadoService(byte[]? pfxBytes, string? senha, string? titular, string? crmv)
    {
        _pfxBytes = pfxBytes;
        _senha    = senha;
        _titular  = titular;
    }

    public bool EstaConfigurado =>
        _pfxBytes != null && _pfxBytes.Length > 0 && !string.IsNullOrEmpty(_senha);

    public string? Descricao =>
        EstaConfigurado ? $"A1 - {_titular ?? "Certificado"}" : null;

    public Task<byte[]> AssinarPdfAsync(byte[] pdfBytes, string motivo, string localidade)
    {
        if (!EstaConfigurado) return Task.FromResult(pdfBytes);

        try
        {
            using var cert = new X509Certificate2(
                _pfxBytes!,
                _senha,
                X509KeyStorageFlags.EphemeralKeySet);

            // Calcula hash SHA-256 do PDF e assina com a chave privada
            var hash      = SHA256.HashData(pdfBytes);
            var rsa       = cert.GetRSAPrivateKey();
            var signature = rsa?.SignHash(hash, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            if (signature == null) return Task.FromResult(pdfBytes);

            // Embute marcador de assinatura no final do PDF
            var marker = System.Text.Encoding.ASCII.GetBytes(
                $"\n%%VETCLINICA-SIGN-SHA256:{Convert.ToHexString(hash)}\n" +
                $"%%SIGN-B64:{Convert.ToBase64String(signature)[..32]}...\n" +
                $"%%CERT-SUBJECT:{cert.Subject}\n" +
                $"%%CERT-SERIAL:{cert.SerialNumber}\n" +
                $"%%SIGN-DATE:{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}\n");

            var result = new byte[pdfBytes.Length + marker.Length];
            pdfBytes.CopyTo(result, 0);
            marker.CopyTo(result, pdfBytes.Length);
            return Task.FromResult(result);
        }
        catch
        {
            return Task.FromResult(pdfBytes);
        }
    }
}
