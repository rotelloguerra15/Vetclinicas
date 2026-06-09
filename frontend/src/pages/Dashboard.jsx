import { useEffect, useState } from 'react'
import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, LineChart, Line, PieChart, Pie, Cell } from 'recharts'
import api from '../api/client'

const STATUS_COR = {
  confirmado:     '#3b82f6',
  pendente:       '#f59e0b',
  em_atendimento: '#8b5cf6',
  concluido:      '#10b981',
  cancelado:      '#ef4444',
  faltou:         '#94a3b8'
}

const STATUS_LABEL = {
  confirmado: 'Confirmado', pendente: 'Pendente',
  em_atendimento: 'Em atend.', concluido: 'Concluido',
  cancelado: 'Cancelado', faltou: 'Faltou'
}

function KPI({ label, valor, sub, cor, destaque }) {
  return (
    <div className={`bg-white rounded-2xl shadow p-5 border-l-4 ${cor} ${destaque ? 'ring-2 ring-red-200' : ''}`}>
      <div className="text-xs text-slate-500 uppercase tracking-wide font-semibold mb-1">{label}</div>
      <div className="text-3xl font-bold text-slate-800">{valor}</div>
      {sub && <div className="text-xs text-slate-400 mt-1">{sub}</div>}
    </div>
  )
}

