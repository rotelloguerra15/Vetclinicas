# 🐾 VetClinica

Sistema de gestão para clínica veterinária + banho e tosa.
Multi-tenant, com agenda, prontuário, ordens de serviço e notificações automáticas via WhatsApp.

Stack: **.NET 8** (API) + **React/Vite** (front) + **PostgreSQL**.
Reaproveita a base multi-tenant + auth + financeiro do projeto de lavanderia.

---

## 📁 Estrutura do projeto

```
vetclinica/
├── database/
│   └── 01-schema.sql          ← schema completo + seed + triggers
├── VetClinica.API/            ← backend .NET 8
│   ├── Controllers/           ← 11 controllers (Auth, Pets, Agenda, OS...)
│   ├── Services/              ← Auth, ZApi (WhatsApp), Agenda, Dispatcher
│   ├── Models/Entities.cs     ← entidades EF Core
│   ├── DTOs/Dtos.cs
│   ├── Data/AppDbContext.cs
│   ├── Middleware/            ← TenantMiddleware (multi-tenant)
│   ├── Program.cs
│   └── appsettings.json
├── frontend/                  ← React + Vite + Tailwind
│   └── src/
│       ├── api/client.js      ← axios + JWT
│       ├── components/Layout.jsx
│       ├── pages/             ← Login, Dashboard, Tutores, Pets, Agenda...
│       └── main.jsx           ← rotas
├── docker-compose.yml         ← sobe o Postgres já com o schema
└── README.md
```

---

## 🚀 Passo a passo — do zero ao rodando

