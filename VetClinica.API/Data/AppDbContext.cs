using Microsoft.EntityFrameworkCore;
using VetClinica.API.Models;

namespace VetClinica.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Tutor> Tutores => Set<Tutor>();
    public DbSet<Pet> Pets => Set<Pet>();
    public DbSet<Servico> Servicos => Set<Servico>();
    public DbSet<Agendamento> Agendamentos => Set<Agendamento>();
    public DbSet<OrdemServico> OrdensServico => Set<OrdemServico>();
    public DbSet<OsServico> OsServicos => Set<OsServico>();
    public DbSet<ProntuarioItem> ProntuarioItens => Set<ProntuarioItem>();
    public DbSet<Vacinacao> Vacinacoes => Set<Vacinacao>();
    public DbSet<CategoriaFinanceira> CategoriasFinanceiras => Set<CategoriaFinanceira>();
    public DbSet<Lancamento> Lancamentos => Set<Lancamento>();
    public DbSet<NotificacaoFila> NotificacoesFila => Set<NotificacaoFila>();
    public DbSet<LinkAgendamento> LinksAgendamento => Set<LinkAgendamento>();
    public DbSet<Produto> Produtos => Set<Produto>();
    public DbSet<MovimentacaoEstoque> MovimentacoesEstoque => Set<MovimentacaoEstoque>();
    public DbSet<Venda> Vendas => Set<Venda>();
    public DbSet<VendaItem> VendaItens => Set<VendaItem>();
    public DbSet<ConfigMensagem> ConfigMensagens => Set<ConfigMensagem>();
    public DbSet<PlatformAdmin> PlatformAdmins => Set<PlatformAdmin>();
    public DbSet<Campanha> Campanhas => Set<Campanha>();
    public DbSet<Caixa> Caixas => Set<Caixa>();
    public DbSet<MovimentacaoCaixa> MovimentacoesCaixa => Set<MovimentacaoCaixa>();
    public DbSet<Conta> Contas => Set<Conta>();
    public DbSet<ContaParcela> ContaParcelas => Set<ContaParcela>();

    // Cadastros auxiliares
    public DbSet<ViaAdministracao> ViasAdministracao => Set<ViaAdministracao>();

    // M4 Compras
    public DbSet<Fornecedor> Fornecedores => Set<Fornecedor>();
    public DbSet<PedidoCompra> PedidosCompra => Set<PedidoCompra>();
    public DbSet<PedidoItem> PedidoItens => Set<PedidoItem>();

    // Movimentacao Bancaria
    public DbSet<ContaBancaria> ContasBancarias => Set<ContaBancaria>();
    public DbSet<MovimentacaoBancaria> MovimentacoesBancarias => Set<MovimentacaoBancaria>();

    // Bot WhatsApp
    public DbSet<BotConfig> BotConfigs => Set<BotConfig>();
    public DbSet<BotConversa> BotConversas => Set<BotConversa>();
    public DbSet<BotLog> BotLogs => Set<BotLog>();

    // RH
    public DbSet<Funcionario> Funcionarios => Set<Funcionario>();
    public DbSet<Comissao> Comissoes => Set<Comissao>();
    public DbSet<FechamentoMensal> FechamentosMensais => Set<FechamentoMensal>();
    public DbSet<ParametrosSistema> ParametrosSistema => Set<ParametrosSistema>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<Tutor>().HasMany(t => t.Pets).WithOne(p => p.Tutor).HasForeignKey(p => p.TutorId);
        mb.Entity<Pet>().HasOne(p => p.Tutor).WithMany(t => t.Pets).HasForeignKey(p => p.TutorId);
        mb.Entity<MovimentacaoCaixa>()
            .HasOne(m => m.Caixa)
            .WithMany(c => c.Movimentacoes)
            .HasForeignKey(m => m.CaixaId);
        mb.Entity<Conta>()
            .HasOne(c => c.Categoria)
            .WithMany()
            .HasForeignKey(c => c.CategoriaId)
            .IsRequired(false);

        // RH
        mb.Entity<Comissao>()
            .HasOne(c => c.Funcionario)
            .WithMany()
            .HasForeignKey(c => c.FuncionarioId);

        mb.Entity<FechamentoMensal>()
            .HasOne(f => f.Funcionario)
            .WithMany()
            .HasForeignKey(f => f.FuncionarioId);

        mb.Entity<FechamentoMensal>()
            .HasIndex(f => new { f.TenantId, f.FuncionarioId, f.Competencia })
            .IsUnique();

        mb.Entity<ParametrosSistema>()
            .HasIndex(p => p.TenantId)
            .IsUnique();

        base.OnModelCreating(mb);
    }
}
