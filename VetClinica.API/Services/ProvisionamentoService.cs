using Microsoft.EntityFrameworkCore;
using Npgsql;
using VetClinica.API.Data;
using VetClinica.API.Models;

namespace VetClinica.API.Services;

public class ProvisionamentoService
{
    private readonly PlatformDbContext      _platform;
    private readonly TenantDbContextFactory _factory;
    private readonly IConfiguration         _cfg;
    private const string SCHEMA_TEMPLATE = "vet_barbarafonseca";

    public ProvisionamentoService(PlatformDbContext platform, TenantDbContextFactory factory, IConfiguration cfg)
    {
        _platform = platform;
        _factory  = factory;
        _cfg      = cfg;
    }

    public record NovaClinicaResult(Guid TenantId, Guid OwnerUserId, string LoginEmail, string SenhaTemporaria, string SchemaName);

    public async Task<NovaClinicaResult> CriarClinica(
        string nomeClinica, string plano, string nomeDono,
        string emailDono, string? telefone, string? tagline)
    {
        var connStr = _cfg.GetConnectionString("Default")!;

        // 1. Gera schema name único
        var schema = GerarSchemaName(nomeClinica);
        var tentativa = schema;
        var i = 2;
        while (await _platform.Tenants.AnyAsync(t => t.SchemaName == tentativa))
            tentativa = $"{schema}_{i++}";
        schema = tentativa;

        // 2. Cria schema PostgreSQL (rápido)
        await using var conn = new NpgsqlConnection(connStr);
        await conn.OpenAsync();
        await using (var cmd = new NpgsqlCommand($"CREATE SCHEMA IF NOT EXISTS \"{schema}\";", conn))
            await cmd.ExecuteNonQueryAsync();

        // 3. Copia estrutura das tabelas do template (rápido)
        var tabelas = new List<string>();
        await using (var cmd = new NpgsqlCommand(
            $"SELECT table_name FROM information_schema.tables WHERE table_schema = '{SCHEMA_TEMPLATE}' AND table_type = 'BASE TABLE' ORDER BY table_name;", conn))
        await using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
                tabelas.Add(reader.GetString(0));
        }
        foreach (var tabela in tabelas)
        {
            try
            {
                await using var cmd = new NpgsqlCommand(
                    $"CREATE TABLE IF NOT EXISTS \"{schema}\".\"{tabela}\" (LIKE \"{SCHEMA_TEMPLATE}\".\"{tabela}\" INCLUDING DEFAULTS INCLUDING CONSTRAINTS);", conn);
                await cmd.ExecuteNonQueryAsync();
            }
            catch { }
        }
        await conn.CloseAsync();

