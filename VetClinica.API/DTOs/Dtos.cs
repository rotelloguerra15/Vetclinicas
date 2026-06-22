namespace VetClinica.API.DTOs;

// ---------- Auth ----------
public record LoginRequest(string Email, string Senha);
public record LoginResponse(string Token, string Nome, string Papel, Guid TenantId, string? Plano = null, DateTime? TrialExpiraEm = null);
public record LoginOutcome(LoginResponse? Resposta, string? CodigoErro, string? Mensagem);

// ---------- Admin / Plataforma ----------
public record AtualizarPagamentoRequest(string? StatusPagamento, DateTime? ProximoFaturamento);

// ---------- Paginação ----------
public record PagedResult<T>(IEnumerable<T> Items, int Total, int Page, int PageSize);

// ---------- Tutor ----------
public record TutorDto(Guid Id, string Nome, string? CpfCnpj, string? Telefone,
    string? Email, string? Endereco, int Pontos, int QtdPets,
    // endereço desmembrado
    string? Logradouro, string? NumEnd, string? Complemento,
    string? Bairro, string? Cidade, string? Estado, string? Cep);

public record TutorCreate(string Nome, string? CpfCnpj, string? Telefone,
    string? Email, string? Obs, short? AniversarioDia, short? AniversarioMes,
    // endereço desmembrado
    string? Logradouro, string? NumEnd, string? Complemento,
    string? Bairro, string? Cidade, string? Estado, string? Cep);

// ---------- Pet ----------
public record PetDto(Guid Id, Guid TutorId, string TutorNome, string Nome, string Especie,
    string? Raca, string Sexo, DateOnly? DataNascimento, decimal? PesoKg, string? FotoUrl,
    // calculado
    string? IdadeFormatada,
    // microchip
    bool TemMicrochip, string? MicrochipNum,
    // plano de saúde
    bool TemPlanoSaude, string? PlanoSaudeNome, string? PlanoSaudeCarteira);

public record PetCreate(Guid TutorId, string Nome, string Especie, string? Raca, string Sexo,
    DateOnly? DataNascimento, string? Pelagem, decimal? PesoKg, bool? Castrado, string? Cor, string? Obs,
    // microchip
    bool TemMicrochip, string? MicrochipNum,
    // plano de saúde
    bool TemPlanoSaude, string? PlanoSaudeNome, string? PlanoSaudeCarteira);

// ---------- Serviço ----------
public record ServicoDto(Guid Id, string Nome, string? Categoria, decimal PrecoBase, int? DuracaoMin, bool Ativo, string? Icone = null);
public record ServicoCreate(string Nome, string? Categoria, string? Descricao, decimal PrecoBase, int? DuracaoMin, string? Icone = null);

// ---------- Agendamento ----------
public record AgendamentoDto(Guid Id, Guid PetId, string PetNome, string Tipo,
    DateTime DataHora, int DuracaoMin, string Status, string Origem, string? Obs);
public record AgendamentoCreate(Guid PetId, Guid? UserId, string Tipo, DateTime DataHora,
    int DuracaoMin, string? Obs, Guid? ServicoId = null);
public record SlotLivre(DateTime Inicio, DateTime Fim);

// ---------- Ordem de Serviço ----------
public record OsDto(Guid Id, Guid PetId, string PetNome, Guid? TutorId, string? TutorNome,
    string Status, decimal? ValorTotal, DateTime? Inicio, DateTime? Fim, string[]? FotosUrls);
public record OsServicoDto(Guid Id, Guid ServicoId, string ServicoNome, decimal PrecoCobrado, string? Obs);
public record OsCreate(Guid PetId, Guid? AgendamentoId, Guid? FuncionarioId, List<OsServicoItem> Servicos);
public record OsServicoItem(Guid ServicoId, decimal PrecoCobrado, string? Obs);
public record OsStatusUpdate(string Status);

