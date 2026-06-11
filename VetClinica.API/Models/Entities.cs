using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VetClinica.API.Models;

// ── RH ────────────────────────────────────────────────────────────────────────

[Table("funcionarios")]
public class Funcionario
{
    [Column("id")] public Guid Id { get; set; }
    [Column("tenant_id")] public Guid TenantId { get; set; }

    [Column("codigo")] public string? Codigo { get; set; }

    // Pessoal
    [Column("nome")] public string Nome { get; set; } = "";
    [Column("cpf")] public string? Cpf { get; set; }
    [Column("rg")] public string? Rg { get; set; }
    [Column("data_nascimento")] public DateOnly? DataNascimento { get; set; }

    // Endereço
    [Column("logradouro")] public string? Logradouro { get; set; }
    [Column("numero")] public string? Numero { get; set; }
    [Column("complemento")] public string? Complemento { get; set; }
    [Column("bairro")] public string? Bairro { get; set; }
    [Column("cidade")] public string? Cidade { get; set; }
    [Column("estado")] public string? Estado { get; set; }
    [Column("cep")] public string? Cep { get; set; }

    // Contato
    [Column("telefone")] public string? Telefone { get; set; }
    [Column("email")] public string? Email { get; set; }

    // Profissional
    [Column("cargo")] public string? Cargo { get; set; }
    [Column("crmv")] public string? Crmv { get; set; }
    [Column("registro_mapa")] public string? RegistroMapa { get; set; }
    [Column("data_admissao")] public DateOnly? DataAdmissao { get; set; }
    [Column("salario")] public decimal Salario { get; set; }
    [Column("percentual_comissao")] public decimal PercentualComissao { get; set; }

    [Column("assina_receituario")] public bool AssinaReceituario { get; set; } = false;
    [Column("cargo_id")]           public Guid? CargoId { get; set; }
    [Column("usuario_id")]         public Guid? UsuarioId { get; set; }

    public Cargo? CargoObj { get; set; }
    public User? Usuario { get; set; }

    // trabalhando | ferias | demitido
    [Column("status")] public string Status { get; set; } = "trabalhando";

    [Column("criado_em")] public DateTime CriadoEm { get; set; }
    [Column("atualizado_em")] public DateTime AtualizadoEm { get; set; }
}

[Table("comissoes")]
public class Comissao
{
    [Column("id")] public Guid Id { get; set; }
    [Column("tenant_id")] public Guid TenantId { get; set; }
    [Column("funcionario_id")] public Guid FuncionarioId { get; set; }

    // os | pdv
    [Column("origem")] public string Origem { get; set; } = "";
    [Column("origem_id")] public Guid OrigemId { get; set; }

    [Column("valor_base")] public decimal ValorBase { get; set; }
    [Column("percentual")] public decimal Percentual { get; set; }
    [Column("valor_comissao")] public decimal ValorComissao { get; set; }

    // pendente | pago | cancelado
    [Column("status")] public string Status { get; set; } = "pendente";
    [Column("fechamento_id")] public Guid? FechamentoId { get; set; }

    [Column("competencia")] public DateOnly Competencia { get; set; }
    [Column("criado_em")] public DateTime CriadoEm { get; set; }

    public Funcionario? Funcionario { get; set; }
}

[Table("fechamento_mensal")]
public class FechamentoMensal
{
    [Column("id")] public Guid Id { get; set; }
    [Column("tenant_id")] public Guid TenantId { get; set; }
    [Column("funcionario_id")] public Guid FuncionarioId { get; set; }

    [Column("competencia")] public DateOnly Competencia { get; set; }

    [Column("salario")] public decimal Salario { get; set; }
    [Column("total_comissoes")] public decimal TotalComissoes { get; set; }
    [Column("total_pagar")] public decimal TotalPagar { get; set; }

    // aberto | fechado | pago
    [Column("status")] public string Status { get; set; } = "aberto";
    [Column("observacoes")] public string? Observacoes { get; set; }
    [Column("fechado_em")] public DateTime? FechadoEm { get; set; }
    [Column("fechado_por")] public Guid? FechadoPor { get; set; }
    [Column("criado_em")] public DateTime CriadoEm { get; set; }

    public Funcionario? Funcionario { get; set; }
}

[Table("parametros_sistema")]
public class ParametrosSistema
{
    [Column("id")] public Guid Id { get; set; }
    [Column("tenant_id")] public Guid TenantId { get; set; }
    [Column("comissao_os_ativo")]    public bool ComissaoOsAtivo { get; set; } = true;
    [Column("comissao_pdv_ativo")]   public bool ComissaoPdvAtivo { get; set; } = false;
    [Column("anthropic_api_key")]    public string? AnthropicApiKey { get; set; }
    [Column("ia_ativo")]             public bool IaAtivo { get; set; } = false;

    // Certificado Digital
    [Column("cert_pfx_encrypted")]   public string? CertPfxEncrypted { get; set; }
    [Column("cert_senha_hash")]      public string? CertSenhaHash { get; set; }
    [Column("cert_tipo")]            public string? CertTipo { get; set; }        // a1 | a3_nuvem
    [Column("cert_titular")]         public string? CertTitular { get; set; }
    [Column("cert_cpf")]             public string? CertCpf { get; set; }
    [Column("cert_validade")]        public DateOnly? CertValidade { get; set; }
    [Column("cert_ativo")]           public bool CertAtivo { get; set; } = false;
    [Column("cert_atualizado_em")]   public DateTime? CertAtualizadoEm { get; set; }

    // SMTP
    [Column("smtp_host")]           public string? SmtpHost { get; set; }
    [Column("smtp_porta")]          public int SmtpPorta { get; set; } = 587;
    [Column("smtp_usuario")]        public string? SmtpUsuario { get; set; }
    [Column("smtp_senha_enc")]      public string? SmtpSenhaEnc { get; set; }
    [Column("smtp_ssl")]            public bool SmtpSsl { get; set; } = true;
    [Column("email_remetente")]     public string? EmailRemetente { get; set; }
    [Column("email_contabil")]      public string? EmailContabil { get; set; }
    [Column("contabil_nome")]       public string? ContabilNome { get; set; }

