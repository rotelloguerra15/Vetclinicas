namespace VetClinica.API.Services.Certificado;

/// <summary>
/// Implementação nula — retorna o PDF sem assinar.
/// Usada quando nenhum certificado está configurado.
/// Também serve de base para implementações futuras de A3 nuvem.
/// 
/// Para adicionar A3 nuvem (ex: Safeweb Cloud API):
/// 1. Crie SafewebCloudCertificadoService : ICertificadoService
/// 2. Implemente AssinarPdfAsync chamando a API da Safeweb
/// 3. Registre no DI em Program.cs baseado no cert_tipo do tenant
/// </summary>
public class NullCertificadoService : ICertificadoService
{
    public bool EstaConfigurado => false;
    public string? Descricao    => null;

    public Task<byte[]> AssinarPdfAsync(byte[] pdfBytes, string motivo, string localidade)
        => Task.FromResult(pdfBytes); // retorna sem modificar
}

/// <summary>
/// PLACEHOLDER — Camada 2: A3 nuvem Safeweb
/// Implementar quando necessário, seguindo a interface ICertificadoService.
/// Documentação API Safeweb: https://developers.safeweb.com.br
/// </summary>
// public class SafewebCloudCertificadoService : ICertificadoService
// {
//     private readonly string _token;
//     private readonly IHttpClientFactory _http;
//
//     public SafewebCloudCertificadoService(string token, IHttpClientFactory http)
//     { _token = token; _http = http; }
//
//     public bool EstaConfigurado => !string.IsNullOrEmpty(_token);
//     public string? Descricao => "A3 Nuvem - Safeweb";
//
//     public async Task<byte[]> AssinarPdfAsync(byte[] pdfBytes, string motivo, string localidade)
//     {
//         // POST https://api.safeweb.com.br/sign
//         // Authorization: Bearer {_token}
//         // Body: { pdf: base64(pdfBytes), motivo, localidade }
//         // Response: { pdf_assinado: base64 }
//         throw new NotImplementedException("Safeweb Cloud API - implementar conforme documentacao");
//     }
// }
