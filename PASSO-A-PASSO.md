# 🐾 VetClinica — Passo a passo (do zero ao rodando)

Guia para Windows + PowerShell. Do arquivo `.zip` até o sistema funcionando no navegador.

---

## ✅ O QUE JÁ FOI ENTREGUE

### Núcleo (reaproveitado da lavanderia)
- Multi-tenant + login JWT + middleware de tenant
- Financeiro: lançamentos + resumo (receitas/despesas/saldo)
- CORS, deploy stack, paginação

### Clínica e banho/tosa
- Cadastro de **Tutores** (com aniversário dia/mês)
- Cadastro de **Pets** (espécie, raça, peso, idade calculada)
- **Detalhe do pet**: timeline do prontuário, carteira de vacinas, histórico
- **Agenda** (7 dias) + confirmação de pendentes
- **Ordens de Serviço** em kanban (aguardando → andamento → pronto → entregue)
- **Prontuário** e **Vacinações** (com status: em dia / vencendo / vencida)

### Estoque e vendas
- **Produtos** com preço de custo/venda e controle de estoque
- **Kardex** (histórico de movimentações)
- **Ajustes**: entrada, avaria, validade, consumo, doação, inventário
- **Sugestão de compra** (produtos abaixo do mínimo → quanto comprar)
- **PDV**: venda com carrinho, desconto, forma de pagamento
- Baixa automática de estoque + lançamento financeiro ao vender (via trigger)

### Automações (WhatsApp via Z-API)
- **Pet pronto** → mensagem + foto ao tutor
- **Lembrete de 30 dias** sem voltar → link de auto-agendamento
- **Motor de mensagens configurável**: aniversário do pet, aniversário do tutor,
  boas-vindas, lembrete de agendamento (X horas antes), NPS pós-atendimento
- **Self-service**: tutor agenda sozinho pelo link, clínica confirma

### Dashboard
- Agendamentos hoje, OS abertas, vacinas vencendo, produtos p/ repor, vendas no mês, total de pets/tutores

---

## 🔜 O QUE FALTA (não implementado)

| Item | Esforço | Observação |
|------|---------|------------|
| Upload real de fotos | Médio | Hoje o campo aceita URL pronta. Falta integrar Supabase Storage / Cloudflare R2 |
| Internação com mapa de plantão | Alto | Funcionalidade forte do SimplesVet para clínica/hospital |
| Comissionamento de equipe | Médio | Cálculo de comissão por tosador/vet |
| Módulo fiscal (NFC-e/NFS-e) | Alto | Recomendado terceirizar (PlugNotas, Focus NFe) |
| Créditos/débitos do cliente ("fiado") | Baixo | Conta corrente do tutor |
| Confirmação de agenda automática | Baixo | Mandar WhatsApp ao confirmar o agendamento |
| Relatórios (DRE por período) | Médio | Financeiro tem o resumo, falta o detalhado |


---

## 🧰 PRÉ-REQUISITOS (instalar uma vez)

Abra o **PowerShell como Administrador** e rode:

```powershell
# .NET 8 SDK
winget install Microsoft.DotNet.SDK.8

# Node.js (versão LTS)
winget install OpenJS.NodeJS.LTS

# Docker Desktop (para o banco PostgreSQL)
winget install Docker.DockerDesktop
```

> Depois de instalar, **feche e reabra o PowerShell** para os comandos `dotnet`, `node` e `docker` ficarem disponíveis.

Confirme que instalou:
```powershell
dotnet --version    # deve mostrar 8.x
node --version      # deve mostrar v20.x ou v22.x
docker --version    # deve mostrar Docker version 2x.x
```

---

## 📂 PASSO 1 — Colocar o projeto no lugar

1. Crie uma pasta para seus projetos, por exemplo `C:\Projetos`.
2. Extraia o `vetclinica.zip` lá dentro.

Resultado esperado:
```
C:\Projetos\vetclinica\
├── VetClinica.API\      (backend .NET)
├── frontend\            (React)
├── database\            (os 3 arquivos .sql)
├── docker-compose.yml
└── README.md
```

No PowerShell, entre na pasta:
```powershell
cd C:\Projetos\vetclinica
```

---

## 🗄️ PASSO 2 — Subir o banco de dados (cria as tabelas)

O `docker-compose.yml` sobe o PostgreSQL **e já cria todas as tabelas** rodando os
três arquivos SQL automaticamente (`01-schema`, `02-estoque-mensagens`, `03-aniversario-tutor`).

> Abra o **Docker Desktop** antes (deixe ele rodando), depois:

```powershell
docker compose up -d
```

Verifique que subiu:
```powershell
docker ps
# deve aparecer um container chamado vetclinica-db na porta 5432
```

### Conferir se as tabelas foram criadas (opcional)
```powershell
docker exec -it vetclinica-db psql -U postgres -d vetclinica -c "\dt"
```
Você deve ver 19 tabelas (tutores, pets, produtos, vendas, etc).

> **Importante:** NÃO rode migrations do Entity Framework. O banco é criado
> pelos arquivos SQL — o backend apenas se conecta às tabelas existentes.

---

## ⚙️ PASSO 3 — Configurar e rodar o BACKEND (.NET)

```powershell
cd C:\Projetos\vetclinica\VetClinica.API

# baixa as dependências
dotnet restore

# roda a API forçando a porta 5000 (que é a que o frontend espera)
dotnet run --urls "http://localhost:5000"
```

Quando aparecer no console:
```
Now listening on: http://localhost:5000
Application started. Press Ctrl+C to shut down.
```
✅ O backend está no ar. **Deixe esse PowerShell aberto** (a API fica rodando aqui).

