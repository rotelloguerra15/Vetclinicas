# VetClinica — Contexto do Projeto

## LEIA ISSO PRIMEIRO
Este arquivo é o contexto completo do projeto. Sempre leia antes de iniciar qualquer tarefa.
O projeto é um SaaS veterinário multi-tenant chamado "Gestão Inteligente" / VetClinica.

---

## Stack
- **Backend:** .NET 8 + EF Core + PostgreSQL
- **Frontend:** React + Vite + Tailwind + Recharts
- **Hospedagem:** Railway (backend + banco) + Vercel (frontend via CLI manual)
- **Repositório:** https://github.com/rotelloguerra15/Vetclinicas.git

## URLs de Produção
- Frontend: https://vetclinicas.vercel.app
- Backend: https://vetclinicas-production.up.railway.app

## Deploy Manual (OBRIGATÓRIO — webhook Vercel quebrado)
```powershell
cd C:\Projetos\vetclinica\frontend
npm run build
cd ..
New-Item -ItemType Directory -Force -Path ".vercel\output\static"
Copy-Item -Recurse -Force "frontend\dist\*" ".vercel\output\static\"
vercel deploy --prod --yes --prebuilt
```

## Banco de Dados (Railway)
- Connection string: `postgresql://postgres:eOpnNTdorWCkkphKLRlNogUWOtiwGibS@gondola.proxy.rlwy.net:16524/railway`

## Rodar Migration
```powershell
$DB = "postgresql://postgres:eOpnNTdorWCkkphKLRlNogUWOtiwGibS@gondola.proxy.rlwy.net:16524/railway"
psql $DB -f "C:\Projetos\vetclinica\database\ARQUIVO.sql"
```

## Tenant Principal
- ID: `4b19602f-0425-4101-914c-5b9aeaf5d4e7`
- Nome: Pet Shop e Consultório Dra. Barbara Fonseca
- Login: barbaratatschveterinaria@gmail.com / admin123
- Super-admin: rotello.guerra@ketra.com.br / Ketra@2026

---

## PADRÕES CRÍTICOS (não ignorar)

1. **Multi-tenant:** coluna `tenant_id` em TODAS as tabelas
2. **Auth:** JWT via `TenantContext` — usar `_t.TenantId`, `_t.UserId`, `_t.Papel`
3. **ORM:** EF Core — NUNCA usar Dapper
4. **NUNCA emojis em strings C# interpoladas** — causa CS1010/CS1056
5. **FK violations:** sempre `SaveChanges()` no pai ANTES de adicionar filhos
6. **Tabela financeira:** chama `contas` (não `lancamentos`)
7. **Venda:** tem `TutorId` direto (não via Pet)
8. **PedidoItem FK:** configurar `HasForeignKey` no `OnModelCreating`
9. **DbSets:** `OrdensServico`, `Vendas`, `Tutores`, `Pets`, `Contas`, `PedidosCompra`, `MetasFaturamento`, `Pelagens`, `Cargos`, `ConciliacoesDiarias`, `RecebimentoAnexos`, `FechamentosContabeis`, `FechamentoDocumentos`
10. **Arquivos:** sempre entregar com nome final correto (sem sufixos `_cur`, `_fix`, `_v2`)
11. **Railway cache:** forçar rebuild com `Add-Content "VetClinica.API/Program.cs" " "` + commit
12. **using:** sempre incluir `using VetClinica.API.Models;` em controllers que usam entities
13. **PowerShell:** NÃO usar `&&` — separar em linhas individuais
14. **SQL com emojis:** NÃO colocar emojis em .sql — encoding WIN1252 quebra no psql Windows
15. **iText7 assinatura:** NÃO tentar integrar iText7+BouncyCastle sem testar localmente — causa cascata de erros
16. **Sempre ler o arquivo atual antes de modificar** — nunca editar às cegas

---

## Migrations Aplicadas (37 total)

