using System.Security.Cryptography;
using System.Text;

namespace VetClinica.API.Services.Certificado;

/// <summary>
/// Criptografa/descriptografa o .pfx e a senha usando AES-256-GCM.
/// A chave de criptografia vem da variável de ambiente CERT_ENCRYPTION_KEY.
/// Se não estiver configurada, gera uma por tenant (menos seguro mas funcional).
/// </summary>
public static class CertificadoCryptoHelper
{
    private static byte[] GetKey()
    {
        var keyStr = Environment.GetEnvironmentVariable("CERT_ENCRYPTION_KEY");
        if (!string.IsNullOrWhiteSpace(keyStr))
        {
            // Usa SHA-256 da chave configurada para garantir 32 bytes
            return SHA256.HashData(Encoding.UTF8.GetBytes(keyStr));
        }
        // Fallback: chave derivada de uma constante + hostname (desenvolvimento apenas)
        var fallback = $"VetClinica_CertKey_{Environment.MachineName}_2026";
        return SHA256.HashData(Encoding.UTF8.GetBytes(fallback));
    }

    public static string Criptografar(byte[] dados)
    {
        var key   = GetKey();
        var nonce = RandomNumberGenerator.GetBytes(12); // 96 bits para AES-GCM
        var tag   = new byte[16];
        var cipher = new byte[dados.Length];

        using var aes = new AesGcm(key, 16);
        aes.Encrypt(nonce, dados, cipher, tag);

        // Formato: nonce(12) + tag(16) + cipher — tudo em Base64
        var combined = new byte[nonce.Length + tag.Length + cipher.Length];
        nonce.CopyTo(combined, 0);
        tag.CopyTo(combined, nonce.Length);
        cipher.CopyTo(combined, nonce.Length + tag.Length);

        return Convert.ToBase64String(combined);
    }

    public static string CriptografarTexto(string texto)
        => Criptografar(Encoding.UTF8.GetBytes(texto));

    public static byte[] Descriptografar(string base64)
    {
        var key      = GetKey();
        var combined = Convert.FromBase64String(base64);

        var nonce  = combined[..12];
        var tag    = combined[12..28];
        var cipher = combined[28..];
        var plain  = new byte[cipher.Length];

        using var aes = new AesGcm(key, 16);
        aes.Decrypt(nonce, cipher, tag, plain);

        return plain;
    }

    public static string DescriptografarTexto(string base64)
        => Encoding.UTF8.GetString(Descriptografar(base64));
}
