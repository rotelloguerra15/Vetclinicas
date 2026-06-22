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

    // Mesma logica do database/044_reparar_drift_colunas_todas_clinicas.sql,
    // mas escopada a UM schema, chamada automaticamente ao fim de CriarClinica.
    // Nota: DO $$ $$ nao aceita parametro do Npgsql -- por isso interpola o
    // nome do schema direto, igual o resto deste metodo ja faz (valor vem de
    // GerarSchemaName, ja sanitizado: so [a-z0-9_]).
    private static string RepararColunasSql(string schemaDestino) => $@"
DO $$
DECLARE
    tbl RECORD;
    col RECORD;
    tipo_sql TEXT;
    default_sql TEXT;
    nulo_sql TEXT;
    ddl TEXT;
BEGIN
    FOR tbl IN
        SELECT DISTINCT table_name FROM information_schema.tables
        WHERE table_schema = '{SCHEMA_TEMPLATE}'
    LOOP
        IF EXISTS (
            SELECT 1 FROM information_schema.tables
            WHERE table_schema = '{schemaDestino}' AND table_name = tbl.table_name
        ) THEN
            FOR col IN
                SELECT c.column_name, c.data_type, c.character_maximum_length,
                       c.numeric_precision, c.numeric_scale, c.is_nullable,
                       c.column_default, c.udt_name
                FROM information_schema.columns c
                WHERE c.table_schema = '{SCHEMA_TEMPLATE}' AND c.table_name = tbl.table_name
                  AND NOT EXISTS (
                      SELECT 1 FROM information_schema.columns c2
                      WHERE c2.table_schema = '{schemaDestino}'
                        AND c2.table_name = tbl.table_name
                        AND c2.column_name = c.column_name
                  )
            LOOP
                tipo_sql := CASE
                    WHEN col.data_type = 'ARRAY' THEN regexp_replace(col.udt_name, '^_', '') || '[]'
                    WHEN col.data_type = 'character varying' AND col.character_maximum_length IS NOT NULL
                        THEN format('varchar(%s)', col.character_maximum_length)
                    WHEN col.data_type = 'numeric' AND col.numeric_precision IS NOT NULL
                        THEN format('numeric(%s,%s)', col.numeric_precision, COALESCE(col.numeric_scale, 0))
                    WHEN col.data_type = 'USER-DEFINED' THEN col.udt_name
                    ELSE col.data_type
                END;
                default_sql := CASE WHEN col.column_default IS NOT NULL
                    THEN format(' DEFAULT %s', col.column_default) ELSE '' END;
                nulo_sql := CASE WHEN col.is_nullable = 'NO' AND col.column_default IS NOT NULL
                    THEN ' NOT NULL' ELSE '' END;
                ddl := format('ALTER TABLE %I.%I ADD COLUMN %I %s%s%s',
                    '{schemaDestino}', tbl.table_name, col.column_name, tipo_sql, default_sql, nulo_sql);
                BEGIN
                    EXECUTE ddl;
                EXCEPTION WHEN OTHERS THEN
                    RAISE WARNING 'Reparo automatico falhou: % -- %', ddl, SQLERRM;
                END;
            END LOOP;
        END IF;
    END LOOP;
END $$;";

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

        // CREATE TABLE IF NOT EXISTS so cria do zero -- se a tabela JA existir
        // (schema reaproveitado de um teste anterior incompleto, por exemplo),
        // fica com a estrutura antiga pra sempre. Esse passo extra garante que
        // a clinica nasce sempre 100% igual ao molde atual, mesmo nesse caso.
        try
        {
            await using var cmdReparo = new NpgsqlCommand(RepararColunasSql(schema), conn);
            await cmdReparo.ExecuteNonQueryAsync();
        }
        catch { /* nao bloqueia a criacao da clinica por causa disso */ }

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

        // 5. Cria usuário owner AGORA em SaveChanges separado
        var senhaTemp = GerarSenhaTemporaria();
        var senhaHash = BCrypt.Net.BCrypt.HashPassword(senhaTemp, 12);
        var opts = new DbContextOptionsBuilder<TenantDbContext>().UseNpgsql(connStr).Options;
        using (var dbUser = new TenantDbContext(opts, schema))
        {
            dbUser.Users.Add(new User
            {
                Id = Guid.NewGuid(), TenantId = tenant.Id, Nome = nomeDono,
                Email = emailDono, SenhaHash = senhaHash,
                Papel = "owner", Ativo = true, CriadoEm = DateTime.UtcNow
            });
            await dbUser.SaveChangesAsync();
        }

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

    public static string GerarSenhaTemporaria()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";
        return new string(Enumerable.Range(0, 10).Select(_ => chars[Random.Shared.Next(chars.Length)]).ToArray());
    }
}
