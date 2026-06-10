import { useEffect, useState, useCallback } from 'react'
import { useNavigate } from 'react-router-dom'
import api from '../api/client'

const STATUS_CONFIG = {
  pendente:        { label: 'Pendente',        cor: 'bg-amber-100 text-amber-700',   dot: 'bg-amber-400'   },
  confirmado:      { label: 'Confirmado',      cor: 'bg-blue-100 text-blue-700',     dot: 'bg-blue-500'    },
  em_atendimento:  { label: 'Em atendimento',  cor: 'bg-purple-100 text-purple-700', dot: 'bg-purple-500'  },
  concluido:       { label: 'Concluído',       cor: 'bg-emerald-100 text-emerald-700', dot: 'bg-emerald-500' },
  cancelado:       { label: 'Cancelado',       cor: 'bg-red-100 text-red-500',       dot: 'bg-red-400'     },
  faltou:          { label: 'Faltou',          cor: 'bg-slate-100 text-slate-500',   dot: 'bg-slate-400'   },
}

const TIPO_EMOJI = {
  consulta: '🩺', banho: '🛁', tosa: '✂️', banho_tosa: '🛁✂️',
  vacina: '💉', retorno: '🔄', cirurgia: '⚕️', exame: '🔬'
}

const TIPOS = ['consulta','banho','tosa','banho_tosa','vacina','retorno','cirurgia','exame']

function fmtHora(d) {
  return new Date(d).toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' })
}
function fmtData(d) {
  return new Date(d).toLocaleDateString('pt-BR', { weekday: 'short', day: '2-digit', month: '2-digit' })
}
function isHoje(d) {
  const hoje = new Date()
  const dt   = new Date(d)
  return dt.toDateString() === hoje.toDateString()
}

