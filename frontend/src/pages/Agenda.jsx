import { useEffect, useState, useCallback } from 'react'
import { useNavigate } from 'react-router-dom'
import api from '../api/client'

const STATUS_CONFIG = {
  pendente:       { label: 'Pendente',       cor: 'bg-amber-100 text-amber-800',    dot: 'bg-amber-400',   borda: 'border-amber-200' },
  confirmado:     { label: 'Confirmado',     cor: 'bg-blue-100 text-blue-800',      dot: 'bg-blue-500',    borda: 'border-blue-200'  },
  em_atendimento: { label: 'Em atendimento', cor: 'bg-purple-100 text-purple-800',  dot: 'bg-purple-500',  borda: 'border-purple-300'},
  concluido:      { label: 'Concluído',      cor: 'bg-emerald-100 text-emerald-800',dot: 'bg-emerald-500', borda: 'border-emerald-200'},
  cancelado:      { label: 'Cancelado',      cor: 'bg-slate-100 text-slate-500',    dot: 'bg-slate-300',   borda: 'border-slate-200' },
  faltou:         { label: 'Faltou',         cor: 'bg-red-50 text-red-400',         dot: 'bg-red-300',     borda: 'border-red-100'   },
}

const TIPO_EMOJI = {
  consulta:'🩺', banho:'🛁', tosa:'✂️', banho_tosa:'🛁✂️',
  vacina:'💉', retorno:'🔄', cirurgia:'⚕️', exame:'🔬'
}
const TIPOS = ['consulta','banho','tosa','banho_tosa','vacina','retorno','cirurgia','exame']

// Horários de funcionamento (08h às 18h, de 30 em 30 min)
const HORARIOS = Array.from({ length: 21 }, (_, i) => {
  const total = 8 * 60 + i * 30
  const h = String(Math.floor(total / 60)).padStart(2, '0')
  const m = String(total % 60).padStart(2, '0')
  return `${h}:${m}`
})

function fmtHora(d) {
  return new Date(d).toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' })
}
function hoje() {
  return new Date().toISOString().split('T')[0]
}
function isHoje(d) {
  return new Date(d).toDateString() === new Date().toDateString()
}