    [Column("criado_em")]            public DateTime CriadoEm { get; set; }
    [Column("atualizado_em")]        public DateTime AtualizadoEm { get; set; }
}

// ===================== MODULO CONTABIL =====================

[Table("fechamentos_contabeis")]
public class FechamentoContabil
{
    [Column("id")]              public Guid Id { get; set; }
    [Column("tenant_id")]       public Guid TenantId { get; set; }
    [Column("ano")]             public int Ano { get; set; }
    [Column("mes")]             public int Mes { get; set; }
    [Column("status")]          public string Status { get; set; } = "aberto";
    [Column("enviado_em")]      public DateTime? EnviadoEm { get; set; }
    [Column("enviado_por")]     public Guid? EnviadoPor { get; set; }
    [Column("email_destino")]   public string? EmailDestino { get; set; }
    [Column("obs")]             public string? Obs { get; set; }
    [Column("criado_em")]       public DateTime CriadoEm { get; set; }

    public List<FechamentoDocumento> Documentos { get; set; } = new();
}

[Table("fechamento_documentos")]
public class FechamentoDocumento
{
    [Column("id")]              public Guid Id { get; set; }
    [Column("tenant_id")]       public Guid TenantId { get; set; }
    [Column("fechamento_id")]   public Guid FechamentoId { get; set; }
    [Column("tipo")]            public string Tipo { get; set; } = "";
    [Column("nome")]            public string Nome { get; set; } = "";
    [Column("descricao")]       public string? Descricao { get; set; }
    [Column("tipo_arquivo")]    public string TipoArquivo { get; set; } = "application/pdf";
    [Column("tamanho_bytes")]   public int TamanhoBytes { get; set; }
    [Column("dados_base64")]    public string DadosBase64 { get; set; } = "";
    [Column("origem")]          public string Origem { get; set; } = "upload";
    [Column("criado_por")]      public Guid? CriadoPor { get; set; }
    [Column("criado_em")]       public DateTime CriadoEm { get; set; }
}

// ─────────────────────────────────────────────────────────────────────────────

[Table("tenants")]
public class Tenant
{
    [Column("id")] public Guid Id { get; set; }
    [Column("nome")] public string Nome { get; set; } = "";
    [Column("plano")] public string Plano { get; set; } = "trial";
    [Column("telefone")] public string? Telefone { get; set; }
    [Column("email")] public string? Email { get; set; }
    [Column("whatsapp_number")] public string? WhatsappNumber { get; set; }
    [Column("whatsapp_token")] public string? WhatsappToken { get; set; }
    [Column("criado_em")] public DateTime CriadoEm { get; set; }
    [Column("logo_url")] public string? LogoUrl { get; set; }
    [Column("tagline")] public string? Tagline { get; set; }
    [Column("cor_primaria")] public string? CorPrimaria { get; set; }
    [Column("suspenso_em")] public DateTime? SuspensoEm { get; set; }
    [Column("ativo")] public bool Ativo { get; set; } = true;
}

[Table("users")]
public class User
{
    [Column("id")] public Guid Id { get; set; }
    [Column("tenant_id")] public Guid TenantId { get; set; }
    [Column("email")] public string Email { get; set; } = "";
    [Column("nome")] public string Nome { get; set; } = "";
    [Column("senha_hash")] public string SenhaHash { get; set; } = "";
    [Column("papel")] public string Papel { get; set; } = "recepcionista";
    [Column("ativo")] public bool Ativo { get; set; } = true;
    [Column("criado_em")] public DateTime CriadoEm { get; set; }
}

[Table("tutores")]
public class Tutor
{
    [Column("id")] public Guid Id { get; set; }
    [Column("tenant_id")] public Guid TenantId { get; set; }
    [Column("nome")] public string Nome { get; set; } = "";
    [Column("cpf_cnpj")] public string? CpfCnpj { get; set; }
    [Column("telefone")] public string? Telefone { get; set; }
    [Column("email")] public string? Email { get; set; }
    [Column("endereco")] public string? Endereco { get; set; }  // legado

    // Endereço desmembrado
    [Column("logradouro")] public string? Logradouro { get; set; }
    [Column("num_end")] public string? NumEnd { get; set; }
    [Column("complemento")] public string? Complemento { get; set; }
    [Column("bairro")] public string? Bairro { get; set; }
    [Column("cidade")] public string? Cidade { get; set; }
    [Column("estado")] public string? Estado { get; set; }
    [Column("cep")] public string? Cep { get; set; }

    [Column("obs")] public string? Obs { get; set; }
    [Column("pontos")] public int Pontos { get; set; }
    [Column("aceita_promocoes")] public bool AceitaPromocoes { get; set; } = true;
    [Column("aniversario_dia")] public short? AniversarioDia { get; set; }
    [Column("aniversario_mes")] public short? AniversarioMes { get; set; }
    [Column("ativo")] public bool Ativo { get; set; } = true;
    [Column("criado_em")] public DateTime CriadoEm { get; set; }

    public List<Pet> Pets { get; set; } = new();
}

[Table("pets")]
public class Pet
{
    [Column("id")] public Guid Id { get; set; }
    [Column("tenant_id")] public Guid TenantId { get; set; }
    [Column("tutor_id")] public Guid TutorId { get; set; }
    [Column("nome")] public string Nome { get; set; } = "";
    [Column("especie")] public string Especie { get; set; } = "cao";
    [Column("raca")] public string? Raca { get; set; }
    [Column("sexo")] public string Sexo { get; set; } = "indefinido";
    [Column("data_nascimento")] public DateOnly? DataNascimento { get; set; }
    [Column("pelagem")] public string? Pelagem { get; set; }
    [Column("peso_kg")] public decimal? PesoKg { get; set; }
    [Column("castrado")] public bool? Castrado { get; set; }
    [Column("cor")] public string? Cor { get; set; }

    // Microchip
    [Column("tem_microchip")] public bool TemMicrochip { get; set; }
    [Column("microchip_num")] public string? MicrochipNum { get; set; }

