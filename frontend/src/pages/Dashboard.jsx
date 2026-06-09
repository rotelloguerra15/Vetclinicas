import { useEffect, useState, useCallback } from 'react'
import {
  BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer,
  PieChart, Pie, Cell, ComposedChart, Line, Legend
} from 'recharts'
import api from '../api/client'

// ── Constantes ────────────────────────────────────────────────────────────────
const MESES = ['Janeiro','Fevereiro','Marco','Abril','Maio','Junho',
               'Julho','Agosto','Setembro','Outubro','Novembro','Dezembro']

const STATUS_COR = {
  confirmado:'#3b82f6', pendente:'#f59e0b', em_atendimento:'#8b5cf6',
  concluido:'#10b981', cancelado:'#ef4444', faltou:'#94a3b8'
}
const STATUS_LABEL = {
  confirmado:'Confirmado', pendente:'Pendente', em_atendimento:'Em atend.',
  concluido:'Concluido', cancelado:'Cancelado', faltou:'Faltou'
}

const fmt  = v => `R$ ${Number(v).toLocaleString('pt-BR', { minimumFractionDigits: 2 })}`
const fmtK = v => v >= 1000 ? `R$ ${(v/1000).toFixed(1)}k` : `R$ ${v}`

// ── Componentes auxiliares ────────────────────────────────────────────────────
function KPI({ label, valor, sub, bordaCor, destaque }) {
  return (
    <div className={`bg-white rounded-2xl shadow p-5 border-l-4 ${bordaCor} ${destaque ? 'ring-2 ring-red-200' : ''}`}>
      <div className="text-xs text-slate-500 uppercase tracking-wide font-semibold mb-1">{label}</div>
      <div className="text-3xl font-bold text-slate-800">{valor}</div>
      {sub && <div className="text-xs text-slate-400 mt-1">{sub}</div>}
    </div>
  )
}

function Alerta({ tipo, msg, sub }) {
  const cores = {
    erro:    'bg-red-50 border-red-200 text-red-700',
    aviso:   'bg-amber-50 border-amber-200 text-amber-700',
    info:    'bg-orange-50 border-orange-200 text-orange-700',
    sucesso: 'bg-emerald-50 border-emerald-200 text-emerald-700',
  }
  return (
    <div className={`border rounded-xl px-4 py-3 flex items-start gap-3 ${cores[tipo]}`}>
      <span className="text-lg mt-0.5">
        {tipo === 'erro' ? '!' : tipo === 'aviso' ? '~' : tipo === 'sucesso' ? 'v' : 'i'}
      </span>
      <div>
        <div className="text-sm font-semibold">{msg}</div>
        {sub && <div className="text-xs opacity-75 mt-0.5">{sub}</div>}
      </div>
    </div>
  )
}

// Tooltip customizado para o grafico de metas
function TooltipMetas({ active, payload }) {
  if (!active || !payload?.length) return null
  const d = payload[0]?.payload
  return (
    <div className="bg-white border border-slate-200 rounded-xl shadow-lg p-3 text-sm">
      <div className="font-semibold text-slate-700 mb-1">{d?.mes}</div>
      <div className="text-blue-600">Meta: {fmt(d?.meta || 0)}</div>
      <div className="text-emerald-600">Realizado: {fmt(d?.realizado || 0)}</div>
      <div className={`font-bold mt-1 ${d?.percentual >= 100 ? 'text-emerald-600' : d?.percentual >= 70 ? 'text-amber-600' : 'text-red-500'}`}>
        {d?.percentual}% da meta
      </div>
    </div>
  )
}