export default function Agenda() {
  const navigate  = useNavigate()
  const [ags, setAgs]         = useState([])
  const [vetLogado, setVet]   = useState(null)
  const [loading, setLoading] = useState(true)
  const [modal, setModal]     = useState(false)
  const [abaView, setAbaView] = useState('hoje') // hoje | semana
  const [iniciando, setIniciando] = useState(null)

  const carregar = useCallback(() => {
    setLoading(true)
    Promise.all([
      api.get('/agendamentos'),
      api.get('/receituario/vet-logado').catch(() => ({ data: { ehVet: false } }))
    ]).then(([rAgs, rVet]) => {
      setAgs(rAgs.data)
      if (rVet.data.ehVet) setVet(rVet.data)
    }).catch(() => {}).finally(() => setLoading(false))
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
      navigate(`/pets/${r.data.petId}`)
    } catch (err) {
      alert(err.response?.data?.erro || 'Erro ao iniciar atendimento.')
    } finally { setIniciando(null) }
  }

  const agHoje   = ags.filter(a => isHoje(a.dataHora) && a.status !== 'cancelado')
  const agSemana = ags.filter(a => a.status !== 'cancelado')
  const emAtend  = agHoje.filter(a => a.status === 'em_atendimento').length
  const pendentes = agHoje.filter(a => a.status === 'pendente').length
  const concluidos = agHoje.filter(a => a.status === 'concluido').length

  // Monta grade de horários do dia com os agendamentos
  const gradeHoje = HORARIOS.map(hora => {
    const [h, m] = hora.split(':').map(Number)
    const ag = agHoje.find(a => {
      const dt = new Date(a.dataHora)
      return dt.getHours() === h && dt.getMinutes() === m
    })
    const agora = new Date()
    const slotDt = new Date()
    slotDt.setHours(h, m, 0, 0)
    const passado = slotDt < agora
    return { hora, ag, passado }
  })

  // Agrupado por dia para visão semanal
  const porDia = agSemana.reduce((acc, a) => {
    const dia = new Date(a.dataHora).toDateString()
    if (!acc[dia]) acc[dia] = []
    acc[dia].push(a)
    return acc
  }, {})

  return (
    <div className="max-w-4xl mx-auto space-y-5">

      {/* Header com info do vet logado */}
      <div className="bg-white rounded-2xl shadow-sm border border-slate-100 p-5">
        <div className="flex items-center justify-between flex-wrap gap-4">
          <div className="flex items-center gap-4">
            <div className="w-14 h-14 rounded-2xl bg-gradient-to-br from-blue-500 to-indigo-600 flex items-center justify-center text-white text-2xl font-bold shadow-md">
              {vetLogado ? vetLogado.nome?.charAt(0) : '📅'}
            </div>
            <div>
              {vetLogado ? (
                <>
                  <h2 className="text-xl font-bold text-slate-800">
                    Agenda — {vetLogado.nome}
                  </h2>
                  <div className="flex items-center gap-2 mt-0.5">
                    {vetLogado.crmv && (
                      <span className="text-xs bg-blue-50 text-blue-700 px-2 py-0.5 rounded-full font-medium border border-blue-100">
                        CRMV {vetLogado.crmv}
                      </span>
                    )}
                    {vetLogado.cargoNome && (
                      <span className="text-xs text-slate-400">{vetLogado.cargoNome}</span>
                    )}
                  </div>
                </>
              ) : (
                <h2 className="text-xl font-bold text-slate-800">Agenda</h2>
              )}
              <p className="text-xs text-slate-400 mt-0.5">
                {new Date().toLocaleDateString('pt-BR', { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' })}
              </p>
            </div>
          </div>
          <button onClick={() => setModal(true)}
            className="bg-slate-900 text-white px-5 py-2.5 rounded-xl text-sm font-semibold hover:bg-slate-800 shadow-sm">
            + Novo agendamento
          </button>
        </div>

        {/* KPIs do dia */}
        <div className="grid grid-cols-4 gap-3 mt-5 pt-4 border-t border-slate-100">
          <div className="text-center">
            <div className="text-2xl font-bold text-slate-800">{agHoje.length}</div>
            <div className="text-xs text-slate-400 mt-0.5">Total hoje</div>
          </div>
          <div className="text-center">
            <div className={`text-2xl font-bold ${emAtend > 0 ? 'text-purple-600' : 'text-slate-400'}`}>{emAtend}</div>
            <div className="text-xs text-slate-400 mt-0.5">Em atendimento</div>
          </div>
          <div className="text-center">
            <div className={`text-2xl font-bold ${pendentes > 0 ? 'text-amber-500' : 'text-slate-400'}`}>{pendentes}</div>
            <div className="text-xs text-slate-400 mt-0.5">Pendentes</div>
          </div>
          <div className="text-center">
            <div className={`text-2xl font-bold ${concluidos > 0 ? 'text-emerald-600' : 'text-slate-400'}`}>{concluidos}</div>
            <div className="text-xs text-slate-400 mt-0.5">Concluídos</div>
          </div>
        </div>
      </div>

      {/* Abas */}
      <div className="flex gap-1 bg-slate-100 rounded-xl p-1 w-fit">
        {[['hoje','📍 Hoje'],['semana','📅 Próximos 7 dias']].map(([v,l]) => (
          <button key={v} onClick={() => setAbaView(v)}
            className={`px-4 py-2 rounded-lg text-sm font-medium transition ${abaView === v ? 'bg-white shadow text-slate-800' : 'text-slate-500 hover:text-slate-700'}`}>
            {l}
          </button>
        ))}
      </div>

      {loading ? (
        <div className="text-center py-16 text-slate-400">
          <div className="text-3xl mb-2 animate-pulse">📅</div>
          <p>Carregando agenda...</p>
        </div>
      ) : abaView === 'hoje' ? (

        /* ── VISÃO DIA — GRADE DE HORÁRIOS ─────────────────────── */
        <div className="bg-white rounded-2xl shadow-sm border border-slate-100 overflow-hidden">
          <div className="px-5 py-3 border-b border-slate-100 bg-slate-50 flex items-center justify-between">
            <span className="text-sm font-semibold text-slate-600">Grade do dia</span>
            <span className="text-xs text-slate-400">{agHoje.length} agendamento{agHoje.length !== 1 ? 's' : ''}</span>
          </div>
          <div className="divide-y divide-slate-50">
            {gradeHoje.map(({ hora, ag, passado }) => {
              const agora = new Date()
              const [h, m] = hora.split(':').map(Number)
              const slotDt = new Date(); slotDt.setHours(h, m, 0, 0)
              const ehAgora = Math.abs(slotDt - agora) < 30 * 60 * 1000 && slotDt >= agora

              if (ag) {
                const sc = STATUS_CONFIG[ag.status] || STATUS_CONFIG.confirmado
                return (
                  <div key={hora}
                    className={`flex items-center gap-4 px-5 py-3 transition hover:bg-slate-50 ${ag.status === 'em_atendimento' ? 'bg-purple-50' : ''} border-l-4 ${sc.borda}`}>
                    {/* Hora */}
                    <div className="w-14 text-center flex-shrink-0">
                      <div className="text-sm font-bold text-slate-700">{hora}</div>
                    </div>

                    {/* Ícone do serviço */}
                    <div className="w-12 h-12 rounded-xl bg-slate-50 border border-slate-100 flex items-center justify-center text-2xl flex-shrink-0">
                      {ag.icone || TIPO_EMOJI[ag.tipo] || '🐾'}
                    </div>

                    {/* Info */}
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center gap-2 flex-wrap">
                        <span className="font-semibold text-slate-800">{ag.petNome}</span>
                        <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${sc.cor}`}>
                          <span className={`inline-block w-1.5 h-1.5 rounded-full mr-1 ${sc.dot} align-middle`}/>
                          {sc.label}
                        </span>
                        {ag.origem === 'tutor_self_service' && (
                          <span className="text-xs bg-blue-50 text-blue-600 px-2 py-0.5 rounded-full">📱 App</span>
                        )}
                      </div>
                      <div className="text-xs text-slate-400 mt-0.5 capitalize">
                        {ag.servicoNome || ag.tipo?.replace('_',' ')}
                        {ag.duracaoMin ? ` · ${ag.duracaoMin}min` : ''}
                        {ag.obs ? ` · ${ag.obs}` : ''}
                      </div>
                    </div>

                    {/* Ações */}
                    <div className="flex gap-2 flex-shrink-0">
                      {ag.status === 'pendente' && (
                        <>
                          <button onClick={() => confirmar(ag.id)}
                            className="text-xs bg-blue-600 text-white px-3 py-1.5 rounded-lg font-medium hover:bg-blue-700">
                            Confirmar
                          </button>
                          <button onClick={() => cancelar(ag.id)}
                            className="text-xs border border-slate-200 text-slate-400 px-3 py-1.5 rounded-lg hover:bg-slate-50">
                            ✕
                          </button>
                        </>
                      )}
                      {ag.status === 'confirmado' && (
                        <>
                          <button onClick={() => iniciarAtendimento(ag)}
                            disabled={iniciando === ag.id}
                            className="text-xs bg-emerald-600 text-white px-4 py-1.5 rounded-lg font-semibold hover:bg-emerald-700 disabled:opacity-50 shadow-sm">
                            {iniciando === ag.id ? '⏳' : '▶ Iniciar'}
                          </button>
                          <button onClick={() => cancelar(ag.id)}
                            className="text-xs border border-slate-200 text-slate-400 px-2 py-1.5 rounded-lg hover:bg-red-50 hover:text-red-400 hover:border-red-200">
                            ✕
                          </button>
                        </>
                      )}
                      {ag.status === 'em_atendimento' && (
                        <button onClick={() => navigate(`/pets/${ag.petId}`)}
                          className="text-xs bg-purple-600 text-white px-4 py-1.5 rounded-lg font-semibold hover:bg-purple-700 shadow-sm">
                          👁 Ver pet
                        </button>
                      )}
                      {ag.status === 'concluido' && (
                        <button onClick={() => navigate(`/pets/${ag.petId}`)}
                          className="text-xs border border-slate-200 text-slate-400 px-3 py-1.5 rounded-lg hover:bg-slate-50">
                          Histórico
                        </button>
                      )}
                    </div>
                  </div>
                )
              }

              // Slot vago
              return (
                <div key={hora}
                  className={`flex items-center gap-4 px-5 py-2.5 ${passado ? 'opacity-30' : 'hover:bg-slate-50'} ${ehAgora ? 'bg-blue-50/50' : ''}`}>
                  <div className="w-14 text-center flex-shrink-0">
                    <div className={`text-sm font-medium ${ehAgora ? 'text-blue-600 font-bold' : passado ? 'text-slate-400' : 'text-slate-400'}`}>
                      {hora}
                      {ehAgora && <div className="text-xs text-blue-500">agora</div>}
                    </div>
                  </div>
                  <div className="w-8 text-center flex-shrink-0 text-slate-200 text-lg">·</div>
                  <div className="flex-1">
                    <span className={`text-xs ${passado ? 'text-slate-300' : 'text-slate-300'} italic`}>
                      {ehAgora ? '— disponível agora' : passado ? '—' : '— vago'}
                    </span>
                  </div>
                  {!passado && (
                    <button
                      onClick={() => {
                        const dataStr = hoje()
                        setModal({ data: dataStr, hora })
                      }}
                      className="text-xs text-slate-300 hover:text-blue-500 hover:bg-blue-50 px-2 py-1 rounded transition opacity-0 hover:opacity-100 group-hover:opacity-100">
                      + agendar
                    </button>
                  )}
                </div>
              )
            })}
          </div>
        </div>

      ) : (

        /* ── VISÃO SEMANA ─────────────────────────────────────── */
        <div className="space-y-4">
          {Object.keys(porDia).length === 0 ? (
            <div className="bg-white rounded-2xl shadow-sm p-12 text-center border border-slate-100">
              <div className="text-4xl mb-3">📭</div>
              <p className="text-slate-500 font-medium">Nenhum agendamento nos próximos 7 dias</p>
            </div>
          ) : Object.entries(porDia).map(([dia, lista]) => {
            const dt = new Date(dia)
            const ehHj = dt.toDateString() === new Date().toDateString()
            return (
              <div key={dia} className="bg-white rounded-2xl shadow-sm border border-slate-100 overflow-hidden">
                <div className={`px-5 py-3 border-b flex items-center justify-between ${ehHj ? 'bg-slate-900' : 'bg-slate-50'}`}>
                  <span className={`text-sm font-semibold ${ehHj ? 'text-white' : 'text-slate-600'}`}>
                    {ehHj ? '📍 Hoje — ' : ''}{dt.toLocaleDateString('pt-BR', { weekday: 'long', day: '2-digit', month: 'long' })}
                  </span>
                  <span className={`text-xs px-2 py-0.5 rounded-full ${ehHj ? 'bg-white/20 text-white' : 'bg-slate-200 text-slate-500'}`}>
                    {lista.length} agendamento{lista.length > 1 ? 's' : ''}
                  </span>
                </div>
                <div className="divide-y divide-slate-50">
                  {lista.sort((a,b) => new Date(a.dataHora) - new Date(b.dataHora)).map(ag => {
                    const sc = STATUS_CONFIG[ag.status] || STATUS_CONFIG.confirmado
                    return (
                      <div key={ag.id} className={`flex items-center gap-4 px-5 py-3 hover:bg-slate-50 border-l-4 ${sc.borda}`}>
                        <div className="w-14 text-center flex-shrink-0">
                          <div className="text-sm font-bold text-slate-700">{fmtHora(ag.dataHora)}</div>
                        </div>
                        <div className="w-10 h-10 rounded-xl bg-slate-50 border border-slate-100 flex items-center justify-center text-xl flex-shrink-0">{ag.icone || TIPO_EMOJI[ag.tipo] || '🐾'}</div>
                        <div className="flex-1 min-w-0">
                          <div className="flex items-center gap-2">
                            <span className="font-semibold text-slate-800">{ag.petNome}</span>
                            <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${sc.cor}`}>
                              <span className={`inline-block w-1.5 h-1.5 rounded-full mr-1 ${sc.dot} align-middle`}/>
                              {sc.label}
                            </span>
                          </div>
                          <div className="text-xs text-slate-400 mt-0.5 capitalize">{ag.servicoNome || ag.tipo?.replace('_',' ')}</div>
                        </div>
                        <div className="flex gap-2 flex-shrink-0">
                          {ag.status === 'pendente' && (
                            <button onClick={() => confirmar(ag.id)}
                              className="text-xs bg-blue-600 text-white px-3 py-1.5 rounded-lg font-medium hover:bg-blue-700">
                              Confirmar
                            </button>
                          )}
                          {ag.status === 'confirmado' && (
                            <button onClick={() => iniciarAtendimento(ag)}
                              disabled={iniciando === ag.id}
                              className="text-xs bg-emerald-600 text-white px-4 py-1.5 rounded-lg font-semibold hover:bg-emerald-700 disabled:opacity-50">
                              {iniciando === ag.id ? '⏳' : '▶ Iniciar'}
                            </button>
                          )}
                          {ag.status === 'em_atendimento' && (
                            <button onClick={() => navigate(`/pets/${ag.petId}`)}
                              className="text-xs bg-purple-600 text-white px-3 py-1.5 rounded-lg font-medium hover:bg-purple-700">
                              👁 Ver pet
                            </button>
                          )}
                        </div>
                      </div>
                    )
                  })}
                </div>
              </div>
            )
          })}
        </div>
      )}

      {modal && (
        <ModalAgendar
          dataInicial={typeof modal === 'object' ? modal.data : hoje()}
          horaInicial={typeof modal === 'object' ? modal.hora : '09:00'}
          onClose={() => setModal(false)}
          onSaved={() => { setModal(false); carregar() }}
        />
      )}
    </div>
  )
}

