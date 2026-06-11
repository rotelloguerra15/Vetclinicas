namespace VetClinica.API.Services.Certificado;

/// <summary>
/// Interface plugável para assinatura digital de PDFs.
/// Camada 1: A1 (PfxCertificadoService)
/// Camada 2: A3 nuvem (ex: SafewebCloudService, CertisignCloudService) — implementar quando necessário
/// </summary>
public interface ICertificadoService
{
    /// <summary>
    /// Assina um PDF e retorna os bytes do PDF assinado.
    /// </summary>
    Task<byte[]> AssinarPdfAsync(byte[] pdfBytes, string motivo, string localidade);

    /// <summary>
    /// Verifica se o serviço está configurado e pronto para assinar.
    /// </summary>
    bool EstaConfigurado { get; }

    /// <summary>
    /// Nome/tipo do certificado configurado (ex: "A1 - Barbara Fonseca CRMV 1234")
    /// </summary>
    string? Descricao { get; }
}