// Card de aniversariante animado
function CardAniversariante({ nome, tipo, especie, tutorNome, telefone }) {
  const isPet = tipo === 'pet'
  const emoji = isPet ? (especie === 'gato' ? 'CAT' : 'DOG') : 'HUM'
  const avatarCor = isPet
    ? especie === 'gato' ? 'bg-purple-100 text-purple-600' : 'bg-amber-100 text-amber-600'
    : 'bg-blue-100 text-blue-600'

  return (
    <div className="flex items-center gap-3 p-3 bg-gradient-to-r from-pink-50 to-purple-50 rounded-xl border border-pink-100 hover:shadow-md transition-shadow">
      <div className={`w-12 h-12 rounded-xl flex items-center justify-center text-2xl font-bold flex-shrink-0 ${avatarCor}`}>
        {isPet
          ? especie === 'gato' ? '🐱' : '🐶'
          : '🎂'
        }
      </div>
      <div className="min-w-0">
        <div className="font-semibold text-slate-800 truncate">{nome}</div>
        <div className="text-xs text-slate-500">
          {isPet ? `Pet de ${tutorNome}` : 'Tutor(a)'}
          {telefone && ` · ${telefone}`}
        </div>
      </div>
      <div className="ml-auto text-pink-400 text-xl flex-shrink-0">🎉</div>
    </div>
  )
}