function ModalAgendar({ dataInicial, horaInicial, onClose, onSaved }) {
  const [pets, setPets]   = useState([])
  const [f, setF]         = useState({
    petId: '', tipo: 'consulta', servicoId: '',
    data: dataInicial || hoje(),
    hora: horaInicial || '09:00',
    duracaoMin: 60, obs: ''
  })
  const [servicos, setServicos] = useState([])
  const [saving, setSaving] = useState(false)
  const [buscaPet, setBuscaPet] = useState('')

  useEffect(() => {
    api.get('/pets', { params: { pageSize: 200 } })
      .then(r => setPets(r.data.items)).catch(() => {})
    api.get('/servicos').then(r => setServicos(r.data)).catch(() => {})
  }, [])

  function escolherServico(svcId) {
    const svc = servicos.find(s => s.id === svcId)
    setF(f => ({
      ...f,
      servicoId: svcId,
      tipo: svcId ? (svc?.categoria || 'consulta') : f.tipo,
      duracaoMin: svcId && svc?.duracaoMin ? svc.duracaoMin : f.duracaoMin
    }))
  }

  const petsFiltrados = pets.filter(p =>
    !buscaPet || p.nome.toLowerCase().includes(buscaPet.toLowerCase()) ||
    p.tutorNome?.toLowerCase().includes(buscaPet.toLowerCase())
  )

  async function salvar(e) {
    e.preventDefault()
    if (!f.petId) return alert('Selecione o pet.')
    setSaving(true)
    try {
      const dataHora = new Date(`${f.data}T${f.hora}:00`).toISOString()
      await api.post('/agendamentos', {
        petId: f.petId, tipo: f.tipo, dataHora,
        duracaoMin: +f.duracaoMin, obs: f.obs || null,
        servicoId: f.servicoId || null
      })
      onSaved()
    } catch (err) {
      alert(err.response?.data?.erro || 'Erro ao agendar.')
    } finally { setSaving(false) }
  }

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4" onClick={onClose}>
      <div className="bg-white rounded-2xl shadow-2xl p-6 w-full max-w-md" onClick={e => e.stopPropagation()}>
        <div className="flex items-center justify-between mb-5">
          <h3 className="font-bold text-lg">📅 Novo agendamento</h3>
          <button onClick={onClose} className="text-slate-400 hover:text-slate-600 text-xl leading-none">×</button>
        </div>
        <form onSubmit={salvar} className="space-y-3">
          <div>
            <label className="text-xs text-slate-500 block mb-1">Buscar pet</label>
            <input className="border rounded-xl px-3 py-2 w-full text-sm mb-1"
              placeholder="Nome do pet ou tutor..."
              value={buscaPet} onChange={e => setBuscaPet(e.target.value)} />
            <select className="border rounded-xl px-3 py-2.5 w-full text-sm" required
              value={f.petId} onChange={e => setF({...f, petId: e.target.value})}>
              <option value="">Selecione o pet *</option>
              {petsFiltrados.map(p => (
                <option key={p.id} value={p.id}>{p.nome} — {p.tutorNome}</option>
              ))}
            </select>
          </div>
          <div>
            <label className="text-xs text-slate-500 block mb-1">Serviço *</label>
            <select className="border rounded-xl px-3 py-2.5 w-full text-sm" required
              value={f.servicoId} onChange={e => escolherServico(e.target.value)}>
              <option value="">Selecione o serviço *</option>
              {servicos.map(s => (
                <option key={s.id} value={s.id}>
                  {s.icone || '🐾'} {s.nome}{s.duracaoMin ? ` (${s.duracaoMin}min)` : ''}
                </option>
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
              <select className="border rounded-xl px-3 py-2.5 w-full text-sm"
                value={f.hora} onChange={e => setF({...f, hora: e.target.value})}>
                {HORARIOS.map(h => <option key={h} value={h}>{h}</option>)}
              </select>
            </div>
          </div>
          <div>
            <label className="text-xs text-slate-500 block mb-1">Duração</label>
            <select className="border rounded-xl px-3 py-2.5 w-full text-sm"
              value={f.duracaoMin} onChange={e => setF({...f, duracaoMin: +e.target.value})}>
              {[15,30,45,60,90,120].map(d => (
                <option key={d} value={d}>{d} minutos</option>
              ))}
            </select>
          </div>
          <div>
            <label className="text-xs text-slate-500 block mb-1">Observação</label>
            <input className="border rounded-xl px-3 py-2.5 w-full text-sm" placeholder="Opcional"
              value={f.obs} onChange={e => setF({...f, obs: e.target.value})} />
          </div>
          <button disabled={saving}
            className="w-full bg-emerald-600 text-white py-3 rounded-xl text-sm font-bold disabled:opacity-50 hover:bg-emerald-700 mt-1">
            {saving ? '⏳ Agendando...' : '✓ Confirmar agendamento'}
          </button>
        </form>
      </div>
    </div>
  )
}
