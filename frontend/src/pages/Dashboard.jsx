import { useEffect, useState } from 'react'
import api from '../api/client'

function Card({ titulo, valor, cor }) {
  return (
    <div className="bg-white rounded-2xl shadow p-6">
      <div className="text-sm text-slate-500">{titulo}</div>
      <div className={`text-3xl font-bold mt-2 ${cor}`}>{valor}</div>
    </div>
  )
}

export default function Dashboard() {
  const [d, setD] = useState(null)

  useEffect(() => {
    api.get('/dashboard').then((r) => setD(r.data)).catch(() => {})
  }, [])

  if (!d) return <p>Carregando...</p>

  return (
    <div>
      <h2 className="text-2xl font-bold mb-6">Visão geral</h2>
      <div className="grid grid-cols-2 md:grid-cols-3 gap-4">
        <Card titulo="Agendamentos hoje" valor={d.agendamentosHoje} cor="text-blue-600" />
        <Card titulo="Atendimentos abertos" valor={d.osAbertas} cor="text-amber-600" />
        <Card titulo="Vacinas vencendo (30d)" valor={d.vacinasVencendo} cor="text-red-600" />
        <Card titulo="Produtos p/ repor" valor={d.produtosAbaixoMinimo ?? 0} cor="text-orange-600" />
        <Card titulo="Vendas no mês" valor={`R$ ${(d.vendasMes ?? 0).toFixed(2)}`} cor="text-emerald-600" />
        <Card titulo="Pets cadastrados" valor={d.totalPets} cor="text-emerald-600" />
        <Card titulo="Tutores" valor={d.totalTutores} cor="text-slate-700" />
      </div>
    </div>
  )
}