// ---------- Prontuário ----------
public record ProntuarioDto(Guid Id, DateTime Data, string Tipo, string? Titulo, string? Motivo, string Descricao, string? Receituario, string? AnexoUrl);
public record ProntuarioCreate(Guid PetId, string Tipo, DateTime? Data, string? Titulo, string? Motivo, string Descricao, string? Receituario, string? AnexoUrl);

// ---------- Vacina ----------
public record VacinaDto(Guid Id, string Vacina, string? Fabricante, DateOnly DataAplicacao, DateOnly? ProximaDose);
public record VacinaCreate(Guid PetId, string Vacina, string? Fabricante, string? Lote,
    DateOnly DataAplicacao, DateOnly? ProximaDose, string? Obs);

// ---------- Financeiro ----------
public record LancamentoDto(Guid Id, DateOnly Data, string Tipo, decimal Valor, string? Descricao);
public record ResumoFinanceiro(decimal TotalReceitas, decimal TotalDespesas, decimal Saldo);

// ---------- Público ----------
public record SlotsResponse(Guid PetId, Guid TutorId, string? PetNome, List<SlotLivre> Slots);
public record AgendarPublicoRequest(DateTime DataHora, string Tipo);

// ---------- Genérico ----------
public record StatusUpdate(string Status);

// ===================== ESTOQUE / PDV =====================
public record ProdutoDto(Guid Id, string Nome, string? Categoria, string Unidade,
    decimal PrecoCusto, decimal PrecoVenda, decimal EstoqueAtual, decimal EstoqueMinimo,
    decimal? EstoqueIdeal, bool AbaixoMinimo);
public record ProdutoCreate(string Nome, string? Codigo, string? CodigoBarras, string? Categoria, string Unidade,
    decimal PrecoCusto, decimal PrecoVenda, decimal EstoqueAtual, decimal EstoqueMinimo,
    decimal? EstoqueIdeal, bool ControlaValidade);
public record AjusteEstoque(decimal Quantidade, string Tipo, string? Motivo);
public record SugestaoCompra(Guid ProdutoId, string Nome, decimal EstoqueAtual,
    decimal EstoqueMinimo, decimal QuantidadeSugerida);

public record VendaItemCreate(Guid ProdutoId, decimal Quantidade, decimal PrecoUnitario);
public record VendaCreate(Guid? TutorId, Guid? OsId, List<VendaItemCreate> Itens, decimal Desconto, string FormaPagamento);
public record VendaDto(Guid Id, Guid? TutorId, string? TutorNome, string Status,
    decimal ValorTotal, string? FormaPagamento, DateTime CriadoEm);

// ===================== MENSAGENS =====================
public record ConfigMensagemDto(Guid Id, string Gatilho, string Canal, bool Ativo,
    int? HorasAntes, string Template);
public record ConfigMensagemUpdate(string Canal, bool Ativo, int? HorasAntes, string Template);

// ===================== PLATAFORMA (SaaS) =====================
public record ClinicaListItem(Guid Id, string Nome, string Plano, bool Ativo,
    bool Suspenso, int TotalUsuarios, int TotalPets, DateTime CriadoEm);
public record NovaClinicaRequest(string NomeClinica, string Plano, string NomeDono,
    string EmailDono, string? Telefone, string? Tagline);
public record NovaClinicaResponse(Guid TenantId, string LoginEmail, string SenhaTemporaria);
public record MudarPlanoRequest(string Plano);

// marca da clínica (white-label)
public record BrandingDto(string Nome, string? Tagline, string? LogoUrl, string? CorPrimaria);
public record BrandingUpdate(string? Tagline, string? LogoUrl, string? CorPrimaria);

// ===================== CAMPANHAS / PROMOÇÕES =====================
public record CampanhaDto(Guid Id, string Titulo, string Mensagem, string? ImagemUrl,
    string Publico, string Status, int TotalDestinatarios, int TotalEnviados,
    string? MotivoReprovacao, DateTime CriadoEm);