    // Plano de saúde
    [Column("tem_plano_saude")] public bool TemPlanoSaude { get; set; }
    [Column("plano_saude_nome")] public string? PlanoSaudeNome { get; set; }
    [Column("plano_saude_carteira")] public string? PlanoSaudeCarteira { get; set; }
    [Column("foto_url")] public string? FotoUrl { get; set; }
    [Column("obs")] public string? Obs { get; set; }
    [Column("ativo")] public bool Ativo { get; set; } = true;
    [Column("criado_em")] public DateTime CriadoEm { get; set; }
    [Column("codigo_sequencial")] public int? CodigoSequencial { get; set; }

    public Tutor? Tutor { get; set; }
}

[Table("servicos")]
public class Servico
{
    [Column("id")]          public Guid Id { get; set; }
    [Column("tenant_id")]   public Guid TenantId { get; set; }
    [Column("nome")]        public string Nome { get; set; } = "";
    [Column("categoria")]   public string? Categoria { get; set; }
    [Column("descricao")]   public string? Descricao { get; set; }
    [Column("preco_base")]  public decimal PrecoBase { get; set; }
    [Column("duracao_min")] public int? DuracaoMin { get; set; }
    [Column("icone")]       public string? Icone { get; set; } = "🐾";
    [Column("ativo")]       public bool Ativo { get; set; } = true;
}

[Table("agendamentos")]
public class Agendamento
{
    [Column("id")] public Guid Id { get; set; }
    [Column("tenant_id")] public Guid TenantId { get; set; }
    [Column("pet_id")] public Guid PetId { get; set; }
    [Column("user_id")] public Guid? UserId { get; set; }
    [Column("tipo")] public string Tipo { get; set; } = "consulta";
    [Column("data_hora")] public DateTime DataHora { get; set; }
    [Column("duracao_min")] public int DuracaoMin { get; set; } = 60;
    [Column("status")] public string Status { get; set; } = "pendente";
    [Column("origem")] public string Origem { get; set; } = "interno";
    [Column("obs")]            public string? Obs { get; set; }
    [Column("servico_id")]     public Guid? ServicoId { get; set; }
    [Column("confirmado_em")]  public DateTime? ConfirmadoEm { get; set; }
    [Column("confirmado_por")] public Guid? ConfirmadoPor { get; set; }
    [Column("criado_em")]      public DateTime CriadoEm { get; set; }

    public Pet? Pet { get; set; }
    public Servico? Servico { get; set; }
}

[Table("ordens_servico")]
public class OrdemServico
{
    [Column("id")] public Guid Id { get; set; }
    [Column("tenant_id")] public Guid TenantId { get; set; }
    [Column("agendamento_id")] public Guid? AgendamentoId { get; set; }
    [Column("pet_id")] public Guid PetId { get; set; }
    [Column("user_id")] public Guid? UserId { get; set; }
    [Column("status")] public string Status { get; set; } = "aguardando";
    [Column("obs")] public string? Obs { get; set; }
    [Column("fotos_urls")] public string[]? FotosUrls { get; set; }
    [Column("valor_total")] public decimal? ValorTotal { get; set; }
    [Column("inicio")] public DateTime? Inicio { get; set; }
    [Column("fim")] public DateTime? Fim { get; set; }
    [Column("entregue_em")] public DateTime? EntregueEm { get; set; }
    [Column("funcionario_id")] public Guid? FuncionarioId { get; set; }
    [Column("criado_em")] public DateTime CriadoEm { get; set; }

    public Pet? Pet { get; set; }
    public Funcionario? Funcionario { get; set; }
}

[Table("os_servicos")]
public class OsServico
{
    [Column("id")] public Guid Id { get; set; }
    [Column("os_id")] public Guid OsId { get; set; }
    [Column("servico_id")] public Guid ServicoId { get; set; }
    [Column("preco_cobrado")] public decimal PrecoCobrado { get; set; }
    [Column("obs")] public string? Obs { get; set; }
}

[Table("prontuario_itens")]
public class ProntuarioItem
{
    [Column("id")] public Guid Id { get; set; }
    [Column("pet_id")] public Guid PetId { get; set; }
    [Column("user_id")] public Guid? UserId { get; set; }
    [Column("data")] public DateTime Data { get; set; }
    [Column("tipo")] public string Tipo { get; set; } = "consulta";
    [Column("titulo")] public string? Titulo { get; set; }
    [Column("descricao")] public string Descricao { get; set; } = "";
    [Column("motivo")] public string? Motivo { get; set; }
    [Column("receituario")] public string? Receituario { get; set; }
    [Column("anexo_url")] public string? AnexoUrl { get; set; }
    [Column("criado_em")] public DateTime CriadoEm { get; set; }
}

[Table("vacinacoes")]
public class Vacinacao
{
    [Column("id")] public Guid Id { get; set; }
    [Column("pet_id")] public Guid PetId { get; set; }
    [Column("user_id")] public Guid? UserId { get; set; }
    [Column("vacina")] public string Vacina { get; set; } = "";
    [Column("fabricante")] public string? Fabricante { get; set; }
    [Column("lote")] public string? Lote { get; set; }
    [Column("data_aplicacao")] public DateOnly DataAplicacao { get; set; }
    [Column("proxima_dose")] public DateOnly? ProximaDose { get; set; }
    [Column("obs")] public string? Obs { get; set; }
    [Column("criado_em")] public DateTime CriadoEm { get; set; }
}

[Table("categorias_financeiras")]
public class CategoriaFinanceira
{
    [Column("id")] public Guid Id { get; set; }
    [Column("tenant_id")] public Guid TenantId { get; set; }
    [Column("nome")] public string Nome { get; set; } = "";
    [Column("tipo")] public string Tipo { get; set; } = "receita";
    [Column("ativo")] public bool Ativo { get; set; } = true;
}

