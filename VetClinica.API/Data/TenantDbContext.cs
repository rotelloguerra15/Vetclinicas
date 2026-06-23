using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using VetClinica.API.Models;

namespace VetClinica.API.Data;

// Contexto operacional de cada clínica.
// O schema PostgreSQL é definido dinamicamente no login (claim "schema" do JWT).
// Exemplo: schema "vet_barbarafonseca" ou "vet_petlove".
// Dados de clínicas nunca se cruzam — cada uma tem seu próprio conjunto de tabelas.
public class TenantDbContext : DbContext
{
    private readonly string _schema;

    // Exposto para o TenantModelCacheKeyFactory (cache de modelo por schema).
    public string Schema => _schema;

    public TenantDbContext(DbContextOptions<TenantDbContext> options, string schema)
        : base(options)
    {
        _schema = schema;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // CRÍTICO multi-schema: sem isto, o EF cacheia o modelo compilado por
        // TIPO de DbContext apenas, ignorando o schema da instância — fazendo
        // todas as instâncias após a primeira reaproveitarem o schema da
        // primeira clínica resolvida desde o boot do processo (vazamento
        // entre tenants). Ver TenantModelCacheKeyFactory.
        optionsBuilder.ReplaceService<IModelCacheKeyFactory, TenantModelCacheKeyFactory>();
        base.OnConfiguring(optionsBuilder);
    }

    // ── Clínica / Tutores / Pets ──────────────────────────────────────────────
    public DbSet<User>                Users                => Set<User>();
    public DbSet<Tutor>               Tutores              => Set<Tutor>();
    public DbSet<Pet>                 Pets                 => Set<Pet>();
    public DbSet<Raca>                Racas                => Set<Raca>();
    public DbSet<Pelagem>             Pelagens             => Set<Pelagem>();

    // ── Agenda / Atendimento ─────────────────────────────────────────────────
    public DbSet<Agendamento>         Agendamentos         => Set<Agendamento>();
    public DbSet<OrdemServico>        OrdensServico        => Set<OrdemServico>();
    public DbSet<OsServico>           OsServicos           => Set<OsServico>();
    public DbSet<ProntuarioItem>      ProntuarioItens      => Set<ProntuarioItem>();
    public DbSet<Vacinacao>           Vacinacoes           => Set<Vacinacao>();
    public DbSet<ViaAdministracao>    ViasAdministracao    => Set<ViaAdministracao>();
    public DbSet<LinkAgendamento>     LinksAgendamento     => Set<LinkAgendamento>();

    // ── Catálogo / Estoque ────────────────────────────────────────────────────
    public DbSet<Servico>             Servicos             => Set<Servico>();
    public DbSet<Produto>             Produtos             => Set<Produto>();
    public DbSet<MovimentacaoEstoque> MovimentacoesEstoque => Set<MovimentacaoEstoque>();

    // ── PDV / Caixa ───────────────────────────────────────────────────────────
    public DbSet<Venda>               Vendas               => Set<Venda>();
    public DbSet<VendaItem>           VendaItens           => Set<VendaItem>();
    public DbSet<CobrancaPix>         CobrancasPix         => Set<CobrancaPix>();
    public DbSet<Caixa>               Caixas               => Set<Caixa>();
    public DbSet<MovimentacaoCaixa>   MovimentacoesCaixa   => Set<MovimentacaoCaixa>();

    // ── M2 Financeiro ─────────────────────────────────────────────────────────
    public DbSet<CategoriaFinanceira>  CategoriasFinanceiras  => Set<CategoriaFinanceira>();
    public DbSet<Conta>                Contas                 => Set<Conta>();
    public DbSet<ContaParcela>         ContaParcelas          => Set<ContaParcela>();
    public DbSet<Lancamento>           Lancamentos            => Set<Lancamento>();
    public DbSet<ContaBancaria>        ContasBancarias        => Set<ContaBancaria>();
    public DbSet<MovimentacaoBancaria> MovimentacoesBancarias => Set<MovimentacaoBancaria>();
    public DbSet<ConciliacaoDiaria>    ConciliacoesDiarias    => Set<ConciliacaoDiaria>();

    // ── M3 RH ─────────────────────────────────────────────────────────────────
    public DbSet<Cargo>               Cargos               => Set<Cargo>();
    public DbSet<Funcionario>         Funcionarios         => Set<Funcionario>();
    public DbSet<Comissao>            Comissoes            => Set<Comissao>();
    public DbSet<FechamentoMensal>    FechamentosMensais   => Set<FechamentoMensal>();

    // ── M4 Compras ────────────────────────────────────────────────────────────
    public DbSet<Fornecedor>              Fornecedores           => Set<Fornecedor>();
    public DbSet<CondicaoPagamento>       CondicoesPagamento     => Set<CondicaoPagamento>();
    public DbSet<PedidoCompra>            PedidosCompra          => Set<PedidoCompra>();
    public DbSet<PedidoItem>              PedidoItens            => Set<PedidoItem>();
    public DbSet<RecebimentoMercadoria>   RecebimentosMercadoria => Set<RecebimentoMercadoria>();
    public DbSet<Contrato>                Contratos              => Set<Contrato>();
    public DbSet<ContratoParcela>         ContratoParcelas       => Set<ContratoParcela>();
    public DbSet<RecebimentoItem>         RecebimentoItens       => Set<RecebimentoItem>();
    public DbSet<RecebimentoAnexo>        RecebimentoAnexos      => Set<RecebimentoAnexo>();

    // ── M5 Planos de Saúde ───────────────────────────────────────────────────
    public DbSet<PlanoSaude>          PlanosSaude          => Set<PlanoSaude>();
    public DbSet<PetPlano>            PetPlanos            => Set<PetPlano>();
    public DbSet<TutorPlano>          TutorPlanos          => Set<TutorPlano>();

