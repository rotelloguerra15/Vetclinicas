import { useEffect, useState } from 'react'
import api from '../api/client'

const MESES = ['Jan','Fev','Mar','Abr','Mai','Jun','Jul','Ago','Set','Out','Nov','Dez']

export default function Relatorios() {
  const [ano, setAno] = useState(new Date().getFullYear())
  const [mes, setMes] = useState(new Date().getMonth() + 1)
  const [resumo, setResumo] = useState(null)
  const [dias, setDias] = useState([])
  const [caixas, setCaixas] = useState([])

  function carregar() {
    api.get('/relatorios/mensal', { params: { ano, mes } }).then(r => setResumo(r.data)).catch(() => {})
    api.get('/relatorios/vendas-por-dia', { params: { ano, mes } }).then(r => setDias(r.data)).catch(() => {})
    api.get('/caixa/historico', { params: { ultimos: 31 } }).then(r => setCaixas(r.data)).catch(() => {})
  }
  useEffect(() => { carregar() }, [ano, mes])

  const maxDia = Math.max(...dias.map(d => d.total), 1)

  return (
    <div>
      <div className="flex justify-between items-center mb-6">
        <h2 className="text-2xl font-bold">Relatórios</h2>
        <div className="flex gap-2">
          <select className="border rounded-lg px-3 py-2 text-sm" value={mes} onChange={e => setMes(+e.target.value)}>
            {MESES.map((m, i) => <option key={i} value={i + 1}>{m}</option>)}
          </select>
          <select className="border rounded-lg px-3 py-2 text-sm" value={ano} onChange={e => setAno(+e.target.value)}>
            {[2024,2025,2026,2027].map(a => <option key={a} value={a}>{a}</option>)}
          </select>
        </div>
      </div>

      {resumo && (
        <div className="grid grid-cols-2 md:grid-cols-4 gap-3 mb-6">
          <Card t="Receitas" v={`R$ ${resumo.receitas.toFixed(2)}`} c="text-emerald-600" />
          <Card t="Despesas" v={`R$ ${resumo.despesas.toFixed(2)}`} c="text-red-600" />
          <Card t="Saldo" v={`R$ ${resumo.saldo.toFixed(2)}`} c={resumo.saldo >= 0 ? 'text-emerald-600' : 'text-red-600'} />
          <Card t="Atendimentos" v={resumo.totalAtendimentos} c="text-blue-600" />
          <Card t="Vendas PDV" v={resumo.totalVendas} c="text-amber-600" />
          <Card t="Novos clientes" v={resumo.novosClientes} c="text-purple-600" />
        </div>
      )}

      <div className="bg-white rounded-2xl shadow p-5 mb-6">
        <h3 className="font-semibold mb-4">Faturamento por dia — {MESES[mes-1]} {ano}</h3>
        <div className="flex items-end gap-[2px] h-48">
          {dias.map((d, i) => {
            const h = maxDia > 0 ? (d.total / maxDia) * 100 : 0
            return (
              <div key={i} className="flex-1 flex flex-col items-center justify-end group relative">
                <div className="absolute -top-6 bg-slate-800 text-white text-[10px] px-1.5 py-0.5 rounded opacity-0 group-hover:opacity-100 whitespace-nowrap">
                  {new Date(d.data).toLocaleDateString('pt-BR', {day:'2-digit',month:'2-digit'})} — R$ {d.total.toFixed(0)}
                </div>
                <div className={`w-full rounded-t ${d.total > 0 ? 'bg-emerald-500' : 'bg-slate-100'}`}
                  style={{ height: `${Math.max(h, 2)}%` }}></div>
              </div>
            )
          })}
        </div>
        <div className="flex justify-between text-[10px] text-slate-400 mt-1">
          <span>1</span><span>15</span><span>{dias.length}</span>
        </div>
      </div>

      {caixas.length > 0 && (
        <div className="bg-white rounded-2xl shadow overflow-hidden">
          <h3 className="font-semibold p-4 pb-2">Histórico de caixa</h3>
          <table className="w-full text-sm">
            <thead className="bg-slate-50 text-left">
              <tr>
                <th className="p-3">Data</th><th className="p-3 text-right">Inicial</th>
                <th className="p-3 text-right">Vendas</th><th className="p-3 text-right">Serviços</th>
                <th className="p-3 text-right">Final</th><th className="p-3">Status</th>
              </tr>
            </thead>
            <tbody>
              {caixas.map(c => (
                <tr key={c.id} className="border-t">
                  <td className="p-3">{new Date(c.data).toLocaleDateString('pt-BR')}</td>
                  <td className="p-3 text-right">R$ {c.saldoInicial.toFixed(2)}</td>
                  <td className="p-3 text-right">R$ {c.totalVendas.toFixed(2)}</td>
                  <td className="p-3 text-right">R$ {c.totalServicos.toFixed(2)}</td>
                  <td className="p-3 text-right">{c.saldoFinal != null ? `R$ ${c.saldoFinal.toFixed(2)}` : '—'}</td>
                  <td className="p-3">
                    <span className={`text-xs px-2 py-1 rounded-full ${c.aberto ? 'bg-emerald-100 text-emerald-700' : 'bg-slate-100 text-slate-600'}`}>
                      {c.aberto ? 'Aberto' : 'Fechado'}
                    </span>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}

function Card({ t, v, c }) {
  return (
    <div className="bg-white rounded-2xl shadow p-4">
      <div className="text-xs text-slate-500">{t}</div>
      <div className={`text-xl font-bold mt-1 ${c}`}>{v}</div>
    </div>
  )
}
