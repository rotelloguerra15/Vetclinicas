using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QRCoder;
using System.Security.Cryptography;
using System.Text;

namespace VetClinica.API.Services;

public class ReceituarioData
{
    // Clínica / emitente
    public string ClinicaNome   { get; set; } = "";
    public string? ClinicaTagline   { get; set; }
    public string? ClinicaTelefone  { get; set; }
    public string? ClinicaEmail     { get; set; }
    public byte[]? LogoBytes        { get; set; }

    // Veterinário
    public string? VetNome          { get; set; }
    public string? VetCrmv          { get; set; }
    public string? VetRegistroMapa  { get; set; }
    public string? VetTelefone      { get; set; }
    public string? VetEmail         { get; set; }

    // Pet
    public string  PetNome          { get; set; } = "";
    public string? PetCodigo        { get; set; }   // ID numérico legível
    public string? PetEspecie       { get; set; }
    public string? PetRaca          { get; set; }
    public string? PetSexo          { get; set; }
    public string? PetIdade         { get; set; }

    // Responsável (tutor)
    public string  TutorNome        { get; set; } = "";
    public string? TutorCpf         { get; set; }
    public string? TutorEndereco    { get; set; }

    // Validação
    public string? CodigoValidacao  { get; set; }  // ex: REC-2026-00123
    public string? UrlValidacao     { get; set; }  // ex: https://vetclinica.com.br/validar/REC-2026-00123
    public string? HashDocumento    { get; set; }  // SHA-256 do conteúdo

    // Receita
    public DateTime Data            { get; set; }
    public string   TipoReceita     { get; set; } = "Receita Veterinária";
    public string?  ViaUso          { get; set; }   // "USO ORAL" / "USO TÓPICO" etc.
    public string?  TipoFarmacia    { get; set; }   // "Farmácia Humana" / "Farmácia Veterinária"
    public string?  Motivo          { get; set; }
    public List<MedicamentoItem> Medicamentos { get; set; } = new();
    public string?  Observacoes     { get; set; }
}

public class MedicamentoItem
{
    public string  Nome       { get; set; } = "";
    public string? Apresentacao { get; set; }  // "comprimido (30 un)"
    public string? Dosagem    { get; set; }
    public string? Frequencia { get; set; }
    public string? Duracao    { get; set; }
    public string? Via        { get; set; }
    public string? Quantidade { get; set; }  // "1 UNIDADE"
}

public class ReceituarioPdfService
{
    private readonly HttpClient _http;
    public ReceituarioPdfService(HttpClient http) { _http = http; }

    public async Task<byte[]?> BaixarLogo(string? logoUrl)
    {
        if (string.IsNullOrWhiteSpace(logoUrl)) return null;
        try
        {
            if (logoUrl.StartsWith("data:"))
            {
                // data URL — extrai base64
                var comma = logoUrl.IndexOf(',');
                if (comma > 0)
                    return Convert.FromBase64String(logoUrl[(comma + 1)..]);
                return null;
            }
            return await _http.GetByteArrayAsync(logoUrl);
        }
        catch { return null; }
    }

