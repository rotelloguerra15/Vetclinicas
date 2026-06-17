using Microsoft.EntityFrameworkCore;
using Npgsql;
using VetClinica.API.Data;
using VetClinica.API.Models;

namespace VetClinica.API.Services;

// Provisiona uma clínica nova copiando a estrutura do schema template.
// Qualquer campo novo adicionado ao banco fica automaticamente disponível
// para todos os novos tenants — sem DDL manual.
public class ProvisionamentoService
{
    private readonly PlatformDbContext      _platform;
    private readonly TenantDbContextFactory _factory;
    private readonly IConfiguration         _cfg;

    // Schema que serve como template de estrutura para novos tenants
    private const string SCHEMA_TEMPLATE = "vet_barbarafonseca";

    public ProvisionamentoService(
        PlatformDbContext platform,
        TenantDbContextFactory factory,
        IConfiguration cfg)
    {
        _platform = platform;
        _factory  = factory;
        _cfg      = cfg;
    }

    public record NovaClinicaResult(
        Guid   TenantId,
        Guid   OwnerUserId,
        string LoginEmail,
        string SenhaTemporaria,
        string SchemaName);

    public async Task<NovaClinicaResult> CriarClinica(
        string nomeClinica, string plano, string nomeDono,
        string emailDono, string? telefone, string? tagline)
    {
        // 1. Gera schema name único
        var schema = GerarSchemaName(nomeClinica);
        var tentativa = schema;
        var i = 2;
        while (await _platform.Tenants.AnyAsync(t => t.SchemaName == tentativa))
            tentativa = $"{schema}_{i++}";
        schema = tentativa;

        // 2. Cria schema e copia estrutura das tabelas do template
        var connStr = _cfg.GetConnectionString("Default")!;
        await using var conn = new NpgsqlConnection(connStr);
        await conn.OpenAsync();

        // Cria o schema novo
        await using (var cmd = new NpgsqlCommand($"CREATE SCHEMA IF NOT EXISTS \"{schema}\";", conn))
            await cmd.ExecuteNonQueryAsync();

        // Lista tabelas do schema template
        var tabelas = new List<string>();
        await using (var cmd = new NpgsqlCommand(
            $"SELECT table_name FROM information_schema.tables WHERE table_schema = '{SCHEMA_TEMPLATE}' AND table_type = 'BASE TABLE' ORDER BY table_name;", conn))
        await using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
                tabelas.Add(reader.GetString(0));
        }

        // Copia estrutura de cada tabela (sem dados, sem FKs por enquanto)
        foreach (var tabela in tabelas)
        {
            await using var cmd = new NpgsqlCommand(
                $"CREATE TABLE IF NOT EXISTS \"{schema}\".\"{tabela}\" (LIKE \"{SCHEMA_TEMPLATE}\".\"{tabela}\" INCLUDING DEFAULTS INCLUDING CONSTRAINTS);", conn);
            await cmd.ExecuteNonQueryAsync();
        }

        await conn.CloseAsync();

        // 3. Registra na platform.tenants
        var tenant = new Tenant
        {
            Id         = Guid.NewGuid(),
            Nome       = nomeClinica,
            Plano      = plano,
            Email      = emailDono,
            Telefone   = telefone,
            Tagline    = tagline ?? "Gestao Inteligente para Clinicas Veterinarias",
            SchemaName = schema,
            CriadoEm  = DateTime.UtcNow,
            Ativo      = true
        };
        _platform.Tenants.Add(tenant);
        await _platform.SaveChangesAsync();

        // 4. Seeds dentro do schema recém-criado
        using var db = _factory.CreateForSchema(schema);

        var senhaTemp = GerarSenhaTemporaria();
        var owner = new User
        {
            Id        = Guid.NewGuid(),
            TenantId  = tenant.Id,
            Nome      = nomeDono,
            Email     = emailDono,
            SenhaHash = BCrypt.Net.BCrypt.HashPassword(senhaTemp, 12),
            Papel     = "owner",
            Ativo     = true,
            CriadoEm = DateTime.UtcNow
        };
        db.Users.Add(owner);
        await db.SaveChangesAsync();

        // Categorias financeiras
        string[] receitas = { "Consultas", "Banho e Tosa", "Vacinas", "Cirurgias",
                               "Exames", "Internacoes", "Vendas PDV", "Outros Servicos" };
        string[] despesas = { "Aluguel", "Agua / Luz / Internet", "Folha de Pagamento",
                               "Medicamentos e Insumos", "Equipamentos", "Marketing",
                               "Impostos e Taxas", "Manutencao", "Outros" };