public record CampanhaCreate(string Titulo, string Mensagem, string? ImagemUrl, string Publico);
public record ReprovarRequest(string Motivo);
public record PreviaPublico(int Total);

// ===================== CAIXA =====================
public record CaixaDto(Guid Id, DateOnly Data, decimal SaldoInicial, decimal TotalVendas,
    decimal TotalServicos, decimal? SaldoFinal, bool Aberto, DateTime AbertoEm, DateTime? FechadoEm);
public record AbrirCaixaRequest(decimal SaldoInicial);
public record FecharCaixaRequest(decimal? SaldoFinal, string? Obs);
public record MovimentacaoRequest(string Tipo, decimal Valor, string? Descricao);
public record MovimentacaoDto(Guid Id, string Tipo, decimal Valor, string? Descricao, DateTime CriadoEm);
public record CaixaDetalheDto(Guid Id, DateOnly Data, decimal SaldoInicial, decimal TotalVendas,
    decimal TotalServicos, decimal? SaldoFinal, bool Aberto, DateTime AbertoEm, DateTime? FechadoEm,
    List<MovimentacaoDto> Movimentacoes);

// ===================== RELATÓRIOS =====================
public record RelatorioMensal(int Ano, int Mes, decimal Receitas, decimal Despesas,
    decimal Saldo, int TotalAtendimentos, int TotalVendas, int NovosClientes);
public record VendaDia(DateOnly Data, int Qtd, decimal Total);

// ===================== FINANCEIRO M2 =====================

// --- Categorias ---
public record CategoriaFinanceiraDto(Guid Id, string Nome, string Tipo, bool Ativo);
public record CategoriaCreate(string Nome, string Tipo, string? Descricao);

// --- Contas a pagar/receber ---
public record ContaDto(
    Guid Id,
    string Tipo,
    string Descricao,
    decimal Valor,
    DateOnly DataCompetencia,
    DateOnly DataVencimento,
    string? FormaPagamento,
    string Status,
    decimal? ValorPago,
    DateOnly? DataBaixa,
    string? ContaBancaria,
    Guid? OsId,
    Guid? VendaId,
    string? CategoriaNome,
    int? DiasAtraso
);

public record ContaCreate(
    string Tipo,                    // receita | despesa
    string Descricao,
    decimal Valor,
    DateOnly DataCompetencia,
    DateOnly DataVencimento,
    string? FormaPagamento,
    Guid? CategoriaId,
    string? ContaBancaria,
    // vínculos opcionais
    Guid? OsId,
    Guid? VendaId
);

// FIX: adicionado ContaBancariaId (Guid) para criar MovimentacaoBancaria ao dar baixa
// ContaBancariaNome mantém compatibilidade com campo string existente na tabela contas
public record BaixaContaRequest(
    decimal ValorPago,
    DateOnly DataBaixa,
    string? FormaPagamento,
    string? ContaBancariaNome,   // nome do banco (armazenado em contas.conta_bancaria)
    Guid? ContaBancariaId,       // id da conta bancária (para criar MovimentacaoBancaria)
    string? ObsBaixa
);

// --- Dashboard financeiro ---
public record ResumoFinanceiroM2(
    decimal TotalReceitas,          // contas recebidas no período
    decimal TotalDespesas,          // contas pagas no período
    decimal Saldo,
    decimal ContasReceberAberto,    // a receber ainda em aberto
    decimal ContasPagarAberto,      // a pagar ainda em aberto
    int ContasVencidas,             // qtd contas em atraso
    decimal ValorVencido            // total em atraso
);

public record FluxoDia(DateOnly Data, decimal Receitas, decimal Despesas);

// --- Atualizar LancamentoDto para incluir conta_id ---
public record LancamentoDtoV2(
    Guid Id,
    DateOnly Data,
    string Tipo,
    decimal Valor,
    string? Descricao,
    Guid? ContaId,
    Guid? OsId,
    Guid? VendaId,
    string? FormaPagamento
);

