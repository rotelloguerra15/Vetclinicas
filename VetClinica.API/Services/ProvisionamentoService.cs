using Microsoft.EntityFrameworkCore;
using VetClinica.API.Data;
using VetClinica.API.Models;

namespace VetClinica.API.Services;

// Cria uma clínica nova "chave na mão": tenant + dono + dados padrão.
// É o que permite escalar a venda — cada novo cliente nasce pronto pra usar.
public class ProvisionamentoService
{
    private readonly AppDbContext _db;
    public ProvisionamentoService(AppDbContext db) => _db = db;

    public record NovaClinicaResult(Guid TenantId, Guid OwnerUserId, string LoginEmail, string SenhaTemporaria);

    public async Task<NovaClinicaResult> CriarClinica(
        string nomeClinica, string plano, string nomeDono, string emailDono,
        string? telefone, string? tagline)
    {
        // 1. cria o tenant
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Nome = nomeClinica,
            Plano = plano,
            Email = emailDono,
            Telefone = telefone,
            Tagline = tagline ?? "Gestão Inteligente para Clínicas Veterinárias",
            CriadoEm = DateTime.UtcNow,
            Ativo = true
        };
        _db.Tenants.Add(tenant);

        // 2. cria o usuário dono com senha temporária
        var senhaTemp = GerarSenhaTemporaria();
        var owner = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Nome = nomeDono,
            Email = emailDono,
            SenhaHash = BCrypt.Net.BCrypt.HashPassword(senhaTemp, 12),
            Papel = "owner",
            Ativo = true,
            CriadoEm = DateTime.UtcNow
        };
        _db.Users.Add(owner);

        // 3. categorias financeiras padrão — completo para o módulo financeiro M2
        //    IMPORTANTE: "Vendas PDV" deve existir pois o VendasController busca por esse nome
        string[] receitas =
        {
            "Consultas",
            "Banho e Tosa",
            "Vacinas",
            "Cirurgias",
            "Exames",
            "Internações",
            "Vendas PDV",       // usado automaticamente pelo VendasController
            "Outros Serviços"
        };

        string[] despesas =
        {
            "Aluguel",
            "Água / Luz / Internet",
            "Folha de Pagamento",
            "Medicamentos e Insumos",
            "Equipamentos",
            "Marketing",
            "Impostos e Taxas",
            "Manutenção",
            "Outros"
        };

        foreach (var c in receitas)
            _db.CategoriasFinanceiras.Add(new CategoriaFinanceira
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                Nome = c,
                Tipo = "receita",
                Ativo = true
            });

        foreach (var c in despesas)
            _db.CategoriasFinanceiras.Add(new CategoriaFinanceira
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                Nome = c,
                Tipo = "despesa",
                Ativo = true
            });

        // 4. serviços iniciais
        (string nome, string cat, decimal preco, int dur)[] servicos =
        {
            ("Banho pequeno porte",   "banho_tosa", 60m,  90),
            ("Banho médio porte",     "banho_tosa", 90m,  120),
            ("Banho grande porte",    "banho_tosa", 140m, 150),
            ("Tosa higiênica",        "banho_tosa", 40m,  45),
            ("Consulta clínica geral","consulta",   150m, 30),
            ("Vacina V8/V10",         "vacina",     85m,  15),
            ("Vacina antirrábica",    "vacina",     65m,  15)
        };
        foreach (var s in servicos)
            _db.Servicos.Add(new Servico
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                Nome = s.nome,
                Categoria = s.cat,
                PrecoBase = s.preco,
                DuracaoMin = s.dur
            });

        // 5. mensagens automáticas padrão (desligadas até configurar o WhatsApp)
        (string gat, string tpl)[] msgs =
        {
            ("aniversario_pet",      "🎉 Hoje é aniversário do {pet}! 🐾 A equipe da {clinica} deseja muitas festas. Que tal um banho especial de presente?"),
            ("aniversario_tutor",    "🎂 Parabéns, {tutor}! A {clinica} deseja um feliz aniversário. Você e o {pet} merecem o melhor! 💙"),
            ("boas_vindas_tutor",    "Olá {tutor}! 🐾 Seja bem-vindo(a) à {clinica}. Estamos felizes em cuidar do {pet}!"),
            ("lembrete_agendamento", "🐕 Oi {tutor}! Lembrando o agendamento do {pet} em {data} às {hora}, na {clinica}."),
            ("pos_atendimento_nps",  "Oi {tutor}! Como foi o atendimento do {pet} na {clinica}? De 0 a 10, quanto nos recomendaria? 🙏")
        };
        foreach (var m in msgs)
            _db.ConfigMensagens.Add(new ConfigMensagem
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                Gatilho = m.gat,
                Canal = "whatsapp",
                Ativo = false,
                Template = m.tpl,
                CriadoEm = DateTime.UtcNow
            });

        await _db.SaveChangesAsync();
        return new NovaClinicaResult(tenant.Id, owner.Id, emailDono, senhaTemp);
    }

    private static string GerarSenhaTemporaria()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";
        var rnd = Random.Shared;
        return new string(Enumerable.Range(0, 10).Select(_ => chars[rnd.Next(chars.Length)]).ToArray());
    }
}
