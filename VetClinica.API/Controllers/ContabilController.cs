using System.IO.Compression;
using System.Net;
using System.Net.Mail;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VetClinica.API.Data;
using VetClinica.API.Middleware;
using VetClinica.API.Models;
using VetClinica.API.Services.Certificado;

namespace VetClinica.API.Controllers;

[ApiController]
[Authorize]
[Route("api/contabil")]
public class ContabilController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TenantContext _t;
    public ContabilController(AppDbContext db, TenantContext t) { _db = db; _t = t; }

    // ── Configuracao SMTP ─────────────────────────────────────────────────────

    [HttpGet("config")]
    public async Task<IActionResult> GetConfig()
    {
        var p = await _db.ParametrosSistema
            .FirstOrDefaultAsync(x => x.TenantId == _t.TenantId);
        if (p == null) return Ok(new { });

        return Ok(new {
            smtpHost       = p.SmtpHost,
            smtpPorta      = p.SmtpPorta,
            smtpUsuario    = p.SmtpUsuario,
            smtpSsl        = p.SmtpSsl,
            emailRemetente = p.EmailRemetente,
            emailContabil  = p.EmailContabil,
            contabilNome   = p.ContabilNome,
            temSmtp        = !string.IsNullOrEmpty(p.SmtpHost) && !string.IsNullOrEmpty(p.SmtpUsuario)
        });
    }

    public record SmtpConfigRequest(
        string? SmtpHost, int SmtpPorta, string? SmtpUsuario,
        string? SmtpSenha, bool SmtpSsl,
        string? EmailRemetente, string? EmailContabil, string? ContabilNome);

    [HttpPut("config")]
    public async Task<IActionResult> SalvarConfig(SmtpConfigRequest dto)
    {
        var p = await _db.ParametrosSistema
            .FirstOrDefaultAsync(x => x.TenantId == _t.TenantId);
        if (p == null) return NotFound(new { erro = "Parametros nao encontrados." });

        p.SmtpHost       = dto.SmtpHost;
        p.SmtpPorta      = dto.SmtpPorta > 0 ? dto.SmtpPorta : 587;
        p.SmtpUsuario    = dto.SmtpUsuario;
        p.SmtpSsl        = dto.SmtpSsl;
        p.EmailRemetente = dto.EmailRemetente;
        p.EmailContabil  = dto.EmailContabil;
        p.ContabilNome   = dto.ContabilNome;

        if (!string.IsNullOrEmpty(dto.SmtpSenha))
            p.SmtpSenhaEnc = CertificadoCryptoHelper.CriptografarTexto(dto.SmtpSenha);

        p.AtualizadoEm = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(new { mensagem = "Configuracao salva." });
    }

    [HttpPost("config/testar")]
    public async Task<IActionResult> TestarSmtp()
    {
        var p = await _db.ParametrosSistema
            .FirstOrDefaultAsync(x => x.TenantId == _t.TenantId);
        if (p == null || string.IsNullOrEmpty(p.SmtpHost))
            return BadRequest(new { erro = "SMTP nao configurado." });

        try
        {
            var senha = string.IsNullOrEmpty(p.SmtpSenhaEnc) ? ""
                : CertificadoCryptoHelper.DescriptografarTexto(p.SmtpSenhaEnc);

            using var client = new SmtpClient(p.SmtpHost, p.SmtpPorta)
            {
                EnableSsl   = p.SmtpSsl,
                Credentials = new NetworkCredential(p.SmtpUsuario, senha)
            };

            var msg = new MailMessage(
                from:    p.EmailRemetente ?? p.SmtpUsuario!,
                to:      p.EmailRemetente ?? p.SmtpUsuario!,
                subject: "VetClinica - Teste de configuracao SMTP",
                body:    "Email de teste enviado com sucesso pelo VetClinica."
            );

            await client.SendMailAsync(msg);
            return Ok(new { mensagem = "Email de teste enviado com sucesso!" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { erro = $"Falha ao enviar: {ex.Message}" });
        }
    }

    // ── Fechamentos Contabeis ─────────────────────────────────────────────────

    [HttpGet("fechamentos")]
    public async Task<IActionResult> ListarFechamentos()
    {
        var lista = await _db.FechamentosContabeis
            .Where(f => f.TenantId == _t.TenantId)
            .OrderByDescending(f => f.Ano).ThenByDescending(f => f.Mes)
            .Select(f => new {
                f.Id, f.Ano, f.Mes, f.Status,
                f.EnviadoEm, f.EmailDestino,
                QtdDocumentos = _db.FechamentoDocumentos.Count(d => d.FechamentoId == f.Id)
            })
            .ToListAsync();
        return Ok(lista);
    }

    [HttpGet("fechamentos/{ano}/{mes}")]
    public async Task<IActionResult> ObterFechamento(int ano, int mes)
    {
        var f = await _db.FechamentosContabeis
            .Include(x => x.Documentos)
            .FirstOrDefaultAsync(x => x.TenantId == _t.TenantId && x.Ano == ano && x.Mes == mes);

        if (f == null)
        {
            // Cria automaticamente se nao existir
            f = new FechamentoContabil
            {
                Id        = Guid.NewGuid(),
                TenantId  = _t.TenantId,
                Ano       = ano, Mes = mes,
                Status    = "aberto",
                CriadoEm = DateTime.UtcNow
            };
            _db.FechamentosContabeis.Add(f);
            await _db.SaveChangesAsync();
        }

        // Monta checklist
        var checklist = await MontarChecklist(f, ano, mes);

        return Ok(new { fechamento = f, checklist });
    }

    private async Task<object> MontarChecklist(FechamentoContabil f, int ano, int mes)
    {
        var de  = new DateOnly(ano, mes, 1);
        var ate = de.AddMonths(1);

        // Verifica o que existe
        var temExtratoBancario   = f.Documentos.Any(d => d.Tipo == "extrato_bancario");
        var temDre               = f.Documentos.Any(d => d.Tipo == "dre");
        var temEstoque           = f.Documentos.Any(d => d.Tipo == "estoque");
        var temRepasses          = f.Documentos.Any(d => d.Tipo == "repasses");
        var temFaturaCartao      = f.Documentos.Any(d => d.Tipo == "fatura_cartao");
        var temNfTomada          = f.Documentos.Any(d => d.Tipo == "nf_tomada");
        var temExtratoAplicacoes = f.Documentos.Any(d => d.Tipo == "extrato_aplicacoes");
        var temContratos         = f.Documentos.Any(d => d.Tipo == "contratos");

        // Verifica dados do sistema para os que podem ser gerados
        var temMovBancaria = await _db.MovimentacoesBancarias
            .AnyAsync(m => m.TenantId == _t.TenantId && m.DataMovimentacao >= de && m.DataMovimentacao < ate);

        var temDespesas = await _db.Contas
            .AnyAsync(c => c.TenantId == _t.TenantId && c.Tipo == "despesa"
                        && c.DataBaixa >= de && c.DataBaixa < ate);

        var temRetiradas = await _db.MovimentacoesCaixa
            .AnyAsync(m => m.TenantId == _t.TenantId && m.Tipo == "retirada"
                        && m.CriadoEm >= de.ToDateTime(TimeOnly.MinValue)
                        && m.CriadoEm < ate.ToDateTime(TimeOnly.MinValue));

        return new {
            itens = new[] {
                new { id = "extrato_bancario",   label = "Extratos bancarios (todas as contas)",         obrigatorio = true,  temNoSistema = temMovBancaria,  temDocumento = temExtratoBancario,  tipo = "upload" },
                new { id = "extrato_aplicacoes", label = "Extratos de aplicacoes/investimentos",         obrigatorio = false, temNoSistema = false,           temDocumento = temExtratoAplicacoes, tipo = "upload" },
                new { id = "dre",                label = "Demonstrativo financeiro (DRE)",               obrigatorio = true,  temNoSistema = temDespesas,     temDocumento = temDre,              tipo = "gerar"  },
                new { id = "despesas",           label = "Despesas no CNPJ (contas a pagar pagas)",      obrigatorio = true,  temNoSistema = temDespesas,     temDocumento = true,                tipo = "gerar"  },
                new { id = "fatura_cartao",      label = "Faturas dos cartoes de credito",               obrigatorio = false, temNoSistema = false,           temDocumento = temFaturaCartao,     tipo = "upload" },
                new { id = "nf_tomada",          label = "Notas de servicos tomados (NF-e recebidas)",   obrigatorio = false, temNoSistema = false,           temDocumento = temNfTomada,         tipo = "upload" },
                new { id = "estoque",            label = "Estoque final do mes (mercadorias revenda)",   obrigatorio = false, temNoSistema = true,            temDocumento = temEstoque,          tipo = "gerar"  },
                new { id = "repasses",           label = "Planilha de repasses ao socio/responsavel",    obrigatorio = true,  temNoSistema = temRetiradas,    temDocumento = temRepasses,         tipo = "gerar"  },
                new { id = "contratos",          label = "Contratos de emprestimos (se houver)",         obrigatorio = false, temNoSistema = false,           temDocumento = temContratos,        tipo = "upload" },
            }
        };
    }

    // ── Documentos ────────────────────────────────────────────────────────────

    public record DocumentoUploadRequest(
        string Tipo, string Nome, string? Descricao,
        string TipoArquivo, string DadosBase64);

    [HttpPost("fechamentos/{ano}/{mes}/documentos")]
    public async Task<IActionResult> UploadDocumento(int ano, int mes, DocumentoUploadRequest req)
    {
        if (req.DadosBase64.Length > 10 * 1024 * 1024)
            return BadRequest(new { erro = "Arquivo muito grande. Maximo 7MB." });

        var f = await _db.FechamentosContabeis
            .FirstOrDefaultAsync(x => x.TenantId == _t.TenantId && x.Ano == ano && x.Mes == mes);

        if (f == null)
        {
            f = new FechamentoContabil
            {
                Id = Guid.NewGuid(), TenantId = _t.TenantId,
                Ano = ano, Mes = mes, Status = "aberto", CriadoEm = DateTime.UtcNow
            };
            _db.FechamentosContabeis.Add(f);
            await _db.SaveChangesAsync();
        }

        var tamanho = Convert.FromBase64String(req.DadosBase64).Length;
        var doc = new FechamentoDocumento
        {
            Id           = Guid.NewGuid(),
            TenantId     = _t.TenantId,
            FechamentoId = f.Id,
            Tipo         = req.Tipo,
            Nome         = req.Nome,
            Descricao    = req.Descricao,
            TipoArquivo  = req.TipoArquivo,
            TamanhoBytes = tamanho,
            DadosBase64  = req.DadosBase64,
            Origem       = "upload",
            CriadoPor    = _t.UserId,
            CriadoEm    = DateTime.UtcNow
        };
        _db.FechamentoDocumentos.Add(doc);
        await _db.SaveChangesAsync();

        return Ok(new { doc.Id, doc.Nome, doc.Tipo, doc.TamanhoBytes });
    }

    [HttpDelete("documentos/{docId}")]
    public async Task<IActionResult> RemoverDocumento(Guid docId)
    {
        var doc = await _db.FechamentoDocumentos
            .FirstOrDefaultAsync(d => d.Id == docId && d.TenantId == _t.TenantId);
        if (doc == null) return NotFound();
        _db.FechamentoDocumentos.Remove(doc);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("documentos/{docId}/download")]
    public async Task<IActionResult> DownloadDocumento(Guid docId)
    {
        var doc = await _db.FechamentoDocumentos
            .FirstOrDefaultAsync(d => d.Id == docId && d.TenantId == _t.TenantId);
        if (doc == null) return NotFound();
        var bytes = Convert.FromBase64String(doc.DadosBase64);
        return File(bytes, doc.TipoArquivo, doc.Nome);
    }

    // ── Gerar documentos do sistema ───────────────────────────────────────────

    [HttpPost("fechamentos/{ano}/{mes}/gerar/{tipo}")]
    public async Task<IActionResult> GerarDocumento(int ano, int mes, string tipo)
    {
        var de  = new DateOnly(ano, mes, 1);
        var ate = de.AddMonths(1);
        var mesLabel = de.ToString("MMMM yyyy", new System.Globalization.CultureInfo("pt-BR"));

        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == _t.TenantId);

        byte[]? csvBytes = null;
        string nome = "", tipoArq = "text/csv";

        switch (tipo)
        {
            case "dre":
            {
                var receitas = await _db.Contas
                    .Where(c => c.TenantId == _t.TenantId && c.Tipo == "receita"
                             && (c.Status == "recebida" || c.Status == "paga")
                             && c.DataBaixa >= de && c.DataBaixa < ate)
                    .ToListAsync();
                var despesas = await _db.Contas
                    .Where(c => c.TenantId == _t.TenantId && c.Tipo == "despesa"
                             && c.Status == "paga"
                             && c.DataBaixa >= de && c.DataBaixa < ate)
                    .ToListAsync();

                var sb = new StringBuilder();
                sb.AppendLine($"DRE - Demonstrativo de Resultado - {mesLabel}");
                sb.AppendLine($"Empresa: {tenant?.Nome}");
                sb.AppendLine();
                sb.AppendLine("RECEITAS");
                sb.AppendLine("Data;Descricao;Forma Pagamento;Valor");
                foreach (var r in receitas.OrderBy(r => r.DataBaixa))
                    sb.AppendLine($"{r.DataBaixa:dd/MM/yyyy};{r.Descricao};{r.FormaPagamento};{r.ValorPago:F2}");
                sb.AppendLine($"TOTAL RECEITAS;;; {receitas.Sum(r => r.ValorPago ?? 0):F2}");
                sb.AppendLine();
                sb.AppendLine("DESPESAS");
                sb.AppendLine("Data;Descricao;Forma Pagamento;Valor");
                foreach (var d in despesas.OrderBy(d => d.DataBaixa))
                    sb.AppendLine($"{d.DataBaixa:dd/MM/yyyy};{d.Descricao};{d.FormaPagamento};{d.ValorPago:F2}");
                sb.AppendLine($"TOTAL DESPESAS;;; {despesas.Sum(d => d.ValorPago ?? 0):F2}");
                sb.AppendLine();
                var resultado = receitas.Sum(r => r.ValorPago ?? 0) - despesas.Sum(d => d.ValorPago ?? 0);
                sb.AppendLine($"RESULTADO DO PERIODO;;; {resultado:F2}");

                csvBytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
                nome = $"DRE_{ano}{mes:D2}.csv";
                break;
            }

            case "estoque":
            {
                var produtos = await _db.Produtos
                    .Where(p => p.TenantId == _t.TenantId && p.Ativo)
                    .OrderBy(p => p.Categoria).ThenBy(p => p.Nome)
                    .ToListAsync();

                var sb = new StringBuilder();
                sb.AppendLine($"Estoque Final - {mesLabel}");
                sb.AppendLine($"Empresa: {tenant?.Nome}");
                sb.AppendLine();
                sb.AppendLine("Codigo;Produto;Categoria;Unidade;Estoque Atual;Estoque Minimo;Preco Custo;Preco Venda;Valor Total Estoque");
                foreach (var p in produtos)
                    sb.AppendLine($"{p.Codigo};{p.Nome};{p.Categoria};{p.Unidade};{p.EstoqueAtual:F3};{p.EstoqueMinimo:F3};{p.PrecoCusto:F2};{p.PrecoVenda:F2};{p.EstoqueAtual * p.PrecoCusto:F2}");
                var valorTotal = produtos.Sum(p => p.EstoqueAtual * p.PrecoCusto);
                sb.AppendLine($";;;;;;;TOTAL;{valorTotal:F2}");

                csvBytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
                nome = $"Estoque_{ano}{mes:D2}.csv";
                break;
            }

            case "repasses":
            {
                var retiradas = await _db.MovimentacoesCaixa
                    .Where(m => m.TenantId == _t.TenantId && m.Tipo == "retirada"
                             && m.CriadoEm >= de.ToDateTime(TimeOnly.MinValue)
                             && m.CriadoEm < ate.ToDateTime(TimeOnly.MinValue))
                    .OrderBy(m => m.CriadoEm)
                    .ToListAsync();

                var transf = await _db.MovimentacoesBancarias
                    .Where(m => m.TenantId == _t.TenantId
                             && (m.Descricao != null && (m.Descricao.ToLower().Contains("repasse") || m.Descricao.ToLower().Contains("socio") || m.Descricao.ToLower().Contains("pro labore")))
                             && m.DataMovimentacao >= de && m.DataMovimentacao < ate)
                    .OrderBy(m => m.DataMovimentacao)
                    .ToListAsync();

                var sb = new StringBuilder();
                sb.AppendLine($"Repasses ao Socio/Responsavel - {mesLabel}");
                sb.AppendLine($"Empresa: {tenant?.Nome}");
                sb.AppendLine();
                sb.AppendLine("Data;Descricao;Valor;Origem");
                foreach (var r in retiradas)
                    sb.AppendLine($"{r.CriadoEm:dd/MM/yyyy};{r.Descricao ?? "Retirada de caixa"};{r.Valor:F2};Caixa");
                foreach (var t in transf)
                    sb.AppendLine($"{t.DataMovimentacao:dd/MM/yyyy};{t.Descricao};{t.Valor:F2};Bancario");

                var total = retiradas.Sum(r => r.Valor) + transf.Sum(t => t.Valor);
                sb.AppendLine($";;TOTAL: {total:F2};");

                csvBytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
                nome = $"Repasses_{ano}{mes:D2}.csv";
                break;
            }

            default:
                return BadRequest(new { erro = "Tipo de documento nao suportado para geracao automatica." });
        }

        // Salva como documento do fechamento
        var f = await _db.FechamentosContabeis
            .FirstOrDefaultAsync(x => x.TenantId == _t.TenantId && x.Ano == ano && x.Mes == mes);

        if (f == null)
        {
            f = new FechamentoContabil { Id = Guid.NewGuid(), TenantId = _t.TenantId, Ano = ano, Mes = mes, Status = "aberto", CriadoEm = DateTime.UtcNow };
            _db.FechamentosContabeis.Add(f);
            await _db.SaveChangesAsync();
        }

        // Remove versão anterior do mesmo tipo
        var anterior = await _db.FechamentoDocumentos
            .Where(d => d.FechamentoId == f.Id && d.Tipo == tipo && d.Origem == "sistema")
            .ToListAsync();
        _db.FechamentoDocumentos.RemoveRange(anterior);

        var doc = new FechamentoDocumento
        {
            Id = Guid.NewGuid(), TenantId = _t.TenantId, FechamentoId = f.Id,
            Tipo = tipo, Nome = nome, TipoArquivo = tipoArq,
            TamanhoBytes = csvBytes!.Length, DadosBase64 = Convert.ToBase64String(csvBytes!),
            Origem = "sistema", CriadoPor = _t.UserId, CriadoEm = DateTime.UtcNow
        };
        _db.FechamentoDocumentos.Add(doc);
        await _db.SaveChangesAsync();

        return Ok(new { doc.Id, doc.Nome, doc.TamanhoBytes, mensagem = $"{nome} gerado com sucesso." });
    }

    // ── Enviar para contabilidade ─────────────────────────────────────────────

    public record EnviarRequest(string? Obs, string? EmailDestino);

    [HttpPost("fechamentos/{ano}/{mes}/enviar")]
    public async Task<IActionResult> Enviar(int ano, int mes, EnviarRequest req)
    {
        var p = await _db.ParametrosSistema.FirstOrDefaultAsync(x => x.TenantId == _t.TenantId);
        if (p == null || string.IsNullOrEmpty(p.SmtpHost))
            return BadRequest(new { erro = "SMTP nao configurado. Acesse Parametros > Contabilidade." });

        var destino = req.EmailDestino ?? p.EmailContabil;
        if (string.IsNullOrEmpty(destino))
            return BadRequest(new { erro = "Email de destino nao informado." });

        var f = await _db.FechamentosContabeis
            .Include(x => x.Documentos)
            .FirstOrDefaultAsync(x => x.TenantId == _t.TenantId && x.Ano == ano && x.Mes == mes);

        if (f == null || f.Documentos.Count == 0)
            return BadRequest(new { erro = "Nenhum documento no fechamento para enviar." });

        var tenant   = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == _t.TenantId);
        var mesLabel = new DateOnly(ano, mes, 1).ToString("MMMM yyyy", new System.Globalization.CultureInfo("pt-BR"));

        try
        {
            var senha = string.IsNullOrEmpty(p.SmtpSenhaEnc) ? ""
                : CertificadoCryptoHelper.DescriptografarTexto(p.SmtpSenhaEnc);

            using var client = new SmtpClient(p.SmtpHost, p.SmtpPorta)
            {
                EnableSsl   = p.SmtpSsl,
                Credentials = new NetworkCredential(p.SmtpUsuario, senha)
            };

            var msg = new MailMessage
            {
                From    = new MailAddress(p.EmailRemetente ?? p.SmtpUsuario!, tenant?.Nome ?? "VetClinica"),
                Subject = $"Documentos Contabeis - {tenant?.Nome} - {mesLabel}",
                IsBodyHtml = true,
                Body    = $@"
<p>Prezados,</p>
<p>Segue em anexo os documentos contabeis referentes ao periodo <strong>{mesLabel}</strong>.</p>
<p><strong>Empresa:</strong> {tenant?.Nome}<br/>
<strong>Documentos anexados:</strong> {f.Documentos.Count} arquivo(s)</p>
{(string.IsNullOrEmpty(req.Obs) ? "" : $"<p><strong>Observacao:</strong> {req.Obs}</p>")}
<p>Atenciosamente,<br/>{tenant?.Nome}</p>
<hr/><p style='font-size:11px;color:#888'>Enviado automaticamente pelo sistema VetClinica</p>"
            };

            msg.To.Add(destino);

            // Cria ZIP com todos os documentos
            using var zipStream = new MemoryStream();
            using (var zip = new ZipArchive(zipStream, ZipArchiveMode.Create, true))
            {
                foreach (var doc in f.Documentos)
                {
                    var entry = zip.CreateEntry($"{doc.Tipo}/{doc.Nome}", CompressionLevel.Fastest);
                    using var entryStream = entry.Open();
                    var bytes = Convert.FromBase64String(doc.DadosBase64);
                    await entryStream.WriteAsync(bytes);
                }
            }

            zipStream.Position = 0;
            msg.Attachments.Add(new Attachment(zipStream,
                $"Contabil_{tenant?.Nome?.Replace(" ", "_")}_{ano}{mes:D2}.zip",
                "application/zip"));

            await client.SendMailAsync(msg);

            // Marca como enviado
            f.Status       = "enviado";
            f.EnviadoEm    = DateTime.UtcNow;
            f.EnviadoPor   = _t.UserId;
            f.EmailDestino = destino;
            f.Obs          = req.Obs;
            await _db.SaveChangesAsync();

            return Ok(new {
                mensagem = $"Email enviado para {destino} com {f.Documentos.Count} documentos.",
                enviados = f.Documentos.Count
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { erro = $"Falha ao enviar email: {ex.Message}" });
        }
    }
}