function Badge({ cor, children }) {
  return <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${cor}`}>{children}</span>
}

const fmt = v => `R$ ${Number(v).toLocaleString('pt-BR', { minimumFractionDigits: 2 })}`
const fmtK = v => v >= 1000 ? `R$ ${(v/1000).toFixed(1)}k` : `R$ ${v}`

export default function Dashboard() {
  const [d, setD] = useState(null)
  const [loading, setLoading] = useState(true)
  const hora = new Date().getHours()
  const saudacao = hora < 12 ? 'Bom dia' : hora < 18 ? 'Boa tarde' : 'Boa noite'
  const nome = localStorage.getItem('nome')?.split(' ')[0] || ''

  useEffect(() => {
    api.get('/dashboard').then(r => { setD(r.data); setLoading(false) }).catch(() => setLoading(false))
  }, [])

  if (loading) return (
    <div className="flex items-center justify-center h-64">
      <div className="text-slate-400 text-sm animate-pulse">Carregando dashboard...</div>
    </div>
  )

  if (!d) return <div className="text-slate-400">Erro ao carregar dados.</div>

  const crescPos = d.crescimentoVendas >= 0

  return (
    <div className="space-y-6">

      {/* Header */}
      <div className="flex justify-between items-start">
        <div>
          <h1 className="text-2xl font-bold text-slate-800">{saudacao}, {nome}!</h1>
          <p className="text-slate-500 text-sm mt-0.5">
            {new Date().toLocaleDateString('pt-BR', { weekday: 'long', day: 'numeric', month: 'long' })}
          </p>
        </div>
        <button onClick={() => window.location.reload()}
          className="text-xs text-slate-400 hover:text-slate-600 border border-slate-200 px-3 py-1.5 rounded-lg">
          Atualizar
        </button>
      </div>

      {/* Alertas */}
      {(d.contasVencidas > 0 || d.agendamentosPendentes > 0 || d.produtosAbaixoMinimo > 0) && (
        <div className="grid grid-cols-1 md:grid-cols-3 gap-3">
          {d.contasVencidas > 0 && (
            <div className="bg-red-50 border border-red-200 rounded-xl px-4 py-3 flex items-center gap-3">
              <span className="text-red-500 text-lg">!</span>
              <div>
                <div className="text-sm font-semibold text-red-700">{d.contasVencidas} conta(s) vencida(s)</div>
                <div className="text-xs text-red-500">Verificar financeiro</div>
              </div>
            </div>
          )}
          {d.agendamentosPendentes > 0 && (
            <div className="bg-amber-50 border border-amber-200 rounded-xl px-4 py-3 flex items-center gap-3">
              <span className="text-amber-500 text-lg">!</span>
              <div>
                <div className="text-sm font-semibold text-amber-700">{d.agendamentosPendentes} agendamento(s) pendente(s)</div>
                <div className="text-xs text-amber-500">Aguardando confirmacao</div>
              </div>
            </div>
          )}
          {d.produtosAbaixoMinimo > 0 && (
            <div className="bg-orange-50 border border-orange-200 rounded-xl px-4 py-3 flex items-center gap-3">
              <span className="text-orange-500 text-lg">!</span>
              <div>
                <div className="text-sm font-semibold text-orange-700">{d.produtosAbaixoMinimo} produto(s) em falta</div>
                <div className="text-xs text-orange-500">Estoque abaixo do minimo</div>
              </div>
            </div>
          )}
        </div>
      )}

      {/* KPIs principais */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        <KPI label="Vendas do mes"
          valor={fmtK(d.vendasMes)}
          sub={`${crescPos ? '+' : ''}${d.crescimentoVendas}% vs mes anterior`}
          cor={crescPos ? 'border-emerald-500' : 'border-red-400'} />
        <KPI label="Receita liquida"
          valor={fmtK(d.lucroMes)}
          sub={`Rec: ${fmtK(d.receitaMes)} | Desp: ${fmtK(d.despesaMes)}`}
          cor="border-blue-500" />
        <KPI label="Agendamentos hoje"
          valor={d.agendamentosHoje}
          sub={d.agendamentosPendentes > 0 ? `${d.agendamentosPendentes} pendente(s)` : 'Todos confirmados'}
          cor="border-purple-500" />
        <KPI label="OS em aberto"
          valor={d.osAbertas}
          sub="Ordens de servico ativas"
          cor="border-amber-500" />
      </div>

      {/* Graficos */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-5">

        {/* Vendas 6 meses */}
        <div className="md:col-span-2 bg-white rounded-2xl shadow p-5">
          <h3 className="font-semibold text-slate-700 mb-4">Vendas — ultimos 6 meses</h3>
          <ResponsiveContainer width="100%" height={200}>
            <BarChart data={d.vendasPorMes} margin={{ top: 0, right: 10, left: 0, bottom: 0 }}>
              <CartesianGrid strokeDasharray="3 3" stroke="#f1f5f9" />
              <XAxis dataKey="mes" tick={{ fontSize: 11 }} />
              <YAxis tickFormatter={fmtK} tick={{ fontSize: 11 }} width={60} />
              <Tooltip formatter={v => fmt(v)} />
              <Bar dataKey="total" fill="#3b82f6" radius={[4,4,0,0]} name="Vendas" />
            </BarChart>
          </ResponsiveContainer>
        </div>

        {/* Agendamentos por status */}
        <div className="bg-white rounded-2xl shadow p-5">
          <h3 className="font-semibold text-slate-700 mb-4">Agenda hoje</h3>
          {d.agsPorStatus.length === 0 ? (
            <div className="flex items-center justify-center h-40 text-slate-400 text-sm">
              Sem agendamentos hoje
            </div>
          ) : (
            <>
              <ResponsiveContainer width="100%" height={160}>
                <PieChart>
                  <Pie data={d.agsPorStatus} dataKey="total" nameKey="status"
                    cx="50%" cy="50%" outerRadius={60} innerRadius={35}>
                    {d.agsPorStatus.map((entry, i) => (
                      <Cell key={i} fill={STATUS_COR[entry.status] || '#94a3b8'} />
                    ))}
                  </Pie>
                  <Tooltip formatter={(v, n) => [v, STATUS_LABEL[n] || n]} />
                </PieChart>
              </ResponsiveContainer>
              <div className="flex flex-wrap gap-2 justify-center mt-2">
                {d.agsPorStatus.map((s, i) => (
                  <span key={i} className="text-xs flex items-center gap-1">
                    <span className="w-2 h-2 rounded-full inline-block"
                      style={{ background: STATUS_COR[s.status] || '#94a3b8' }} />
                    {STATUS_LABEL[s.status] || s.status}: {s.total}
                  </span>
                ))}
              </div>
            </>
          )}
        </div>
      </div>

      {/* Linha inferior */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-5">

        {/* Proximos agendamentos */}
        <div className="md:col-span-2 bg-white rounded-2xl shadow p-5">
          <h3 className="font-semibold text-slate-700 mb-3">Proximos agendamentos</h3>
          {d.proximosAgs.length === 0 ? (
            <p className="text-slate-400 text-sm">Nenhum agendamento futuro</p>
          ) : (
            <div className="space-y-2">
              {d.proximosAgs.map(a => (
                <div key={a.id} className="flex items-center justify-between py-2 border-b last:border-0">
                  <div className="flex items-center gap-3">
                    <div className="w-10 h-10 bg-blue-50 rounded-xl flex items-center justify-center text-blue-600 font-bold text-sm">
                      {new Date(a.dataHora).toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' })}
                    </div>
                    <div>
                      <div className="font-medium text-sm">{a.petNome}</div>
                      <div className="text-xs text-slate-400">{a.tipo}</div>
                    </div>
                  </div>
                  <Badge cor={`${STATUS_COR[a.status] ? '' : 'bg-slate-100 text-slate-600'}`}
                    style={{ background: STATUS_COR[a.status] + '20', color: STATUS_COR[a.status] }}>
                    {STATUS_LABEL[a.status] || a.status}
                  </Badge>
                </div>
              ))}
            </div>
          )}
        </div>

        {/* Indicadores rapidos */}
        <div className="bg-white rounded-2xl shadow p-5 space-y-4">
          <h3 className="font-semibold text-slate-700">Indicadores</h3>

          {[
            { label: 'Total de pets', valor: d.totalPets, max: null, cor: 'bg-blue-500' },
            { label: 'Tutores ativos', valor: d.totalTutores, max: null, cor: 'bg-purple-500' },
            { label: 'Vacinas vencendo', valor: d.vacinasVencendo, max: null,
              cor: d.vacinasVencendo > 0 ? 'bg-amber-500' : 'bg-emerald-500' },
            { label: 'Contas a vencer (7d)', valor: d.contasVencer, max: null,
              cor: d.contasVencer > 0 ? 'bg-orange-500' : 'bg-emerald-500' },
          ].map((item, i) => (
            <div key={i} className="flex items-center justify-between">
              <span className="text-sm text-slate-600">{item.label}</span>
              <div className="flex items-center gap-2">
                <span className={`w-2 h-2 rounded-full ${item.cor}`} />
                <span className="font-bold text-slate-800">{item.valor}</span>
              </div>
            </div>
          ))}

          <div className="pt-2 border-t">
            <div className="text-xs text-slate-400 mb-2">Resultado do mes</div>
            <div className={`text-xl font-bold ${d.lucroMes >= 0 ? 'text-emerald-600' : 'text-red-600'}`}>
              {fmt(d.lucroMes)}
            </div>
            <div className="text-xs text-slate-400">
              {d.lucroMes >= 0 ? 'Superavit' : 'Deficit'} em {new Date().toLocaleDateString('pt-BR', { month: 'long' })}
            </div>
          </div>
        </div>
      </div>
    </div>
  )
}
