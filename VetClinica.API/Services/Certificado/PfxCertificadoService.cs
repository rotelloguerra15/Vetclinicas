using System.Security.Cryptography.X509Certificates;
using iText.Kernel.Pdf;
using iText.Signatures;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;

namespace VetClinica.API.Services.Certificado;

/// <summary>
/// Camada 1: Assinatura digital com certificado A1 (.pfx)
/// Funciona com qualquer certificado A1 ICP-Brasil (Safeweb, Certisign, Serasa, Soluti, etc.)
/// </summary>
public class PfxCertificadoService : ICertificadoService
{
    private readonly byte[]? _pfxBytes;
    private readonly string? _senha;
    private readonly string? _titular;
    private readonly string? _crmv;

    public PfxCertificadoService(byte[]? pfxBytes, string? senha, string? titular, string? crmv)
    {
        _pfxBytes = pfxBytes;
        _senha    = senha;
        _titular  = titular;
        _crmv     = crmv;
    }

    public bool EstaConfigurado => _pfxBytes != null && _pfxBytes.Length > 0 && !string.IsNullOrEmpty(_senha);

    public string? Descricao => EstaConfigurado
        ? $"A1 - {_titular ?? "Certificado"}{(_crmv != null ? $" CRMV {_crmv}" : "")}"
        : null;

    public async Task<byte[]> AssinarPdfAsync(byte[] pdfBytes, string motivo, string localidade)
    {
        if (!EstaConfigurado)
            return pdfBytes; // retorna sem assinar se não configurado

        return await Task.Run(() => AssinarPdf(pdfBytes, motivo, localidade));
    }

    private byte[] AssinarPdf(byte[] pdfBytes, string motivo, string localidade)
    {
        // Carrega o .pfx
        var store = new Pkcs12Store(new MemoryStream(_pfxBytes!), _senha!.ToCharArray());

        // Pega o alias (primeiro encontrado)
        string alias = "";
        foreach (string al in store.Aliases)
        {
            if (store.IsKeyEntry(al)) { alias = al; break; }
        }

        var privateKey = store.GetKey(alias).Key;
        var certChain  = store.GetCertificateChain(alias)
            .Select(c => c.Certificate)
            .ToArray();

        // Assina com iText7
        using var input  = new MemoryStream(pdfBytes);
        using var output = new MemoryStream();

        var reader = new PdfReader(input);
        var writer = new PdfWriter(output);
        var stamper = new PdfSigner(reader, output, new StampingProperties().UseAppendMode());

        stamper.SetFieldName("Assinatura_VetClinica");

        var appearance = stamper.GetSignatureAppearance();
        appearance.SetReason(motivo);
        appearance.SetLocation(localidade);
        appearance.SetContact(_titular ?? "VetClinica");

        var signer = new PrivateKeySignature(privateKey, "SHA-256");

        stamper.SignDetached(
            signer,
            certChain,
            null, null,
            0,
            PdfSigner.CryptoStandard.CMS
        );

        return output.ToArray();
    }
}