| # | Arquivo | Descrição |
|---|---------|-----------|
| 01 | 01-schema.sql | Schema base |
| 02 | 02-estoque-mensagens.sql | Estoque, mensagens, vendas |
| 03 | 03-aniversario-tutor.sql | Campos aniversário tutor |
| 04 | 04-branding-fonseca.sql | Branding inicial |
| 05 | 05-plataforma-saas.sql | Plataforma SaaS multi-tenant |
| 06 | 06-promocoes.sql | Promoções/campanhas |
| 07 | 07-fix-enums-to-varchar.sql | Fix enums |
| 08 | 08-prontuario-campos.sql | Prontuário |
| 09 | 09-caixa.sql | Caixa PDV |
| 10 | 010_funcionarios.sql | Funcionários RH |
| 11 | 011_parametros_sistema.sql | Parâmetros sistema |
| 12 | 012_comissoes.sql | Comissões |
| 13 | 013_fechamento_mensal.sql | Fechamento mensal |
| 14 | 014_os_funcionario.sql | Vínculo OS/funcionário |
| 15 | 015_movimentacoes_caixa.sql | Movimentações caixa |
| 16 | 016_m1_ajustes.sql | Endereço tutor + microchip + plano saúde |
| 17 | 017_crmv_funcionario.sql | CRMV funcionários |
| 18 | 018_receituario_campos.sql | Registro MAPA + código pet |
| 19 | 019_vias_administracao.sql | Vias de administração |
| 20 | 020_bot_whatsapp.sql | Bot WhatsApp |
| 21 | 021_movimentacao_bancaria.sql | Contas bancárias e movimentações |
| 22 | 022_fornecedores_compras.sql | Fornecedores e pedidos de compra |
| 23 | 023_condicoes_recebimento.sql | Condições pagamento + recebimento mercadoria |
| 24 | 024_codigo_cadastros.sql | Campo `codigo` em produtos/fornecedores/funcionários |
| 25 | 025_seed_mensagens.sql | Templates padrão mensagens automáticas |
| 26 | 026_contas_financeiro.sql | Tabela `contas` (financeiro) |
| 27 | 027_metas_faturamento.sql | Tabela `metas_faturamento` |
| 28 | 028_conciliacao_bancaria.sql | `conciliacoes_diarias` + campo `origem` |
| 29 | 029_ia_diagnostico.sql | `anthropic_api_key` e `ia_ativo` em parametros_sistema |
| 30 | 030_assina_receituario.sql | Campo `assina_receituario` em funcionarios |
| 31 | 031_cargos_funcionario_usuario.sql | Tabela `cargos` + `cargo_id` + `usuario_id` em funcionarios |
| 32 | 032_agendamento_servico.sql | Campo `servico_id` em agendamentos |
| 33 | 033_servico_icone.sql | Campo `icone` em servicos |
| 34 | 034_pelagens.sql | Tabela `pelagens` |
| 35 | 035_certificado_digital.sql | Campos certificado A1 em parametros_sistema |
| 36 | 036_recebimento_anexos.sql | Tabela `recebimento_anexos` |
| 37 | 037_modulo_contabil.sql | SMTP em parametros_sistema + `fechamentos_contabeis` + `fechamento_documentos` |

---

## Módulos Implementados

### Clínica
- Auth, multi-tenant, JWT
- **Tutores** — endereço, código, aniversário, painel de pets vinculados, redirect para cadastro de pet após criar tutor
- **Pets** — microchip, plano saúde, idade estimada (anos+meses→data), código, pelagem via cadastro, campo castrado com "Não sei"
- Prontuário, Vacinas
- **Agenda** — grade de horários 08h-18h, slots vagos, header com nome/CRMV do vet logado, KPIs do dia, abas Hoje/Semana, seleção de serviço com multi-seleção e duração somada automática
- **Iniciar atendimento** — cria OS vinculada, muda status agendamento, navega direto para PetDetalhe
- OS Kanban, Atendimentos
- **Receituário PDF** — QR Code + código único `REC-2026-XXXXXXXX` no rodapé
- **Validação eletrônica** — `/validar/:codigo` página pública sem login
- **IA Diagnóstico** — assistente no receituário, microfone, Claude API por tenant

### Vendas / PDV
- PDV/Caixa — retirada, depósito, fechamento gera `Conta` por forma de pagamento + `MovimentacaoBancaria`
- Venda finalizada gera `MovimentacaoBancaria` automática
- **Serviços** — ícone emoji, picker visual, categorias, duração
- Estoque, código de barras
- **Dashboard de Vendas** — produtos mais vendidos, serviços mais executados, dia a dia do mês, pizzas de composição

### Compras (M4)
- Fornecedores, Condições de pagamento, Pedidos de compra
- **Recebimento de Mercadoria** — conferência de itens, atualiza estoque, **anexos de NF** (PDF/JPG/XML, drag-drop, download)

### Financeiro (M2)
- Contas a pagar/receber — baixa gera `MovimentacaoBancaria` automática
- Movimentação bancária + Conciliação Diária
- Categorias financeiras

### RH (M3)
- **Cargos** — cadastro com toggle `pode_receituario`
- **Funcionários** — vínculo com usuário (`usuario_id`), cargo (`cargo_id`)
- Comissões, fechamento mensal, relatórios RH

### Certificado Digital
- Upload `.pfx` A1 em Parâmetros → Assinatura Digital
- Criptografia AES-256 (chave via `CERT_ENCRYPTION_KEY` env var)
- Interface plugável `ICertificadoService` — pronta para A3 nuvem
- **STATUS:** assinatura visual PDF pendente — validar com .pfx real da Dra. Barbara

### Módulo Contábil (NOVO)
- **Fechamento mensal** — seleciona mês/ano, checklist de 9 documentos
- **Gerar do sistema:** DRE (receitas-despesas CSV), Estoque final (CSV), Repasses ao sócio (CSV)
- **Upload manual:** extratos bancários, faturas cartão, NF-e tomadas, extratos aplicações, contratos
- **Envio por email** — empacota tudo em ZIP organizado por tipo, envia via SMTP configurado
- **Histórico** de fechamentos com data de envio
- SMTP configurável em Parâmetros (Gmail, Outlook, Zoho, qualquer servidor)

