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
const ABA_MSGS   = 'mensagens'
const ABA_LOG    = 'log'

export default function Bot() {
  const [aba, setAba] = useState(ABA_CONFIG)
  const [cfg, setCfg] = useState(null)
  const [salvando, setSalvando] = useState(false)
  const [flash, setFlash] = useState(null)

  // Log state
  const [logs, setLogs] = useState([])
  const [resumo, setResumo] = useState(null)
  const [filtroTel, setFiltroTel] = useState('')
  const [filtroDirecao, setFiltroDirecao] = useState('')
  const [logPage, setLogPage] = useState(1)
  const [logTotal, setLogTotal] = useState(0)
  const [diasResumo, setDiasResumo] = useState(7)

  useEffect(() => {
    api.get('/bot/config').then(r => setCfg(r.data)).catch(() => {})
  }, [])

  useEffect(() => {
    if (aba === ABA_LOG) {
      carregarLogs()
      carregarResumo()
    }
  }, [aba, logPage, filtroDirecao, diasResumo])

  function carregarLogs() {
    api.get('/bot/logs', {
      params: { telefone: filtroTel || undefined, direcao: filtroDirecao || undefined, page: logPage, pageSize: 50 }
    }).then(r => { setLogs(r.data.items); setLogTotal(r.data.total) }).catch(() => {})
  }

  function carregarResumo() {
    api.get('/bot/logs/resumo', { params: { dias: diasResumo } })
      .then(r => setResumo(r.data)).catch(() => {})
  }

  function msg(tipo, texto) {
    setFlash({ tipo, texto })
    setTimeout(() => setFlash(null), 3000)
  }

  async function salvar(e) {
    e.preventDefault()
    setSalvando(true)
    try {
      await api.put('/bot/config', cfg)
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

  const webhookUrl = `${window.location.origin.replace('5173', '5010')}/api/webhook/whatsapp/SEU_TENANT_ID`

  if (!cfg) return <div className="text-slate-400 text-sm p-4">Carregando...</div>

  return (
    <div className="max-w-3xl">
      <div className="flex items-center justify-between mb-6">
        <h2 className="text-2xl font-bold">Bot WhatsApp</h2>
        <div className="flex items-center gap-3">
          <span className={`text-xs px-3 py-1 rounded-full font-medium ${cfg.ativo ? 'bg-emerald-100 text-emerald-700' : 'bg-slate-100 text-slate-500'}`}>
            {cfg.ativo ? 'Bot ativo' : 'Bot inativo'}
          </span>
        </div>
      </div>

      {flash && (
        <div className={`px-4 py-3 rounded-lg text-sm font-medium mb-4 ${flash.tipo === 'ok' ? 'bg-emerald-50 text-emerald-700 border border-emerald-200' : 'bg-red-50 text-red-700 border border-red-200'}`}>
          {flash.tipo === 'ok' ? '✅' : '❌'} {flash.texto}
        </div>
      )}

      {/* Abas */}
      <div className="flex gap-1 border-b mb-6">
        {[
          { k: ABA_CONFIG, l: '⚙️ Configuracao' },
          { k: ABA_MSGS,   l: '💬 Mensagens' },
          { k: ABA_LOG,    l: '📊 Log' },
        ].map(t => (
          <button key={t.k} onClick={() => setAba(t.k)}
            className={`px-4 py-2 text-sm font-medium border-b-2 -mb-px ${aba === t.k ? 'border-slate-900 text-slate-900' : 'border-transparent text-slate-400 hover:text-slate-600'}`}>
            {t.l}
          </button>
        ))}
      </div>

      {/* ── ABA: CONFIGURACAO ─────────────────────────────────────────────── */}
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

          {/* Webhook URL */}
          <div className="bg-blue-50 border border-blue-200 rounded-2xl p-5">
            <p className="font-semibold text-blue-800 mb-1">URL do Webhook (Z-API)</p>
            <p className="text-xs text-blue-600 mb-2">Configure esta URL na Z-API em "Webhook de mensagens recebidas". Substitua SEU_TENANT_ID pelo ID da clinica.</p>
            <div className="flex gap-2">
              <input readOnly value={webhookUrl}
                className="border border-blue-200 rounded-lg px-3 py-1.5 flex-1 text-xs font-mono bg-white" />
              <button type="button"
                onClick={() => { navigator.clipboard.writeText(webhookUrl); msg('ok', 'URL copiada!') }}
                className="bg-blue-600 text-white px-3 py-1.5 rounded-lg text-xs">
                Copiar
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
                  <button key={d.val} type="button"
                    onClick={() => toggleDia(d.val)}
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
            <p className="text-xs text-slate-400">O timeout reinicia a conversa se o tutor nao responder dentro do prazo.</p>
          </div>

          <button type="submit" disabled={salvando}
            className="bg-slate-900 text-white px-6 py-2.5 rounded-lg font-medium disabled:opacity-40 w-full">
            {salvando ? 'Salvando...' : 'Salvar configuracoes'}
          </button>
        </form>
      )}

      {/* ── ABA: MENSAGENS ────────────────────────────────────────────────── */}
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
              <textarea
                rows={3}
                className="border rounded-lg px-3 py-2 w-full text-sm resize-none"
                value={cfg[m.campo] ?? ''}
                onChange={e => setCfg({ ...cfg, [m.campo]: e.target.value })}
              />
            </div>
          ))}

          <button type="submit" disabled={salvando}
            className="bg-slate-900 text-white px-6 py-2.5 rounded-lg font-medium disabled:opacity-40 w-full">
            {salvando ? 'Salvando...' : 'Salvar mensagens'}
          </button>
        </form>
      )}

      {/* ── ABA: LOG ──────────────────────────────────────────────────────── */}
      {aba === ABA_LOG && (
        <div className="space-y-5">

          {/* Resumo */}
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

          {/* Filtros */}
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
            <button onClick={carregarLogs}
              className="bg-slate-200 px-4 rounded-lg text-sm hover:bg-slate-300">
              Buscar
            </button>
          </div>

          {/* Tabela de logs */}
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

          {/* Paginação */}
          {logTotal > 50 && (
            <div className="flex items-center justify-between text-sm">
              <span className="text-slate-400">{logTotal} registros</span>
              <div className="flex gap-2">
                <button disabled={logPage === 1}
                  onClick={() => setLogPage(p => p - 1)}
                  className="px-3 py-1.5 border rounded-lg disabled:opacity-40">← Anterior</button>
                <span className="px-3 py-1.5 text-slate-600">Pag {logPage}</span>
                <button disabled={logPage * 50 >= logTotal}
                  onClick={() => setLogPage(p => p + 1)}
                  className="px-3 py-1.5 border rounded-lg disabled:opacity-40">Proxima →</button>
              </div>
            </div>
          )}
        </div>
      )}
    </div>
  )
}
