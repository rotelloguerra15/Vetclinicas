using Microsoft.EntityFrameworkCore;
using Npgsql;
using VetClinica.API.Data;
using VetClinica.API.Models;

namespace VetClinica.API.Services;

// Provisiona uma clínica nova "chave na mão" no PostgreSQL:
// 1. Gera o schema name (ex: "vet_barbarafonseca")
// 2. Cria o schema no PostgreSQL + todas as tabelas
// 3. Registra em platform.tenants
// 4. Insere owner + seeds (categorias, serviços, raças, etc.)
public class ProvisionamentoService
{
    private readonly PlatformDbContext      _platform;
    private readonly TenantDbContextFactory _factory;
    private readonly IConfiguration         _cfg;

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

        // 2. Cria schema + tabelas no PostgreSQL
        await CriarSchemaPostgres(schema);

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
            ("Vacina antirrábica",     "vacina",     65m,  15)
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

        // Raças seed
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

    // ── Criação do schema + tabelas no PostgreSQL ─────────────────────────────
    private async Task CriarSchemaPostgres(string schema)
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        var connStr = _cfg.GetConnectionString("Default")!;
        await using var conn = new NpgsqlConnection(connStr);
        await conn.OpenAsync();

        // Cria o schema
        await using (var cmd = new NpgsqlCommand(
            $"CREATE SCHEMA IF NOT EXISTS \"{schema}\";", conn))
            await cmd.ExecuteNonQueryAsync();