export default function Agenda() {
  const navigate = useNavigate()
  const [ags, setAgs]         = useState([])
  const [loading, setLoading] = useState(true)
  const [modal, setModal]     = useState(false)
  const [filtroStatus, setFiltroStatus] = useState('todos')
  const [filtroDia, setFiltroDia]       = useState('semana')
  const [iniciando, setIniciando]       = useState(null)

  const carregar = useCallback(() => {
    setLoading(true)
    api.get('/agendamentos').then(r => setAgs(r.data)).catch(() => {}).finally(() => setLoading(false))
  }, [])

  useEffect(() => { carregar() }, [carregar])

  async function confirmar(id) {
    await api.put(`/agendamentos/${id}/confirmar`).catch(() => {})
    carregar()
  }

  async function cancelar(id) {
    if (!confirm('Cancelar este agendamento?')) return
    await api.put(`/agendamentos/${id}/status`, { status: 'cancelado' }).catch(() => {})
    carregar()
  }

  async function iniciarAtendimento(ag) {
    if (iniciando) return
    setIniciando(ag.id)
    try {
      const r = await api.post(`/agendamentos/${ag.id}/iniciar-atendimento`)
      carregar()
      // Navega para o pet direto
      navigate(`/pets/${r.data.petId}`)
    } catch (err) {
      alert(err.response?.data?.erro || 'Erro ao iniciar atendimento.')
    } finally {
      setIniciando(null)
    }
  }

  // Filtros
  const agsFiltrados = ags.filter(a => {
    if (filtroStatus !== 'todos' && a.status !== filtroStatus) return false
    if (filtroDia === 'hoje') return isHoje(a.dataHora)
    return true
  })

  // Agrupado por dia
  const porDia = agsFiltrados.reduce((acc, a) => {
    const dia = new Date(a.dataHora).toDateString()
    if (!acc[dia]) acc[dia] = []
    acc[dia].push(a)
    return acc
  }, {})

  const hoje = ags.filter(a => isHoje(a.dataHora) && a.status !== 'cancelado').length
  const pendentes = ags.filter(a => a.status === 'pendente').length
  const emAtendimento = ags.filter(a => a.status === 'em_atendimento').length

  return (
    <div className="max-w-4xl mx-auto space-y-5">

      {/* Header */}
      <div className="flex items-center justify-between flex-wrap gap-3">
        <div>
          <h2 className="text-2xl font-bold text-slate-800">📅 Agenda</h2>
          <p className="text-sm text-slate-400 mt-0.5">
            {hoje > 0 ? `${hoje} agendamento${hoje > 1 ? 's' : ''} hoje` : 'Nenhum agendamento hoje'}
            {pendentes > 0 ? ` · ${pendentes} pendente${pendentes > 1 ? 's' : ''}` : ''}
          </p>
        </div>
        <button onClick={() => setModal(true)}
          className="bg-slate-900 text-white px-4 py-2 rounded-xl text-sm font-medium hover:bg-slate-800">
          + Novo agendamento
        </button>
      </div>

      {/* KPIs */}
      <div className="grid grid-cols-3 gap-3">
        <div className="bg-white rounded-xl p-4 shadow-sm border border-slate-100 text-center">
          <div className="text-2xl font-bold text-blue-600">{hoje}</div>
          <div className="text-xs text-slate-500 mt-0.5">Hoje</div>
        </div>
        <div className={`rounded-xl p-4 shadow-sm border text-center ${emAtendimento > 0 ? 'bg-purple-50 border-purple-200' : 'bg-white border-slate-100'}`}>
          <div className={`text-2xl font-bold ${emAtendimento > 0 ? 'text-purple-600' : 'text-slate-600'}`}>{emAtendimento}</div>
          <div className="text-xs text-slate-500 mt-0.5">Em atendimento</div>
        </div>
        <div className={`rounded-xl p-4 shadow-sm border text-center ${pendentes > 0 ? 'bg-amber-50 border-amber-200' : 'bg-white border-slate-100'}`}>
          <div className={`text-2xl font-bold ${pendentes > 0 ? 'text-amber-600' : 'text-slate-600'}`}>{pendentes}</div>
          <div className="text-xs text-slate-500 mt-0.5">Aguardando confirmação</div>
        </div>
      </div>

      {/* Filtros */}
      <div className="flex gap-2 flex-wrap">
        <div className="flex gap-1 bg-slate-100 rounded-lg p-1">
          {[['semana','Próximos 7 dias'],['hoje','Hoje']].map(([v,l]) => (
            <button key={v} onClick={() => setFiltroDia(v)}
              className={`px-3 py-1.5 rounded-md text-sm font-medium transition ${filtroDia === v ? 'bg-white shadow text-slate-800' : 'text-slate-500 hover:text-slate-700'}`}>
              {l}
            </button>
          ))}
        </div>
        <div className="flex gap-1 bg-slate-100 rounded-lg p-1">
          {[['todos','Todos'],['confirmado','Confirmados'],['em_atendimento','Em atendimento'],['pendente','Pendentes']].map(([v,l]) => (
            <button key={v} onClick={() => setFiltroStatus(v)}
              className={`px-3 py-1.5 rounded-md text-sm font-medium transition ${filtroStatus === v ? 'bg-white shadow text-slate-800' : 'text-slate-500 hover:text-slate-700'}`}>
              {l}
            </button>
          ))}
        </div>
      </div>

      {/* Lista agrupada por dia */}
      {loading ? (
        <div className="text-center py-12 text-slate-400">Carregando...</div>
      ) : Object.keys(porDia).length === 0 ? (
        <div className="bg-white rounded-2xl shadow p-12 text-center">
          <div className="text-4xl mb-3">📭</div>
          <p className="text-slate-500 font-medium">Nenhum agendamento encontrado</p>
          <p className="text-slate-400 text-sm mt-1">Tente ajustar os filtros ou crie um novo agendamento</p>
        </div>
      ) : Object.entries(porDia).map(([dia, lista]) => {
        const dt = new Date(dia)
        const ehHoje = dt.toDateString() === new Date().toDateString()
        return (
          <div key={dia}>
            {/* Separador de dia */}
            <div className="flex items-center gap-3 mb-2">
              <div className={`text-xs font-semibold px-3 py-1 rounded-full ${ehHoje ? 'bg-slate-900 text-white' : 'bg-slate-100 text-slate-500'}`}>
                {ehHoje ? '📍 Hoje' : ''} {dt.toLocaleDateString('pt-BR', { weekday: 'long', day: '2-digit', month: 'long' })}
              </div>
              <div className="flex-1 h-px bg-slate-100" />
              <span className="text-xs text-slate-400">{lista.length} agendamento{lista.length > 1 ? 's' : ''}</span>
            </div>

            <div className="space-y-2">
              {lista.sort((a,b) => new Date(a.dataHora) - new Date(b.dataHora)).map(ag => {
                const sc = STATUS_CONFIG[ag.status] || STATUS_CONFIG.pendente
                const emAndamento = ag.status === 'em_atendimento'
                return (
                  <div key={ag.id}
                    className={`bg-white rounded-2xl shadow-sm border transition hover:shadow-md ${emAndamento ? 'border-purple-200 ring-1 ring-purple-200' : 'border-slate-100'}`}>
                    <div className="flex items-center gap-4 p-4">

                      {/* Hora */}
                      <div className="text-center min-w-[52px]">
                        <div className="text-lg font-bold text-slate-700">{fmtHora(ag.dataHora)}</div>
                        <div className="text-xs text-slate-400">{ag.duracaoMin}min</div>
                      </div>

                      <div className="w-px h-10 bg-slate-100" />

                      {/* Tipo */}
                      <div className="text-2xl min-w-[32px] text-center">
                        {TIPO_EMOJI[ag.tipo] || '📋'}
                      </div>

                      {/* Info */}
                      <div className="flex-1 min-w-0">
                        <div className="flex items-center gap-2 flex-wrap">
                          <span className="font-semibold text-slate-800">{ag.petNome}</span>
                          {ag.origem === 'tutor_self_service' && (
                            <span className="text-xs bg-blue-50 text-blue-600 px-2 py-0.5 rounded-full">📱 App tutor</span>
                          )}
                        </div>
                        <div className="text-sm text-slate-500 mt-0.5 capitalize">
                          {ag.tipo.replace('_', ' ')}
                          {ag.obs && <span className="text-slate-400"> · {ag.obs}</span>}
                        </div>
                      </div>

                      {/* Status */}
                      <div>
                        <span className={`text-xs font-medium px-2.5 py-1 rounded-full ${sc.cor}`}>
                          <span className={`inline-block w-1.5 h-1.5 rounded-full mr-1.5 ${sc.dot} align-middle`} />
                          {sc.label}
                        </span>
                      </div>

                      {/* Ações */}
                      <div className="flex gap-2 flex-shrink-0">
                        {ag.status === 'pendente' && (
                          <>
                            <button onClick={() => confirmar(ag.id)}
                              className="text-xs bg-blue-600 text-white px-3 py-1.5 rounded-lg hover:bg-blue-700 font-medium">
                              Confirmar
                            </button>
                            <button onClick={() => cancelar(ag.id)}
                              className="text-xs border border-slate-200 text-slate-500 px-3 py-1.5 rounded-lg hover:bg-slate-50">
                              Recusar
                            </button>
                          </>
                        )}
                        {(ag.status === 'confirmado' || ag.status === 'em_atendimento') && (
                          <button
                            onClick={() => ag.status === 'em_atendimento'
                              ? navigate(`/pets/${ag.petId}`)
                              : iniciarAtendimento(ag)}
                            disabled={iniciando === ag.id}
                            className={`text-xs px-4 py-1.5 rounded-lg font-semibold transition disabled:opacity-50 ${
                              ag.status === 'em_atendimento'
                                ? 'bg-purple-600 text-white hover:bg-purple-700'
                                : 'bg-emerald-600 text-white hover:bg-emerald-700'
                            }`}>
                            {iniciando === ag.id ? '⏳'
                              : ag.status === 'em_atendimento' ? '👁 Ver pet'
                              : '▶ Iniciar atendimento'}
                          </button>
                        )}
                        {ag.status === 'concluido' && (
                          <button onClick={() => navigate(`/pets/${ag.petId}`)}
                            className="text-xs border border-slate-200 text-slate-500 px-3 py-1.5 rounded-lg hover:bg-slate-50">
                            Ver histórico
                          </button>
                        )}
                        {ag.status === 'confirmado' && (
                          <button onClick={() => cancelar(ag.id)}
                            className="text-xs border border-red-200 text-red-400 px-3 py-1.5 rounded-lg hover:bg-red-50">
                            Cancelar
                          </button>
                        )}
                      </div>
                    </div>
                  </div>
                )
              })}
            </div>
          </div>
        )
      })}

      {modal && (
        <ModalAgendar
          onClose={() => setModal(false)}
          onSaved={() => { setModal(false); carregar() }}
        />
      )}
    </div>
  )
}

