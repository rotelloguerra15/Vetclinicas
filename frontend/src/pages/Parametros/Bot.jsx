import { useEffect, useState } from 'react'
import api from '../../api/client'

const DIAS = [
  { val: '1', label: 'Seg' },
  { val: '2', label: 'Ter' },
  { val: '3', label: 'Qua' },
  { val: '4', label: 'Qui' },
  { val: '5', label: 'Sex' },
  { val: '6', label: 'Sab' },
  { val: '0', label: 'Dom' },
]

const MENSAGENS = [
  { campo: 'msgBoasVindas',          label: 'Boas-vindas',              vars: ['{clinica}', '{tutor}'] },
  { campo: 'msgQualServico',         label: 'Pedido de servico',        vars: [] },
  { campo: 'msgQualData',            label: 'Pedido de data',           vars: [] },
  { campo: 'msgHorariosDisponiveis', label: 'Horarios disponiveis',     vars: ['{data}'] },
  { campo: 'msgConfirmacao',         label: 'Confirmacao de pedido',    vars: ['{pet}', '{servico}', '{data}', '{hora}'] },
  { campo: 'msgSemHorarios',         label: 'Sem horarios disponiveis', vars: [] },
  { campo: 'msgForaHorario',         label: 'Fora do horario',          vars: ['{inicio}', '{fim}'] },
  { campo: 'msgErro',                label: 'Opcao invalida',           vars: [] },
  { campo: 'msgCancelar',            label: 'Cancelamento',             vars: [] },
]

const ABA_CONFIG = 'config'
const ABA_META   = 'meta'
const ABA_MSGS   = 'mensagens'
const ABA_LOG    = 'log'

const WEBHOOK_URL = 'https://vetclinicas-production.up.railway.app/api/bot/webhook'

