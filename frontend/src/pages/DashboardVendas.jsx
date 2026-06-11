import { useEffect, useState, useCallback } from 'react'
import {
  ComposedChart, BarChart, Bar, LineChart, Line,
  XAxis, YAxis, CartesianGrid, Tooltip, Legend,
  ResponsiveContainer, Cell, PieChart, Pie
} from 'recharts'
import api from '../api/client'

const MESES = ['Jan','Fev','Mar','Abr','Mai','Jun','Jul','Ago','Set','Out','Nov','Dez']
const CORES_PROD = ['#6366f1','#8b5cf6','#a855f7','#c026d3','#db2777','#e11d48','#f97316','#f59e0b']
const CORES_SVC  = ['#06b6d4','#0891b2','#0e7490','#155e75','#0f766e','#047857','#065f46','#1e3a5f']

function fmt(v) {
  return Number(v ?? 0).toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
}
function fmtK(v) {
  if (v >= 1000) return `R$${(v/1000).toFixed(1)}k`
  return `R$${Number(v ?? 0).toFixed(0)}`
}
function hoje() { return new Date() }

function TooltipCustom({ active, payload, label }) {
  if (!active || !payload?.length) return null
  return (
    <div style={{ background: '#1e293b', border: '1px solid #334155', borderRadius: 10, padding: '10px 14px' }}>
      <p style={{ color: '#94a3b8', fontSize: 12, marginBottom: 6 }}>{label}</p>
      {payload.map((p, i) => (
        <p key={i} style={{ color: p.color, fontSize: 13, fontWeight: 600 }}>
          {p.name}: {typeof p.value === 'number' && p.value > 100 ? fmt(p.value) : p.value}
        </p>
      ))}
    </div>
  )
}

function KpiCard({ label, value, sub, color, icon }) {
  return (
    <div style={{
      background: 'white', borderRadius: 16, padding: '16px 20px',
      boxShadow: '0 1px 3px rgba(0,0,0,0.08)', border: '1px solid #f1f5f9',
      position: 'relative', overflow: 'hidden'
    }}>
      <div style={{ position: 'absolute', right: 12, top: 12, fontSize: 28, opacity: 0.15 }}>{icon}</div>
      <div style={{ fontSize: 12, color: '#64748b', fontWeight: 500, marginBottom: 4 }}>{label}</div>
      <div style={{ fontSize: 24, fontWeight: 700, color: color || '#1e293b' }}>{value}</div>
      {sub && <div style={{ fontSize: 12, color: '#94a3b8', marginTop: 4 }}>{sub}</div>}
    </div>
  )
}

function RankingBar({ posicao, nome, valor, total, cor, qtd }) {
  const pct = total > 0 ? (valor / total) * 100 : 0
  return (
    <div style={{ marginBottom: 12 }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 4, alignItems: 'center' }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 8, minWidth: 0, flex: 1 }}>
          <span style={{ width: 24, height: 24, borderRadius: 6, background: cor, color: 'white', fontSize: 11, fontWeight: 700, display: 'flex', alignItems: 'center', justifyContent: 'center', flexShrink: 0 }}>
            {posicao}
          </span>
          <span style={{ fontSize: 13, color: '#334155', fontWeight: 500, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>{nome}</span>
        </div>
        <div style={{ textAlign: 'right', flexShrink: 0, marginLeft: 8 }}>
          <div style={{ fontSize: 13, fontWeight: 700, color: cor }}>{fmt(valor)}</div>
          {qtd && <div style={{ fontSize: 11, color: '#94a3b8' }}>{qtd}x</div>}
        </div>
      </div>
      <div style={{ height: 6, background: '#f1f5f9', borderRadius: 3, overflow: 'hidden' }}>
        <div style={{ height: '100%', borderRadius: 3, width: `${pct}%`, background: cor, transition: 'width 0.8s ease' }} />
      </div>
    </div>
  )
}