[Table("lancamentos")]
public class Lancamento
{
    [Column("id")] public Guid Id { get; set; }
    [Column("tenant_id")] public Guid TenantId { get; set; }
    [Column("os_id")] public Guid? OsId { get; set; }
    [Column("categoria_id")] public Guid? CategoriaId { get; set; }
    [Column("data")] public DateOnly Data { get; set; }
    [Column("tipo")] public string Tipo { get; set; } = "receita";
    [Column("valor")] public decimal Valor { get; set; }
    [Column("descricao")] public string? Descricao { get; set; }
    [Column("criado_em")] public DateTime CriadoEm { get; set; }
}

[Table("notificacoes_fila")]
public class NotificacaoFila
{
    [Column("id")] public Guid Id { get; set; }
    [Column("tenant_id")] public Guid TenantId { get; set; }
    [Column("agendamento_id")] public Guid? AgendamentoId { get; set; }
    [Column("os_id")] public Guid? OsId { get; set; }
    [Column("pet_id")] public Guid? PetId { get; set; }
    [Column("tutor_id")] public Guid? TutorId { get; set; }
    [Column("canal")] public string Canal { get; set; } = "whatsapp";
    [Column("tipo")] public string? Tipo { get; set; }
    [Column("status")] public string Status { get; set; } = "pendente";
    [Column("destinatario")] public string? Destinatario { get; set; }
    [Column("payload", TypeName = "jsonb")] public string? Payload { get; set; }
    [Column("agendado_para")] public DateTime AgendadoPara { get; set; }
    [Column("tentativas")] public int Tentativas { get; set; }
    [Column("enviado_em")] public DateTime? EnviadoEm { get; set; }
    [Column("erro")] public string? Erro { get; set; }
    [Column("criado_em")] public DateTime CriadoEm { get; set; }
}

[Table("links_agendamento")]
public class LinkAgendamento
{
    [Column("id")] public Guid Id { get; set; }
    [Column("tenant_id")] public Guid TenantId { get; set; }
    [Column("tutor_id")] public Guid TutorId { get; set; }
    [Column("pet_id")] public Guid? PetId { get; set; }
    [Column("token")] public string Token { get; set; } = "";
    [Column("expira_em")] public DateTime ExpiraEm { get; set; }
    [Column("usado_em")] public DateTime? UsadoEm { get; set; }
    [Column("criado_em")] public DateTime CriadoEm { get; set; }
}

// ===================== MÓDULO ESTOQUE + VENDAS =====================

[Table("produtos")]
public class Produto
{
    [Column("id")] public Guid Id { get; set; }
    [Column("tenant_id")] public Guid TenantId { get; set; }
    [Column("nome")] public string Nome { get; set; } = "";
    [Column("codigo")] public string? Codigo { get; set; }
    [Column("codigo_barras")] public string? CodigoBarras { get; set; }
    [Column("categoria")] public string? Categoria { get; set; }
    [Column("unidade")] public string Unidade { get; set; } = "un";
    [Column("preco_custo")] public decimal PrecoCusto { get; set; }
    [Column("preco_venda")] public decimal PrecoVenda { get; set; }
    [Column("estoque_atual")] public decimal EstoqueAtual { get; set; }
    [Column("estoque_minimo")] public decimal EstoqueMinimo { get; set; }
    [Column("estoque_ideal")] public decimal? EstoqueIdeal { get; set; }
    [Column("controla_validade")] public bool ControlaValidade { get; set; }
    [Column("ativo")] public bool Ativo { get; set; } = true;
    [Column("criado_em")] public DateTime CriadoEm { get; set; }
}

[Table("movimentacoes_estoque")]
public class MovimentacaoEstoque
{
    [Column("id")] public Guid Id { get; set; }
    [Column("tenant_id")] public Guid TenantId { get; set; }
    [Column("produto_id")] public Guid ProdutoId { get; set; }
    [Column("tipo")] public string Tipo { get; set; } = "ajuste";
    [Column("quantidade")] public decimal Quantidade { get; set; }
    [Column("estoque_apos")] public decimal EstoqueApos { get; set; }
    [Column("custo_unitario")] public decimal? CustoUnitario { get; set; }
    [Column("motivo")] public string? Motivo { get; set; }
    [Column("venda_id")] public Guid? VendaId { get; set; }
    [Column("user_id")] public Guid? UserId { get; set; }
    [Column("data")] public DateTime Data { get; set; }
}

[Table("vendas")]
public class Venda
{
    [Column("id")] public Guid Id { get; set; }
    [Column("tenant_id")] public Guid TenantId { get; set; }
    [Column("tutor_id")] public Guid? TutorId { get; set; }
    [Column("user_id")] public Guid? UserId { get; set; }
    [Column("status")] public string Status { get; set; } = "aberta";
    [Column("valor_produtos")] public decimal ValorProdutos { get; set; }
    [Column("valor_desconto")] public decimal ValorDesconto { get; set; }
    [Column("valor_total")] public decimal ValorTotal { get; set; }
    [Column("forma_pagamento")] public string? FormaPagamento { get; set; }
    [Column("finalizada_em")] public DateTime? FinalizadaEm { get; set; }
    [Column("criado_em")] public DateTime CriadoEm { get; set; }
}

[Table("venda_itens")]
public class VendaItem
{
    [Column("id")] public Guid Id { get; set; }
    [Column("venda_id")] public Guid VendaId { get; set; }
    [Column("produto_id")] public Guid ProdutoId { get; set; }
    [Column("quantidade")] public decimal Quantidade { get; set; }
    [Column("preco_unitario")] public decimal PrecoUnitario { get; set; }
    [Column("subtotal")] public decimal Subtotal { get; set; }
}

[Table("config_mensagens")]
public class ConfigMensagem
{
    [Column("id")] public Guid Id { get; set; }
    [Column("tenant_id")] public Guid TenantId { get; set; }
    [Column("gatilho")] public string Gatilho { get; set; } = "";
    [Column("canal")] public string Canal { get; set; } = "whatsapp";
    [Column("ativo")] public bool Ativo { get; set; } = true;
    [Column("horas_antes")] public int? HorasAntes { get; set; }
    [Column("template")] public string Template { get; set; } = "";
    [Column("hora_envio")] public TimeOnly? HoraEnvio { get; set; }
    [Column("criado_em")] public DateTime CriadoEm { get; set; }
}

