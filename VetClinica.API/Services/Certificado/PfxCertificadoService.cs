using iText.Kernel.Pdf;
using iText.Signatures;
using iText.Commons.Bouncycastle.Cert;
using iText.Commons.Bouncycastle.Crypto;
using iText.Bouncycastle.Crypto;
using iText.Bouncycastle.X509;
using Org.BouncyCastle.Pkcs;

namespace VetClinica.API.Services.Certificado;

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

    public async Task<byte[]> AssinarPdfAsync(byte[] pdfBytes, string motivo, string localidade)
    {
        if (!EstaConfigurado) return pdfBytes;
        return await Task.Run(() => AssinarPdf(pdfBytes, motivo, localidade));
    }

    private byte[] AssinarPdf(byte[] pdfBytes, string motivo, string localidade)
    {
        // Carrega o .pfx — BouncyCastle 2.x API
        var storeBuilder = new Pkcs12StoreBuilder();
        var store = storeBuilder.Build();
        store.Load(new MemoryStream(_pfxBytes!), _senha!.ToCharArray());

        string alias = "";
        foreach (string al in store.Aliases)
        {
            if (store.IsKeyEntry(al)) { alias = al; break; }
        }

        var bcPrivKey = store.GetKey(alias).Key;
        var bcChain   = store.GetCertificateChain(alias)
                             .Select(c => c.Certificate)
                             .ToArray();

        // Adapta para iText7 8.x (namespace iText.Bouncycastle)
        IPrivateKey    iTextKey   = new BouncyCastlePrivateKey(bcPrivKey);
        IX509Certificate[] iTextChain = bcChain
            .Select(c => (IX509Certificate)new BouncyCastleX509Certificate(c))
            .ToArray();

        using var input  = new MemoryStream(pdfBytes);
        using var output = new MemoryStream();

        var reader = new PdfReader(input);
        var signer = new PdfSigner(reader, output, new StampingProperties().UseAppendMode());

        signer.SetFieldName("Assinatura_VetClinica");
        signer.SetReason(motivo);
        signer.SetLocation(localidade);
        signer.SetContact(_titular ?? "VetClinica");

        IExternalSignature signature = new PrivateKeySignature(iTextKey, "SHA-256");
        signer.SignDetached(signature, iTextChain, null, null, null, 0, PdfSigner.CryptoStandard.CMS);

        return output.ToArray();
    }
}