        // 4. Registra na platform.tenants
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(), Nome = nomeClinica, Plano = plano,
            Email = emailDono, Telefone = telefone,
            Tagline = tagline ?? "Gestao Inteligente para Clinicas Veterinarias",
            SchemaName = schema, CriadoEm = DateTime.UtcNow, Ativo = true
        };
        _platform.Tenants.Add(tenant);
        await _platform.SaveChangesAsync();

        // 5. Cria usuário owner AGORA (não em background)
        var senhaTemp = GerarSenhaTemporaria();
        var senhaHash = BCrypt.Net.BCrypt.HashPassword(senhaTemp, 12);
        var opts = new DbContextOptionsBuilder<TenantDbContext>().UseNpgsql(connStr).Options;
        using var db = new TenantDbContext(opts, schema);
        db.Users.Add(new User
        {
            Id = Guid.NewGuid(), TenantId = tenant.Id, Nome = nomeDono,
            Email = emailDono, SenhaHash = senhaHash,
            Papel = "owner", Ativo = true, CriadoEm = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        // 6. Seeds em background (não bloqueia)
        var tenantId = tenant.Id;
        var schemaName = schema;
        _ = Task.Run(async () =>
        {
            try { await SeedEmBackground(connStr, tenantId, schemaName); }
            catch (Exception ex) { Console.WriteLine($"[Seed] ERRO: {ex.Message}"); }
        });

        return new NovaClinicaResult(tenant.Id, Guid.Empty, emailDono, senhaTemp, schema);
    }

    private async Task SeedEmBackground(string connStr, Guid tenantId, string schema)
    {
        var opts = new DbContextOptionsBuilder<TenantDbContext>().UseNpgsql(connStr).Options;
        using var db = new TenantDbContext(opts, schema);

        string[] receitas = { "Consultas", "Banho e Tosa", "Vacinas", "Cirurgias", "Exames", "Internacoes", "Vendas PDV", "Outros Servicos" };
        string[] despesas = { "Aluguel", "Agua / Luz / Internet", "Folha de Pagamento", "Medicamentos e Insumos", "Equipamentos", "Marketing", "Impostos e Taxas", "Manutencao", "Outros" };
        foreach (var c in receitas)
            db.CategoriasFinanceiras.Add(new CategoriaFinanceira { Id = Guid.NewGuid(), TenantId = tenantId, Nome = c, Tipo = "receita", Ativo = true });
        foreach (var c in despesas)
            db.CategoriasFinanceiras.Add(new CategoriaFinanceira { Id = Guid.NewGuid(), TenantId = tenantId, Nome = c, Tipo = "despesa", Ativo = true });

        (string nome, string cat, decimal preco, int dur)[] servicos =
        {
            ("Banho pequeno porte", "banho_tosa", 60m, 90), ("Banho medio porte", "banho_tosa", 90m, 120),
            ("Banho grande porte", "banho_tosa", 140m, 150), ("Tosa higienica", "banho_tosa", 40m, 45),
            ("Consulta clinica geral", "consulta", 150m, 30), ("Vacina V8/V10", "vacina", 85m, 15),
            ("Vacina antirrabica", "vacina", 65m, 15)
        };
        foreach (var s in servicos)
            db.Servicos.Add(new Servico { Id = Guid.NewGuid(), TenantId = tenantId, Nome = s.nome, Categoria = s.cat, PrecoBase = s.preco, DuracaoMin = s.dur });

        (string gat, string tpl)[] msgs =
        {
            ("aniversario_pet", "Hoje e aniversario do {pet}! A equipe da {clinica} deseja muitas festas."),
            ("aniversario_tutor", "Parabens, {tutor}! A {clinica} deseja um feliz aniversario."),
            ("boas_vindas_tutor", "Ola {tutor}! Seja bem-vindo(a) a {clinica}."),
            ("lembrete_agendamento", "Oi {tutor}! Lembrando o agendamento do {pet} em {data} as {hora}."),
            ("pos_atendimento_nps", "Oi {tutor}! Como foi o atendimento do {pet}? De 0 a 10, quanto nos recomendaria?")
        };
        foreach (var m in msgs)
            db.ConfigMensagens.Add(new ConfigMensagem { Id = Guid.NewGuid(), TenantId = tenantId, Gatilho = m.gat, Canal = "whatsapp", Ativo = false, Template = m.tpl, CriadoEm = DateTime.UtcNow });

        foreach (var r in new[] { "SRD", "Labrador", "Golden Retriever", "Bulldog", "Poodle", "Yorkshire", "Shih Tzu", "Beagle", "Pastor Alemao", "Dachshund" })
            db.Racas.Add(new Raca { Id = Guid.NewGuid(), TenantId = tenantId, Nome = r, Especie = "cao", Ativo = true });
        foreach (var r in new[] { "SRD", "Persa", "Siames", "Maine Coon", "Ragdoll", "Bengal" })
            db.Racas.Add(new Raca { Id = Guid.NewGuid(), TenantId = tenantId, Nome = r, Especie = "gato", Ativo = true });
        foreach (var p in new[] { "Curto", "Longo", "Ondulado", "Crespo", "Dupla camada", "Sem pelo" })
            db.Pelagens.Add(new Pelagem { Id = Guid.NewGuid(), TenantId = tenantId, Nome = p, Ativo = true });

        db.ParametrosSistema.Add(new ParametrosSistema { Id = Guid.NewGuid(), TenantId = tenantId });
        db.BotConfigs.Add(new BotConfig { Id = Guid.NewGuid(), TenantId = tenantId, Ativo = false });

        await db.SaveChangesAsync();
        Console.WriteLine($"[Seed] Schema {schema} seeds concluidos.");
    }

    public static string GerarSchemaName(string nomeClinica)
    {
        var slug = nomeClinica.ToLowerInvariant()
            .Replace("ã","a").Replace("â","a").Replace("á","a").Replace("à","a")
            .Replace("ê","e").Replace("é","e").Replace("è","e").Replace("í","i")
            .Replace("ô","o").Replace("õ","o").Replace("ó","o").Replace("ú","u")
            .Replace("ç","c").Replace("ñ","n");
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9]", "");
        if (slug.Length > 40) slug = slug[..40];
        return $"vet_{slug}";
    }

    private static string GerarSenhaTemporaria()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";
        return new string(Enumerable.Range(0, 10).Select(_ => chars[Random.Shared.Next(chars.Length)]).ToArray());
    }
}