// ===================== CAMADA DE PLATAFORMA (SaaS) =====================

[Table("platform_admins")]
public class PlatformAdmin
{
    [Column("id")] public Guid Id { get; set; }
    [Column("nome")] public string Nome { get; set; } = "";
    [Column("email")] public string Email { get; set; } = "";
    [Column("senha_hash")] public string SenhaHash { get; set; } = "";
    [Column("ativo")] public bool Ativo { get; set; } = true;
    [Column("criado_em")] public DateTime CriadoEm { get; set; }
}

[Table("campanhas")]
public class Campanha
{
    [Column("id")] public Guid Id { get; set; }
    [Column("tenant_id")] public Guid TenantId { get; set; }
    [Column("titulo")] public string Titulo { get; set; } = "";
    [Column("mensagem")] public string Mensagem { get; set; } = "";
    [Column("imagem_url")] public string? ImagemUrl { get; set; }
    [Column("publico")] public string Publico { get; set; } = "todos";
    [Column("canal")] public string Canal { get; set; } = "whatsapp";
    [Column("status")] public string Status { get; set; } = "aguardando_aprovacao";
    [Column("total_destinatarios")] public int TotalDestinatarios { get; set; }
    [Column("total_enviados")] public int TotalEnviados { get; set; }
    [Column("criado_por")] public Guid? CriadoPor { get; set; }
    [Column("aprovado_por")] public Guid? AprovadoPor { get; set; }
    [Column("aprovado_em")] public DateTime? AprovadoEm { get; set; }
    [Column("motivo_reprovacao")] public string? MotivoReprovacao { get; set; }
    [Column("enviada_em")] public DateTime? EnviadaEm { get; set; }
    [Column("criado_em")] public DateTime CriadoEm { get; set; }
}

[Table("caixas")]
public class Caixa
{
    [Column("id")] public Guid Id { get; set; }
    [Column("tenant_id")] public Guid TenantId { get; set; }
    [Column("user_id")] public Guid? UserId { get; set; }
    [Column("data")] public DateOnly Data { get; set; }
    [Column("saldo_inicial")] public decimal SaldoInicial { get; set; }
    [Column("total_vendas")] public decimal TotalVendas { get; set; }
    [Column("total_servicos")] public decimal TotalServicos { get; set; }
    [Column("saldo_final")] public decimal? SaldoFinal { get; set; }
    [Column("aberto_em")] public DateTime AbertoEm { get; set; }
    [Column("fechado_em")] public DateTime? FechadoEm { get; set; }
    [Column("obs")] public string? Obs { get; set; }
    [Column("requer_aprovacao")] public bool RequerAprovacao { get; set; }
    [Column("aprovado_por")] public Guid? AprovadoPor { get; set; }
    [Column("aprovado_em")] public DateTime? AprovadoEm { get; set; }

    public List<MovimentacaoCaixa> Movimentacoes { get; set; } = new();
}

[Table("movimentacoes_caixa")]
public class MovimentacaoCaixa
{
    [Column("id")] public Guid Id { get; set; }
    [Column("tenant_id")] public Guid TenantId { get; set; }
    [Column("caixa_id")] public Guid CaixaId { get; set; }
    [Column("user_id")] public Guid? UserId { get; set; }
    [Column("tipo")] public string Tipo { get; set; } = "";
    [Column("valor")] public decimal Valor { get; set; }
    [Column("descricao")] public string? Descricao { get; set; }
    [Column("criado_em")] public DateTime CriadoEm { get; set; }

    public Caixa? Caixa { get; set; }
}

// ===================== MÓDULO FINANCEIRO M2 =====================

[Table("contas")]
public class Conta
{
    [Column("id")]               public Guid Id { get; set; }
    [Column("tenant_id")]        public Guid TenantId { get; set; }
    [Column("os_id")]            public Guid? OsId { get; set; }
    [Column("venda_id")]         public Guid? VendaId { get; set; }
    [Column("pedido_id")]        public Guid? PedidoId { get; set; }
    [Column("tipo")]             public string Tipo { get; set; } = "receita"; // receita | despesa
    [Column("categoria_id")]     public Guid? CategoriaId { get; set; }
    [Column("descricao")]        public string Descricao { get; set; } = "";
    [Column("valor")]            public decimal Valor { get; set; }
    [Column("data_competencia")] public DateOnly DataCompetencia { get; set; }
    [Column("data_vencimento")]  public DateOnly DataVencimento { get; set; }
    [Column("forma_pagamento")]  public string? FormaPagamento { get; set; }
    [Column("status")]           public string Status { get; set; } = "aberta"; // aberta|paga|recebida|cancelada
    [Column("valor_pago")]       public decimal? ValorPago { get; set; }
    [Column("data_baixa")]       public DateOnly? DataBaixa { get; set; }
    [Column("conta_bancaria")]   public string? ContaBancaria { get; set; }
    [Column("obs_baixa")]        public string? ObsBaixa { get; set; }
    [Column("criado_por")]       public Guid? CriadoPor { get; set; }
    [Column("criado_em")]        public DateTime CriadoEm { get; set; }
    [Column("atualizado_em")]    public DateTime AtualizadoEm { get; set; }

    public CategoriaFinanceira? Categoria { get; set; }
}

[Table("conta_parcelas")]
public class ContaParcela
{
    [Column("id")]              public Guid Id { get; set; }
    [Column("conta_id")]        public Guid ContaId { get; set; }
    [Column("numero")]          public short Numero { get; set; }
    [Column("valor")]           public decimal Valor { get; set; }
    [Column("data_vencimento")] public DateOnly DataVencimento { get; set; }
    [Column("status")]          public string Status { get; set; } = "aberta";
    [Column("data_baixa")]      public DateOnly? DataBaixa { get; set; }
    [Column("valor_pago")]      public decimal? ValorPago { get; set; }
}