### Pré-requisitos
- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Node.js 18+](https://nodejs.org)
- [Docker](https://www.docker.com) (ou um PostgreSQL instalado)

---

### 1️⃣ Subir o banco de dados

**Opção A — Docker (recomendado):**
```bash
cd vetclinica
docker compose up -d
```
Isso sobe o PostgreSQL na porta 5432 e **já roda o `01-schema.sql`** automaticamente (cria tabelas, triggers e o seed inicial).

**Opção B — Postgres manual:**
```bash
createdb vetclinica
psql -d vetclinica -f database/01-schema.sql
psql -d vetclinica -f database/02-estoque-mensagens.sql
psql -d vetclinica -f database/03-aniversario-tutor.sql
psql -d vetclinica -f database/04-branding-fonseca.sql
```

---

### 2️⃣ Rodar o backend (.NET)

```bash
cd VetClinica.API
dotnet restore
dotnet run
```

A API sobe em `http://localhost:5000` (ou `5001` https).
Swagger disponível em `http://localhost:5000/swagger`.

> Se a porta for diferente, ajuste o `VITE_API_URL` no `.env` do frontend.

---

### 3️⃣ Rodar o frontend (React)

```bash
cd frontend
npm install
npm run dev
```

Abre em `http://localhost:5173`.

---

### 4️⃣ Login inicial

Já existe um usuário no seed:

| Campo | Valor                  |
|-------|------------------------|
| Email | `admin@clinicafonseca.com.br`|
| Senha | `admin123`             |

> ⚠️ Troque essa senha em produção.

---

## 🔑 Configuração importante

### JWT
No `appsettings.json`, troque a chave por uma aleatória de **no mínimo 32 caracteres**:
```json
"Jwt": { "Key": "sua_chave_super_secreta_aqui_min_32_chars" }
```

### WhatsApp (Z-API)
1. Crie uma conta em [z-api.io](https://z-api.io) e uma instância.
2. Conecte seu número escaneando o QR Code.
3. Preencha no `appsettings.json`:
```json
"ZApi": {
  "InstanceId": "sua_instance",
  "Token": "seu_token",
  "ClientToken": "seu_client_token"
}
```

> **Alternativa grátis:** Evolution API (open source) hospedada no seu Railway.
> Basta reescrever os endpoints em `Services/ZApiService.cs`.

---

## 🔁 Como funcionam as automações

### Pet pronto → WhatsApp
Quando uma OS muda para `pronto` (na tela de Atendimentos), o backend enfileira
uma notificação `pet_pronto`. O worker `NotificacaoDispatcher` (roda a cada 5 min)
envia a mensagem + fotos pelo WhatsApp do tutor.

### Lembrete de 30 dias → link de agendamento
Quando uma OS é marcada como `entregue`, um **trigger no PostgreSQL**
(`fn_agenda_lembrete_banho`) já agenda automaticamente uma notificação para
**30 dias depois**. Não precisa de código no backend — está tudo no banco.

Quando chega a data, o worker gera um **link único** e manda no WhatsApp:
> 🐕 Já faz 30 dias que o Rex não passa por aqui. Que tal agendar um banho? 👉 [link]

### Self-service do tutor
O link abre a página `/agendar/:token` (sem login). O tutor escolhe dia/hora,
e isso cria um agendamento com `status = pendente` e `origem = tutor_self_service`.
A recepção vê na **Agenda** com a tag 📱 e confirma com um clique.

### Lançamento financeiro automático
Ao entregar a OS, outro trigger (`fn_os_gera_lancamento`) cria automaticamente
o lançamento de receita no financeiro. O mesmo vale para vendas no PDV
(`fn_venda_finaliza`), que ainda dá baixa no estoque de cada item vendido.

### Estoque inteligente
Cada produto tem `estoque_minimo` (ponto de reposição) e `estoque_ideal` (alvo).
A tela de Estoque → aba "Sugestão de compra" lista o que está no/abaixo do mínimo
e calcula quanto comprar para repor até o ideal. Toda saída (venda, avaria,
validade, consumo, doação) vira uma linha no kardex (`movimentacoes_estoque`).

### Motor de mensagens automáticas
A tela de Mensagens permite ligar/desligar e editar o texto de cada gatilho
(aniversário do pet, lembrete de agendamento, NPS pós-atendimento, boas-vindas...).
Os textos usam marcadores `{tutor}`, `{pet}`, `{clinica}`, `{data}`, `{hora}`.
O worker `AgendadorMensagens` roda de hora em hora, varre aniversários do dia e
agendamentos dentro da janela do lembrete, e enfileira as mensagens — que o
`NotificacaoDispatcher` então envia pelo WhatsApp.

---

## 🌐 Deploy (mesma stack da lavanderia)

| Camada    | Onde         | Como                                      |
|-----------|--------------|-------------------------------------------|
| Backend   | Railway      | conecta o repo, define as env vars        |
| Frontend  | Vercel       | `npm run build`, deploy automático        |
| Banco     | Neon/Railway | cria o DB, roda o `01-schema.sql`         |

**Variáveis de ambiente no Railway:**
```
ConnectionStrings__Default=Host=...;Database=...;Username=...;Password=...
Jwt__Key=...
ZApi__InstanceId=...
ZApi__Token=...
ZApi__ClientToken=...
App__FrontendUrl=https://seu-front.vercel.app
```

---

## ✅ O que já está pronto

- [x] Multi-tenant + JWT (reaproveitado)
- [x] CRUD de Tutores e Pets
- [x] Cadastro de Serviços com preços
- [x] Agenda (7 dias) + confirmação de pendentes
- [x] Ordens de serviço (kanban: aguardando → andamento → pronto → entregue)
- [x] Prontuário e vacinações
- [x] Financeiro (lançamentos + resumo)
- [x] Dashboard com indicadores
- [x] Notificação "pet pronto" via WhatsApp
- [x] Lembrete automático de 30 dias (trigger)
- [x] Self-service de agendamento (página pública)
- [x] Detalhe do pet: timeline do prontuário, carteira de vacinas e histórico
- [x] Estoque: produtos, kardex, ajustes (avaria/validade/consumo) e sugestão de compra
- [x] PDV: venda com baixa automática de estoque + lançamento financeiro (via trigger)
- [x] Motor de mensagens automáticas configurável (aniversário, lembrete, NPS, boas-vindas)

## 🔜 Próximos passos sugeridos

- [ ] Upload real de fotos (Cloudflare R2 / Supabase Storage)
- [x] Aniversário do tutor (dia/mês) habilitando o gatilho de mensagem
- [ ] Internação com mapa de plantão
- [ ] Comissionamento de equipe
- [ ] Integração fiscal (PlugNotas / Focus NFe)
- [ ] Relatórios financeiros (DRE por período)
- [ ] Controle de estoque de medicamentos/insumos
- [ ] App do tutor (PWA) para acompanhar o pet em tempo real