// ===================== RH =====================

public record FuncionarioDto(
    Guid Id,
    string Nome,
    string? Cargo,
    string? Crmv,
    string? RegistroMapa,
    string? Telefone,
    string? Email,
    decimal Salario,
    decimal PercentualComissao,
    string Status,
    DateOnly? DataAdmissao
);

public record FuncionarioCreate(
    string Nome,
    string? Codigo,
    string? Cpf,
    string? Rg,
    DateOnly? DataNascimento,
    string? Logradouro,
    string? Numero,
    string? Complemento,
    string? Bairro,
    string? Cidade,
    string? Estado,
    string? Cep,
    string? Telefone,
    string? Email,
    string? Cargo,
    string? Crmv,
    string? RegistroMapa,
    DateOnly? DataAdmissao,
    decimal Salario,
    decimal PercentualComissao,
    string Status
);

public record FuncionarioStatusUpdate(string Status);

public record ComissaoDto(
    Guid Id,
    Guid FuncionarioId,
    string NomeFuncionario,
    string Origem,
    decimal ValorBase,
    decimal Percentual,
    decimal ValorComissao,
    string Status,
    DateOnly Competencia,
    DateTime CriadoEm
);

public record FechamentoCreate(
    Guid FuncionarioId,
    DateOnly Competencia,
    string? Observacoes
);

public record FechamentoDto(
    Guid Id,
    Guid FuncionarioId,
    string NomeFuncionario,
    DateOnly Competencia,
    decimal Salario,
    decimal TotalComissoes,
    decimal TotalPagar,
    string Status,
    string? Observacoes,
    DateTime? FechadoEm
);

public record ParametrosSistemaDto(
    bool ComissaoOsAtivo,
    bool ComissaoPdvAtivo
);

public record RelatorioRHItem(
    string NomeFuncionario,
    string? Cargo,
    decimal Salario,
    decimal TotalComissoes,
    decimal TotalPagar,
    int TotalOs,
    string Status
);

public record ProdutividadeItem(
    string NomeFuncionario,
    int TotalOs,
    decimal TotalFaturado,
    decimal TotalComissao
);

public record CustoMensalItem(
    string Mes,
    decimal CustoTotal
);

// ---------- M5: Planos de Saúde ----------
public record PlanoSaudeDto(Guid Id, string Nome, string? Operadora, decimal DescontoPercent, string? Obs, bool Ativo);
public record PlanoSaudeCreate(string Nome, string? Operadora, decimal DescontoPercent, string? Obs);

public record VincularPlanoRequest(Guid PlanoId, string? NumCarteirinha, DateOnly? Validade, decimal? DescontoPercent);

public record PetPlanoDto(
    Guid Id, Guid PetId, Guid PlanoId, string PlanoNome, string? Operadora,
    string? NumCarteirinha, DateOnly? Validade, decimal DescontoEfetivo, bool Ativo);

public record TutorPlanoDto(
    Guid Id, Guid TutorId, Guid PlanoId, string PlanoNome, string? Operadora,
    string? NumCarteirinha, DateOnly? Validade, decimal DescontoEfetivo, bool Ativo);

public record DescontoPlanoResponse(Guid? PlanoId, string? PlanoNome, decimal Desconto, DateOnly? Validade);

// ── Auth: recuperacao de senha ────────────────────────────────────────────────
public record EsqueciSenhaRequest(string Email);
public record RedefinirSenhaRequest(string Token, string NovaSenha);

// ── Admin: SMTP config ────────────────────────────────────────────────────────
public record SmtpConfigRequest(string Host, string Porta, string Usuario, string Senha, string Ssl, string Remetente,
    string? Provider = null, string? ResendApiKey = null, string? ResendRemetente = null,
    string? GraphTenantId = null, string? GraphClientId = null, string? GraphClientSecret = null, string? GraphRemetente = null);