        foreach (var c in receitas)
            db.CategoriasFinanceiras.Add(new CategoriaFinanceira
                { Id = Guid.NewGuid(), TenantId = tenant.Id, Nome = c, Tipo = "receita", Ativo = true });
        foreach (var c in despesas)
            db.CategoriasFinanceiras.Add(new CategoriaFinanceira
                { Id = Guid.NewGuid(), TenantId = tenant.Id, Nome = c, Tipo = "despesa", Ativo = true });

        // Serviços iniciais
        (string nome, string cat, decimal preco, int dur)[] servicos =
        {
            ("Banho pequeno porte",    "banho_tosa", 60m,  90),
            ("Banho medio porte",      "banho_tosa", 90m,  120),
            ("Banho grande porte",     "banho_tosa", 140m, 150),
            ("Tosa higienica",         "banho_tosa", 40m,  45),
            ("Consulta clinica geral", "consulta",   150m, 30),
            ("Vacina V8/V10",          "vacina",     85m,  15),
            ("Vacina antirrabica",     "vacina",     65m,  15)
        };
        foreach (var s in servicos)
            db.Servicos.Add(new Servico
                { Id = Guid.NewGuid(), TenantId = tenant.Id, Nome = s.nome,
                  Categoria = s.cat, PrecoBase = s.preco, DuracaoMin = s.dur });

        // Mensagens automáticas (desligadas)
        (string gat, string tpl)[] msgs =
        {
            ("aniversario_pet",      "Hoje e aniversario do {pet}! A equipe da {clinica} deseja muitas festas."),
            ("aniversario_tutor",    "Parabens, {tutor}! A {clinica} deseja um feliz aniversario."),
            ("boas_vindas_tutor",    "Ola {tutor}! Seja bem-vindo(a) a {clinica}. Estamos felizes em cuidar do {pet}!"),
            ("lembrete_agendamento", "Oi {tutor}! Lembrando o agendamento do {pet} em {data} as {hora}, na {clinica}."),
            ("pos_atendimento_nps",  "Oi {tutor}! Como foi o atendimento do {pet} na {clinica}? De 0 a 10, quanto nos recomendaria?")
        };
        foreach (var m in msgs)
            db.ConfigMensagens.Add(new ConfigMensagem
                { Id = Guid.NewGuid(), TenantId = tenant.Id, Gatilho = m.gat,
                  Canal = "whatsapp", Ativo = false, Template = m.tpl, CriadoEm = DateTime.UtcNow });

        // Raças
        var racasCao  = new[] { "SRD", "Labrador", "Golden Retriever", "Bulldog", "Poodle",
                                 "Yorkshire", "Shih Tzu", "Beagle", "Pastor Alemao", "Dachshund" };
        var racasGato = new[] { "SRD", "Persa", "Siames", "Maine Coon", "Ragdoll", "Bengal" };
        foreach (var r in racasCao)
            db.Racas.Add(new Raca { Id = Guid.NewGuid(), TenantId = tenant.Id, Nome = r, Especie = "cao", Ativo = true });
        foreach (var r in racasGato)
            db.Racas.Add(new Raca { Id = Guid.NewGuid(), TenantId = tenant.Id, Nome = r, Especie = "gato", Ativo = true });

        // Pelagens
        foreach (var p in new[] { "Curto", "Longo", "Ondulado", "Crespo", "Dupla camada", "Sem pelo" })
            db.Pelagens.Add(new Pelagem { Id = Guid.NewGuid(), TenantId = tenant.Id, Nome = p, Ativo = true });

        // Parâmetros e Bot config
        db.ParametrosSistema.Add(new ParametrosSistema { Id = Guid.NewGuid(), TenantId = tenant.Id });
        db.BotConfigs.Add(new BotConfig { Id = Guid.NewGuid(), TenantId = tenant.Id, Ativo = false });

        await db.SaveChangesAsync();

        return new NovaClinicaResult(tenant.Id, owner.Id, emailDono, senhaTemp, schema);
    }

    public static string GerarSchemaName(string nomeClinica)
    {
        var slug = nomeClinica
            .ToLowerInvariant()
            .Replace("ã","a").Replace("â","a").Replace("á","a").Replace("à","a")
            .Replace("ê","e").Replace("é","e").Replace("è","e")
            .Replace("í","i").Replace("ì","i")
            .Replace("ô","o").Replace("õ","o").Replace("ó","o").Replace("ò","o")
            .Replace("ú","u").Replace("ù","u")
            .Replace("ç","c").Replace("ñ","n");

        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9]", "");
        if (slug.Length > 40) slug = slug[..40];
        return $"vet_{slug}";
    }

    private static string GerarSenhaTemporaria()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";
        return new string(Enumerable.Range(0, 10)
            .Select(_ => chars[Random.Shared.Next(chars.Length)]).ToArray());
    }
}