        // Executa cada statement do DDL
        var ddl = GerarDdl(schema);
        foreach (var stmt in ddl.Split("-- SPLIT --", StringSplitOptions.RemoveEmptyEntries))
        {
            var s = stmt.Trim();
            if (string.IsNullOrWhiteSpace(s)) continue;
            await using var cmd = new NpgsqlCommand(s, conn);
            await cmd.ExecuteNonQueryAsync();
        }
    }

    // ── DDL PostgreSQL completo ───────────────────────────────────────────────
    // Cria todas as tabelas dentro do schema da clínica.
    // Equivalente às 40 migrations existentes, em formato PostgreSQL.
    private static string GerarDdl(string sc) => $"""
        CREATE TABLE IF NOT EXISTS "{sc}".tenants (
            id           UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
            nome         TEXT        NOT NULL,
            plano        TEXT        NOT NULL DEFAULT 'trial',
            telefone     TEXT,
            email        TEXT,
            logo_url     TEXT,
            tagline      TEXT,
            cor_primaria TEXT,
            suspenso_em  TIMESTAMPTZ,
            ativo        BOOLEAN     NOT NULL DEFAULT TRUE,
            criado_em    TIMESTAMPTZ NOT NULL DEFAULT NOW(),
            schema_name  TEXT,
            whatsapp_number TEXT,
            whatsapp_token  TEXT
        );
        -- SPLIT --
        CREATE TABLE IF NOT EXISTS "{sc}".users (
            id         UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
            tenant_id  UUID        NOT NULL,
            nome       TEXT        NOT NULL,
            email      TEXT        NOT NULL,
            senha_hash TEXT        NOT NULL,
            papel      TEXT        NOT NULL DEFAULT 'atendente',
            ativo      BOOLEAN     NOT NULL DEFAULT TRUE,
            criado_em  TIMESTAMPTZ NOT NULL DEFAULT NOW()
        );
        -- SPLIT --
        CREATE TABLE IF NOT EXISTS "{sc}".racas (
            id        UUID    NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
            tenant_id UUID    NOT NULL,
            nome      TEXT    NOT NULL,
            especie   TEXT    NOT NULL DEFAULT 'cao',
            ativo     BOOLEAN NOT NULL DEFAULT TRUE,
            criado_em TIMESTAMPTZ NOT NULL DEFAULT NOW()
        );
        -- SPLIT --
        CREATE TABLE IF NOT EXISTS "{sc}".pelagens (
            id        UUID    NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
            tenant_id UUID    NOT NULL,
            nome      TEXT    NOT NULL,
            ativo     BOOLEAN NOT NULL DEFAULT TRUE,
            criado_em TIMESTAMPTZ NOT NULL DEFAULT NOW()
        );
        -- SPLIT --
        CREATE TABLE IF NOT EXISTS "{sc}".tutores (
            id               UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
            tenant_id        UUID        NOT NULL,
            codigo           TEXT,
            nome             TEXT        NOT NULL,
            cpf_cnpj         TEXT,
            telefone         TEXT,
            email            TEXT,
            logradouro       TEXT,
            numero           TEXT,
            complemento      TEXT,
            bairro           TEXT,
            cidade           TEXT,
            estado           TEXT,
            cep              TEXT,
            data_nascimento  DATE,
            aniversario_dia  SMALLINT,
            aniversario_mes  SMALLINT,
            observacoes      TEXT,
            aceita_promocoes BOOLEAN     NOT NULL DEFAULT TRUE,
            ativo            BOOLEAN     NOT NULL DEFAULT TRUE,
            endereco         TEXT,
            obs              TEXT,
            pontos           INT         NOT NULL DEFAULT 0,
            num_end          TEXT,
            criado_em        TIMESTAMPTZ NOT NULL DEFAULT NOW(),
            atualizado_em    TIMESTAMPTZ NOT NULL DEFAULT NOW()
        );
        -- SPLIT --
        CREATE TABLE IF NOT EXISTS "{sc}".pets (
            id              UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
            tenant_id       UUID        NOT NULL,
            tutor_id        UUID        NOT NULL REFERENCES "{sc}".tutores(id),
            nome            TEXT        NOT NULL,
            especie         TEXT,
            raca            TEXT,
            raca_id         UUID        REFERENCES "{sc}".racas(id),
            pelagem_id      UUID        REFERENCES "{sc}".pelagens(id),
            sexo            TEXT,
            data_nascimento TIMESTAMPTZ,
            peso            NUMERIC(8,2),
            microchip       TEXT,
            castrado        BOOLEAN     NOT NULL DEFAULT FALSE,
            peso_kg         NUMERIC(8,2),
            pelagem         TEXT,
            cor             TEXT,
            obs             TEXT,
            tem_microchip   BOOLEAN     NOT NULL DEFAULT FALSE,
            microchip_num   TEXT,
            tem_plano_saude BOOLEAN     NOT NULL DEFAULT FALSE,
            plano_saude_nome TEXT,
            plano_saude_carteira TEXT,
            codigo_sequencial INT,
            foto_url        TEXT,
            observacoes     TEXT,
            ativo           BOOLEAN     NOT NULL DEFAULT TRUE,
            criado_em       TIMESTAMPTZ NOT NULL DEFAULT NOW(),
            atualizado_em   TIMESTAMPTZ NOT NULL DEFAULT NOW()
        );
        -- SPLIT --
        CREATE TABLE IF NOT EXISTS "{sc}".servicos (
            id          UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
            tenant_id   UUID        NOT NULL,
            codigo      TEXT,
            nome        TEXT        NOT NULL,
            categoria   TEXT,
            descricao   TEXT,
            icone       TEXT,
            preco_base  NUMERIC(12,2) NOT NULL DEFAULT 0,
            duracao_min INT           NOT NULL DEFAULT 30,
            ativo       BOOLEAN       NOT NULL DEFAULT TRUE,
            criado_em   TIMESTAMPTZ   NOT NULL DEFAULT NOW()
        );
        -- SPLIT --
        CREATE TABLE IF NOT EXISTS "{sc}".vias_administracao (
            id        UUID    NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
            tenant_id UUID    NOT NULL,
            nome      TEXT    NOT NULL,
            ativo     BOOLEAN NOT NULL DEFAULT TRUE,
            criado_em TIMESTAMPTZ NOT NULL DEFAULT NOW()
        );
        -- SPLIT --
        CREATE TABLE IF NOT EXISTS "{sc}".agendamentos (
            id            UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
            tenant_id     UUID        NOT NULL,
            pet_id        UUID        NOT NULL REFERENCES "{sc}".pets(id),
            user_id       UUID        REFERENCES "{sc}".users(id),
            servico_id    UUID        REFERENCES "{sc}".servicos(id),
            tipo          TEXT,
            data_hora     TIMESTAMPTZ NOT NULL,
            duracao_min   INT         NOT NULL DEFAULT 30,
            status        TEXT        NOT NULL DEFAULT 'pendente',
            origem        TEXT,
            obs           TEXT,
            confirmado_em TIMESTAMPTZ,
            confirmado_por UUID,
            criado_em     TIMESTAMPTZ NOT NULL DEFAULT NOW()
        );
        -- SPLIT --
        CREATE TABLE IF NOT EXISTS "{sc}".ordens_servico (
            id             UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
            tenant_id      UUID        NOT NULL,
            agendamento_id UUID        REFERENCES "{sc}".agendamentos(id),
            pet_id         UUID        NOT NULL REFERENCES "{sc}".pets(id),
            user_id        UUID        REFERENCES "{sc}".users(id),
            funcionario_id UUID,
            status         TEXT        NOT NULL DEFAULT 'aguardando',
            valor_total    NUMERIC(12,2) NOT NULL DEFAULT 0,
            fotos_urls     TEXT[],
            inicio         TIMESTAMPTZ,
            fim            TIMESTAMPTZ,
            entregue_em    TIMESTAMPTZ,
            obs            TEXT,
            criado_em      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
            atualizado_em  TIMESTAMPTZ NOT NULL DEFAULT NOW()
        );
        -- SPLIT --
        CREATE TABLE IF NOT EXISTS "{sc}".os_servicos (
            id            UUID          NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
            os_id         UUID          NOT NULL REFERENCES "{sc}".ordens_servico(id),
            servico_id    UUID          NOT NULL REFERENCES "{sc}".servicos(id),
            preco_cobrado NUMERIC(12,2) NOT NULL DEFAULT 0,
            obs           TEXT
        );
        -- SPLIT --
        CREATE TABLE IF NOT EXISTS "{sc}".prontuario_itens (
            id          UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
            tenant_id   UUID        NOT NULL,
            pet_id      UUID        NOT NULL REFERENCES "{sc}".pets(id),
            user_id     UUID,
            data        TIMESTAMPTZ NOT NULL DEFAULT NOW(),
            tipo        TEXT        NOT NULL,
            titulo      TEXT,
            descricao   TEXT,
            data_evento DATE,
            codigo_unico TEXT,
            motivo      TEXT,
            receituario TEXT,
            anexo_url   TEXT,
            criado_em   TIMESTAMPTZ NOT NULL DEFAULT NOW()
        );
        -- SPLIT --
        CREATE TABLE IF NOT EXISTS "{sc}".vacinacoes (
            id              UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
            tenant_id       UUID        NOT NULL,
            pet_id          UUID        NOT NULL REFERENCES "{sc}".pets(id),
            user_id        UUID,
            vacina         TEXT,
            nome_vacina     TEXT        NOT NULL,
            fabricante      TEXT,
            lote            TEXT,
            data_aplicacao  DATE        NOT NULL,
            proxima_dose    DATE,
            veterinario     TEXT,
            obs             TEXT,
            criado_em       TIMESTAMPTZ NOT NULL DEFAULT NOW()
        );
        -- SPLIT --
        CREATE TABLE IF NOT EXISTS "{sc}".links_agendamento (
            id        UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
            tenant_id UUID        NOT NULL,
            token     TEXT        NOT NULL,
            pet_id    UUID        REFERENCES "{sc}".pets(id),
            tutor_id  UUID        REFERENCES "{sc}".tutores(id),
            expires_at TIMESTAMPTZ,
            expira_em  TIMESTAMPTZ,
            usado     BOOLEAN     NOT NULL DEFAULT FALSE,
            usado_em   TIMESTAMPTZ,
            criado_em TIMESTAMPTZ NOT NULL DEFAULT NOW()
        );
        -- SPLIT --
        CREATE TABLE IF NOT EXISTS "{sc}".produtos (
            id              UUID          NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
            tenant_id       UUID          NOT NULL,
            codigo          TEXT,
            nome            TEXT          NOT NULL,
            categoria       TEXT,
            unidade         TEXT          NOT NULL DEFAULT 'un',
            preco_venda     NUMERIC(12,2) NOT NULL DEFAULT 0,
            preco_custo     NUMERIC(12,2) NOT NULL DEFAULT 0,
            estoque_atual   NUMERIC(12,2) NOT NULL DEFAULT 0,
            estoque_minimo  NUMERIC(12,2) NOT NULL DEFAULT 0,
            estoque_ideal   NUMERIC(12,2),
            codigo_barras   TEXT,
            controla_validade BOOLEAN     NOT NULL DEFAULT FALSE,
            ativo           BOOLEAN       NOT NULL DEFAULT TRUE,
            criado_em       TIMESTAMPTZ   NOT NULL DEFAULT NOW()
        );
        -- SPLIT --
        CREATE TABLE IF NOT EXISTS "{sc}".movimentacoes_estoque (
            id         UUID          NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
            tenant_id  UUID          NOT NULL,
            produto_id UUID          NOT NULL REFERENCES "{sc}".produtos(id),
            tipo       TEXT          NOT NULL,
            quantidade   NUMERIC(12,2) NOT NULL,
            estoque_apos NUMERIC(12,2) NOT NULL DEFAULT 0,
            custo_unitario NUMERIC(12,2),
            motivo       TEXT,
            venda_id     UUID,
            user_id      UUID,
            data         TIMESTAMPTZ NOT NULL DEFAULT NOW(),
            criado_em    TIMESTAMPTZ NOT NULL DEFAULT NOW()
        );
        -- SPLIT --
        CREATE TABLE IF NOT EXISTS "{sc}".caixas (
            id               UUID          NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
            tenant_id        UUID          NOT NULL,
            user_id          UUID          REFERENCES "{sc}".users(id),
            data             DATE          NOT NULL,
            saldo_inicial    NUMERIC(12,2) NOT NULL DEFAULT 0,
            total_vendas     NUMERIC(12,2) NOT NULL DEFAULT 0,
            total_servicos   NUMERIC(12,2) NOT NULL DEFAULT 0,
            saldo_final      NUMERIC(12,2),
            aberto_em        TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
            fechado_em       TIMESTAMPTZ,
            obs              TEXT,
            requer_aprovacao BOOLEAN       NOT NULL DEFAULT FALSE,
            aprovado_por     UUID,
            aprovado_em      TIMESTAMPTZ
        );
        -- SPLIT --
        CREATE TABLE IF NOT EXISTS "{sc}".movimentacoes_caixa (
            id        UUID          NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
            tenant_id UUID          NOT NULL,
            caixa_id  UUID          NOT NULL REFERENCES "{sc}".caixas(id),
            user_id   UUID          REFERENCES "{sc}".users(id),
            tipo      TEXT          NOT NULL,
            valor     NUMERIC(12,2) NOT NULL,
            descricao TEXT,
            criado_em TIMESTAMPTZ   NOT NULL DEFAULT NOW()
        );
        -- SPLIT --
        CREATE TABLE IF NOT EXISTS "{sc}".vendas (
            id              UUID          NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
            tenant_id       UUID          NOT NULL,
            tutor_id        UUID          REFERENCES "{sc}".tutores(id),
            caixa_id        UUID          REFERENCES "{sc}".caixas(id),
            os_id           UUID          REFERENCES "{sc}".ordens_servico(id),
            numero          TEXT,
            valor_subtotal  NUMERIC(12,2) NOT NULL DEFAULT 0,
            valor_produtos  NUMERIC(12,2) NOT NULL DEFAULT 0,
            valor_desconto  NUMERIC(12,2) NOT NULL DEFAULT 0,
            desconto        NUMERIC(12,2) NOT NULL DEFAULT 0,
            valor_total     NUMERIC(12,2) NOT NULL DEFAULT 0,
            user_id         UUID,
            forma_pagamento TEXT,
            status          TEXT          NOT NULL DEFAULT 'finalizada',
            finalizada_em   TIMESTAMPTZ,
            criado_em       TIMESTAMPTZ   NOT NULL DEFAULT NOW()
        );
        -- SPLIT --
        CREATE TABLE IF NOT EXISTS "{sc}".venda_itens (
            id             UUID          NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
            tenant_id      UUID          NOT NULL,
            venda_id       UUID          NOT NULL REFERENCES "{sc}".vendas(id),
            produto_id     UUID          REFERENCES "{sc}".produtos(id),
            servico_id     UUID          REFERENCES "{sc}".servicos(id),
            descricao      TEXT,
            quantidade     NUMERIC(12,2) NOT NULL DEFAULT 1,
            preco_unitario NUMERIC(12,2) NOT NULL DEFAULT 0,
            desconto       NUMERIC(12,2) NOT NULL DEFAULT 0,
            preco_total    NUMERIC(12,2) NOT NULL DEFAULT 0,
            subtotal       NUMERIC(12,2) NOT NULL DEFAULT 0
        );
        -- SPLIT --
        CREATE TABLE IF NOT EXISTS "{sc}".categorias_financeiras (
            id        UUID    NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
            tenant_id UUID    NOT NULL,
            nome      TEXT    NOT NULL,
            tipo      TEXT    NOT NULL,
            ativo     BOOLEAN NOT NULL DEFAULT TRUE
        );
        -- SPLIT --
        CREATE TABLE IF NOT EXISTS "{sc}".contas (
            id               UUID          NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
            tenant_id        UUID          NOT NULL,
            os_id            UUID,
            venda_id         UUID,
            pedido_id        UUID,
            tipo             TEXT          NOT NULL,
            descricao        TEXT          NOT NULL,
            valor            NUMERIC(12,2) NOT NULL,
            valor_pago       NUMERIC(12,2),
            data_competencia DATE,
            data_vencimento  DATE,
            data_baixa       DATE,
            forma_pagamento  TEXT,
            status           TEXT          NOT NULL DEFAULT 'aberta',
            categoria_id     UUID          REFERENCES "{sc}".categorias_financeiras(id),
            conta_bancaria   TEXT,
            obs_baixa        TEXT,
            criado_por       UUID,
            criado_em        TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
            atualizado_em    TIMESTAMPTZ   NOT NULL DEFAULT NOW()
        );
        -- SPLIT --
        CREATE TABLE IF NOT EXISTS "{sc}".conta_parcelas (
            id              UUID          NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
            tenant_id       UUID          NOT NULL,
            conta_id        UUID          NOT NULL REFERENCES "{sc}".contas(id),
            numero          INT           NOT NULL,
            valor           NUMERIC(12,2) NOT NULL,
            data_vencimento DATE          NOT NULL,
            data_pagamento  DATE,
            data_baixa      DATE,
            valor_pago      NUMERIC(12,2),
            status          TEXT          NOT NULL DEFAULT 'aberto'
        );
        -- SPLIT --
        CREATE TABLE IF NOT EXISTS "{sc}".lancamentos (
            id           UUID          NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
            tenant_id    UUID          NOT NULL,
            os_id        UUID,
            tipo         TEXT          NOT NULL,
            descricao    TEXT          NOT NULL,
            valor        NUMERIC(12,2) NOT NULL,
            data         DATE          NOT NULL,
            categoria_id UUID          REFERENCES "{sc}".categorias_financeiras(id),
            criado_em    TIMESTAMPTZ   NOT NULL DEFAULT NOW()
        );
        -- SPLIT --
        CREATE TABLE IF NOT EXISTS "{sc}".contas_bancarias (
            id            UUID          NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
            tenant_id     UUID          NOT NULL,
            nome          TEXT          NOT NULL,
            banco         TEXT,
            agencia       TEXT,
            conta         TEXT,
            tipo          TEXT,
            saldo_inicial NUMERIC(12,2) NOT NULL DEFAULT 0,
            ativo         BOOLEAN       NOT NULL DEFAULT TRUE,
            criado_em     TIMESTAMPTZ   NOT NULL DEFAULT NOW()
        );
        -- SPLIT --
        CREATE TABLE IF NOT EXISTS "{sc}".movimentacoes_bancarias (
            id                UUID          NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
            tenant_id         UUID          NOT NULL,
            conta_bancaria_id UUID          NOT NULL REFERENCES "{sc}".contas_bancarias(id),
            tipo              TEXT          NOT NULL,
            descricao         TEXT          NOT NULL,
            valor             NUMERIC(12,2) NOT NULL,
            data_movimentacao DATE          NOT NULL,
            conta_id          UUID,
            categoria_id      UUID,
            conta_destino_id  UUID,
            origem            TEXT,
            conciliado        BOOLEAN       NOT NULL DEFAULT FALSE,
            criado_por        UUID,
            criado_em         TIMESTAMPTZ   NOT NULL DEFAULT NOW()
        );
        -- SPLIT --
        CREATE TABLE IF NOT EXISTS "{sc}".conciliacoes_diarias (
            id                UUID          NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
            tenant_id         UUID          NOT NULL,
            conta_bancaria_id UUID          NOT NULL REFERENCES "{sc}".contas_bancarias(id),
            data_conciliacao  DATE          NOT NULL,
            saldo_anterior    NUMERIC(12,2) NOT NULL DEFAULT 0,
            total_entradas    NUMERIC(12,2) NOT NULL DEFAULT 0,
            total_saidas      NUMERIC(12,2) NOT NULL DEFAULT 0,
            saldo_final       NUMERIC(12,2) NOT NULL DEFAULT 0,
            saldo_sistema     NUMERIC(12,2) NOT NULL DEFAULT 0,
            saldo_extrato     NUMERIC(12,2) NOT NULL DEFAULT 0,
            diferenca         NUMERIC(12,2) NOT NULL DEFAULT 0,
            status            TEXT          NOT NULL DEFAULT 'pendente',
            observacoes       TEXT,
            observacao        TEXT,
            fechado_por       UUID,
            fechado_em        TIMESTAMPTZ,
            criado_em         TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
            CONSTRAINT uq_conciliacao_{sc} UNIQUE (conta_bancaria_id, data_conciliacao)
        );
        -- SPLIT --
        CREATE TABLE IF NOT EXISTS "{sc}".cargos (
            id               UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
            tenant_id        UUID        NOT NULL,
            nome             TEXT        NOT NULL,
            pode_receituario BOOLEAN     NOT NULL DEFAULT FALSE,
            ativo            BOOLEAN     NOT NULL DEFAULT TRUE,
            criado_em        TIMESTAMPTZ NOT NULL DEFAULT NOW()
        );
        -- SPLIT --
        CREATE TABLE IF NOT EXISTS "{sc}".funcionarios (
            id                  UUID          NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
            tenant_id           UUID          NOT NULL,
            codigo              TEXT,
            nome                TEXT          NOT NULL,
            cpf                 TEXT,
            rg                  TEXT,
            data_nascimento     DATE,
            logradouro          TEXT,
            numero              TEXT,
            complemento         TEXT,
            bairro              TEXT,
            cidade              TEXT,
            estado              TEXT,
            cep                 TEXT,
            telefone            TEXT,
            email               TEXT,
            cargo               TEXT,
            cargo_id            UUID          REFERENCES "{sc}".cargos(id),
            usuario_id          UUID          REFERENCES "{sc}".users(id),
            crmv                TEXT,
            registro_mapa       TEXT,
            data_admissao       DATE,
            salario             NUMERIC(12,2) NOT NULL DEFAULT 0,
            percentual_comissao NUMERIC(5,2)  NOT NULL DEFAULT 0,
            assina_receituario  BOOLEAN       NOT NULL DEFAULT FALSE,
            status              TEXT          NOT NULL DEFAULT 'trabalhando',
            criado_em           TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
            atualizado_em       TIMESTAMPTZ   NOT NULL DEFAULT NOW()
        );
        -- SPLIT --
        CREATE TABLE IF NOT EXISTS "{sc}".comissoes (
            id             UUID          NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
            tenant_id      UUID          NOT NULL,
            funcionario_id UUID          NOT NULL REFERENCES "{sc}".funcionarios(id),
            origem         TEXT          NOT NULL,
            origem_id      UUID          NOT NULL,
            valor_base     NUMERIC(12,2) NOT NULL DEFAULT 0,
            percentual     NUMERIC(5,2)  NOT NULL DEFAULT 0,
            valor_comissao NUMERIC(12,2) NOT NULL DEFAULT 0,
            status         TEXT          NOT NULL DEFAULT 'pendente',
            fechamento_id  UUID,
            competencia    DATE          NOT NULL,
            criado_em      TIMESTAMPTZ   NOT NULL DEFAULT NOW()
        );
        -- SPLIT --
        CREATE TABLE IF NOT EXISTS "{sc}".fechamento_mensal (
            id             UUID          NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
            tenant_id      UUID          NOT NULL,
            funcionario_id UUID          NOT NULL REFERENCES "{sc}".funcionarios(id),
            competencia    DATE          NOT NULL,
            salario        NUMERIC(12,2) NOT NULL DEFAULT 0,
            total_comissoes NUMERIC(12,2) NOT NULL DEFAULT 0,
            total_pagar    NUMERIC(12,2) NOT NULL DEFAULT 0,
            status         TEXT          NOT NULL DEFAULT 'aberto',
            observacoes    TEXT,
            fechado_em     TIMESTAMPTZ,
            fechado_por    UUID,
            criado_em      TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
            CONSTRAINT uq_fechamento_{sc} UNIQUE (funcionario_id, competencia)
        );
        -- SPLIT --
        CREATE TABLE IF NOT EXISTS "{sc}".parametros_sistema (
            id                   UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
            tenant_id            UUID        NOT NULL UNIQUE,
            comissao_os_ativo    BOOLEAN     NOT NULL DEFAULT TRUE,
            comissao_pdv_ativo   BOOLEAN     NOT NULL DEFAULT FALSE,
            criado_em            TIMESTAMPTZ DEFAULT NOW(),
            anthropic_api_key    TEXT,
            ia_ativo             BOOLEAN     NOT NULL DEFAULT FALSE,
            cert_pfx_encrypted   TEXT,
            cert_senha_hash      TEXT,
            cert_tipo            TEXT,
            cert_titular         TEXT,
            cert_cpf             TEXT,
            cert_validade        DATE,
            cert_ativo           BOOLEAN     NOT NULL DEFAULT FALSE,
            cert_atualizado_em   TIMESTAMPTZ,
            smtp_host            TEXT,
            smtp_porta           INT         NOT NULL DEFAULT 587,
            smtp_usuario         TEXT,
            smtp_senha_enc       TEXT,
            smtp_ssl             BOOLEAN     NOT NULL DEFAULT TRUE,
            email_remetente      TEXT,
            email_contabil       TEXT,
            contabil_nome        TEXT,
            atualizado_em        TIMESTAMPTZ NOT NULL DEFAULT NOW()
        );
        -- SPLIT --
        CREATE TABLE IF NOT EXISTS "{sc}".fornecedores (
            id        UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
            tenant_id UUID        NOT NULL,
            codigo    TEXT,
            nome      TEXT        NOT NULL,
            cnpj      TEXT,
            telefone  TEXT,
            email     TEXT,
            contato   TEXT,
            logradouro TEXT,
            numero    TEXT,
            num_end   TEXT,
            bairro    TEXT,
            cidade    TEXT,
            estado    TEXT,
            cep       TEXT,
            obs       TEXT,
            ativo     BOOLEAN     NOT NULL DEFAULT TRUE,
            criado_em TIMESTAMPTZ NOT NULL DEFAULT NOW()
        );
        -- SPLIT --
        CREATE TABLE IF NOT EXISTS "{sc}".condicoes_pagamento (
            id            UUID    NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
            tenant_id     UUID    NOT NULL,
            nome          TEXT    NOT NULL,
            parcelas      INT     NOT NULL DEFAULT 1,
            intervalo_dias INT    NOT NULL DEFAULT 30,
            ativo         BOOLEAN NOT NULL DEFAULT TRUE,
            criado_em     TIMESTAMPTZ NOT NULL DEFAULT NOW()
        );
        -- SPLIT --
        CREATE TABLE IF NOT EXISTS "{sc}".pedidos_compra (
            id                  UUID          NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
            tenant_id           UUID          NOT NULL,
            numero              TEXT,
            fornecedor_id       UUID          REFERENCES "{sc}".fornecedores(id),
            condicao_pagamento_id UUID        REFERENCES "{sc}".condicoes_pagamento(id),
            forma_pagamento     TEXT,
            status              TEXT          NOT NULL DEFAULT 'rascunho',
            data_pedido         DATE          NOT NULL,
            data_recebimento    DATE,
            valor_total         NUMERIC(12,2) NOT NULL DEFAULT 0,
            parcelas            INT           NOT NULL DEFAULT 1,
            obs                 TEXT,
            criado_por          UUID,
            criado_em           TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
            atualizado_em       TIMESTAMPTZ   NOT NULL DEFAULT NOW()
        );
        -- SPLIT --
        CREATE TABLE IF NOT EXISTS "{sc}".pedido_itens (
            id             UUID          NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
            pedido_id      UUID          NOT NULL REFERENCES "{sc}".pedidos_compra(id),
            produto_id     UUID          REFERENCES "{sc}".produtos(id),
            nome_produto   TEXT,
            quantidade     NUMERIC(12,2) NOT NULL DEFAULT 1,
            unidade        TEXT,
            valor_unitario NUMERIC(12,2) NOT NULL DEFAULT 0,
            valor_total    NUMERIC(12,2) NOT NULL DEFAULT 0,
            uso            TEXT,
            criado_em      TIMESTAMPTZ   NOT NULL DEFAULT NOW()
        );
        -- SPLIT --
        CREATE TABLE IF NOT EXISTS "{sc}".recebimentos_mercadoria (
            id               UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
            tenant_id        UUID        NOT NULL,
            pedido_id        UUID        NOT NULL REFERENCES "{sc}".pedidos_compra(id),
            numero_nf        TEXT,
            data_recebimento DATE        NOT NULL,
            status           TEXT        NOT NULL DEFAULT 'pendente',
            observacoes      TEXT,
            obs              TEXT,
            criado_por       UUID,
            criado_em        TIMESTAMPTZ NOT NULL DEFAULT NOW()
        );
        -- SPLIT --
        CREATE TABLE IF NOT EXISTS "{sc}".recebimento_itens (
            id                  UUID          NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
            recebimento_id      UUID          NOT NULL REFERENCES "{sc}".recebimentos_mercadoria(id),
            pedido_item_id      UUID,
            produto_id          UUID          REFERENCES "{sc}".produtos(id),
            nome_produto        TEXT,
            quantidade_pedida   NUMERIC(12,2) NOT NULL DEFAULT 0,
            quantidade_recebida NUMERIC(12,2) NOT NULL DEFAULT 0,
            valor_unitario      NUMERIC(12,2) NOT NULL DEFAULT 0,
            uso                 TEXT,
            criado_em           TIMESTAMPTZ   NOT NULL DEFAULT NOW()
        );
        -- SPLIT --
        CREATE TABLE IF NOT EXISTS "{sc}".recebimento_anexos (
            id             UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
            tenant_id      UUID        NOT NULL,
            recebimento_id UUID        NOT NULL REFERENCES "{sc}".recebimentos_mercadoria(id),
            nome_arquivo   TEXT        NOT NULL,
            nome           TEXT        NOT NULL DEFAULT '',
            url            TEXT        NOT NULL DEFAULT '',
            tipo_arquivo   TEXT        NOT NULL DEFAULT 'application/octet-stream',
            tamanho_bytes  INT         NOT NULL DEFAULT 0,
            dados_base64   TEXT        NOT NULL DEFAULT '',
            descricao      TEXT,
            criado_por     UUID,
            criado_em      TIMESTAMPTZ NOT NULL DEFAULT NOW()
        );
        -- SPLIT --
        CREATE TABLE IF NOT EXISTS "{sc}".planos_saude (
            id              UUID          NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
            tenant_id       UUID          NOT NULL,
            nome            TEXT          NOT NULL,
            operadora       TEXT,
            desconto_padrao NUMERIC(5,2)  NOT NULL DEFAULT 0,
            desconto_percent NUMERIC(5,2) NOT NULL DEFAULT 0,
            obs             TEXT,
            ativo           BOOLEAN       NOT NULL DEFAULT TRUE,
            criado_em       TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
            atualizado_em   TIMESTAMPTZ   NOT NULL DEFAULT NOW()
        );
        -- SPLIT --
        CREATE TABLE IF NOT EXISTS "{sc}".pet_planos (
            id              UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
            tenant_id       UUID        NOT NULL,
            pet_id          UUID        NOT NULL REFERENCES "{sc}".pets(id),
            plano_id        UUID        NOT NULL REFERENCES "{sc}".planos_saude(id),
            numero_carteira  TEXT,
            num_carteirinha  TEXT,
            data_inicio      DATE,
            data_fim         DATE,
            validade         DATE,
            desconto_percent NUMERIC(5,2),
            ativo            BOOLEAN     NOT NULL DEFAULT TRUE,
            criado_em        TIMESTAMPTZ NOT NULL DEFAULT NOW(),
            atualizado_em    TIMESTAMPTZ NOT NULL DEFAULT NOW()
        );
        -- SPLIT --
        CREATE TABLE IF NOT EXISTS "{sc}".tutor_planos (
            id              UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
            tenant_id       UUID        NOT NULL,
            tutor_id        UUID        NOT NULL REFERENCES "{sc}".tutores(id),
            plano_id        UUID        NOT NULL REFERENCES "{sc}".planos_saude(id),
            numero_carteira  TEXT,
            num_carteirinha  TEXT,
            data_inicio      DATE,
            validade         DATE,
            desconto_percent NUMERIC(5,2),
            ativo            BOOLEAN     NOT NULL DEFAULT TRUE,
            criado_em        TIMESTAMPTZ NOT NULL DEFAULT NOW(),
            atualizado_em    TIMESTAMPTZ NOT NULL DEFAULT NOW()
        );
        -- SPLIT --
        CREATE TABLE IF NOT EXISTS "{sc}".fechamentos_contabeis (
            id            UUID          NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
            tenant_id     UUID          NOT NULL,
            ano           INT           NOT NULL,
            mes           INT           NOT NULL,
            status        TEXT          NOT NULL DEFAULT 'aberto',
            enviado_em    TIMESTAMPTZ,
            enviado_por   UUID,
            email_destino TEXT,
            obs           TEXT,
            criado_em     TIMESTAMPTZ   NOT NULL DEFAULT NOW()
        );
        -- SPLIT --
        CREATE TABLE IF NOT EXISTS "{sc}".fechamento_documentos (
            id            UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
            tenant_id     UUID        NOT NULL,
            fechamento_id UUID        NOT NULL REFERENCES "{sc}".fechamentos_contabeis(id),
            tipo          TEXT        NOT NULL,
            nome          TEXT        NOT NULL,
            descricao     TEXT,
            tipo_arquivo  TEXT        NOT NULL,
            tamanho_bytes INT         NOT NULL DEFAULT 0,
            dados_base64  TEXT        NOT NULL,
            origem        TEXT,
            criado_por    UUID,
            criado_em     TIMESTAMPTZ NOT NULL DEFAULT NOW()
        );
        -- SPLIT --
        CREATE TABLE IF NOT EXISTS "{sc}".bot_config (
            id                       UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
            tenant_id                UUID        NOT NULL,
            ativo                    BOOLEAN     NOT NULL DEFAULT FALSE,
            hora_inicio              TIME        NOT NULL DEFAULT '08:00:00',
            hora_fim                 TIME        NOT NULL DEFAULT '18:00:00',
            dias_semana              TEXT        NOT NULL DEFAULT '1,2,3,4,5,6',
            dias_antecedencia_min    INT         NOT NULL DEFAULT 0,
            dias_antecedencia_max    INT         NOT NULL DEFAULT 30,
            msg_boas_vindas          TEXT        NOT NULL DEFAULT '',
            msg_qual_pet             TEXT        NOT NULL DEFAULT '',
            msg_qual_servico         TEXT        NOT NULL DEFAULT '',
            msg_qual_data            TEXT        NOT NULL DEFAULT '',
            msg_horarios_disponiveis TEXT        NOT NULL DEFAULT '',
            msg_confirmacao          TEXT        NOT NULL DEFAULT '',
            msg_sem_horarios         TEXT        NOT NULL DEFAULT '',
            msg_fora_horario         TEXT        NOT NULL DEFAULT '',
            msg_erro                 TEXT        NOT NULL DEFAULT '',
            msg_cancelar             TEXT        NOT NULL DEFAULT '',
            timeout_conversa_min     INT         NOT NULL DEFAULT 30,
            criado_em                TIMESTAMPTZ NOT NULL DEFAULT NOW(),
            atualizado_em            TIMESTAMPTZ NOT NULL DEFAULT NOW(),
            meta_phone_number_id     TEXT,
            meta_waba_id             TEXT,
            meta_token               TEXT,
            webhook_verify_token     TEXT
        );
        -- SPLIT --
        CREATE TABLE IF NOT EXISTS "{sc}".bot_conversas (
            id            UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
            tenant_id     UUID        NOT NULL,
            telefone         TEXT        NOT NULL,
            estado           TEXT        NOT NULL DEFAULT 'inicio',
            contexto         TEXT,
            tutor_id         UUID,
            pet_id           UUID,
            pet_nome_digitado TEXT,
            servico_id       UUID,
            servico_nome     TEXT,
            duracao_min      INT,
            data_escolhida   DATE,
            ultima_msg_em    TIMESTAMPTZ NOT NULL DEFAULT NOW(),
            criado_em        TIMESTAMPTZ NOT NULL DEFAULT NOW(),
            atualizado_em    TIMESTAMPTZ NOT NULL DEFAULT NOW()
        );
        -- SPLIT --
        CREATE TABLE IF NOT EXISTS "{sc}".bot_logs (
            id          UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
            tenant_id   UUID        NOT NULL,
            conversa_id  UUID        REFERENCES "{sc}".bot_conversas(id),
            telefone     TEXT        NOT NULL DEFAULT '',
            direcao      TEXT        NOT NULL,
            mensagem     TEXT,
            estado_antes TEXT,
            estado_apos  TEXT,
            erro         TEXT,
            criado_em    TIMESTAMPTZ NOT NULL DEFAULT NOW()
        );
        -- SPLIT --
        CREATE TABLE IF NOT EXISTS "{sc}".config_mensagens (
            id        UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
            tenant_id UUID        NOT NULL,
            gatilho   TEXT        NOT NULL,
            canal     TEXT        NOT NULL DEFAULT 'whatsapp',
            ativo     BOOLEAN     NOT NULL DEFAULT FALSE,
            horas_antes INT,
            template  TEXT,
            hora_envio TIME        DEFAULT '09:00:00',
            criado_em TIMESTAMPTZ NOT NULL DEFAULT NOW()
        );
        -- SPLIT --
        CREATE TABLE IF NOT EXISTS "{sc}".campanhas (
            id                  UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
            tenant_id           UUID        NOT NULL,
            titulo              TEXT        NOT NULL,
            mensagem            TEXT,
            imagem_url          TEXT,
            publico             TEXT        NOT NULL DEFAULT 'todos',
            canal               TEXT        NOT NULL DEFAULT 'whatsapp',
            status              TEXT        NOT NULL DEFAULT 'aguardando_aprovacao',
            total_destinatarios INT         NOT NULL DEFAULT 0,
            total_enviados      INT         NOT NULL DEFAULT 0,
            motivo_reprovacao   TEXT,
            criado_por          UUID,
            aprovado_por        UUID,
            aprovado_em         TIMESTAMPTZ,
            enviada_em          TIMESTAMPTZ,
            criado_em           TIMESTAMPTZ NOT NULL DEFAULT NOW()
        );
        -- SPLIT --
        CREATE TABLE IF NOT EXISTS "{sc}".notificacoes_fila (
            id             UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
            tenant_id      UUID        NOT NULL,
            agendamento_id UUID,
            os_id          UUID,
            pet_id         UUID,
            tutor_id       UUID        REFERENCES "{sc}".tutores(id),
            canal          TEXT        NOT NULL DEFAULT 'whatsapp',
            tipo           TEXT        NOT NULL,
            destinatario   TEXT        NOT NULL,
            payload        TEXT,
            status         TEXT        NOT NULL DEFAULT 'pendente',
            tentativas     INT         NOT NULL DEFAULT 0,
            agendado_para  TIMESTAMPTZ,
            processado_em  TIMESTAMPTZ,
            enviado_em     TIMESTAMPTZ,
            erro           TEXT,
            criado_em      TIMESTAMPTZ NOT NULL DEFAULT NOW()
        );
        -- SPLIT --
        CREATE TABLE IF NOT EXISTS "{sc}".metas_faturamento (
            id        UUID          NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
            tenant_id UUID          NOT NULL,
            ano       INT           NOT NULL,
            mes       INT           NOT NULL,
            valor_meta    NUMERIC(12,2) NOT NULL DEFAULT 0,
            criado_em     TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
            atualizado_em TIMESTAMPTZ   NOT NULL DEFAULT NOW()
        );
        """;

    // Gera schema name seguro para PostgreSQL a partir do nome da clínica
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
