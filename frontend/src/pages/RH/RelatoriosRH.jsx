import { useEffect, useState } from 'react'
import { BarChart, Bar, LineChart, Line, XAxis, YAxis, Tooltip, ResponsiveContainer, CartesianGrid } from 'recharts'
import api from '../../api/client'

function fmt(v) {
  return Number(v ?? 0).toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
}

function mesAtual() {
  const d = new Date()
  return `${d.getFullYear()}-${String(d.getMonth()+1).padStart(2,'0')}`
}

export default function RelatoriosRH() {
  const [competencia, setCompetencia] = useState(mesAtual())
  const [relatorio, setRelatorio]     = useState([])
  const [custoMensal, setCustoMensal] = useState([])
  const [produtividade, setProdutividade] = useState([])
  const [loading, setLoading] = useState(true)

  useEffect(() => { carregar() }, [competencia])

  async function carregar() {
    setLoading(true)
    try {
      const comp = `${competencia}-01`
      const [r1, r2, r3] = await Promise.all([
        api.get(`/rh/relatorios/mensal?competencia=${comp}`),
        api.get('/rh/relatorios/custo-mensal'),
        api.get(`/rh/relatorios/produtividade?competencia=${comp}`)
      ])
      setRelatorio(r1.data)
      setCustoMensal(r2.data)
      setProdutividade(r3.data)
    } finally { setLoading(false) }
  }

  const totalSalarios  = relatorio.reduce((s, r) => s + r.salario, 0)
  const totalComissoes = relatorio.reduce((s, r) => s + r.totalComissoes, 0)
  const totalGeral     = relatorio.reduce((s, r) => s + r.totalPagar, 0)

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold">Relatórios — RH</h1>
          <p className="text-sm text-slate-500">Custos e produtividade da equipe</p>
        </div>
        <input type="month" value={competencia}
          onChange={e => setCompetencia(e.target.value)}
          className="border rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
      </div>

      {/* Cards resumo */}
      <div className="grid grid-cols-3 gap-4 mb-6">
        {[
          { l: 'Total Salários',  v: fmt(totalSalarios),  cls: 'blue' },
          { l: 'Total Comissões', v: fmt(totalComissoes), cls: 'green' },
          { l: 'Custo Total',     v: fmt(totalGeral),     cls: 'purple' },
        ].map(c => (
          <div key={c.l} className={`bg-${c.cls}-50 border border-${c.cls}-200 rounded-xl p-4`}>
            <p className={`text-xs font-medium text-${c.cls}-600 mb-1`}>{c.l}</p>
            <p className={`text-xl font-bold text-${c.cls}-700`}>{c.v}</p>
          </div>
        ))}
      </div>

      {/* Gráfico custo mensal */}
      <div className="bg-white rounded-xl border p-5 mb-4">
        <h2 className="text-sm font-semibold text-slate-700 mb-4">Custo Total — Últimos 6 Meses</h2>
        <ResponsiveContainer width="100%" height={200}>
          <LineChart data={custoMensal}>
            <CartesianGrid strokeDasharray="3 3" stroke="#f1f5f9" />
            <XAxis dataKey="mes" tick={{ fontSize: 11 }} />
            <YAxis tick={{ fontSize: 11 }} tickFormatter={v => `R$${(v/1000).toFixed(0)}k`} />
            <Tooltip formatter={v => fmt(v)} />
            <Line type="monotone" dataKey="custoTotal" stroke="#3b82f6" strokeWidth={2} dot={{ r: 3 }} name="Custo Total" />
          </LineChart>
        </ResponsiveContainer>
      </div>

      {/* Gráfico produtividade */}
      <div className="bg-white rounded-xl border p-5 mb-4">
        <h2 className="text-sm font-semibold text-slate-700 mb-4">Produtividade — OSs por Funcionário</h2>
        <ResponsiveContainer width="100%" height={200}>
          <BarChart data={produtividade}>
            <CartesianGrid strokeDasharray="3 3" stroke="#f1f5f9" />
            <XAxis dataKey="nomeFuncionario" tick={{ fontSize: 11 }} />
            <YAxis tick={{ fontSize: 11 }} />
            <Tooltip />
            <Bar dataKey="totalOs" fill="#3b82f6" radius={[4,4,0,0]} name="OSs" />
          </BarChart>
        </ResponsiveContainer>
      </div>

      {/* Tabela mensal */}
      <div className="bg-white rounded-xl border">
        <div className="px-5 py-3 border-b">
          <h2 className="text-sm font-semibold text-slate-700">Fechamento Detalhado</h2>
        </div>
        {loading ? (
          <p className="text-slate-400 text-center py-8">Carregando...</p>
        ) : (
          <table className="w-full text-sm">
            <thead>
              <tr className="bg-slate-50 text-xs text-slate-500 uppercase">
                <th className="px-4 py-3 text-left">Funcionário</th>
                <th className="px-4 py-3 text-left">Cargo</th>
                <th className="px-4 py-3 text-right">OSs</th>
                <th className="px-4 py-3 text-right">Salário</th>
                <th className="px-4 py-3 text-right">Comissões</th>
                <th className="px-4 py-3 text-right font-semibold">Total</th>
              </tr>
            </thead>
            <tbody className="divide-y">
              {relatorio.map((r, i) => (
                <tr key={i} className="hover:bg-slate-50">
                  <td className="px-4 py-3 font-medium text-slate-800">{r.nomeFuncionario}</td>
                  <td className="px-4 py-3 text-slate-500">{r.cargo || '—'}</td>
                  <td className="px-4 py-3 text-right text-slate-700">{r.totalOs}</td>
                  <td className="px-4 py-3 text-right text-slate-700">{fmt(r.salario)}</td>
                  <td className="px-4 py-3 text-right text-green-600">{fmt(r.totalComissoes)}</td>
                  <td className="px-4 py-3 text-right font-semibold text-blue-700">{fmt(r.totalPagar)}</td>
                </tr>
              ))}
              {relatorio.length > 0 && (
                <tr className="bg-slate-50 font-semibold text-sm">
                  <td colSpan={3} className="px-4 py-3 text-slate-700">Total</td>
                  <td className="px-4 py-3 text-right">{fmt(totalSalarios)}</td>
                  <td className="px-4 py-3 text-right text-green-600">{fmt(totalComissoes)}</td>
                  <td className="px-4 py-3 text-right text-blue-700">{fmt(totalGeral)}</td>
                </tr>
              )}
              {relatorio.length === 0 && (
                <tr><td colSpan={6} className="text-center py-8 text-slate-400">Sem dados para este período.</td></tr>
              )}
            </tbody>
          </table>
        )}
      </div>
    </div>
  )
}