[Table("pelagens")]
public class Pelagem
{
    [Column("id")]        public Guid Id { get; set; }
    [Column("tenant_id")] public Guid TenantId { get; set; }
    [Column("nome")]      public string Nome { get; set; } = "";
    [Column("ativo")]     public bool Ativo { get; set; } = true;
    [Column("criado_em")] public DateTime CriadoEm { get; set; }
}

[Table("vias_administracao")]
public class ViaAdministracao
{
    [Column("id")]        public Guid Id { get; set; }
    [Column("tenant_id")] public Guid TenantId { get; set; }
    [Column("nome")]      public string Nome { get; set; } = "";
    [Column("ativo")]     public bool Ativo { get; set; } = true;
    [Column("criado_em")] public DateTime CriadoEm { get; set; }
}

// ===================== RECEBIMENTO ANEXOS =====================

[Table("recebimento_anexos")]
public class RecebimentoAnexo
{
    [Column("id")]                public Guid Id { get; set; }
    [Column("tenant_id")]         public Guid TenantId { get; set; }
    [Column("recebimento_id")]    public Guid RecebimentoId { get; set; }
    [Column("nome")]              public string Nome { get; set; } = "";
    [Column("tipo_arquivo")]      public string TipoArquivo { get; set; } = "";
    [Column("tamanho_bytes")]     public int TamanhoBytes { get; set; }
    [Column("dados_base64")]      public string DadosBase64 { get; set; } = "";
    [Column("descricao")]         public string? Descricao { get; set; }
    [Column("criado_por")]        public Guid? CriadoPor { get; set; }
    [Column("criado_em")]         public DateTime CriadoEm { get; set; }
}

// ===================== BOT WHATSAPP =====================

[Table("bot_config")]
public class BotConfig
{
    [Column("id")]            public Guid Id { get; set; }
    [Column("tenant_id")]     public Guid TenantId { get; set; }
    [Column("ativo")]         public bool Ativo { get; set; }

    // Horário
    [Column("hora_inicio")]   public TimeOnly HoraInicio { get; set; }
    [Column("hora_fim")]      public TimeOnly HoraFim { get; set; }
    [Column("dias_semana")]   public string DiasSemana { get; set; } = "1,2,3,4,5,6";

    // Janela de agendamento
    [Column("dias_antecedencia_min")] public int DiasAntecedenciaMin { get; set; }
    [Column("dias_antecedencia_max")] public int DiasAntecedenciaMax { get; set; } = 30;

    // Mensagens
    [Column("msg_boas_vindas")]          public string MsgBoasVindas { get; set; } = "";
    [Column("msg_qual_pet")]             public string MsgQualPet { get; set; } = "";
    [Column("msg_qual_servico")]         public string MsgQualServico { get; set; } = "";
    [Column("msg_qual_data")]            public string MsgQualData { get; set; } = "";
    [Column("msg_horarios_disponiveis")] public string MsgHorariosDisponiveis { get; set; } = "";
    [Column("msg_confirmacao")]          public string MsgConfirmacao { get; set; } = "";
    [Column("msg_sem_horarios")]         public string MsgSemHorarios { get; set; } = "";
    [Column("msg_fora_horario")]         public string MsgForaHorario { get; set; } = "";
    [Column("msg_erro")]                 public string MsgErro { get; set; } = "";
    [Column("msg_cancelar")]             public string MsgCancelar { get; set; } = "";

    [Column("timeout_conversa_min")]  public int TimeoutConversaMin { get; set; } = 30;

    [Column("criado_em")]     public DateTime CriadoEm { get; set; }
    [Column("atualizado_em")] public DateTime AtualizadoEm { get; set; }
}

[Table("bot_conversas")]
public class BotConversa
{
    [Column("id")]               public Guid Id { get; set; }
    [Column("tenant_id")]        public Guid TenantId { get; set; }
    [Column("telefone")]         public string Telefone { get; set; } = "";
    [Column("estado")]           public string Estado { get; set; } = "inicio";
    [Column("tutor_id")]         public Guid? TutorId { get; set; }
    [Column("pet_id")]           public Guid? PetId { get; set; }
    [Column("pet_nome_digitado")] public string? PetNomeDigitado { get; set; }
    [Column("servico_id")]       public Guid? ServicoId { get; set; }
    [Column("servico_nome")]     public string? ServicoNome { get; set; }
    [Column("duracao_min")]      public int? DuracaoMin { get; set; }
    [Column("data_escolhida")]   public DateOnly? DataEscolhida { get; set; }
    [Column("ultima_msg_em")]    public DateTime UltimaMsgEm { get; set; }
    [Column("criado_em")]        public DateTime CriadoEm { get; set; }
}

[Table("bot_logs")]
public class BotLog
{
    [Column("id")]          public Guid Id { get; set; }
    [Column("tenant_id")]   public Guid TenantId { get; set; }
    [Column("telefone")]    public string Telefone { get; set; } = "";
    [Column("direcao")]     public string Direcao { get; set; } = ""; // entrada | saida
    [Column("mensagem")]    public string Mensagem { get; set; } = "";
    [Column("estado_antes")] public string? EstadoAntes { get; set; }
    [Column("estado_apos")] public string? EstadoApos { get; set; }
    [Column("erro")]        public string? Erro { get; set; }
    [Column("criado_em")]   public DateTime CriadoEm { get; set; }
}

// ===================== MOVIMENTACAO BANCARIA =====================

[Table("contas_bancarias")]
public class ContaBancaria
{
    [Column("id")]             public Guid Id { get; set; }
    [Column("tenant_id")]      public Guid TenantId { get; set; }
    [Column("nome")]           public string Nome { get; set; } = "";
    [Column("banco")]          public string? Banco { get; set; }
    [Column("agencia")]        public string? Agencia { get; set; }
    [Column("conta")]          public string? Conta { get; set; }
    [Column("tipo")]           public string Tipo { get; set; } = "corrente";
    [Column("saldo_inicial")]  public decimal SaldoInicial { get; set; }
    [Column("ativo")]          public bool Ativo { get; set; } = true;
    [Column("criado_em")]      public DateTime CriadoEm { get; set; }