function ModalAgendar({ onClose, onSaved }) {
  const [pets, setPets]   = useState([])
  const [f, setF]         = useState({
    petId: '', tipo: 'consulta',
    data: new Date().toISOString().split('T')[0],
    hora: '09:00', duracaoMin: 60, obs: ''
  })
  const [saving, setSaving] = useState(false)

  useEffect(() => {
    api.get('/pets', { params: { pageSize: 200 } })
      .then(r => setPets(r.data.items)).catch(() => {})
  }, [])

  async function salvar(e) {
    e.preventDefault()
    setSaving(true)
    try {
      const dataHora = new Date(`${f.data}T${f.hora}:00`).toISOString()
      await api.post('/agendamentos', {
        petId: f.petId, tipo: f.tipo, dataHora,
        duracaoMin: +f.duracaoMin, obs: f.obs || null
      })
      onSaved()
    } catch (err) {
      alert(err.response?.data?.erro || 'Erro ao agendar.')
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50 p-4" onClick={onClose}>
      <div className="bg-white rounded-2xl shadow-xl p-6 w-full max-w-md" onClick={e => e.stopPropagation()}>
        <div className="flex items-center justify-between mb-5">
          <h3 className="font-bold text-lg">📅 Novo agendamento</h3>
          <button onClick={onClose} className="text-slate-400 hover:text-slate-600 text-xl">×</button>
        </div>
        <form onSubmit={salvar} className="space-y-3">
          <div>
            <label className="text-xs text-slate-500 block mb-1">Pet *</label>
            <select className="border rounded-xl px-3 py-2.5 w-full text-sm" required
              value={f.petId} onChange={e => setF({...f, petId: e.target.value})}>
              <option value="">Selecione o pet...</option>
              {pets.map(p => <option key={p.id} value={p.id}>{p.nome} — {p.tutorNome}</option>)}
            </select>
          </div>
          <div>
            <label className="text-xs text-slate-500 block mb-1">Tipo</label>
            <select className="border rounded-xl px-3 py-2.5 w-full text-sm"
              value={f.tipo} onChange={e => setF({...f, tipo: e.target.value})}>
              {TIPOS.map(t => (
                <option key={t} value={t}>{TIPO_EMOJI[t]} {t.replace('_',' ')}</option>
              ))}
            </select>
          </div>
          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="text-xs text-slate-500 block mb-1">Data</label>
              <input type="date" className="border rounded-xl px-3 py-2.5 w-full text-sm" required
                value={f.data} onChange={e => setF({...f, data: e.target.value})} />
            </div>
            <div>
              <label className="text-xs text-slate-500 block mb-1">Hora</label>
              <input type="time" className="border rounded-xl px-3 py-2.5 w-full text-sm" required
                value={f.hora} onChange={e => setF({...f, hora: e.target.value})} />
            </div>
          </div>
          <div>
            <label className="text-xs text-slate-500 block mb-1">Duração (minutos)</label>
            <input type="number" className="border rounded-xl px-3 py-2.5 w-full text-sm"
              value={f.duracaoMin} onChange={e => setF({...f, duracaoMin: e.target.value})} />
          </div>
          <div>
            <label className="text-xs text-slate-500 block mb-1">Observação</label>
            <input className="border rounded-xl px-3 py-2.5 w-full text-sm" placeholder="Opcional"
              value={f.obs} onChange={e => setF({...f, obs: e.target.value})} />
          </div>
          <button disabled={saving}
            className="w-full bg-emerald-600 text-white py-3 rounded-xl text-sm font-semibold disabled:opacity-50 hover:bg-emerald-700 mt-2">
            {saving ? '⏳ Agendando...' : '✓ Agendar'}
          </button>
        </form>
      </div>
    </div>
  )
}