### Dashboards (menu unificado)
- **Clínica** — KPIs, agenda, aniversariantes
- **Financeiro BI** — receitas, despesas, metas (gauge), evolução 12 meses, rankings
- **RH** — produtividade, comissões, custo mensal
- **Vendas** — produtos + serviços top 10, dia a dia, pizzas composição

### Gestão à Vista (TV)
- Colunas: Agendados | Aguardando | Em Atendimento | Pronto | Stats
- Loop de notícias RSS corrigido (useRef para cache)

### Cadastros
- Usuários, Vias de administração, **Pelagens**, **Cargos**, Condições de pagamento

### Parâmetros / Configurações
- Logo, branding, comissões, bot WhatsApp
- IA (chave Anthropic por tenant)
- Certificado Digital A1
- **SMTP + Email contabilidade** (novo)

---

## BACKLOG

### Bugs / Validações Pendentes
- [ ] **Validar assinatura digital PDF** — aguarda Dra. Barbara testar com `.pfx` real A1
- [ ] **Gestão à Vista** — confirmar loops corrigidos em produção

### Funcionalidades Pendentes
- [ ] **M5 — Planos de saúde** — cadastro e vínculo com pet
- [ ] **Webhook Vercel → GitHub** — deploy ainda é manual
- [ ] **Assinatura PDF visual (cadeado Adobe)** — testar iText7 localmente com .pfx real antes de commitar
- [ ] **Tempo de produtividade** — homem-hora por atendimento (OS.Inicio/Fim já coletados)
- [ ] **PDV — Fechamento de caixa pendências:**
  - Caixa fechado não pode reabrir sem aprovação
  - Saldo físico (dinheiro) carrega saldo do dia anterior
  - Aba Retirada/Depósito separada
  - Saldo final = saldo anterior + entradas dinheiro − retiradas − contas pagas dinheiro

### Backlog Técnico
- [ ] **Consulta automática NF-e via SEFAZ** — usar certificado A1 do CNPJ (não e-CPF), consultar NF-e emitidas contra o CNPJ e importar para módulo contábil
- [ ] **WhatsApp Meta Cloud API** — substituir Z-API
- [ ] **Storage de imagens** — Cloudflare R2 ou Supabase (atualmente anexos ficam em base64 no banco — OK até ~50MB/mês)
- [ ] **Integração Asaas** — PIX/boleto + suspensão automática de tenant inadimplente
- [ ] **Self-service cadastro de tenant** — trial 7 dias grátis
- [ ] **Certificado A3 nuvem** — Safeweb Cloud API (interface ICertificadoService já plugável)
- [ ] **Reconectar webhook Vercel → GitHub**

---

## Estrutura de Arquivos

```
C:\Projetos\vetclinica\
  VetClinica.API\
    Controllers\
      IaController.cs
      ValidacaoController.cs
      CertificadoController.cs
      CargosController.cs
      PelagensController.cs
      RelatoriosController.cs
      ContabilController.cs          (NOVO)
      AgendamentosController.cs      (iniciar-atendimento)
      RecebimentoController.cs       (anexos NF)
      VendasController.cs            (MovimentacaoBancaria ao finalizar)
      CaixaController.cs             (fechamento → contas por forma pagamento)
      ReceituarioController.cs       (QR Code + codigo validacao + assinatura)
    Models\
      Entities.cs
    DTOs\
      Dtos.cs
    Data\
      AppDbContext.cs
    Services\
      ReceituarioPdfService.cs       (QR Code no rodape)
      BotWhatsAppService.cs
      Certificado\
        ICertificadoService.cs
        PfxCertificadoService.cs     (X509Certificate2 nativo .NET 8)
        NullCertificadoService.cs
        CertificadoCryptoHelper.cs   (AES-256-GCM)
  frontend\src\
    pages\
      Agenda.jsx                     (grade horarios, iniciar atendimento, servicos)
      PetDetalhe.jsx                 (receituario modal grande, IA, validacao)
      Tutores.jsx                    (painel pets vinculados, redirect apos criar)
      Pets.jsx                       (idade estimada, pelagem, castrado nao sei)
      Recebimentos.jsx               (anexos NF drag-drop)
      Contabil.jsx                   (NOVO — fechamento contabil)
      Parametros.jsx                 (SMTP adicionado)
      DashboardVendas.jsx            (NOVO)
      Validar.jsx                    (pagina publica validacao receituario)
      Cadastros/Pelagens.jsx         (NOVO)
      RH/Cargos.jsx                  (NOVO)
      RH/Funcionarios.jsx            (cargo_id + usuario_id)
    components\
      Layout.jsx                     (menu: Dashboards, Contabil)
    api\
      client.js
  database\                          (37 migrations)
  projeto.md
```

---

## Variáveis de Ambiente (Railway)

| Variável | Valor |
|----------|-------|
| `ConnectionStrings__Default` | string de conexão PostgreSQL |
| `Jwt__Key` | chave JWT (min 32 chars) |
| `App__FrontendUrl` | https://vetclinicas.vercel.app |
| `CERT_ENCRYPTION_KEY` | chave para criptografar .pfx (recomendado adicionar) |
| `QuestPDF__LicenseType` | Community |
