import { useEffect, useState, useCallback } from 'react'
import {
  ComposedChart, BarChart, Bar, LineChart, Line,
  PieChart, Pie, Cell, XAxis, YAxis, CartesianGrid,
  Tooltip, Legend, ResponsiveContainer, Area, AreaChart,
  RadialBarChart, RadialBar
} from 'recharts'
import api from '../api/client'

const MESES_LABEL = ['Jan','Fev','Mar','Abr','Mai','Jun','Jul','Ago','Set','Out','Nov','Dez']
const MESES_FULL  = ['Janeiro','Fevereiro','Março','Abril','Maio','Junho','Julho','Agosto','Setembro','Outubro','Novembro','Dezembro']

const PALETTE = ['#6366f1','#06b6d4','#10b981','#f59e0b','#ef4444','#8b5cf6','#ec4899','#14b8a6','#f97316','#84cc16']

function fmt(v) { return Number(v ?? 0).toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' }) }
function fmtK(v) {
  if (v >= 1000) return `R$${(v/1000).toFixed(1)}k`
  return `R$${Number(v).toFixed(0)}`
}

// ── Pet art SVG inline ────────────────────────────────────────────────────────
function PetIcon({ tipo = 'pata', size = 32, color = '#6366f1', opacity = 1 }) {
  const icons = {
    pata: (
      <svg width={size} height={size} viewBox="0 0 64 64" fill="none" style={{ opacity }}>
        <ellipse cx="32" cy="44" rx="14" ry="11" fill={color}/>
        <ellipse cx="16" cy="34" rx="6" ry="8" fill={color}/>
        <ellipse cx="48" cy="34" rx="6" ry="8" fill={color}/>
        <ellipse cx="22" cy="22" rx="5" ry="7" fill={color}/>
        <ellipse cx="42" cy="22" rx="5" ry="7" fill={color}/>
      </svg>
    ),
    osso: (
      <svg width={size} height={size} viewBox="0 0 64 64" fill="none" style={{ opacity }}>
        <rect x="18" y="28" width="28" height="8" rx="4" fill={color}/>
        <circle cx="14" cy="24" r="6" fill={color}/><circle cx="14" cy="40" r="6" fill={color}/>
        <circle cx="50" cy="24" r="6" fill={color}/><circle cx="50" cy="40" r="6" fill={color}/>
      </svg>
    ),
    gato: (
      <svg width={size} height={size} viewBox="0 0 64 64" fill="none" style={{ opacity }}>
        <ellipse cx="32" cy="38" rx="16" ry="14" fill={color}/>
        <polygon points="18,28 12,14 24,22" fill={color}/>
        <polygon points="46,28 52,14 40,22" fill={color}/>
        <circle cx="26" cy="36" r="3" fill="white" opacity="0.6"/>
        <circle cx="38" cy="36" r="3" fill="white" opacity="0.6"/>
      </svg>
    ),
    coracao: (
      <svg width={size} height={size} viewBox="0 0 64 64" fill="none" style={{ opacity }}>
        <path d="M32 52 C32 52 8 38 8 22 C8 14 14 8 22 8 C26 8 30 10 32 14 C34 10 38 8 42 8 C50 8 56 14 56 22 C56 38 32 52 32 52Z" fill={color}/>
      </svg>
    ),
    cifrao: (
      <svg width={size} height={size} viewBox="0 0 64 64" fill="none" style={{ opacity }}>
        <text x="32" y="46" textAnchor="middle" fontSize="36" fontWeight="bold" fill={color}>$</text>
      </svg>
    )
  }
  return icons[tipo] || icons.pata
}

// ── Tooltip customizado ───────────────────────────────────────────────────────
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

// ── Card KPI ──────────────────────────────────────────────────────────────────
function KpiCard({ label, value, sub, color, icon, trend }) {
  return (
    <div style={{
      background: 'white', borderRadius: 16, padding: '16px 20px',
      boxShadow: '0 1px 3px rgba(0,0,0,0.08)', border: '1px solid #f1f5f9',
      position: 'relative', overflow: 'hidden'
    }}>
      <div style={{ position: 'absolute', right: -8, top: -8, opacity: 0.08 }}>
        <PetIcon tipo={icon || 'pata'} size={72} color={color || '#6366f1'} />
      </div>
      <div style={{ fontSize: 12, color: '#64748b', fontWeight: 500, marginBottom: 4 }}>{label}</div>
      <div style={{ fontSize: 24, fontWeight: 700, color: color || '#1e293b', lineHeight: 1.1 }}>{value}</div>
      {sub && <div style={{ fontSize: 12, color: '#94a3b8', marginTop: 4 }}>{sub}</div>}
      {trend !== undefined && (
        <div style={{ fontSize: 12, color: trend >= 0 ? '#10b981' : '#ef4444', marginTop: 4, fontWeight: 600 }}>
          {trend >= 0 ? '▲' : '▼'} {Math.abs(trend).toFixed(1)}%
        </div>
      )}
    </div>
  )
}

// ── Barra horizontal ranking ──────────────────────────────────────────────────
function RankingBar({ nome, valor, total, cor, posicao }) {
  const pct = total > 0 ? (valor / total) * 100 : 0
  return (
    <div style={{ marginBottom: 10 }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 4 }}>
        <span style={{ fontSize: 13, color: '#334155', fontWeight: 500 }}>
          <span style={{ color: cor, fontWeight: 700, marginRight: 6 }}>#{posicao}</span>{nome}
        </span>
        <span style={{ fontSize: 13, fontWeight: 700, color: cor }}>{fmt(valor)}</span>
      </div>
      <div style={{ height: 8, background: '#f1f5f9', borderRadius: 4, overflow: 'hidden' }}>
        <div style={{
          height: '100%', borderRadius: 4,
          width: `${pct}%`,
          background: `linear-gradient(90deg, ${cor}, ${cor}99)`,
          transition: 'width 0.8s ease'
        }} />
      </div>
    </div>
  )
}

