using iText.Commons.Bouncycastle.Cert;
using iText.Kernel.Pdf;
using iText.Signatures;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using iText.Bouncycastle.Crypto;
using iText.Bouncycastle.X509;

namespace VetClinica.API.Services.Certificado;

/// <summary>
/// Camada 1: Assinatura digital com certificado A1 (.pfx)
/// Compatível com iText7 8.x + BouncyCastle 2.x
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

    public bool EstaConfigurado => _pfxBytes != null && _pfxBytes.Length > 0 && !string.IsNullOrEmpty(_senha);

    public string? Descricao => EstaConfigurado ? $"A1 - {_titular ?? "Certificado"}" : null;

    public async Task<byte[]> AssinarPdfAsync(byte[] pdfBytes, string motivo, string localidade)
    {
        if (!EstaConfigurado) return pdfBytes;
        return await Task.Run(() => AssinarPdf(pdfBytes, motivo, localidade));
    }

    private byte[] AssinarPdf(byte[] pdfBytes, string motivo, string localidade)
    {
        // Carrega o .pfx com a API nova do BouncyCastle 2.x
        var builder = new Pkcs12StoreBuilder();
        var store   = builder.Build();
        store.Load(new MemoryStream(_pfxBytes!), _senha!.ToCharArray());

        // Pega o alias com chave privada
        string alias = "";
        foreach (string al in store.Aliases)
        {
            if (store.IsKeyEntry(al)) { alias = al; break; }
        }

        var privateKeyBc = store.GetKey(alias).Key;
        var chainBc = store.GetCertificateChain(alias)
            .Select(c => c.Certificate)
            .ToArray();

        // Converte para tipos iText7 8.x
        var privateKey = new BouncyCastlePrivateKey(privateKeyBc);
        var chain      = chainBc.Select(c => (IX509Certificate)new BouncyCastleX509Certificate(c)).ToArray();

        using var input  = new MemoryStream(pdfBytes);
        using var output = new MemoryStream();

        var reader  = new PdfReader(input);
        var signer  = new PdfSigner(reader, output, new StampingProperties().UseAppendMode());

        signer.SetFieldName("Assinatura_VetClinica");
        signer.SetReason(motivo);
        signer.SetLocation(localidade);
        signer.SetContact(_titular ?? "VetClinica");

        var signature = new PrivateKeySignature(privateKey, "SHA-256");
        signer.SignDetached(signature, chain, null, null, null, 0, PdfSigner.CryptoStandard.CMS);

        return output.ToArray();
    }
}