    public ICollection<MovimentacaoBancaria> Movimentacoes { get; set; } = new List<MovimentacaoBancaria>();
}

[Table("movimentacoes_bancarias")]
public class MovimentacaoBancaria
{
    [Column("id")]                 public Guid Id { get; set; }
    [Column("tenant_id")]          public Guid TenantId { get; set; }
    [Column("conta_bancaria_id")]  public Guid ContaBancariaId { get; set; }
    [Column("tipo")]               public string Tipo { get; set; } = "entrada"; // entrada | saida | transferencia
    [Column("valor")]              public decimal Valor { get; set; }
    [Column("descricao")]          public string Descricao { get; set; } = "";
    [Column("data_movimentacao")]  public DateOnly DataMovimentacao { get; set; }
    [Column("categoria_id")]       public Guid? CategoriaId { get; set; }
    [Column("conta_id")]           public Guid? ContaId { get; set; }
    [Column("conta_destino_id")]   public Guid? ContaDestinoId { get; set; }
    [Column("conciliado")]         public bool Conciliado { get; set; }
    [Column("origem")]             public string? Origem { get; set; } // manual | baixa_financeiro | caixa
    [Column("criado_por")]         public Guid? CriadoPor { get; set; }
    [Column("criado_em")]          public DateTime CriadoEm { get; set; }

    public ContaBancaria? ContaBancaria { get; set; }
    public CategoriaFinanceira? Categoria { get; set; }
}

// ===================== CONCILIAÇÃO BANCÁRIA DIÁRIA =====================

[Table("conciliacoes_diarias")]
public class ConciliacaoDiaria
{
    [Column("id")]                 public Guid Id { get; set; }
    [Column("tenant_id")]          public Guid TenantId { get; set; }
    [Column("conta_bancaria_id")]  public Guid ContaBancariaId { get; set; }
    [Column("data_conciliacao")]   public DateOnly DataConciliacao { get; set; }
    [Column("saldo_anterior")]     public decimal SaldoAnterior { get; set; }
    [Column("total_entradas")]     public decimal TotalEntradas { get; set; }
    [Column("total_saidas")]       public decimal TotalSaidas { get; set; }
    [Column("saldo_final")]        public decimal SaldoFinal { get; set; }
    [Column("saldo_extrato")]      public decimal? SaldoExtrato { get; set; }
    [Column("diferenca")]          public decimal? Diferenca { get; set; }
    [Column("observacao")]         public string? Observacao { get; set; }
    [Column("fechado_por")]        public Guid? FechadoPor { get; set; }
    [Column("fechado_em")]         public DateTime FechadoEm { get; set; }
    [Column("criado_em")]          public DateTime CriadoEm { get; set; }

    public ContaBancaria? ContaBancaria { get; set; }
}


// ===================== CARGOS =====================

[Table("cargos")]
public class Cargo
{
    [Column("id")]                public Guid Id { get; set; }
    [Column("tenant_id")]         public Guid TenantId { get; set; }
    [Column("nome")]              public string Nome { get; set; } = "";
    [Column("pode_receituario")]  public bool PodeReceituario { get; set; } = false;
    [Column("ativo")]             public bool Ativo { get; set; } = true;
    [Column("criado_em")]         public DateTime CriadoEm { get; set; }
}

// ===================== M4 COMPRAS =====================

[Table("fornecedores")]
public class Fornecedor
{
    [Column("id")]          public Guid Id { get; set; }
    [Column("tenant_id")]   public Guid TenantId { get; set; }
    [Column("nome")]        public string Nome { get; set; } = "";
    [Column("codigo")]       public string? Codigo { get; set; }
    [Column("cnpj")]        public string? Cnpj { get; set; }
    [Column("telefone")]    public string? Telefone { get; set; }
    [Column("email")]       public string? Email { get; set; }
    [Column("contato")]     public string? Contato { get; set; }
    [Column("logradouro")]  public string? Logradouro { get; set; }
    [Column("num_end")]     public string? NumEnd { get; set; }
    [Column("bairro")]      public string? Bairro { get; set; }
    [Column("cidade")]      public string? Cidade { get; set; }
    [Column("estado")]      public string? Estado { get; set; }
    [Column("cep")]         public string? Cep { get; set; }
    [Column("obs")]         public string? Obs { get; set; }
    [Column("ativo")]       public bool Ativo { get; set; } = true;
    [Column("criado_em")]   public DateTime CriadoEm { get; set; }
}

[Table("pedidos_compra")]
public class PedidoCompra
{
    [Column("id")]               public Guid Id { get; set; }
    [Column("tenant_id")]        public Guid TenantId { get; set; }
    [Column("fornecedor_id")]    public Guid? FornecedorId { get; set; }
    [Column("numero")]           public int Numero { get; set; }
    [Column("status")]           public string Status { get; set; } = "rascunho";
    [Column("data_pedido")]      public DateOnly DataPedido { get; set; }
    [Column("data_recebimento")] public DateOnly? DataRecebimento { get; set; }
    [Column("valor_total")]      public decimal ValorTotal { get; set; }
    [Column("condicao_pagamento_id")] public Guid? CondicaoPagamentoId { get; set; }
    [Column("parcelas")]             public int Parcelas { get; set; } = 1;
    [Column("forma_pagamento")]      public string? FormaPagamento { get; set; }
    [Column("obs")]              public string? Obs { get; set; }
    [Column("criado_por")]       public Guid? CriadoPor { get; set; }
    [Column("criado_em")]        public DateTime CriadoEm { get; set; }
    [Column("atualizado_em")]    public DateTime AtualizadoEm { get; set; }

    public Fornecedor? Fornecedor { get; set; }
    public CondicaoPagamento? CondicaoPagamento { get; set; }
    public ICollection<PedidoItem> Itens { get; set; } = new List<PedidoItem>();
}