### Testar a API rapidinho
Abra **outro** PowerShell e rode:
```powershell
# faz login com o usuário do seed e mostra o token
$body = '{"email":"admin@clinicafonseca.com.br","senha":"admin123"}'
Invoke-RestMethod -Uri "http://localhost:5000/api/auth/login" -Method Post -Body $body -ContentType "application/json"
```
Se voltar um objeto com `token`, `nome` e `papel` → a API e o banco estão conversando. 🎉

> Também dá para abrir o **Swagger** no navegador: `http://localhost:5000/swagger`

---

## 💻 PASSO 4 — Rodar o FRONTEND (React)

Abra **mais um** PowerShell (deixe o backend rodando no outro):

```powershell
cd C:\Projetos\vetclinica\frontend

# instala as dependências (demora 1-2 min na primeira vez)
npm install

# sobe o frontend
npm run dev
```

Quando aparecer:
```
VITE ready in ... ms
➜  Local:   http://localhost:5173/
```
✅ Abra `http://localhost:5173` no navegador.

---

## 🔑 PASSO 5 — Entrar no sistema

Na tela de login use o usuário criado pelo seed:

| Campo | Valor |
|-------|-------|
| Email | `admin@clinicafonseca.com.br` |
| Senha | `admin123` |

Você já vai ver produtos, serviços e categorias de exemplo (vieram no seed).

---

## 🧪 PASSO 6 — Roteiro de teste (validar tudo funcionando)

1. **Dashboard** — confira os cards (já deve mostrar produtos p/ repor, pois o seed
   tem 2 produtos abaixo do mínimo).
2. **Tutores** → "+ Novo tutor" → preencha nome, telefone e **aniversário** (dia/mês) → Salvar.
3. **Pets** → "+ Novo pet" → vincule ao tutor → Salvar. Clique no card para ver o detalhe.
4. No detalhe do pet → aba **Prontuário** → "+ Novo registro". Depois aba **Vacinas** → registre uma.
5. **Estoque** → aba "Sugestão de compra" → veja Antipulgas e Coleira sugeridos.
6. **PDV** → clique num produto para jogar no carrinho → escolha pagamento → "Finalizar venda".
   - Volte em **Estoque**: o estoque do produto vendido baixou.
   - Volte no **Dashboard**: "Vendas no mês" subiu.
7. **Mensagens** → ligue/desligue gatilhos e edite um texto.
8. **Agenda** → confira; agendamentos do tutor (self-service) aparecem com a tag 📱.

---

## 📱 PASSO 7 — Ligar o WhatsApp (quando quiser)

As mensagens só saem de verdade depois de configurar a Z-API:

1. Crie conta em [z-api.io](https://z-api.io), crie uma instância e conecte seu número (QR Code).
2. Pare o backend (Ctrl+C) e edite `VetClinica.API\appsettings.json`:
```json
"ZApi": {
  "BaseUrl": "https://api.z-api.io",
  "InstanceId": "SUA_INSTANCE",
  "Token": "SEU_TOKEN",
  "ClientToken": "SEU_CLIENT_TOKEN"
}
```
3. Rode o backend de novo. Os dois workers (envio + agendador de aniversários/lembretes)
   já sobem junto com a API.

> Alternativa grátis: **Evolution API** (open source), reescrevendo os endpoints em
> `Services\ZApiService.cs`.

---

## 🔁 Comandos do dia a dia (resumo)

```powershell
# Subir o banco
cd C:\Projetos\vetclinica
docker compose up -d

# Rodar backend (terminal 1)
cd VetClinica.API
dotnet run --urls "http://localhost:5000"

# Rodar frontend (terminal 2)
cd ..\frontend
npm run dev

# Parar o banco (quando terminar)
cd C:\Projetos\vetclinica
docker compose down
```

---

## 🆘 Problemas comuns

| Sintoma | Causa provável | Solução |
|---------|----------------|---------|
| `dotnet: comando não encontrado` | PowerShell aberto antes de instalar | Feche e reabra o PowerShell |
| API não conecta no banco | Docker não está rodando | Abra o Docker Desktop e `docker compose up -d` |
| Front mostra erro de rede / 401 | Backend não está na porta 5000 | Rode com `--urls "http://localhost:5000"` |
| Login falha | Banco sem o seed | Confira o passo 2 (`\dt` deve listar as tabelas) |
| Porta 5432 ocupada | Já tem um Postgres local | Pare o outro, ou mude a porta no `docker-compose.yml` e no `appsettings.json` |
| `npm install` trava | Cache npm corrompido | `npm cache clean --force` e tente de novo |

---

## 🎨 Personalização da marca

O sistema já vem com o nome **Clínica Veterinária Fonseca** e a frase
**Gestão Inteligente para Clínicas Veterinárias**.

**Para subir a logo:** salve o arquivo como `logo.png` e coloque em
`frontend\public\logo.png`. Ela aparece sozinha no login e no menu.
Enquanto não houver logo, mostra o emoji 🐾.

**Para trocar nome/frase:** edite `frontend\src\lib\branding.js`.

## 🌐 Quando for para produção

- **Backend** → Railway (conecta o repositório, define as variáveis de ambiente)
- **Frontend** → Vercel (`npm run build`, deploy automático)
- **Banco** → Neon ou Railway Postgres (rode os 3 SQL uma vez)

Variáveis no Railway:
```
ConnectionStrings__Default=Host=...;Database=...;Username=...;Password=...
Jwt__Key=uma_chave_aleatoria_de_no_minimo_32_caracteres
ZApi__InstanceId=...
ZApi__Token=...
ZApi__ClientToken=...
App__FrontendUrl=https://seu-front.vercel.app
```