    private static byte[] GerarQrCode(string url)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrData      = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.M);
        using var qrCode      = new PngByteQRCode(qrData);
        return qrCode.GetGraphic(5);
    }

    public static string GerarHash(ReceituarioData d)
    {
        var conteudo = $"{d.CodigoValidacao}|{d.PetNome}|{d.TutorNome}|{d.Data:yyyyMMddHHmmss}|{string.Join(",", d.Medicamentos.Select(m => m.Nome))}";
        var bytes = Encoding.UTF8.GetBytes(conteudo);
        return Convert.ToHexString(SHA256.HashData(bytes))[..16]; // primeiros 16 chars
    }

    public byte[] Gerar(ReceituarioData d)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        // Nome de exibição no cabeçalho: preferência ao vet, fallback à clínica
        var emitentePrincipal = d.VetNome ?? d.ClinicaNome;
        var emitenteCrmv      = d.VetCrmv;
        var emitenteMapa      = d.VetRegistroMapa;
        var emitenteTel       = d.VetTelefone ?? d.ClinicaTelefone;
        var emitenteEmail     = d.VetEmail    ?? d.ClinicaEmail;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.8f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));

                // ════════════════════════════════════════════════════════════
                // CABEÇALHO — logo + nome/CRMV/MAPA + contatos (direita)
                // ════════════════════════════════════════════════════════════
                page.Header().Column(hdr =>
                {
                    hdr.Item().Row(row =>
                    {
                        // Logo
                        if (d.LogoBytes != null && d.LogoBytes.Length > 0)
                        {
                            row.ConstantItem(70).Height(70).Image(d.LogoBytes).FitArea();
                            row.ConstantItem(10);
                        }

                        // Nome + CRMV + MAPA
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text(emitentePrincipal).Bold().FontSize(15).FontColor("#1e3a5f");
                            if (!string.IsNullOrEmpty(emitenteCrmv))
                                c.Item().Text($"CRMV {emitenteCrmv}").Bold().FontSize(10).FontColor("#2563eb");
                            if (!string.IsNullOrEmpty(emitenteMapa))
                                c.Item().Text($"Registro no MAPA {emitenteMapa}").FontSize(9).FontColor("#2563eb");
                        });

                        // Contatos (direita)
                        row.ConstantItem(180).AlignRight().Column(c =>
                        {
                            if (!string.IsNullOrEmpty(emitenteTel))
                                c.Item().Text($"Telefone: {emitenteTel}").FontSize(9);
                            if (!string.IsNullOrEmpty(emitenteEmail))
                                c.Item().Text($"E-mail:").FontSize(9);
                            if (!string.IsNullOrEmpty(emitenteEmail))
                                c.Item().Text(emitenteEmail).FontSize(9).FontColor("#374151");
                        });
                    });

                    hdr.Item().PaddingTop(6).LineHorizontal(1f).LineColor("#cbd5e1");
                    hdr.Item().PaddingTop(6);

                    // Tipo de receita + vias
                    hdr.Item().Row(row =>
                    {
                        row.RelativeItem().Text(d.TipoReceita).Bold().FontSize(10);
                        row.ConstantItem(200).AlignRight()
                            .Text("1ª Via para Farmácia - 2ª Via para Paciente")
                            .FontSize(8).FontColor("#64748b");
                    });

                    hdr.Item().PaddingTop(6);

                    // ── 3 colunas: Emitente | Animal | Responsável ──────────
                    hdr.Item().Row(row =>
                    {
                        // Emitente
                        row.RelativeItem().Border(0.5f).BorderColor("#cbd5e1")
                            .Padding(8).Column(c =>
                        {
                            c.Item().Text("Identificação do Emitente").Bold().FontSize(8);
                            c.Item().PaddingTop(4).Text(t =>
                            {
                                t.Span("Nome: ").Bold();
                                t.Span(emitentePrincipal);
                            });
                            if (!string.IsNullOrEmpty(emitenteCrmv))
                                c.Item().Text(t =>
                                {
                                    t.Span("CRMV: ").Bold();
                                    t.Span(emitenteCrmv);
                                });
                        });

                        row.ConstantItem(6);

                        // Animal
                        row.RelativeItem().Border(0.5f).BorderColor("#cbd5e1")
                            .Padding(8).Column(c =>
                        {
                            c.Item().AlignCenter().Text("Animal").Bold().FontSize(8);
                            c.Item().PaddingTop(4).Text(t =>
                            {
                                t.Span("ID: ").Bold(); t.Span(d.PetCodigo ?? "—");
                            });
                            c.Item().Text(t => { t.Span("Nome: ").Bold(); t.Span(d.PetNome); });
                            if (!string.IsNullOrEmpty(d.PetEspecie))
                                c.Item().Text(t => { t.Span("Espécie: ").Bold(); t.Span(d.PetEspecie); });
                            if (!string.IsNullOrEmpty(d.PetRaca))
                                c.Item().Text(t => { t.Span("Raça: ").Bold(); t.Span(d.PetRaca); });
                            if (!string.IsNullOrEmpty(d.PetSexo))
                                c.Item().Text(t => { t.Span("Sexo: ").Bold(); t.Span(d.PetSexo); });
                            if (!string.IsNullOrEmpty(d.PetIdade))
                                c.Item().Text(t => { t.Span("Idade: ").Bold(); t.Span(d.PetIdade); });
                        });

                        row.ConstantItem(6);

                        // Responsável
                        row.RelativeItem().Border(0.5f).BorderColor("#cbd5e1")
                            .Padding(8).Column(c =>
                        {
                            c.Item().AlignCenter().Text("Responsável").Bold().FontSize(8);
                            c.Item().PaddingTop(4).Text(t =>
                            {
                                t.Span("Nome: ").Bold(); t.Span(d.TutorNome);
                            });
                            if (!string.IsNullOrEmpty(d.TutorCpf))
                                c.Item().Text(t => { t.Span("CPF: ").Bold(); t.Span(d.TutorCpf); });
                            if (!string.IsNullOrEmpty(d.TutorEndereco))
                                c.Item().Text(t => { t.Span("Endereço: ").Bold(); t.Span(d.TutorEndereco); });
                        });
                    });

                    hdr.Item().PaddingTop(10);
                });

                // ════════════════════════════════════════════════════════════
                // CONTEÚDO — via de uso + prescrições
                // ════════════════════════════════════════════════════════════
                page.Content().Column(col =>
                {
                    if (!string.IsNullOrEmpty(d.ViaUso))
                    {
                        col.Item().Text(d.ViaUso.ToUpper()).Bold().FontSize(11);
                        col.Item().LineHorizontal(0.5f).LineColor("#cbd5e1");
                        col.Item().PaddingTop(6);
                    }

                    if (!string.IsNullOrEmpty(d.TipoFarmacia))
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Text(d.TipoFarmacia).FontSize(9).FontColor("#64748b");
                        });
                        col.Item().PaddingTop(4);
                    }

                    foreach (var m in d.Medicamentos)
                    {
                        // Nome em bold + apresentação
                        col.Item().PaddingBottom(2).Text(t =>
                        {
                            t.Span(m.Nome).Bold().FontSize(11);
                            if (!string.IsNullOrEmpty(m.Apresentacao))
                                t.Span($", {m.Apresentacao}").FontSize(10).FontColor("#374151");

                            if (!string.IsNullOrEmpty(m.Quantidade))
                            {
                                t.Span("   "); // spacer
                            }
                        });

                        // Posologia
                        var posologia = new List<string>();
                        if (!string.IsNullOrEmpty(m.Dosagem))    posologia.Add(m.Dosagem);
                        if (!string.IsNullOrEmpty(m.Frequencia)) posologia.Add(m.Frequencia);
                        if (!string.IsNullOrEmpty(m.Duracao))    posologia.Add(m.Duracao);
                        if (!string.IsNullOrEmpty(m.Via))        posologia.Add($"via {m.Via}");

                        if (posologia.Any())
                            col.Item().PaddingBottom(8).Text(string.Join(", ", posologia)).FontColor("#374151");
                    }

                    if (!string.IsNullOrEmpty(d.Motivo))
                    {
                        col.Item().PaddingTop(4).Text(t =>
                        {
                            t.Span("Observação: ").Bold().FontSize(9);
                            t.Span(d.Motivo).FontSize(9);
                        });
                    }

                    if (!string.IsNullOrEmpty(d.Observacoes))
                    {
                        col.Item().PaddingTop(2).Text(d.Observacoes).FontSize(9).FontColor("#64748b");
                    }
                });

                // ════════════════════════════════════════════════════════════
                // RODAPÉ — assinatura | comprador | fornecedor | validação
                // ════════════════════════════════════════════════════════════
                page.Footer().Column(footer =>
                {
                    footer.Item().PaddingTop(10).Row(row =>
                    {
                        // Data + nome vet + assinatura
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text(t => t.Span(d.Data.ToString("dd 'DE' MMMM 'DE' yyyy", new System.Globalization.CultureInfo("pt-BR")).ToUpper()).Bold().FontSize(9));
                            c.Item().PaddingTop(2).Text(emitentePrincipal).Bold().FontSize(9);
                            if (!string.IsNullOrEmpty(emitenteCrmv))
                                c.Item().Text($"CRMV {emitenteCrmv}").FontSize(8).FontColor("#64748b");
                            c.Item().PaddingTop(12).LineHorizontal(0.5f).LineColor("#94a3b8");
                            c.Item().PaddingTop(2).Text("Assinatura").FontSize(8).FontColor("#94a3b8");
                        });

                        row.ConstantItem(8);

                        // Identificação do Comprador
                        row.RelativeItem().Border(0.5f).BorderColor("#cbd5e1").Padding(8).Column(c =>
                        {
                            c.Item().Text("Identificação do Comprador").Bold().FontSize(8);
                            c.Item().PaddingTop(4).Text("Nome:").FontSize(8);
                            c.Item().PaddingTop(2).Text("RG:").FontSize(8);
                            c.Item().PaddingTop(2).Text("Endereço:").FontSize(8);
                            c.Item().PaddingTop(2).Text("Cidade e UF:").FontSize(8);
                            c.Item().PaddingTop(2).Text("Telefone:").FontSize(8);
                        });

                        row.ConstantItem(8);

                        // Identificação do Fornecedor
                        row.RelativeItem().Border(0.5f).BorderColor("#cbd5e1").Padding(8).Column(c =>
                        {
                            c.Item().Text("Identificação do\nFornecedor").Bold().FontSize(8);
                            c.Item().PaddingTop(4).Text("Data:").FontSize(8);
                            c.Item().PaddingTop(16).LineHorizontal(0.5f).LineColor("#94a3b8");
                            c.Item().PaddingTop(2).Text("Assinatura do Farmacêutico").FontSize(8).FontColor("#94a3b8");
                        });
                    });

                    // ── Faixa de validação eletrônica ──────────────────────
                    if (!string.IsNullOrEmpty(d.CodigoValidacao) && !string.IsNullOrEmpty(d.UrlValidacao))
                    {
                        var qrBytes = GerarQrCode(d.UrlValidacao);

                        footer.Item().PaddingTop(8)
                            .Border(0.5f).BorderColor("#e2e8f0")
                            .Background("#f8fafc")
                            .Padding(6)
                            .Row(row =>
                            {
                                // QR Code
                                row.ConstantItem(52).Image(qrBytes);

                                row.ConstantItem(8);

                                // Textos de validação
                                row.RelativeItem().Column(c =>
                                {
                                    c.Item().Text("DOCUMENTO ELETRÔNICO VERIFICÁVEL")
                                        .Bold().FontSize(7).FontColor("#1e293b");
                                    c.Item().PaddingTop(2).Text(t =>
                                    {
                                        t.Span("Código: ").Bold().FontSize(7).FontColor("#475569");
                                        t.Span(d.CodigoValidacao).FontSize(7).FontColor("#0f172a");
                                    });
                                    if (!string.IsNullOrEmpty(d.HashDocumento))
                                    {
                                        c.Item().PaddingTop(1).Text(t =>
                                        {
                                            t.Span("Hash: ").Bold().FontSize(6).FontColor("#94a3b8");
                                            t.Span(d.HashDocumento).FontSize(6).FontColor("#94a3b8");
                                        });
                                    }
                                    c.Item().PaddingTop(2).Text(t =>
                                    {
                                        t.Span("Valide em: ").Bold().FontSize(7).FontColor("#475569");
                                        t.Span(d.UrlValidacao).FontSize(7).FontColor("#2563eb");
                                    });
                                    c.Item().PaddingTop(2).Text(
                                        "Aponte a câmera do celular para o QR Code ou acesse o link acima para verificar a autenticidade.")
                                        .FontSize(6).FontColor("#94a3b8").Italic();
                                });
                            });
                    }
                });
            });
        }).GeneratePdf();
    }
}