[Table("pedido_itens")]
public class PedidoItem
{
    [Column("id")]              public Guid Id { get; set; }
    [Column("pedido_id")]       public Guid PedidoId { get; set; }
    [Column("produto_id")]      public Guid? ProdutoId { get; set; }
    [Column("nome_produto")]    public string NomeProduto { get; set; } = "";
    [Column("quantidade")]      public decimal Quantidade { get; set; }
    [Column("unidade")]         public string Unidade { get; set; } = "un";
    [Column("valor_unitario")]  public decimal ValorUnitario { get; set; }
    [Column("valor_total")]     public decimal ValorTotal { get; set; }
    [Column("uso")]             public string Uso { get; set; } = "venda"; // venda | interno
    [Column("criado_em")]       public DateTime CriadoEm { get; set; }
}

// ===================== CONDICOES PAGAMENTO + RECEBIMENTO =====================

[Table("condicoes_pagamento")]
public class CondicaoPagamento
{
    [Column("id")]              public Guid Id { get; set; }
    [Column("tenant_id")]       public Guid TenantId { get; set; }
    [Column("nome")]            public string Nome { get; set; } = "";
    [Column("parcelas")]        public int Parcelas { get; set; } = 1;
    [Column("intervalo_dias")]  public int IntervaloDias { get; set; } = 30;
    [Column("ativo")]           public bool Ativo { get; set; } = true;
    [Column("criado_em")]       public DateTime CriadoEm { get; set; }
}

[Table("recebimentos_mercadoria")]
public class RecebimentoMercadoria
{
    [Column("id")]               public Guid Id { get; set; }
    [Column("tenant_id")]        public Guid TenantId { get; set; }
    [Column("pedido_id")]        public Guid? PedidoId { get; set; }
    [Column("data_recebimento")] public DateOnly DataRecebimento { get; set; }
    [Column("status")]           public string Status { get; set; } = "pendente";
    [Column("obs")]              public string? Obs { get; set; }
    [Column("criado_por")]       public Guid? CriadoPor { get; set; }
    [Column("criado_em")]        public DateTime CriadoEm { get; set; }

    public PedidoCompra? Pedido { get; set; }
    public ICollection<RecebimentoItem> Itens { get; set; } = new List<RecebimentoItem>();
}

[Table("recebimento_itens")]
public class RecebimentoItem
{
    [Column("id")]                   public Guid Id { get; set; }
    [Column("recebimento_id")]       public Guid RecebimentoId { get; set; }
    [Column("pedido_item_id")]       public Guid? PedidoItemId { get; set; }
    [Column("produto_id")]           public Guid? ProdutoId { get; set; }
    [Column("nome_produto")]         public string NomeProduto { get; set; } = "";
    [Column("quantidade_pedida")]    public decimal QuantidadePedida { get; set; }
    [Column("quantidade_recebida")]  public decimal QuantidadeRecebida { get; set; }
    [Column("valor_unitario")]       public decimal ValorUnitario { get; set; }
    [Column("uso")]                  public string Uso { get; set; } = "venda";
    [Column("criado_em")]            public DateTime CriadoEm { get; set; }

    public Produto? Produto { get; set; }
}

[Table("metas_faturamento")]
public class MetaFaturamento
{
    [Column("id")]            public Guid Id { get; set; }
    [Column("tenant_id")]     public Guid TenantId { get; set; }
    [Column("ano")]           public int Ano { get; set; }
    [Column("mes")]           public int Mes { get; set; }
    [Column("valor_meta")]    public decimal ValorMeta { get; set; }
    [Column("criado_em")]     public DateTime CriadoEm { get; set; }
    [Column("atualizado_em")] public DateTime AtualizadoEm { get; set; }
}

// ── M5: Planos de Saúde ──────────────────────────────────────────────────────

[Table("planos_saude")]
public class PlanoSaude
{
    [Column("id")]               public Guid Id { get; set; }
    [Column("tenant_id")]        public Guid TenantId { get; set; }
    [Column("nome")]             public string Nome { get; set; } = "";
    [Column("operadora")]        public string? Operadora { get; set; }
    [Column("desconto_percent")] public decimal DescontoPercent { get; set; } = 0;
    [Column("ativo")]            public bool Ativo { get; set; } = true;
    [Column("obs")]              public string? Obs { get; set; }
    [Column("criado_em")]        public DateTime CriadoEm { get; set; }
    [Column("atualizado_em")]    public DateTime AtualizadoEm { get; set; }

    public List<PetPlano> PetPlanos { get; set; } = new();
    public List<TutorPlano> TutorPlanos { get; set; } = new();
}

[Table("pet_planos")]
public class PetPlano
{
    [Column("id")]               public Guid Id { get; set; }
    [Column("tenant_id")]        public Guid TenantId { get; set; }
    [Column("pet_id")]           public Guid PetId { get; set; }
    [Column("plano_id")]         public Guid PlanoId { get; set; }
    [Column("num_carteirinha")]  public string? NumCarteirinha { get; set; }
    [Column("validade")]         public DateOnly? Validade { get; set; }
    [Column("desconto_percent")] public decimal? DescontoPercent { get; set; }
    [Column("ativo")]            public bool Ativo { get; set; } = true;
    [Column("criado_em")]        public DateTime CriadoEm { get; set; }
    [Column("atualizado_em")]    public DateTime AtualizadoEm { get; set; }

    public Pet? Pet { get; set; }
    public PlanoSaude? Plano { get; set; }
}

[Table("tutor_planos")]
public class TutorPlano
{
    [Column("id")]               public Guid Id { get; set; }
    [Column("tenant_id")]        public Guid TenantId { get; set; }
    [Column("tutor_id")]         public Guid TutorId { get; set; }
    [Column("plano_id")]         public Guid PlanoId { get; set; }
    [Column("num_carteirinha")]  public string? NumCarteirinha { get; set; }
    [Column("validade")]         public DateOnly? Validade { get; set; }
    [Column("desconto_percent")] public decimal? DescontoPercent { get; set; }
    [Column("ativo")]            public bool Ativo { get; set; } = true;
    [Column("criado_em")]        public DateTime CriadoEm { get; set; }
    [Column("atualizado_em")]    public DateTime AtualizadoEm { get; set; }

    public Tutor? Tutor { get; set; }
    public PlanoSaude? Plano { get; set; }
}