    // ── Módulo Contábil ──────────────────────────────────────────────────────
    public DbSet<FechamentoContabil>  FechamentosContabeis  => Set<FechamentoContabil>();
    public DbSet<FechamentoDocumento> FechamentoDocumentos  => Set<FechamentoDocumento>();

    // ── WhatsApp Bot ──────────────────────────────────────────────────────────
    public DbSet<BotConfig>           BotConfigs           => Set<BotConfig>();
    public DbSet<BotConversa>         BotConversas         => Set<BotConversa>();
    public DbSet<BotLog>              BotLogs              => Set<BotLog>();

    // ── Outros ───────────────────────────────────────────────────────────────
    public DbSet<ConfigMensagem>      ConfigMensagens      => Set<ConfigMensagem>();
    public DbSet<Campanha>            Campanhas            => Set<Campanha>();
    public DbSet<NotificacaoFila>     NotificacoesFila     => Set<NotificacaoFila>();
    public DbSet<ParametrosSistema>   ParametrosSistema    => Set<ParametrosSistema>();
    public DbSet<MetaFaturamento>     MetasFaturamento     => Set<MetaFaturamento>();

    // DbSet para Tenant — necessário em controllers que leem dados do tenant (nome, logo, etc.)
    // Os tenants ficam no schema "platform", mas alguns controllers precisam ler nome/cor.
    // A leitura cross-schema é feita via PlatformDbContext injetado separadamente.
    // Este DbSet é mantido para compatibilidade com GestaoVistaController e similares
    // que leem o tenant via _db.Tenants — nesses casos, o tenant precisa estar no schema do tenant tbm.
    // ALTERNATIVA: injetar PlatformDbContext nesses controllers específicos.
    // Por ora mantemos a tabela "tenants" replicada no schema do tenant (simplifica a migração).
    public DbSet<Tenant>              Tenants              => Set<Tenant>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        // Define o schema dinamicamente para TODAS as tabelas deste contexto
        mb.HasDefaultSchema(_schema);

        // ── Relacionamentos (idênticos ao AppDbContext original) ──────────────

        mb.Entity<Tutor>().HasMany(t => t.Pets).WithOne(p => p.Tutor).HasForeignKey(p => p.TutorId);
        mb.Entity<Pet>().HasOne(p => p.Tutor).WithMany(t => t.Pets).HasForeignKey(p => p.TutorId);

        mb.Entity<MovimentacaoCaixa>()
            .HasOne(m => m.Caixa)
            .WithMany(c => c.Movimentacoes)
            .HasForeignKey(m => m.CaixaId);

        mb.Entity<PedidoCompra>()
            .HasMany(p => p.Itens)
            .WithOne()
            .HasForeignKey(i => i.PedidoId);

        mb.Entity<RecebimentoMercadoria>()
            .HasMany(r => r.Itens)
            .WithOne()
            .HasForeignKey(i => i.RecebimentoId);

        mb.Entity<Conta>()
            .HasOne(c => c.Categoria)
            .WithMany()
            .HasForeignKey(c => c.CategoriaId)
            .IsRequired(false);

        mb.Entity<Comissao>()
            .HasOne(c => c.Funcionario)
            .WithMany()
            .HasForeignKey(c => c.FuncionarioId);

        mb.Entity<FechamentoMensal>()
            .HasOne(f => f.Funcionario)
            .WithMany()
            .HasForeignKey(f => f.FuncionarioId);

        mb.Entity<FechamentoMensal>()
            .HasIndex(f => new { f.FuncionarioId, f.Competencia })
            .IsUnique();

        mb.Entity<ParametrosSistema>()
            .HasIndex(p => p.TenantId)
            .IsUnique();

        mb.Entity<Funcionario>()
            .HasOne(f => f.CargoObj)
            .WithMany()
            .HasForeignKey(f => f.CargoId)
            .IsRequired(false);

        mb.Entity<Funcionario>()
            .HasOne(f => f.Usuario)
            .WithMany()
            .HasForeignKey(f => f.UsuarioId)
            .IsRequired(false);

        mb.Entity<ContaBancaria>()
            .HasMany(c => c.Movimentacoes)
            .WithOne(m => m.ContaBancaria)
            .HasForeignKey(m => m.ContaBancariaId);

        mb.Entity<ConciliacaoDiaria>()
            .HasOne(c => c.ContaBancaria)
            .WithMany()
            .HasForeignKey(c => c.ContaBancariaId);

        mb.Entity<ConciliacaoDiaria>()
            .HasIndex(c => new { c.ContaBancariaId, c.DataConciliacao })
            .IsUnique();

        mb.Entity<FechamentoContabil>()
            .HasMany(f => f.Documentos)
            .WithOne()
            .HasForeignKey(d => d.FechamentoId);

        mb.Entity<PlanoSaude>()
            .HasMany(p => p.PetPlanos)
            .WithOne(pp => pp.Plano)
            .HasForeignKey(pp => pp.PlanoId);

        mb.Entity<PlanoSaude>()
            .HasMany(p => p.TutorPlanos)
            .WithOne(tp => tp.Plano)
            .HasForeignKey(tp => tp.PlanoId);

        mb.Entity<PetPlano>()
            .HasOne(pp => pp.Pet)
            .WithMany()
            .HasForeignKey(pp => pp.PetId);

        mb.Entity<TutorPlano>()
            .HasOne(tp => tp.Tutor)
            .WithMany()
            .HasForeignKey(tp => tp.TutorId);

        base.OnModelCreating(mb);
    }
}