export default function Bot() {
  const [aba, setAba]           = useState(ABA_CONFIG)
  const [cfg, setCfg]           = useState(null)
  const [salvando, setSalvando] = useState(false)
  const [flash, setFlash]       = useState(null)

  // Log state
  const [logs, setLogs]               = useState([])
  const [resumo, setResumo]           = useState(null)
  const [filtroTel, setFiltroTel]     = useState('')
  const [filtroDirecao, setFiltroDirecao] = useState('')
  const [logPage, setLogPage]         = useState(1)
  const [logTotal, setLogTotal]       = useState(0)
  const [diasResumo, setDiasResumo]   = useState(7)

  useEffect(() => {
    api.get('/bot-config/config')
      .then(r => setCfg(r.data))
      .catch(() => setCfg({ ativo: false, diasSemana: '1,2,3,4,5,6', horaInicio: '08:00', horaFim: '18:00', diasAntecedenciaMin: 0, diasAntecedenciaMax: 30, timeoutConversaMin: 30, msgBoasVindas: '', msgQualPet: '', msgQualServico: '', msgQualData: '', msgHorariosDisponiveis: '', msgConfirmacao: '', msgSemHorarios: '', msgForaHorario: '', msgErro: '', msgCancelar: '', metaPhoneNumberId: '', metaWabaId: '' }))
  }, [])

  useEffect(() => {
    if (aba === ABA_LOG) { carregarLogs(); carregarResumo() }
  }, [aba, logPage, filtroDirecao, diasResumo])

  function carregarLogs() {
    api.get('/bot-config/logs', {
      params: { telefone: filtroTel || undefined, direcao: filtroDirecao || undefined, page: logPage, pageSize: 50 }
    }).then(r => { setLogs(r.data.items); setLogTotal(r.data.total) }).catch(() => {})
  }

  function carregarResumo() {
    api.get('/bot-config/logs/resumo', { params: { dias: diasResumo } })
      .then(r => setResumo(r.data)).catch(() => {})
  }

  function msg(tipo, texto) {
    setFlash({ tipo, texto })
    setTimeout(() => setFlash(null), 3500)
  }

  async function salvar(e) {
    e.preventDefault()
    setSalvando(true)
    try {
      await api.put('/bot-config/config', cfg)
      msg('ok', 'Configuracoes salvas!')
    } catch { msg('erro', 'Erro ao salvar.') }
    finally { setSalvando(false) }
  }

  function toggleDia(val) {
    const atual = cfg.diasSemana.split(',').filter(Boolean)
    const novo  = atual.includes(val) ? atual.filter(d => d !== val) : [...atual, val]
    setCfg({ ...cfg, diasSemana: novo.sort().join(',') })
  }

  const diasAtivos = cfg?.diasSemana?.split(',').filter(Boolean) ?? []

  if (!cfg) return <div className="text-slate-400 text-sm p-4">Carregando...</div>

  return (
    <div className="max-w-3xl">
      <div className="flex items-center justify-between mb-6">
        <h2 className="text-2xl font-bold">Bot WhatsApp</h2>
        <span className={`text-xs px-3 py-1 rounded-full font-medium ${cfg.ativo ? 'bg-emerald-100 text-emerald-700' : 'bg-slate-100 text-slate-500'}`}>
          {cfg.ativo ? 'Bot ativo' : 'Bot inativo'}
        </span>
      </div>

      {flash && (
        <div className={`px-4 py-3 rounded-lg text-sm font-medium mb-4 ${flash.tipo === 'ok' ? 'bg-emerald-50 text-emerald-700 border border-emerald-200' : 'bg-red-50 text-red-700 border border-red-200'}`}>
          {flash.tipo === 'ok' ? '✅' : '❌'} {flash.texto}
        </div>
      )}

      {/* Abas */}
      <div className="flex gap-1 border-b mb-6 flex-wrap">
        {[
          { k: ABA_CONFIG, l: 'Configuracao' },
          { k: ABA_META,   l: 'Meta API' },
          { k: ABA_MSGS,   l: 'Mensagens' },
          { k: ABA_LOG,    l: 'Log' },
        ].map(t => (
          <button key={t.k} onClick={() => setAba(t.k)}
            className={`px-4 py-2 text-sm font-medium border-b-2 -mb-px ${aba === t.k ? 'border-slate-900 text-slate-900' : 'border-transparent text-slate-400 hover:text-slate-600'}`}>
            {t.l}
          </button>
        ))}
      </div>

      {/* ── ABA: CONFIGURACAO ───────────────────────────────────────────────── */}
      {aba === ABA_CONFIG && (
        <form onSubmit={salvar} className="space-y-5">

          {/* Ativacao */}
          <div className="bg-white rounded-2xl shadow p-5">
            <div className="flex items-center justify-between">
              <div>
                <p className="font-semibold">Ativar bot de agendamento</p>
                <p className="text-xs text-slate-400 mt-0.5">O bot responde automaticamente mensagens recebidas no WhatsApp da clinica</p>
              </div>
              <button type="button"
                onClick={() => setCfg({ ...cfg, ativo: !cfg.ativo })}
                className={`relative w-12 h-6 rounded-full transition ${cfg.ativo ? 'bg-emerald-500' : 'bg-slate-300'}`}>
                <span className={`absolute top-0.5 w-5 h-5 bg-white rounded-full shadow transition-all ${cfg.ativo ? 'left-6' : 'left-0.5'}`} />
              </button>
            </div>
          </div>

          {/* Horario */}
          <div className="bg-white rounded-2xl shadow p-5 space-y-4">
            <p className="font-semibold">Horario de atendimento do bot</p>
            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="text-xs text-slate-500 block mb-1">Inicio</label>
                <input type="time" className="border rounded-lg px-3 py-2 w-full"
                  value={cfg.horaInicio?.substring(0, 5) ?? '08:00'}
                  onChange={e => setCfg({ ...cfg, horaInicio: e.target.value })} />
              </div>
              <div>
                <label className="text-xs text-slate-500 block mb-1">Fim</label>
                <input type="time" className="border rounded-lg px-3 py-2 w-full"
                  value={cfg.horaFim?.substring(0, 5) ?? '18:00'}
                  onChange={e => setCfg({ ...cfg, horaFim: e.target.value })} />
              </div>
            </div>
            <div>
              <label className="text-xs text-slate-500 block mb-2">Dias da semana</label>
              <div className="flex gap-2">
                {DIAS.map(d => (
                  <button key={d.val} type="button" onClick={() => toggleDia(d.val)}
                    className={`w-10 h-10 rounded-full text-sm font-medium border transition ${
                      diasAtivos.includes(d.val)
                        ? 'bg-slate-900 text-white border-slate-900'
                        : 'bg-white text-slate-500 border-slate-200 hover:border-slate-400'
                    }`}>
                    {d.label}
                  </button>
                ))}
              </div>
            </div>
          </div>

          {/* Janela de agendamento */}
          <div className="bg-white rounded-2xl shadow p-5 space-y-3">
            <p className="font-semibold">Janela de agendamento</p>
            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="text-xs text-slate-500 block mb-1">Antecedencia minima (dias)</label>
                <input type="number" min="0" max="30" className="border rounded-lg px-3 py-2 w-full"
                  value={cfg.diasAntecedenciaMin ?? 0}
                  onChange={e => setCfg({ ...cfg, diasAntecedenciaMin: +e.target.value })} />
              </div>
              <div>
                <label className="text-xs text-slate-500 block mb-1">Ate quantos dias a frente</label>
                <input type="number" min="1" max="90" className="border rounded-lg px-3 py-2 w-full"
                  value={cfg.diasAntecedenciaMax ?? 30}
                  onChange={e => setCfg({ ...cfg, diasAntecedenciaMax: +e.target.value })} />
              </div>
              <div>
                <label className="text-xs text-slate-500 block mb-1">Timeout de conversa (min)</label>
                <input type="number" min="5" max="120" className="border rounded-lg px-3 py-2 w-full"
                  value={cfg.timeoutConversaMin ?? 30}
                  onChange={e => setCfg({ ...cfg, timeoutConversaMin: +e.target.value })} />
              </div>
            </div>
          </div>

          <button type="submit" disabled={salvando}
            className="bg-slate-900 text-white px-6 py-2.5 rounded-lg font-medium disabled:opacity-40 w-full">
            {salvando ? 'Salvando...' : 'Salvar configuracoes'}
          </button>
        </form>
      )}

      {/* ── ABA: META API ─────────────────────────────────────────────────── */}
      {aba === ABA_META && (
        <form onSubmit={salvar} className="space-y-5">

          {/* Instrucoes */}
          <div className="bg-blue-50 border border-blue-200 rounded-2xl p-5">
            <p className="font-semibold text-blue-800 mb-2">Como configurar a Meta Cloud API</p>
            <ol className="text-sm text-blue-700 space-y-1 list-decimal list-inside">
              <li>Acesse <strong>developers.facebook.com</strong> → Crie um app tipo "Business"</li>
              <li>Adicione o produto <strong>WhatsApp</strong> ao app</li>
              <li>Em <strong>WhatsApp → Configuracao</strong>, copie o <strong>Phone Number ID</strong></li>
              <li>Em <strong>Business Manager → Usuarios do sistema</strong>, gere um token permanente com permissao <code>whatsapp_business_messaging</code></li>
              <li>Configure o webhook abaixo apontando para a URL do sistema</li>
            </ol>
          </div>

          {/* Webhook URL */}
          <div className="bg-white rounded-2xl shadow p-5">
            <p className="font-semibold mb-1">URL do Webhook</p>
            <p className="text-xs text-slate-400 mb-3">
              Configure esta URL no painel Meta → WhatsApp → Configuracao → Webhook.
              Campos: <strong>messages</strong>
            </p>
            <div className="flex gap-2 mb-3">
              <input readOnly value={WEBHOOK_URL}
                className="border rounded-lg px-3 py-2 flex-1 text-xs font-mono bg-slate-50" />
              <button type="button"
                onClick={() => { navigator.clipboard.writeText(WEBHOOK_URL); msg('ok', 'URL copiada!') }}
                className="bg-blue-600 text-white px-3 py-2 rounded-lg text-xs">
                Copiar
              </button>
            </div>
            <div>
              <label className="text-xs text-slate-500 block mb-1">Verify Token (defina e coloque no Railway como <code>Meta__WebhookVerifyToken</code>)</label>
              <input readOnly value="vetclinica_webhook_2026"
                className="border rounded-lg px-3 py-2 w-full text-xs font-mono bg-slate-50" />
            </div>
          </div>

          {/* Credenciais */}
          <div className="bg-white rounded-2xl shadow p-5 space-y-4">
            <p className="font-semibold">Credenciais Meta</p>
            <p className="text-xs text-amber-600 bg-amber-50 border border-amber-200 rounded-lg px-3 py-2">
              (!) O token e o Phone Number ID devem ser salvos como variáveis de ambiente no Railway, não aqui.
              Use os nomes: <code>Meta__PhoneNumberId</code>, <code>Meta__Token</code>, <code>Meta__WabaId</code>
            </p>

            <div>
              <label className="text-xs text-slate-500 block mb-1">Phone Number ID (salvo no banco para o webhook)</label>
              <input
                className="border rounded-lg px-3 py-2 w-full text-sm font-mono"
                placeholder="Ex: 123456789012345"
                value={cfg.metaPhoneNumberId ?? ''}
                onChange={e => setCfg({ ...cfg, metaPhoneNumberId: e.target.value })} />
              <p className="text-xs text-slate-400 mt-1">
                Encontrado em: Meta for Developers → Seu app → WhatsApp → Configuracao → Numero de telefone
              </p>
            </div>

            <div>
              <label className="text-xs text-slate-500 block mb-1">WhatsApp Business Account ID (WABA ID)</label>
              <input
                className="border rounded-lg px-3 py-2 w-full text-sm font-mono"
                placeholder="Ex: 987654321098765"
                value={cfg.metaWabaId ?? ''}
                onChange={e => setCfg({ ...cfg, metaWabaId: e.target.value })} />
            </div>
          </div>

          {/* Templates */}
          <div className="bg-white rounded-2xl shadow p-5">
            <p className="font-semibold mb-3">Templates necessários no Meta</p>
            <p className="text-xs text-slate-500 mb-3">
              Crie estes templates em <strong>Meta for Developers → WhatsApp → Modelos de mensagem</strong>.
              Todos em Português (BR). Aguardem aprovação (24-48h).
            </p>
            {[
              {
                nome: 'lembrete_agendamento',
                categoria: 'UTILITY',
                corpo: 'Olá {{1}}! 👋\nLembramos que seu pet *{{2}}* tem agendamento amanhã às *{{3}}* na {{4}}.\nQualquer dúvida, responda esta mensagem.',
                vars: '{{1}} tutorNome  {{2}} petNome  {{3}} horario  {{4}} clinicaNome'
              },
              {
                nome: 'pet_pronto',
                categoria: 'UTILITY',
                corpo: 'Olá {{1}}! 🐾\nSeu pet *{{2}}* está pronto e esperando por você na {{3}}.\nAté logo!',
                vars: '{{1}} tutorNome  {{2}} petNome  {{3}} clinicaNome'
              },
              {
                nome: 'promocao_marketing',
                categoria: 'MARKETING',
                corpo: 'Olá {{1}}! 🎉\n{{2}}\nAgende agora: {{3}}',
                vars: '{{1}} tutorNome  {{2}} textoCampanha  {{3}} linkAgendamento'
              },
            ].map(t => (
              <div key={t.nome} className="border border-slate-200 rounded-xl p-4 mb-3">
                <div className="flex items-center gap-2 mb-2">
                  <code className="text-sm font-bold text-blue-700">{t.nome}</code>
                  <span className="text-xs bg-slate-100 text-slate-500 px-2 py-0.5 rounded">{t.categoria}</span>
                </div>
                <pre className="text-xs text-slate-600 bg-slate-50 rounded-lg p-3 whitespace-pre-wrap mb-2">{t.corpo}</pre>
                <p className="text-xs text-slate-400">Vars: {t.vars}</p>
              </div>
            ))}
          </div>

          {/* Variaveis Railway */}
          <div className="bg-slate-900 rounded-2xl p-5">
            <p className="font-semibold text-white mb-3">Variáveis para adicionar no Railway</p>
            <div className="space-y-2 font-mono text-xs">
              {[
                ['Meta__PhoneNumberId',      'SEU_PHONE_NUMBER_ID'],
                ['Meta__Token',              'SEU_TOKEN_PERMANENTE'],
                ['Meta__WabaId',             'SEU_WABA_ID'],
                ['Meta__WebhookVerifyToken', 'vetclinica_webhook_2026'],
              ].map(([k, v]) => (
                <div key={k} className="flex gap-2">
                  <span className="text-emerald-400 shrink-0">{k}</span>
                  <span className="text-slate-400">=</span>
                  <span className="text-yellow-300">{v}</span>
                </div>
              ))}
            </div>
          </div>

          <button type="submit" disabled={salvando}
            className="bg-slate-900 text-white px-6 py-2.5 rounded-lg font-medium disabled:opacity-40 w-full">
            {salvando ? 'Salvando...' : 'Salvar Phone Number ID'}
          </button>
        </form>
      )}

      {/* ── ABA: MENSAGENS ──────────────────────────────────────────────────── */}
      {aba === ABA_MSGS && (
        <form onSubmit={salvar} className="space-y-4">
          <div className="bg-amber-50 border border-amber-200 rounded-xl p-4 text-sm text-amber-800">
            Use as variaveis disponiveis entre chaves. Ex: <code>{'{clinica}'}</code> sera substituido pelo nome da clinica.
          </div>
          {MENSAGENS.map(m => (
            <div key={m.campo} className="bg-white rounded-2xl shadow p-5">
              <div className="flex items-center justify-between mb-2">
                <label className="font-medium text-sm">{m.label}</label>
                {m.vars.length > 0 && (
                  <div className="flex gap-1">
                    {m.vars.map(v => (
                      <span key={v} className="text-xs bg-slate-100 text-slate-500 px-2 py-0.5 rounded font-mono">{v}</span>
                    ))}
                  </div>
                )}
              </div>
              <textarea rows={3}
                className="border rounded-lg px-3 py-2 w-full text-sm resize-none"
                value={cfg[m.campo] ?? ''}
                onChange={e => setCfg({ ...cfg, [m.campo]: e.target.value })} />
            </div>
          ))}
          <button type="submit" disabled={salvando}
            className="bg-slate-900 text-white px-6 py-2.5 rounded-lg font-medium disabled:opacity-40 w-full">
            {salvando ? 'Salvando...' : 'Salvar mensagens'}
          </button>
        </form>
      )}

      {/* ── ABA: LOG ────────────────────────────────────────────────────────── */}
      {aba === ABA_LOG && (
        <div className="space-y-5">
          <div className="flex items-center gap-3 mb-2">
            <p className="font-semibold">Resumo</p>
            <select className="border rounded-lg px-2 py-1 text-sm"
              value={diasResumo} onChange={e => setDiasResumo(+e.target.value)}>
              <option value={7}>Ultimos 7 dias</option>
              <option value={14}>Ultimos 14 dias</option>
              <option value={30}>Ultimos 30 dias</option>
            </select>
          </div>
          {resumo && (
            <div className="grid grid-cols-3 gap-3">
              {[
                { l: 'Msgs recebidas',    v: resumo.mensagensEntrada,    cor: 'bg-blue-50 text-blue-700' },
                { l: 'Msgs enviadas',     v: resumo.mensagensSaida,      cor: 'bg-slate-50 text-slate-700' },
                { l: 'Telefones unicos',  v: resumo.telefonesUnicos,     cor: 'bg-purple-50 text-purple-700' },
                { l: 'Conversas ativas',  v: resumo.conversasAtivas,     cor: 'bg-amber-50 text-amber-700' },
                { l: 'Agendamentos',      v: resumo.conversasConcluidas, cor: 'bg-emerald-50 text-emerald-700' },
                { l: 'Erros',             v: resumo.erros,               cor: 'bg-red-50 text-red-700' },
              ].map(c => (
                <div key={c.l} className={`rounded-xl p-4 ${c.cor}`}>
                  <div className="text-2xl font-bold">{c.v}</div>
                  <div className="text-xs mt-0.5 opacity-70">{c.l}</div>
                </div>
              ))}
            </div>
          )}
          <div className="flex gap-2">
            <input className="border rounded-lg px-3 py-2 flex-1 text-sm" placeholder="Filtrar por telefone..."
              value={filtroTel} onChange={e => setFiltroTel(e.target.value)}
              onKeyDown={e => e.key === 'Enter' && carregarLogs()} />
            <select className="border rounded-lg px-3 py-2 text-sm"
              value={filtroDirecao} onChange={e => { setFiltroDirecao(e.target.value); setLogPage(1) }}>
              <option value="">Todas</option>
              <option value="entrada">Entrada</option>
              <option value="saida">Saida</option>
            </select>
            <button onClick={carregarLogs} className="bg-slate-200 px-4 rounded-lg text-sm hover:bg-slate-300">
              Buscar
            </button>
          </div>
          <div className="bg-white rounded-2xl shadow overflow-hidden">
            <table className="w-full text-xs">
              <thead className="bg-slate-50 text-left">
                <tr>
                  <th className="p-3">Data/Hora</th>
                  <th className="p-3">Telefone</th>
                  <th className="p-3">Dir.</th>
                  <th className="p-3">Mensagem</th>
                  <th className="p-3">Estado</th>
                </tr>
              </thead>
              <tbody>
                {logs.map(l => (
                  <tr key={l.id} className="border-t hover:bg-slate-50">
                    <td className="p-3 whitespace-nowrap text-slate-400">
                      {new Date(l.criadoEm).toLocaleString('pt-BR', { dateStyle: 'short', timeStyle: 'short' })}
                    </td>
                    <td className="p-3 font-mono">{l.telefone}</td>
                    <td className="p-3">
                      <span className={`px-2 py-0.5 rounded-full text-xs font-medium ${l.direcao === 'entrada' ? 'bg-blue-100 text-blue-700' : 'bg-slate-100 text-slate-600'}`}>
                        {l.direcao === 'entrada' ? '← entrada' : '→ saida'}
                      </span>
                    </td>
                    <td className="p-3 max-w-xs truncate" title={l.mensagem}>{l.mensagem}</td>
                    <td className="p-3 text-slate-400">
                      {l.estadoAntes && l.estadoApos && l.estadoAntes !== l.estadoApos
                        ? <span>{l.estadoAntes} → {l.estadoApos}</span>
                        : l.estadoApos || l.estadoAntes || '—'}
                    </td>
                  </tr>
                ))}
                {logs.length === 0 && (
                  <tr><td colSpan="5" className="p-6 text-center text-slate-400">Nenhum log encontrado</td></tr>
                )}
              </tbody>
            </table>
          </div>
          {logTotal > 50 && (
            <div className="flex items-center justify-between text-sm">
              <span className="text-slate-400">{logTotal} registros</span>
              <div className="flex gap-2">
                <button disabled={logPage === 1} onClick={() => setLogPage(p => p - 1)}
                  className="px-3 py-1.5 border rounded-lg disabled:opacity-40">← Anterior</button>
                <span className="px-3 py-1.5 text-slate-600">Pag {logPage}</span>
                <button disabled={logPage * 50 >= logTotal} onClick={() => setLogPage(p => p + 1)}
                  className="px-3 py-1.5 border rounded-lg disabled:opacity-40">Proxima →</button>
              </div>
            </div>
          )}
        </div>
      )}
    </div>
  )
}
