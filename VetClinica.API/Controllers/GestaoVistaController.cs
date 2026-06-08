using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VetClinica.API.Data;

namespace VetClinica.API.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/gestao-vista")]
public class GestaoVistaController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IHttpClientFactory _http;

    private static readonly List<(string Id, string Nome, string Cor, string Icone, string Url)> Feeds =
    [
        ("bbc",    "BBC Brasil",       "#bb1919", "📰", "https://feeds.bbci.co.uk/portuguese/rss.xml"),
        ("g1",     "G1 Brasil",        "#e83a3a", "🗞️", "https://g1.globo.com/rss/g1/"),
        ("folha",  "Folha de SP",      "#0066cc", "📄", "https://feeds.folha.uol.com.br/emcimadahora/rss091.xml"),
        ("r7",     "R7 Noticias",      "#e07000", "📺", "https://noticias.r7.com/feed.xml"),
        ("saude",  "Saude Animal",     "#7c3aed", "🐾", "https://www.petlove.com.br/dicas/feed"),
    ];

    public GestaoVistaController(AppDbContext db, IHttpClientFactory http)
    {
        _db = db;
        _http = http;
    }

    // GET api/gestao-vista/{tenantId}
    [HttpGet("{tenantId:guid}")]
    public async Task<IActionResult> Get(Guid tenantId)
    {
        var hoje = DateTime.Today;
        var amanha = hoje.AddDays(1);

        var tenant = await _db.Tenants
            .Where(t => t.Id == tenantId && t.Ativo)
            .Select(t => new { t.Nome, t.Tagline, t.LogoUrl })
            .FirstOrDefaultAsync();

        if (tenant == null)
            return NotFound(new { erro = "Clinica nao encontrada" });

        var ordens = await (
            from o in _db.OrdensServico
            join p in _db.Pets on o.PetId equals p.Id
            join tu in _db.Tutores on p.TutorId equals tu.Id into gt
            from tu in gt.DefaultIfEmpty()
            where o.TenantId == tenantId
               && o.Status != "cancelado"
               && o.Status != "entregue"
            orderby o.CriadoEm
            select new
            {
                o.Id,
                PetNome = p.Nome,
                PetEspecie = p.Especie,
                PetRaca = p.Raca,
                TutorNome = tu != null ? tu.Nome : null,
                o.Status,
                o.Inicio,
                o.Fim,
                o.CriadoEm
            }
        ).ToListAsync();

        var osIds = ordens.Select(o => o.Id).ToList();
        var servicos = await (
            from oss in _db.OsServicos
            join s in _db.Servicos on oss.ServicoId equals s.Id
            where osIds.Contains(oss.OsId)
            select new { oss.OsId, ServicoNome = s.Nome }
        ).ToListAsync();

        var svcPorOs = servicos
            .GroupBy(s => s.OsId)
            .ToDictionary(g => g.Key, g => g.Select(s => s.ServicoNome).ToList());

        var ordensResult = ordens.Select(o => new
        {
            o.Id,
            o.PetNome,
            o.PetEspecie,
            o.PetRaca,
            o.TutorNome,
            o.Status,
            o.Inicio,
            o.Fim,
            o.CriadoEm,
            Servicos = svcPorOs.TryGetValue(o.Id, out var s) ? s : new List<string>()
        }).ToList();

        var agendamentos = await _db.Agendamentos
            .Where(a => a.TenantId == tenantId
                     && a.DataHora >= hoje
                     && a.DataHora < amanha
                     && a.Status != "cancelado")
            .OrderBy(a => a.DataHora)
            .Select(a => new
            {
                a.Id,
                PetNome = _db.Pets.Where(p => p.Id == a.PetId).Select(p => p.Nome).FirstOrDefault(),
                a.Tipo,
                a.DataHora,
                a.DuracaoMin,
                a.Status
            })
            .ToListAsync();

        var totalHoje = await _db.Agendamentos
            .CountAsync(a => a.TenantId == tenantId
                          && a.DataHora >= hoje && a.DataHora < amanha
                          && a.Status != "cancelado");

        var finalizadosHoje = await _db.OrdensServico
            .CountAsync(o => o.TenantId == tenantId
                          && o.Status == "entregue"
                          && o.EntregueEm >= hoje && o.EntregueEm < amanha);

        return Ok(new
        {
            clinica = new { tenant.Nome, tenant.Tagline, tenant.LogoUrl },
            contadores = new
            {
                totalHoje,
                finalizadosHoje,
                aguardando = ordensResult.Count(o => o.Status == "aguardando"),
                emAndamento = ordensResult.Count(o => o.Status == "em_andamento"),
                prontos = ordensResult.Count(o => o.Status == "pronto")
            },
            ordens = ordensResult,
            agendamentos
        });
    }

    // GET api/gestao-vista/noticias/{feedId}
    [HttpGet("noticias/{feedId}")]
    public async Task<IActionResult> Noticias(string feedId)
    {
        var feed = Feeds.FirstOrDefault(f => f.Id == feedId);
        if (feed == default)
            return NotFound(new { erro = "Feed nao encontrado" });

        try
        {
            var client = _http.CreateClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; VetClinica/1.0)");
            client.Timeout = TimeSpan.FromSeconds(8);

            var xml = await client.GetStringAsync(feed.Url);

            // Parse manual do XML para extrair imagens (media:content, enclosure, og:image)
            var doc = new System.Xml.XmlDocument();
            doc.LoadXml(xml);

            var nsMgr = new System.Xml.XmlNamespaceManager(doc.NameTable);
            nsMgr.AddNamespace("media", "http://search.yahoo.com/mrss/");
            nsMgr.AddNamespace("content", "http://purl.org/rss/1.0/modules/content/");

            var itemNodes = doc.SelectNodes("//item");
            var items = new List<object>();

            if (itemNodes != null)
            {
                foreach (System.Xml.XmlNode node in itemNodes)
                {
                    if (items.Count >= 4) break;

                    var title = node.SelectSingleNode("title")?.InnerText ?? "";
                    var description = node.SelectSingleNode("description")?.InnerText ?? "";
                    var pubDate = node.SelectSingleNode("pubDate")?.InnerText ?? "";
                    var link = node.SelectSingleNode("link")?.InnerText
                               ?? node.SelectSingleNode("link")?.Attributes?["href"]?.Value ?? "";

                    // Tenta extrair imagem em varias fontes
                    string? imageUrl = null;

                    // 1. media:content url=
                    imageUrl ??= node.SelectSingleNode("media:content", nsMgr)?.Attributes?["url"]?.Value;

                    // 2. media:thumbnail url=
                    imageUrl ??= node.SelectSingleNode("media:thumbnail", nsMgr)?.Attributes?["url"]?.Value;

                    // 3. enclosure type="image/*"
                    var enclosure = node.SelectSingleNode("enclosure");
                    if (imageUrl == null && enclosure?.Attributes?["type"]?.Value?.StartsWith("image") == true)
                        imageUrl = enclosure.Attributes?["url"]?.Value;

                    // 4. <img> dentro do description
                    if (imageUrl == null && description.Contains("<img"))
                    {
                        var imgMatch = System.Text.RegularExpressions.Regex.Match(description, @"<img[^>]+src=[""']([^""']+)[""']");
                        if (imgMatch.Success) imageUrl = imgMatch.Groups[1].Value;
                    }

                    // 5. content:encoded
                    if (imageUrl == null)
                    {
                        var encoded = node.SelectSingleNode("content:encoded", nsMgr)?.InnerText ?? "";
                        if (encoded.Contains("<img"))
                        {
                            var imgMatch = System.Text.RegularExpressions.Regex.Match(encoded, @"<img[^>]+src=[""']([^""']+)[""']");
                            if (imgMatch.Success) imageUrl = imgMatch.Groups[1].Value;
                        }
                    }

                    // Limpa HTML do description
                    var descClean = System.Text.RegularExpressions.Regex.Replace(description, "<[^>]+>", "").Trim();

                    // Parseia data
                    DateTime.TryParse(pubDate, out var parsedDate);

                    items.Add(new
                    {
                        title = System.Net.WebUtility.HtmlDecode(title),
                        description = System.Net.WebUtility.HtmlDecode(descClean),
                        pubDate = parsedDate != default ? parsedDate.ToString("o") : pubDate,
                        link,
                        imageUrl
                    });
                }
            }

            return Ok(new
            {
                id = feed.Id,
                nome = feed.Nome,
                cor = feed.Cor,
                icone = feed.Icone,
                items
            });
        }
        catch
        {
            return Ok(new
            {
                id = feed.Id,
                nome = feed.Nome,
                cor = feed.Cor,
                icone = feed.Icone,
                items = new List<object>(),
                erro = true
            });
        }
    }

    // GET api/gestao-vista/feeds — lista os feeds disponíveis
    [HttpGet("feeds")]
    public IActionResult ListarFeeds()
    {
        return Ok(Feeds.Select(f => new { f.Id, f.Nome, f.Cor, f.Icone }));
    }
}
