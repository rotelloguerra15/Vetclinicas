import { useEffect, useState } from 'react'
import api from '../../api/client'

function fmt(v) {
  return Number(v ?? 0).toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
}

function mesAtual() {
  const d = new Date()
  return `${d.getFullYear()}-${String(d.getMonth()+1).padStart(2,'0')}`
}

const STATUS_CFG = {
  aberto:  { label: 'Pendente', cls: 'bg-slate-100 text-slate-600' },
  fechado: { label: 'Fechado',  cls: 'bg-blue-100 text-blue-700' },
  pago:    { label: 'Pago',     cls: 'bg-green-100 text-green-700' },
}

export default function Fechamento() {
  const [competencia, setCompetencia] = useState(mesAtual())
  const [funcionarios, setFuncionarios] = useState([])
  const [fechamentos, setFechamentos]   = useState([])
  const [loading, setLoading] = useState(true)
  const [saving, setSaving]   = useState(null)

  useEffect(() => { carregar() }, [competencia])

  async function carregar() {
    setLoading(true)
    try {
      const comp = `${competencia}-01`
      const [r1, r2] = await Promise.all([
        api.get('/rh/funcionarios'),
        api.get(`/rh/fechamento?competencia=${comp}`)
      ])
      setFuncionarios(r1.data.filter(f => f.status !== 'demitido'))
      setFechamentos(r2.data)
    } finally { setLoading(false) }
  }

  function getFechamento(funcId) {
    return fechamentos.find(f => f.funcionarioId === funcId)
  }

  async function fechar(funcionarioId) {
    setSaving(funcionarioId)
    try {
      await api.post('/rh/fechamento', {
        funcionarioId,
        competencia: `${competencia}-01`,
        observacoes: null
      })
      await carregar()
    } catch (e) {
      alert('Erro: ' + (e.response?.data?.erro || e.message))
    } finally { setSaving(null) }
  }

  async function marcarPago(fechamentoId) {
    setSaving(fechamentoId)
    try {
      await api.patch(`/rh/fechamento/${fechamentoId}/pago`)
      await carregar()
    } catch (e) {
      alert('Erro: ' + (e.response?.data?.erro || e.message))
    } finally { setSaving(null) }
  }

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold">Fechamento Mensal</h1>
          <p className="text-sm text-slate-500">Fechamento de salários e comissões</p>
        </div>
        <input type="month" value={competencia}
          onChange={e => setCompetencia(e.target.value)}
          className="border rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
      </div>

      {/* Totais do mês */}
      {fechamentos.length > 0 && (
        <div className="grid grid-cols-3 gap-4 mb-6">
          {[
            { l: 'Total Salários',   v: fmt(fechamentos.reduce((s,f) => s + f.salario, 0)),        cls: 'blue' },
            { l: 'Total Comissões',  v: fmt(fechamentos.reduce((s,f) => s + f.totalComissoes, 0)), cls: 'green' },
            { l: 'Custo Total',      v: fmt(fechamentos.reduce((s,f) => s + f.totalPagar, 0)),     cls: 'purple' },
          ].map(c => (
            <div key={c.l} className={`bg-${c.cls}-50 border border-${c.cls}-200 rounded-xl p-4`}>
              <p className={`text-xs font-medium text-${c.cls}-600 mb-1`}>{c.l}</p>
              <p className={`text-xl font-bold text-${c.cls}-700`}>{c.v}</p>
            </div>
          ))}
        </div>
      )}

      {loading ? (
        <p className="text-slate-400 text-center py-10">Carregando...</p>
      ) : (
        <div className="bg-white rounded-xl border divide-y">
          {funcionarios.length === 0 && (
            <p className="text-slate-400 text-center py-10">Nenhum funcionário ativo.</p>
          )}
          {funcionarios.map(func => {
            const fech = getFechamento(func.id)
            const cfg  = STATUS_CFG[fech?.status || 'aberto']
            return (
              <div key={func.id} className="flex items-center justify-between px-4 py-4">
                <div className="flex items-center gap-3">
                  <div className="w-9 h-9 rounded-full bg-blue-100 flex items-center justify-center text-blue-700 font-semibold text-sm">
                    {func.nome.charAt(0).toUpperCase()}
                  </div>
                  <div>
                    <p className="font-medium text-slate-800 text-sm">{func.nome}</p>
                    <p className="text-xs text-slate-500">{func.cargo || '—'}</p>
                  </div>
                </div>

                <div className="flex items-center gap-6">
                  {fech ? (
                    <div className="text-right text-sm">
                      <p className="text-slate-500">Salário: <span className="text-slate-700">{fmt(fech.salario)}</span></p>
                      <p className="text-slate-500">Comissões: <span className="text-green-600">{fmt(fech.totalComissoes)}</span></p>
                      <p className="font-semibold text-blue-700">Total: {fmt(fech.totalPagar)}</p>
                    </div>
                  ) : (
                    <p className="text-sm text-slate-400">Sem fechamento</p>
                  )}

                  <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${cfg.cls}`}>{cfg.label}</span>

                  {!fech && (
                    <button onClick={() => fechar(func.id)} disabled={saving === func.id}
                      className="text-sm bg-blue-600 text-white px-3 py-1.5 rounded-lg hover:bg-blue-700 disabled:opacity-50">
                      {saving === func.id ? '...' : 'Fechar mês'}
                    </button>
                  )}
                  {fech?.status === 'fechado' && (
                    <button onClick={() => marcarPago(fech.id)} disabled={saving === fech.id}
                      className="text-sm bg-green-600 text-white px-3 py-1.5 rounded-lg hover:bg-green-700 disabled:opacity-50">
                      {saving === fech.id ? '...' : 'Marcar pago'}
                    </button>
                  )}
                </div>
              </div>
            )
          })}
        </div>
      )}
    </div>
  )
}