// ── Componente principal ──────────────────────────────────────────────────────
export default function Dashboard() {
  const hoje = new Date()
  const [anoSel, setAnoSel]       = useState(hoje.getFullYear())
  const [mesSel, setMesSel]       = useState(hoje.getMonth() + 1)
  const [d, setD]                 = useState(null)
  const [loading, setLoading]     = useState(true)
  const [ultimaAtt, setUltimaAtt] = useState(null)
  const [editandoMeta, setEditandoMeta] = useState(false)
  const [novaMeta, setNovaMeta]   = useState('')
  const [salvandoMeta, setSalvandoMeta] = useState(false)

  const saudacao = () => {
    const h = new Date().getHours()
    return h < 12 ? 'Bom dia' : h < 18 ? 'Boa tarde' : 'Boa noite'
  }
  const nome = localStorage.getItem('nome')?.split(' ')[0] || ''

  const carregar = useCallback(() => {
    setLoading(true)
    api.get('/dashboard', { params: { ano: anoSel, mes: mesSel } })
      .then(r => {
        setD(r.data)
        setNovaMeta(r.data.metaMes?.toString() || '0')
        setUltimaAtt(new Date())
        setLoading(false)
      })
      .catch(() => setLoading(false))
  }, [anoSel, mesSel])

  // Auto-refresh a cada 5 minutos
  useEffect(() => {
    carregar()
    const interval = setInterval(carregar, 5 * 60 * 1000)
    return () => clearInterval(interval)
  }, [carregar])

  async function salvarMeta() {
    setSalvandoMeta(true)
    try {
      await api.put('/dashboard/meta', { ano: anoSel, mes: mesSel, valorMeta: parseFloat(novaMeta) || 0 })
      carregar()
      setEditandoMeta(false)
    } catch { alert('Erro ao salvar meta.') }
    finally { setSalvandoMeta(false) }
  }

  // Anos para seletor
  const anos = [hoje.getFullYear() - 1, hoje.getFullYear()]

  // ── Render ──────────────────────────────────────────────────────────────────
  return (
    <div className="space-y-5 pb-8">

      {/* Header com seletor de periodo */}
      <div className="flex flex-wrap justify-between items-start gap-3">
        <div>
          <h1 className="text-2xl font-bold text-slate-800">{saudacao()}, {nome}!</h1>
          <p className="text-slate-500 text-sm mt-0.5">
            {hoje.toLocaleDateString('pt-BR', { weekday:'long', day:'numeric', month:'long', year:'numeric' })}
          </p>
        </div>

        <div className="flex items-center gap-2 flex-wrap">
          {/* Seletor mes/ano */}
          <select value={mesSel} onChange={e => setMesSel(+e.target.value)}
            className="border rounded-lg px-3 py-1.5 text-sm bg-white shadow-sm">
            {MESES.map((m, i) => <option key={i} value={i+1}>{m}</option>)}
          </select>
          <select value={anoSel} onChange={e => setAnoSel(+e.target.value)}
            className="border rounded-lg px-3 py-1.5 text-sm bg-white shadow-sm">
            {anos.map(a => <option key={a} value={a}>{a}</option>)}
          </select>

          {/* Ultima atualizacao + botao refresh */}
          <div className="flex items-center gap-1.5">
            {ultimaAtt && (
              <span className="text-xs text-slate-400">
                Att: {ultimaAtt.toLocaleTimeString('pt-BR', { hour:'2-digit', minute:'2-digit' })}
              </span>
            )}
            <button onClick={carregar} disabled={loading}
              className={`border rounded-lg p-1.5 text-slate-500 hover:bg-slate-100 transition ${loading ? 'animate-spin' : ''}`}>
              <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2}
                  d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
              </svg>
            </button>
          </div>
        </div>
      </div>

      {loading && !d && (
        <div className="flex items-center justify-center h-40">
          <div className="text-slate-400 animate-pulse">Carregando...</div>
        </div>
      )}

      {d && (
        <>
          {/* Alertas */}
          {(d.contasVencidas > 0 || d.agendamentosPendentes > 0 || d.produtosAbaixoMinimo > 0 || d.totalAniversariantes > 0) && (
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-3">
              {d.contasVencidas > 0 && <Alerta tipo="erro" msg={`${d.contasVencidas} conta(s) vencida(s)`} sub="Verificar financeiro" />}
              {d.agendamentosPendentes > 0 && <Alerta tipo="aviso" msg={`${d.agendamentosPendentes} agendamento(s) pendente(s)`} sub="Aguardando confirmacao" />}
              {d.produtosAbaixoMinimo > 0 && <Alerta tipo="info" msg={`${d.produtosAbaixoMinimo} produto(s) em falta`} sub="Estoque abaixo do minimo" />}
              {d.totalAniversariantes > 0 && <Alerta tipo="sucesso" msg={`${d.totalAniversariantes} aniversariante(s) hoje!`} sub="Pets e tutores" />}
            </div>
          )}

          {/* KPIs */}
          <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
            <KPI label="Vendas do mes"
              valor={fmtK(d.vendasMes)}
              sub={`${d.crescimentoVendas >= 0 ? '+' : ''}${d.crescimentoVendas}% vs mes anterior`}
              bordaCor={d.crescimentoVendas >= 0 ? 'border-emerald-500' : 'border-red-400'} />
            <KPI label="Meta do mes"
              valor={`${d.percentualMeta}%`}
              sub={`${fmtK(d.vendasMes)} de ${fmtK(d.metaMes)}`}
              bordaCor={d.percentualMeta >= 100 ? 'border-emerald-500' : d.percentualMeta >= 70 ? 'border-amber-500' : 'border-red-400'} />
            <KPI label="Agendamentos hoje"
              valor={d.agendamentosHoje}
              sub={d.agendamentosPendentes > 0 ? `${d.agendamentosPendentes} pendente(s)` : 'Todos confirmados'}
              bordaCor="border-purple-500" />
            <KPI label="Resultado do mes"
              valor={fmtK(d.lucroMes)}
              sub={d.lucroMes >= 0 ? 'Superavit' : 'Deficit'}
              bordaCor={d.lucroMes >= 0 ? 'border-blue-500' : 'border-red-400'} />
          </div>

          {/* Graficos linha 1 */}
          <div className="grid grid-cols-1 lg:grid-cols-3 gap-5">

            {/* Metas x Realizado */}
            <div className="lg:col-span-2 bg-white rounded-2xl shadow p-5">
              <div className="flex justify-between items-center mb-4">
                <h3 className="font-semibold text-slate-700">Metas x Realizado</h3>
                {editandoMeta ? (
                  <div className="flex items-center gap-2">
                    <span className="text-xs text-slate-500">Meta {MESES[mesSel-1]}:</span>
                    <input type="number" step="100" className="border rounded-lg px-2 py-1 text-sm w-32"
                      value={novaMeta} onChange={e => setNovaMeta(e.target.value)} />
                    <button onClick={salvarMeta} disabled={salvandoMeta}
                      className="bg-emerald-600 text-white text-xs px-3 py-1 rounded-lg disabled:opacity-40">
                      Salvar
                    </button>
                    <button onClick={() => setEditandoMeta(false)}
                      className="text-xs text-slate-400 hover:underline">Cancelar</button>
                  </div>
                ) : (
                  <button onClick={() => setEditandoMeta(true)}
                    className="text-xs text-blue-600 hover:underline border border-blue-200 px-3 py-1 rounded-lg">
                    Definir meta
                  </button>
                )}
              </div>
              <ResponsiveContainer width="100%" height={220}>
                <ComposedChart data={d.metasRealizado} margin={{ top: 5, right: 10, left: 0, bottom: 0 }}>
                  <CartesianGrid strokeDasharray="3 3" stroke="#f1f5f9" />
                  <XAxis dataKey="mes" tick={{ fontSize: 11 }} />
                  <YAxis yAxisId="left" tickFormatter={fmtK} tick={{ fontSize: 11 }} width={65} />
                  <YAxis yAxisId="right" orientation="right" tickFormatter={v => `${v}%`}
                    tick={{ fontSize: 11 }} width={45} domain={[0, 150]} />
                  <Tooltip content={<TooltipMetas />} />
                  <Legend iconSize={10} wrapperStyle={{ fontSize: 11 }} />
                  <Bar yAxisId="left" dataKey="meta" fill="#e2e8f0" radius={[4,4,0,0]} name="Meta" />
                  <Bar yAxisId="left" dataKey="realizado" radius={[4,4,0,0]} name="Realizado"
                    fill="#3b82f6">
                    {d.metasRealizado.map((entry, i) => (
                      <Cell key={i}
                        fill={entry.percentual >= 100 ? '#10b981' : entry.percentual >= 70 ? '#f59e0b' : '#3b82f6'} />
                    ))}
                  </Bar>
                  <Line yAxisId="right" type="monotone" dataKey="percentual"
                    stroke="#8b5cf6" strokeWidth={2} dot={{ r: 4 }} name="% Meta" />
                </ComposedChart>
              </ResponsiveContainer>
              <div className="flex gap-4 mt-2 text-xs text-slate-400 justify-center">
                <span className="flex items-center gap-1"><span className="w-3 h-3 rounded bg-emerald-500 inline-block" /> Acima 100%</span>
                <span className="flex items-center gap-1"><span className="w-3 h-3 rounded bg-amber-500 inline-block" /> 70-100%</span>
                <span className="flex items-center gap-1"><span className="w-3 h-3 rounded bg-blue-500 inline-block" /> Abaixo 70%</span>
              </div>
            </div>

            {/* Agenda hoje (pizza) */}
            <div className="bg-white rounded-2xl shadow p-5">
              <h3 className="font-semibold text-slate-700 mb-4">Agenda hoje</h3>
              {d.agsPorStatus.length === 0 ? (
                <div className="flex flex-col items-center justify-center h-40 text-slate-300">
                  <div className="text-4xl mb-2">📅</div>
                  <div className="text-sm">Sem agendamentos</div>
                </div>
              ) : (
                <>
                  <ResponsiveContainer width="100%" height={160}>
                    <PieChart>
                      <Pie data={d.agsPorStatus} dataKey="total" nameKey="status"
                        cx="50%" cy="50%" outerRadius={65} innerRadius={40}
                        paddingAngle={3}>
                        {d.agsPorStatus.map((entry, i) => (
                          <Cell key={i} fill={STATUS_COR[entry.status] || '#94a3b8'} />
                        ))}
                      </Pie>
                      <Tooltip formatter={(v, n) => [v, STATUS_LABEL[n] || n]} />
                    </PieChart>
                  </ResponsiveContainer>
                  <div className="flex flex-wrap gap-x-3 gap-y-1 justify-center mt-1">
                    {d.agsPorStatus.map((s, i) => (
                      <span key={i} className="text-xs flex items-center gap-1 text-slate-600">
                        <span className="w-2 h-2 rounded-full"
                          style={{ background: STATUS_COR[s.status] || '#94a3b8', display:'inline-block' }} />
                        {STATUS_LABEL[s.status] || s.status}: <b>{s.total}</b>
                      </span>
                    ))}
                  </div>
                </>
              )}
            </div>
          </div>

          {/* Linha 2: proximos agendamentos + aniversariantes */}
          <div className="grid grid-cols-1 lg:grid-cols-3 gap-5">

            {/* Proximos agendamentos */}
            <div className="lg:col-span-2 bg-white rounded-2xl shadow p-5">
              <h3 className="font-semibold text-slate-700 mb-3">Proximos agendamentos</h3>
              {d.proximosAgs.length === 0 ? (
                <div className="flex flex-col items-center justify-center h-28 text-slate-300">
                  <div className="text-3xl mb-1">🗓</div>
                  <div className="text-sm">Nenhum agendamento futuro</div>
                </div>
              ) : (
                <div className="space-y-2">
                  {d.proximosAgs.map(a => (
                    <div key={a.id} className="flex items-center gap-3 py-2 border-b last:border-0">
                      <div className="w-14 h-10 bg-slate-100 rounded-xl flex flex-col items-center justify-center flex-shrink-0">
                        <span className="text-xs font-bold text-slate-700 leading-none">
                          {new Date(a.dataHora).toLocaleTimeString('pt-BR', { hour:'2-digit', minute:'2-digit' })}
                        </span>
                        <span className="text-[10px] text-slate-400">
                          {new Date(a.dataHora).toLocaleDateString('pt-BR', { day:'2-digit', month:'2-digit' })}
                        </span>
                      </div>
                      <div className="flex-1 min-w-0">
                        <div className="font-medium text-sm truncate">{a.petNome}</div>
                        <div className="text-xs text-slate-400">{a.tipo}</div>
                      </div>
                      <span className="text-xs px-2 py-0.5 rounded-full font-medium flex-shrink-0"
                        style={{
                          background: (STATUS_COR[a.status] || '#94a3b8') + '20',
                          color: STATUS_COR[a.status] || '#94a3b8'
                        }}>
                        {STATUS_LABEL[a.status] || a.status}
                      </span>
                    </div>
                  ))}
                </div>
              )}
            </div>

            {/* Indicadores + OS */}
            <div className="bg-white rounded-2xl shadow p-5 space-y-3">
              <h3 className="font-semibold text-slate-700">Indicadores</h3>
              {[
                { label:'Pets ativos',          valor: d.totalPets,           cor:'bg-blue-400' },
                { label:'Tutores ativos',        valor: d.totalTutores,         cor:'bg-purple-400' },
                { label:'OS em aberto',          valor: d.osAbertas,            cor: d.osAbertas > 0 ? 'bg-amber-400' : 'bg-emerald-400' },
                { label:'Vacinas vencendo (30d)',valor: d.vacinasVencendo,      cor: d.vacinasVencendo > 0 ? 'bg-orange-400' : 'bg-emerald-400' },
                { label:'Contas a vencer (7d)',  valor: d.contasVencer,         cor: d.contasVencer > 0 ? 'bg-orange-400' : 'bg-emerald-400' },
                { label:'Contas vencidas',       valor: d.contasVencidas,       cor: d.contasVencidas > 0 ? 'bg-red-500' : 'bg-emerald-400' },
              ].map((item, i) => (
                <div key={i} className="flex items-center justify-between py-1 border-b last:border-0">
                  <span className="text-sm text-slate-600">{item.label}</span>
                  <div className="flex items-center gap-2">
                    <span className={`w-2 h-2 rounded-full ${item.cor}`} />
                    <span className="font-bold text-slate-800">{item.valor}</span>
                  </div>
                </div>
              ))}
            </div>
          </div>

          {/* Aniversariantes */}
          {(d.aniversariantesPets?.length > 0 || d.aniversariantesTutores?.length > 0) && (
            <div className="bg-gradient-to-br from-pink-50 via-white to-purple-50 rounded-2xl shadow p-5 border border-pink-100">
              <div className="flex items-center gap-2 mb-4">
                <span className="text-2xl">🎂</span>
                <h3 className="font-semibold text-slate-700 text-lg">Aniversariantes de hoje!</h3>
                <span className="ml-auto bg-pink-100 text-pink-600 text-xs font-bold px-3 py-1 rounded-full">
                  {d.totalAniversariantes} hoje
                </span>
              </div>
              <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3">
                {d.aniversariantesPets?.map(p => (
                  <CardAniversariante key={p.id} nome={p.nome} tipo="pet"
                    especie={p.especie} tutorNome={p.tutorNome} telefone={p.tutorFone} />
                ))}
                {d.aniversariantesTutores?.map(t => (
                  <CardAniversariante key={t.id} nome={t.nome} tipo="tutor" telefone={t.telefone} />
                ))}
              </div>
            </div>
          )}
        </>
      )}
    </div>
  )
}
