using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.X509;
using Org.BouncyCastle.Crypto;
using iText.Kernel.Pdf;
using iText.Signatures;

namespace VetClinica.API.Services.Certificado;

/// <summary>
/// Camada 1: Assinatura digital com certificado A1 (.pfx)
/// Usa iText7 com wrapper manual do BouncyCastle para compatibilidade máxima
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

    public async Task<byte[]> AssinarPdfAsync(byte[] pdfBytes, string motivo, string localidade)
    {
        if (!EstaConfigurado) return pdfBytes;
        return await Task.Run(() => AssinarPdf(pdfBytes, motivo, localidade));
    }

    private byte[] AssinarPdf(byte[] pdfBytes, string motivo, string localidade)
    {
        // Carrega .pfx usando BouncyCastle 2.x
        var storeBuilder = new Pkcs12StoreBuilder();
        var store = storeBuilder.Build();
        store.Load(new MemoryStream(_pfxBytes!), _senha!.ToCharArray());

        string alias = "";
        foreach (string al in store.Aliases)
        {
            if (store.IsKeyEntry(al)) { alias = al; break; }
        }

        var bcKey   = store.GetKey(alias).Key;
        var bcChain = store.GetCertificateChain(alias)
                           .Select(c => c.Certificate)
                           .ToArray();

        // Usa IExternalSignature com wrapper BouncyCastle puro
        IExternalSignature pks = new BouncyCastleKeySignature(bcKey);
        var chain = bcChain.Cast<iText.Commons.Bouncycastle.Cert.IX509Certificate>().ToArray();

        using var input  = new MemoryStream(pdfBytes);
        using var output = new MemoryStream();

        var reader = new PdfReader(input);
        var signer = new PdfSigner(reader, output, new StampingProperties().UseAppendMode());
        signer.SetFieldName("Assinatura_VetClinica");
        signer.SetReason(motivo);
        signer.SetLocation(localidade);

        signer.SignDetached(pks, chain, null, null, null, 0, PdfSigner.CryptoStandard.CMS);

        return output.ToArray();
    }
}

/// <summary>
/// Wrapper que implementa IExternalSignature usando chave BouncyCastle
/// </summary>
internal class BouncyCastleKeySignature : IExternalSignature
{
    private readonly AsymmetricKeyParameter _key;

    public BouncyCastleKeySignature(AsymmetricKeyParameter key) => _key = key;

    public string GetEncryptionAlgorithm() => "RSA";
    public string GetHashAlgorithm() => "SHA-256";

    public byte[] Sign(byte[] message)
    {
        var signer = Org.BouncyCastle.Security.SignerUtilities.GetSigner("SHA256withRSA");
        signer.Init(true, _key);
        signer.BlockUpdate(message, 0, message.Length);
        return signer.GenerateSignature();
    }

    public IExternalSignature WithSignMechanism(string signatureMechanismOid) => this;
}