export default function DashboardVendas() {
  const now = hoje()
  const [ano, setAno]   = useState(now.getFullYear())
  const [mes, setMes]   = useState(now.getMonth() + 1)
  const [loading, setLoading] = useState(true)

  const [resumo, setResumo]         = useState(null)
  const [diasData, setDiasData]     = useState([])
  const [topServicos, setTopServicos] = useState([])
  const [topProdutos, setTopProdutos] = useState([])

  const carregar = useCallback(() => {
    setLoading(true)
    Promise.all([
      api.get('/relatorios/mensal',         { params: { ano, mes } }),
      api.get('/relatorios/vendas-por-dia', { params: { ano, mes } }),
      api.get('/relatorios/top-servicos',   { params: { ano, mes, top: 10 } }),
      api.get('/relatorios/top-produtos',   { params: { ano, mes, top: 10 } }),
    ]).then(([r1, r2, r3, r4]) => {
      setResumo(r1.data)
      setDiasData(r2.data)
      setTopServicos(r3.data)
      setTopProdutos(r4.data)
    }).catch(() => {}).finally(() => setLoading(false))
  }, [ano, mes])

  useEffect(() => { carregar() }, [carregar])

  const totalServicos = topServicos.reduce((s, x) => s + x.total, 0)
  const totalProdutos = topProdutos.reduce((s, x) => s + x.total, 0)

  // Melhor dia do mês
  const melhorDia = diasData.reduce((best, d) => d.total > (best?.total ?? 0) ? d : best, null)

  // Dias sem venda
  const diasSemVenda = diasData.filter(d => d.total === 0).length

  return (
    <div style={{ maxWidth: 1100, margin: '0 auto' }}>

      {/* Header */}
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 24, flexWrap: 'wrap', gap: 12 }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
          <div style={{ fontSize: 36 }}>🛒</div>
          <div>
            <h2 style={{ fontSize: 22, fontWeight: 700, margin: 0 }}>Dashboard de Vendas</h2>
            <p style={{ fontSize: 13, color: '#64748b', margin: 0 }}>Produtos, serviços e faturamento do mês</p>
          </div>
        </div>
        <div style={{ display: 'flex', gap: 8 }}>
          <select className="border rounded-lg px-3 py-2 text-sm" value={mes} onChange={e => setMes(+e.target.value)}>
            {MESES.map((m, i) => <option key={i} value={i+1}>{m}</option>)}
          </select>
          <select className="border rounded-lg px-3 py-2 text-sm" value={ano} onChange={e => setAno(+e.target.value)}>
            {[2024,2025,2026,2027].map(a => <option key={a} value={a}>{a}</option>)}
          </select>
          <button onClick={carregar}
            className="bg-slate-900 text-white px-4 py-2 rounded-lg text-sm hover:bg-slate-800">
            Atualizar
          </button>
        </div>
      </div>

      {loading ? (
        <div style={{ textAlign: 'center', padding: 60, color: '#94a3b8' }}>
          <div style={{ fontSize: 36, marginBottom: 12 }}>⏳</div>
          <p>Carregando dados...</p>
        </div>
      ) : (
        <div style={{ display: 'flex', flexDirection: 'column', gap: 20 }}>

          {/* KPIs */}
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(160px, 1fr))', gap: 12 }}>
            <KpiCard label="Faturamento total" value={fmt(resumo?.receitas ?? 0)} color="#10b981" icon="💰" />
            <KpiCard label="Vendas PDV" value={fmt(totalProdutos)} color="#6366f1" icon="🛍️"
              sub={`${topProdutos.reduce((s,x) => s + x.quantidade, 0)} itens vendidos`} />
            <KpiCard label="Serviços" value={fmt(totalServicos)} color="#06b6d4" icon="🩺"
              sub={`${topServicos.reduce((s,x) => s + x.quantidade, 0)} atendimentos`} />
            <KpiCard label="Melhor dia" color="#f59e0b" icon="🏆"
              value={melhorDia ? new Date(melhorDia.data + 'T12:00').toLocaleDateString('pt-BR', { day: '2-digit', month: '2-digit' }) : '—'}
              sub={melhorDia ? fmt(melhorDia.total) : ''} />
            <KpiCard label="Dias sem venda" value={diasSemVenda} color={diasSemVenda > 5 ? '#ef4444' : '#94a3b8'} icon="📭"
              sub={`de ${new Date(ano, mes, 0).getDate()} dias no mês`} />
          </div>

          {/* Gráfico dia a dia */}
          <div style={{ background: 'white', borderRadius: 16, padding: '20px 20px 12px', boxShadow: '0 1px 3px rgba(0,0,0,0.07)' }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 16 }}>
              <span style={{ fontSize: 20 }}>📅</span>
              <h3 style={{ margin: 0, fontSize: 15, fontWeight: 700 }}>
                Faturamento dia a dia — {MESES[mes-1]} {ano}
              </h3>
            </div>
            <ResponsiveContainer width="100%" height={220}>
              <ComposedChart data={diasData} margin={{ top: 4, right: 8, left: 0, bottom: 0 }}>
                <CartesianGrid strokeDasharray="3 3" stroke="#f1f5f9" />
                <XAxis dataKey="data"
                  tickFormatter={d => new Date(d + 'T12:00').getDate()}
                  tick={{ fontSize: 10 }} />
                <YAxis tickFormatter={fmtK} tick={{ fontSize: 10 }} />
                <Tooltip content={<TooltipCustom />} />
                <Legend />
                <Bar dataKey="servicos" name="Serviços" stackId="a" fill="#06b6d4" radius={[0,0,0,0]} />
                <Bar dataKey="vendas"   name="Produtos PDV" stackId="a" fill="#6366f1" radius={[3,3,0,0]} />
                <Line dataKey="total" name="Total" stroke="#f59e0b" strokeWidth={2} dot={false} />
              </ComposedChart>
            </ResponsiveContainer>
          </div>

          {/* Rankings lado a lado */}
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 20 }}>

            {/* Top Serviços */}
            <div style={{ background: 'white', borderRadius: 16, padding: 20, boxShadow: '0 1px 3px rgba(0,0,0,0.07)' }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 16 }}>
                <span style={{ fontSize: 20 }}>🩺</span>
                <h3 style={{ margin: 0, fontSize: 15, fontWeight: 700 }}>Serviços mais executados</h3>
              </div>
              {topServicos.length === 0 ? (
                <p style={{ textAlign: 'center', color: '#94a3b8', fontSize: 13, padding: '20px 0' }}>Sem dados no período</p>
              ) : (
                <>
                  {topServicos.map((s, i) => (
                    <RankingBar key={i} posicao={i+1} nome={s.nome}
                      valor={s.total} total={totalServicos}
                      cor={CORES_SVC[i % CORES_SVC.length]}
                      qtd={s.quantidade} />
                  ))}
                  <div style={{ borderTop: '1px solid #f1f5f9', paddingTop: 8, marginTop: 4, display: 'flex', justifyContent: 'space-between', fontSize: 12, color: '#64748b' }}>
                    <span>Total</span>
                    <span style={{ fontWeight: 700, color: '#06b6d4' }}>{fmt(totalServicos)}</span>
                  </div>
                </>
              )}
            </div>

            {/* Top Produtos */}
            <div style={{ background: 'white', borderRadius: 16, padding: 20, boxShadow: '0 1px 3px rgba(0,0,0,0.07)' }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 16 }}>
                <span style={{ fontSize: 20 }}>🛍️</span>
                <h3 style={{ margin: 0, fontSize: 15, fontWeight: 700 }}>Produtos mais vendidos</h3>
              </div>
              {topProdutos.length === 0 ? (
                <p style={{ textAlign: 'center', color: '#94a3b8', fontSize: 13, padding: '20px 0' }}>Sem dados no período</p>
              ) : (
                <>
                  {topProdutos.map((p, i) => (
                    <RankingBar key={i} posicao={i+1} nome={p.nome}
                      valor={p.total} total={totalProdutos}
                      cor={CORES_PROD[i % CORES_PROD.length]}
                      qtd={Math.round(p.quantidade)} />
                  ))}
                  <div style={{ borderTop: '1px solid #f1f5f9', paddingTop: 8, marginTop: 4, display: 'flex', justifyContent: 'space-between', fontSize: 12, color: '#64748b' }}>
                    <span>Total</span>
                    <span style={{ fontWeight: 700, color: '#6366f1' }}>{fmt(totalProdutos)}</span>
                  </div>
                </>
              )}
            </div>
          </div>

          {/* Pizza serviços + Pizza produtos */}
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 20 }}>
            <div style={{ background: 'white', borderRadius: 16, padding: '20px 16px 12px', boxShadow: '0 1px 3px rgba(0,0,0,0.07)' }}>
              <h3 style={{ margin: '0 0 12px', fontSize: 14, fontWeight: 700 }}>Composição — Serviços</h3>
              {topServicos.length > 0 ? (
                <ResponsiveContainer width="100%" height={200}>
                  <PieChart>
                    <Pie data={topServicos.slice(0,6)} dataKey="total" nameKey="nome"
                      cx="50%" cy="50%" outerRadius={75} innerRadius={35}
                      paddingAngle={2}
                      label={({ name, percent }) => `${name.slice(0,10)} ${(percent*100).toFixed(0)}%`}
                      labelLine={false} fontSize={10}>
                      {topServicos.slice(0,6).map((_, i) => <Cell key={i} fill={CORES_SVC[i % CORES_SVC.length]} />)}
                    </Pie>
                    <Tooltip formatter={v => fmt(v)} />
                  </PieChart>
                </ResponsiveContainer>
              ) : <p style={{ textAlign: 'center', color: '#94a3b8', padding: '30px 0' }}>Sem dados</p>}
            </div>

            <div style={{ background: 'white', borderRadius: 16, padding: '20px 16px 12px', boxShadow: '0 1px 3px rgba(0,0,0,0.07)' }}>
              <h3 style={{ margin: '0 0 12px', fontSize: 14, fontWeight: 700 }}>Composição — Produtos PDV</h3>
              {topProdutos.length > 0 ? (
                <ResponsiveContainer width="100%" height={200}>
                  <PieChart>
                    <Pie data={topProdutos.slice(0,6)} dataKey="total" nameKey="nome"
                      cx="50%" cy="50%" outerRadius={75} innerRadius={35}
                      paddingAngle={2}
                      label={({ name, percent }) => `${name.slice(0,10)} ${(percent*100).toFixed(0)}%`}
                      labelLine={false} fontSize={10}>
                      {topProdutos.slice(0,6).map((_, i) => <Cell key={i} fill={CORES_PROD[i % CORES_PROD.length]} />)}
                    </Pie>
                    <Tooltip formatter={v => fmt(v)} />
                  </PieChart>
                </ResponsiveContainer>
              ) : <p style={{ textAlign: 'center', color: '#94a3b8', padding: '30px 0' }}>Sem dados</p>}
            </div>
          </div>

        </div>
      )}
    </div>
  )
}