// ── Página principal ──────────────────────────────────────────────────────────
export default function Relatorios() {
  const [ano, setAno]   = useState(new Date().getFullYear())
  const [mes, setMes]   = useState(new Date().getMonth() + 1)
  const [aba, setAba]   = useState('visao-geral') // visao-geral | servicos | custos | metas

  const [resumo, setResumo]           = useState(null)
  const [diasData, setDiasData]       = useState([])
  const [topServicos, setTopServicos] = useState([])
  const [topProdutos, setTopProdutos] = useState([])
  const [custos, setCustos]           = useState([])
  const [receitas, setReceitas]       = useState([])
  const [evolucao, setEvolucao]       = useState([])
  const [metas, setMetas]             = useState([])
  const [editandoMeta, setEditandoMeta] = useState(null)
  const [valorMetaEdit, setValorMetaEdit] = useState('')
  const [loading, setLoading]         = useState(true)

  const carregar = useCallback(() => {
    setLoading(true)
    Promise.all([
      api.get('/relatorios/mensal',           { params: { ano, mes } }),
      api.get('/relatorios/vendas-por-dia',   { params: { ano, mes } }),
      api.get('/relatorios/top-servicos',     { params: { ano, mes, top: 8 } }),
      api.get('/relatorios/top-produtos',     { params: { ano, mes, top: 8 } }),
      api.get('/relatorios/custos-categoria', { params: { ano, mes } }),
      api.get('/relatorios/receitas-categoria',{ params: { ano, mes } }),
      api.get('/relatorios/evolucao',         { params: { meses: 12 } }),
      api.get('/relatorios/metas',            { params: { ano } }),
    ]).then(([r1,r2,r3,r4,r5,r6,r7,r8]) => {
      setResumo(r1.data)
      setDiasData(r2.data)
      setTopServicos(r3.data)
      setTopProdutos(r4.data)
      setCustos(r5.data)
      setReceitas(r6.data)
      setEvolucao(r7.data)
      setMetas(r8.data)
    }).catch(() => {}).finally(() => setLoading(false))
  }, [ano, mes])

  useEffect(() => { carregar() }, [carregar])

  async function salvarMeta(mesNum) {
    const val = parseFloat(valorMetaEdit)
    if (isNaN(val) || val < 0) return
    await api.put(`/relatorios/metas/${ano}/${mesNum}`, { valorMeta: val }).catch(() => {})
    setEditandoMeta(null)
    setValorMetaEdit('')
    const r = await api.get('/relatorios/metas', { params: { ano } })
    setMetas(r.data)
  }

  const mesAtual = metas.find(m => m.mes === mes)
  const totalCustos = custos.reduce((s, c) => s + c.total, 0)
  const totalReceitas_ = receitas.reduce((s, r) => s + r.total, 0)

  const abas = [
    { id: 'visao-geral', label: '📊 Visão Geral' },
    { id: 'servicos',    label: '🏆 Rankings' },
    { id: 'custos',      label: '💸 Custos' },
    { id: 'metas',       label: '🎯 Metas' },
  ]

  return (
    <div style={{ maxWidth: 1100, margin: '0 auto' }}>
      {/* Header */}
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 24, flexWrap: 'wrap', gap: 12 }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
          <PetIcon tipo="coracao" size={36} color="#6366f1" />
          <div>
            <h2 style={{ fontSize: 22, fontWeight: 700, margin: 0 }}>Business Intelligence</h2>
            <p style={{ fontSize: 13, color: '#64748b', margin: 0 }}>Análise completa da clínica</p>
          </div>
        </div>
        <div style={{ display: 'flex', gap: 8 }}>
          <select className="border rounded-lg px-3 py-2 text-sm" value={mes} onChange={e => setMes(+e.target.value)}>
            {MESES_LABEL.map((m, i) => <option key={i} value={i+1}>{m}</option>)}
          </select>
          <select className="border rounded-lg px-3 py-2 text-sm" value={ano} onChange={e => setAno(+e.target.value)}>
            {[2024,2025,2026,2027].map(a => <option key={a} value={a}>{a}</option>)}
          </select>
        </div>
      </div>

      {/* KPIs */}
      {resumo && (
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(160px, 1fr))', gap: 12, marginBottom: 24 }}>
          <KpiCard label="Receitas" value={fmt(resumo.receitas)} color="#10b981" icon="coracao"
            sub={mesAtual?.meta > 0 ? `Meta: ${fmt(mesAtual.meta)}` : undefined}
            trend={mesAtual?.meta > 0 ? ((resumo.receitas / mesAtual.meta - 1) * 100) : undefined} />
          <KpiCard label="Despesas" value={fmt(resumo.despesas)} color="#ef4444" icon="osso" />
          <KpiCard label="Saldo" value={fmt(resumo.saldo)} color={resumo.saldo >= 0 ? '#6366f1' : '#ef4444'} icon="cifrao" />
          <KpiCard label="Atendimentos" value={resumo.totalAtendimentos} color="#06b6d4" icon="pata" />
          <KpiCard label="Vendas PDV" value={resumo.totalVendas} color="#f59e0b" icon="osso" />
          <KpiCard label="Novos clientes" value={resumo.novosClientes} color="#8b5cf6" icon="gato" />
        </div>
      )}

      {/* Abas */}
      <div style={{ display: 'flex', gap: 4, borderBottom: '2px solid #f1f5f9', marginBottom: 24, overflowX: 'auto' }}>
        {abas.map(a => (
          <button key={a.id} onClick={() => setAba(a.id)}
            style={{
              padding: '10px 18px', fontSize: 14, fontWeight: 600, border: 'none', cursor: 'pointer',
              borderBottom: aba === a.id ? '2px solid #6366f1' : '2px solid transparent',
              color: aba === a.id ? '#6366f1' : '#64748b',
              background: 'transparent', marginBottom: -2, whiteSpace: 'nowrap'
            }}>
            {a.label}
          </button>
        ))}
      </div>

      {loading && <div style={{ textAlign: 'center', padding: 40, color: '#94a3b8' }}>Carregando dados...</div>}

      {/* ── ABA: VISÃO GERAL ─────────────────────────────────────────────────── */}
      {!loading && aba === 'visao-geral' && (
        <div style={{ display: 'flex', flexDirection: 'column', gap: 20 }}>

          {/* Faturamento por dia */}
          <div style={{ background: 'white', borderRadius: 16, padding: '20px 20px 12px', boxShadow: '0 1px 3px rgba(0,0,0,0.07)' }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 16 }}>
              <PetIcon tipo="pata" size={22} color="#6366f1" />
              <h3 style={{ margin: 0, fontSize: 15, fontWeight: 700 }}>
                Faturamento por dia — {MESES_FULL[mes-1]} {ano}
              </h3>
            </div>
            <ResponsiveContainer width="100%" height={200}>
              <ComposedChart data={diasData} margin={{ top: 4, right: 8, left: 0, bottom: 0 }}>
                <CartesianGrid strokeDasharray="3 3" stroke="#f1f5f9" />
                <XAxis dataKey="data" tickFormatter={d => {
                  const dt = new Date(d + 'T12:00:00')
                  return dt.getDate()
                }} tick={{ fontSize: 10 }} />
                <YAxis tickFormatter={fmtK} tick={{ fontSize: 10 }} />
                <Tooltip content={<TooltipCustom />} formatter={(v) => fmt(v)} />
                <Legend />
                <Bar dataKey="servicos" name="Serviços" stackId="a" fill="#6366f1" radius={[0,0,0,0]} />
                <Bar dataKey="vendas"   name="Vendas PDV" stackId="a" fill="#06b6d4" radius={[3,3,0,0]} />
                <Line dataKey="total" name="Total" stroke="#f59e0b" strokeWidth={2} dot={false} />
              </ComposedChart>
            </ResponsiveContainer>
          </div>

          {/* Evolução 12 meses */}
          <div style={{ background: 'white', borderRadius: 16, padding: '20px 20px 12px', boxShadow: '0 1px 3px rgba(0,0,0,0.07)' }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 16 }}>
              <PetIcon tipo="coracao" size={22} color="#10b981" />
              <h3 style={{ margin: 0, fontSize: 15, fontWeight: 700 }}>Evolução — últimos 12 meses</h3>
            </div>
            <ResponsiveContainer width="100%" height={200}>
              <AreaChart data={evolucao} margin={{ top: 4, right: 8, left: 0, bottom: 0 }}>
                <defs>
                  <linearGradient id="gradRec" x1="0" y1="0" x2="0" y2="1">
                    <stop offset="5%" stopColor="#10b981" stopOpacity={0.3}/>
                    <stop offset="95%" stopColor="#10b981" stopOpacity={0}/>
                  </linearGradient>
                  <linearGradient id="gradDesp" x1="0" y1="0" x2="0" y2="1">
                    <stop offset="5%" stopColor="#ef4444" stopOpacity={0.2}/>
                    <stop offset="95%" stopColor="#ef4444" stopOpacity={0}/>
                  </linearGradient>
                </defs>
                <CartesianGrid strokeDasharray="3 3" stroke="#f1f5f9" />
                <XAxis dataKey="mes" tick={{ fontSize: 10 }} />
                <YAxis tickFormatter={fmtK} tick={{ fontSize: 10 }} />
                <Tooltip content={<TooltipCustom />} />
                <Legend />
                <Area type="monotone" dataKey="receitas" name="Receitas" stroke="#10b981" fill="url(#gradRec)" strokeWidth={2} />
                <Area type="monotone" dataKey="despesas" name="Despesas" stroke="#ef4444" fill="url(#gradDesp)" strokeWidth={2} />
                <Line  type="monotone" dataKey="saldo"    name="Saldo"    stroke="#6366f1" strokeWidth={2} dot={false} strokeDasharray="4 2" />
              </AreaChart>
            </ResponsiveContainer>
          </div>

          {/* Pizza receitas vs custos por categoria lado a lado */}
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 16 }}>
            <div style={{ background: 'white', borderRadius: 16, padding: '20px 16px 12px', boxShadow: '0 1px 3px rgba(0,0,0,0.07)' }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 12 }}>
                <PetIcon tipo="coracao" size={20} color="#10b981" />
                <h3 style={{ margin: 0, fontSize: 14, fontWeight: 700 }}>Receitas por categoria</h3>
              </div>
              {receitas.length > 0 ? (
                <ResponsiveContainer width="100%" height={180}>
                  <PieChart>
                    <Pie data={receitas} dataKey="total" nameKey="categoria" cx="50%" cy="50%" outerRadius={70} label={({ name, percent }) => `${name} ${(percent*100).toFixed(0)}%`} labelLine={false} fontSize={10}>
                      {receitas.map((_, i) => <Cell key={i} fill={PALETTE[i % PALETTE.length]} />)}
                    </Pie>
                    <Tooltip formatter={(v) => fmt(v)} />
                  </PieChart>
                </ResponsiveContainer>
              ) : <p style={{ textAlign: 'center', color: '#94a3b8', fontSize: 13, padding: '20px 0' }}>Sem dados no período</p>}
            </div>
            <div style={{ background: 'white', borderRadius: 16, padding: '20px 16px 12px', boxShadow: '0 1px 3px rgba(0,0,0,0.07)' }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 12 }}>
                <PetIcon tipo="osso" size={20} color="#ef4444" />
                <h3 style={{ margin: 0, fontSize: 14, fontWeight: 700 }}>Despesas por categoria</h3>
              </div>
              {custos.length > 0 ? (
                <ResponsiveContainer width="100%" height={180}>
                  <PieChart>
                    <Pie data={custos} dataKey="total" nameKey="categoria" cx="50%" cy="50%" outerRadius={70} label={({ name, percent }) => `${name} ${(percent*100).toFixed(0)}%`} labelLine={false} fontSize={10}>
                      {custos.map((_, i) => <Cell key={i} fill={['#ef4444','#f97316','#f59e0b','#eab308','#84cc16','#ec4899'][i % 6]} />)}
                    </Pie>
                    <Tooltip formatter={(v) => fmt(v)} />
                  </PieChart>
                </ResponsiveContainer>
              ) : <p style={{ textAlign: 'center', color: '#94a3b8', fontSize: 13, padding: '20px 0' }}>Sem dados no período</p>}
            </div>
          </div>
        </div>
      )}

      {/* ── ABA: RANKINGS ───────────────────────────────────────────────────── */}
      {!loading && aba === 'servicos' && (
        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 20 }}>
          {/* Top serviços */}
          <div style={{ background: 'white', borderRadius: 16, padding: 20, boxShadow: '0 1px 3px rgba(0,0,0,0.07)' }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 16 }}>
              <PetIcon tipo="gato" size={22} color="#6366f1" />
              <h3 style={{ margin: 0, fontSize: 15, fontWeight: 700 }}>Top Serviços realizados</h3>
            </div>
            {topServicos.length === 0
              ? <p style={{ textAlign: 'center', color: '#94a3b8', fontSize: 13 }}>Sem dados no período</p>
              : topServicos.map((s, i) => (
                <RankingBar key={i} posicao={i+1} nome={s.nome}
                  valor={s.total} total={topServicos[0].total}
                  cor={PALETTE[i % PALETTE.length]} />
              ))
            }
          </div>

          {/* Top produtos */}
          <div style={{ background: 'white', borderRadius: 16, padding: 20, boxShadow: '0 1px 3px rgba(0,0,0,0.07)' }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 16 }}>
              <PetIcon tipo="osso" size={22} color="#06b6d4" />
              <h3 style={{ margin: 0, fontSize: 15, fontWeight: 700 }}>Top Produtos vendidos</h3>
            </div>
            {topProdutos.length === 0
              ? <p style={{ textAlign: 'center', color: '#94a3b8', fontSize: 13 }}>Sem dados no período</p>
              : topProdutos.map((p, i) => (
                <RankingBar key={i} posicao={i+1} nome={p.nome}
                  valor={p.total} total={topProdutos[0].total}
                  cor={['#06b6d4','#0891b2','#0e7490','#155e75','#164e63','#083344','#06b6d4','#22d3ee'][i % 8]} />
              ))
            }
          </div>

          {/* Gráfico barras serviços */}
          <div style={{ background: 'white', borderRadius: 16, padding: '20px 16px 12px', boxShadow: '0 1px 3px rgba(0,0,0,0.07)', gridColumn: '1 / -1' }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 16 }}>
              <PetIcon tipo="pata" size={22} color="#8b5cf6" />
              <h3 style={{ margin: 0, fontSize: 15, fontWeight: 700 }}>Faturamento: Serviços vs Produtos</h3>
            </div>
            <ResponsiveContainer width="100%" height={220}>
              <BarChart
                data={[
                  ...topServicos.slice(0,5).map(s => ({ nome: s.nome.slice(0,14), valor: s.total, tipo: 'Serviço' })),
                  ...topProdutos.slice(0,5).map(p => ({ nome: p.nome.slice(0,14), valor: p.total, tipo: 'Produto' }))
                ]}
                margin={{ top: 4, right: 8, left: 0, bottom: 40 }}>
                <CartesianGrid strokeDasharray="3 3" stroke="#f1f5f9" />
                <XAxis dataKey="nome" tick={{ fontSize: 10 }} angle={-30} textAnchor="end" />
                <YAxis tickFormatter={fmtK} tick={{ fontSize: 10 }} />
                <Tooltip content={<TooltipCustom />} />
                <Bar dataKey="valor" name="Faturamento" radius={[4,4,0,0]}>
                  {[...topServicos.slice(0,5), ...topProdutos.slice(0,5)].map((_, i) => (
                    <Cell key={i} fill={i < 5 ? '#6366f1' : '#06b6d4'} />
                  ))}
                </Bar>
              </BarChart>
            </ResponsiveContainer>
          </div>
        </div>
      )}

      {/* ── ABA: CUSTOS ─────────────────────────────────────────────────────── */}
      {!loading && aba === 'custos' && (
        <div style={{ display: 'flex', flexDirection: 'column', gap: 20 }}>
          {/* KPIs custo */}
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(180px, 1fr))', gap: 12 }}>
            <KpiCard label="Total despesas" value={fmt(totalCustos)} color="#ef4444" icon="osso" />
            <KpiCard label="Categorias com custo" value={custos.length} color="#f97316" icon="pata" />
            <KpiCard label="Maior categoria" value={custos[0]?.categoria || '—'} color="#8b5cf6" icon="gato"
              sub={custos[0] ? fmt(custos[0].total) : undefined} />
            <KpiCard label="Margem estimada"
              value={resumo ? `${totalCustos > 0 && resumo.receitas > 0 ? (((resumo.receitas - totalCustos) / resumo.receitas) * 100).toFixed(1) : '—'}%` : '—'}
              color="#10b981" icon="cifrao" />
          </div>

          {/* Ranking custos */}
          <div style={{ background: 'white', borderRadius: 16, padding: 20, boxShadow: '0 1px 3px rgba(0,0,0,0.07)' }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 16 }}>
              <PetIcon tipo="osso" size={22} color="#ef4444" />
              <h3 style={{ margin: 0, fontSize: 15, fontWeight: 700 }}>Categorias mais custosas — {MESES_FULL[mes-1]}</h3>
            </div>
            {custos.length === 0
              ? <p style={{ textAlign: 'center', color: '#94a3b8', padding: '20px 0' }}>Nenhuma despesa registrada no período.</p>
              : custos.map((c, i) => (
                <RankingBar key={i} posicao={i+1} nome={c.categoria}
                  valor={c.total} total={totalCustos}
                  cor={['#ef4444','#f97316','#f59e0b','#eab308','#84cc16','#ec4899'][i % 6]} />
              ))
            }
          </div>

          {/* Gráfico composição */}
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 16 }}>
            <div style={{ background: 'white', borderRadius: 16, padding: '20px 16px 12px', boxShadow: '0 1px 3px rgba(0,0,0,0.07)' }}>
              <h3 style={{ margin: '0 0 12px', fontSize: 14, fontWeight: 700 }}>Composição dos custos</h3>
              {custos.length > 0 ? (
                <ResponsiveContainer width="100%" height={220}>
                  <PieChart>
                    <Pie data={custos} dataKey="total" nameKey="categoria"
                      cx="50%" cy="50%" innerRadius={50} outerRadius={90}
                      paddingAngle={3}>
                      {custos.map((_, i) => (
                        <Cell key={i} fill={['#ef4444','#f97316','#f59e0b','#eab308','#84cc16','#ec4899'][i % 6]} />
                      ))}
                    </Pie>
                    <Tooltip formatter={v => fmt(v)} />
                    <Legend formatter={v => <span style={{ fontSize: 11 }}>{v}</span>} />
                  </PieChart>
                </ResponsiveContainer>
              ) : <p style={{ textAlign: 'center', color: '#94a3b8', padding: '30px 0' }}>Sem dados</p>}
            </div>
            <div style={{ background: 'white', borderRadius: 16, padding: '20px 16px 12px', boxShadow: '0 1px 3px rgba(0,0,0,0.07)' }}>
              <h3 style={{ margin: '0 0 12px', fontSize: 14, fontWeight: 700 }}>Receita vs Despesa (12 meses)</h3>
              <ResponsiveContainer width="100%" height={220}>
                <BarChart data={evolucao} margin={{ top: 4, right: 4, left: 0, bottom: 0 }}>
                  <CartesianGrid strokeDasharray="3 3" stroke="#f1f5f9" />
                  <XAxis dataKey="mes" tick={{ fontSize: 9 }} />
                  <YAxis tickFormatter={fmtK} tick={{ fontSize: 9 }} />
                  <Tooltip content={<TooltipCustom />} />
                  <Bar dataKey="receitas" name="Receitas" fill="#10b981" radius={[2,2,0,0]} />
                  <Bar dataKey="despesas" name="Despesas" fill="#ef4444" radius={[2,2,0,0]} />
                </BarChart>
              </ResponsiveContainer>
            </div>
          </div>
        </div>
      )}

      {/* ── ABA: METAS ──────────────────────────────────────────────────────── */}
      {!loading && aba === 'metas' && (
        <div style={{ display: 'flex', flexDirection: 'column', gap: 20 }}>
          {/* Gauge meta do mês atual */}
          {mesAtual && mesAtual.meta > 0 && resumo && (
            <div style={{ background: 'white', borderRadius: 16, padding: 20, boxShadow: '0 1px 3px rgba(0,0,0,0.07)', textAlign: 'center' }}>
              <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', gap: 8, marginBottom: 12 }}>
                <PetIcon tipo="coracao" size={22} color="#6366f1" />
                <h3 style={{ margin: 0, fontSize: 15, fontWeight: 700 }}>
                  Meta de {MESES_FULL[mes-1]} {ano}
                </h3>
              </div>
              <ResponsiveContainer width="100%" height={180}>
                <RadialBarChart cx="50%" cy="50%" innerRadius="55%" outerRadius="80%"
                  data={[{ name: 'Realizado', value: Math.min(mesAtual.percentual, 100), fill: mesAtual.atingido ? '#10b981' : '#6366f1' }]}
                  startAngle={180} endAngle={0}>
                  <RadialBar dataKey="value" cornerRadius={8} background={{ fill: '#f1f5f9' }} />
                </RadialBarChart>
              </ResponsiveContainer>
              <div style={{ marginTop: -60 }}>
                <div style={{ fontSize: 36, fontWeight: 800, color: mesAtual.atingido ? '#10b981' : '#6366f1' }}>
                  {mesAtual.percentual.toFixed(1)}%
                </div>
                <div style={{ fontSize: 13, color: '#64748b' }}>
                  {fmt(resumo.receitas)} de {fmt(mesAtual.meta)}
                </div>
                {mesAtual.atingido
                  ? <div style={{ color: '#10b981', fontWeight: 700, marginTop: 4 }}>🎉 Meta atingida!</div>
                  : <div style={{ color: '#f59e0b', fontWeight: 600, marginTop: 4 }}>Faltam {fmt(mesAtual.meta - resumo.receitas)}</div>
                }
              </div>
            </div>
          )}

          {/* Tabela de metas do ano */}
          <div style={{ background: 'white', borderRadius: 16, padding: 20, boxShadow: '0 1px 3px rgba(0,0,0,0.07)' }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 16 }}>
              <PetIcon tipo="cifrao" size={22} color="#f59e0b" />
              <h3 style={{ margin: 0, fontSize: 15, fontWeight: 700 }}>Metas x Realizado — {ano}</h3>
            </div>
            <div style={{ overflowX: 'auto' }}>
              <table style={{ width: '100%', fontSize: 13, borderCollapse: 'collapse' }}>
                <thead>
                  <tr style={{ background: '#f8fafc' }}>
                    <th style={{ padding: '8px 12px', textAlign: 'left', color: '#64748b', fontWeight: 600 }}>Mês</th>
                    <th style={{ padding: '8px 12px', textAlign: 'right', color: '#64748b', fontWeight: 600 }}>Meta</th>
                    <th style={{ padding: '8px 12px', textAlign: 'right', color: '#64748b', fontWeight: 600 }}>Realizado</th>
                    <th style={{ padding: '8px 12px', textAlign: 'right', color: '#64748b', fontWeight: 600 }}>%</th>
                    <th style={{ padding: '8px 12px', textAlign: 'center', color: '#64748b', fontWeight: 600 }}>Status</th>
                    <th style={{ padding: '8px 12px', textAlign: 'center', color: '#64748b', fontWeight: 600 }}>Editar</th>
                  </tr>
                </thead>
                <tbody>
                  {metas.map(m => (
                    <tr key={m.mes} style={{ borderTop: '1px solid #f1f5f9', background: m.mes === mes ? '#fafbff' : 'white' }}>
                      <td style={{ padding: '8px 12px', fontWeight: m.mes === mes ? 700 : 400 }}>{m.label}</td>
                      <td style={{ padding: '8px 12px', textAlign: 'right' }}>
                        {editandoMeta === m.mes ? (
                          <input type="number" step="100" style={{ width: 110, border: '1px solid #6366f1', borderRadius: 6, padding: '2px 6px', fontSize: 13 }}
                            value={valorMetaEdit} onChange={e => setValorMetaEdit(e.target.value)}
                            onKeyDown={e => e.key === 'Enter' && salvarMeta(m.mes)}
                            autoFocus />
                        ) : (
                          m.meta > 0 ? fmt(m.meta) : <span style={{ color: '#cbd5e1' }}>—</span>
                        )}
                      </td>
                      <td style={{ padding: '8px 12px', textAlign: 'right', fontWeight: 600, color: m.realizado > 0 ? '#1e293b' : '#cbd5e1' }}>
                        {m.realizado > 0 ? fmt(m.realizado) : '—'}
                      </td>
                      <td style={{ padding: '8px 12px', textAlign: 'right' }}>
                        {m.meta > 0 && m.realizado > 0 ? (
                          <span style={{ color: m.atingido ? '#10b981' : m.percentual > 75 ? '#f59e0b' : '#ef4444', fontWeight: 600 }}>
                            {m.percentual.toFixed(1)}%
                          </span>
                        ) : '—'}
                      </td>
                      <td style={{ padding: '8px 12px', textAlign: 'center' }}>
                        {m.meta > 0 && m.realizado > 0
                          ? m.atingido
                            ? <span style={{ background: '#dcfce7', color: '#16a34a', fontSize: 11, fontWeight: 700, padding: '2px 8px', borderRadius: 20 }}>✓ Atingida</span>
                            : <span style={{ background: '#fef9c3', color: '#ca8a04', fontSize: 11, fontWeight: 700, padding: '2px 8px', borderRadius: 20 }}>Em andamento</span>
                          : <span style={{ color: '#cbd5e1', fontSize: 11 }}>—</span>
                        }
                      </td>
                      <td style={{ padding: '8px 12px', textAlign: 'center' }}>
                        {editandoMeta === m.mes ? (
                          <div style={{ display: 'flex', gap: 4, justifyContent: 'center' }}>
                            <button onClick={() => salvarMeta(m.mes)} style={{ background: '#6366f1', color: 'white', border: 'none', borderRadius: 6, padding: '3px 10px', fontSize: 12, cursor: 'pointer' }}>✓</button>
                            <button onClick={() => { setEditandoMeta(null); setValorMetaEdit('') }} style={{ background: '#f1f5f9', border: 'none', borderRadius: 6, padding: '3px 10px', fontSize: 12, cursor: 'pointer' }}>✕</button>
                          </div>
                        ) : (
                          <button onClick={() => { setEditandoMeta(m.mes); setValorMetaEdit(m.meta > 0 ? String(m.meta) : '') }}
                            style={{ background: 'none', border: '1px solid #e2e8f0', borderRadius: 6, padding: '3px 10px', fontSize: 11, cursor: 'pointer', color: '#64748b' }}>
                            {m.meta > 0 ? 'Editar' : '+ Definir'}
                          </button>
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>

          {/* Gráfico metas vs realizado */}
          <div style={{ background: 'white', borderRadius: 16, padding: '20px 16px 12px', boxShadow: '0 1px 3px rgba(0,0,0,0.07)' }}>
            <h3 style={{ margin: '0 0 16px', fontSize: 15, fontWeight: 700 }}>Metas x Realizado — visão anual</h3>
            <ResponsiveContainer width="100%" height={220}>
              <ComposedChart data={metas} margin={{ top: 4, right: 8, left: 0, bottom: 0 }}>
                <CartesianGrid strokeDasharray="3 3" stroke="#f1f5f9" />
                <XAxis dataKey="label" tick={{ fontSize: 11 }} />
                <YAxis tickFormatter={fmtK} tick={{ fontSize: 10 }} />
                <Tooltip content={<TooltipCustom />} />
                <Legend />
                <Bar dataKey="meta"      name="Meta"      fill="#e0e7ff" radius={[3,3,0,0]} />
                <Bar dataKey="realizado" name="Realizado" fill="#6366f1" radius={[3,3,0,0]} />
                <Line dataKey="percentual" name="% da meta" stroke="#f59e0b" strokeWidth={2} dot={{ r: 3 }} yAxisId={0} hide />
              </ComposedChart>
            </ResponsiveContainer>
          </div>
        </div>
      )}
    </div>
  )
}
