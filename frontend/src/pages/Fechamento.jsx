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

// ─── Modal detalhe do funcionário ────────────────────────────────────────────
function ModalDetalhe({ func, competencia, fechamento, onFechar, onPago, onClose }) {
  const [comissoes, setComissoes] = useState([])
  const [loading, setLoading]     = useState(true)
  const [saving, setSaving]       = useState(false)

  useEffect(() => {
    async function carregar() {
      setLoading(true)
      try {
        const comp = `${competencia}-01`
        const { data } = await api.get(`/rh/comissoes?funcionarioId=${func.id}&competencia=${comp}`)
        setComissoes(data)
      } finally { setLoading(false) }
    }
    carregar()
  }, [func.id, competencia])

  async function fechar() {
    setSaving(true)
    try {
      await onFechar(func.id)
      onClose()
    } finally { setSaving(false) }
  }

  async function marcarPago() {
    setSaving(true)
    try {
      await onPago(fechamento.id)
      onClose()
    } finally { setSaving(false) }
  }

  const totalComissao = comissoes.reduce((s, c) => s + c.valorComissao, 0)
  const cfg = STATUS_CFG[fechamento?.status || 'aberto']

  return (
    <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50 p-4"
      onClick={onClose}>
      <div className="bg-white rounded-2xl shadow-xl w-full max-w-lg max-h-[90vh] overflow-y-auto"
        onClick={e => e.stopPropagation()}>

        {/* Header */}
        <div className="flex items-center justify-between p-5 border-b">
          <div>
            <h3 className="font-bold text-lg">{func.nome}</h3>
            <p className="text-sm text-slate-500">{func.cargo || '—'}</p>
          </div>
          <button onClick={onClose} className="text-slate-400 hover:text-slate-600 text-xl">×</button>
        </div>

        {/* Resumo */}
        <div className="grid grid-cols-3 gap-3 p-5 border-b">
          <div className="bg-slate-50 rounded-xl p-3 text-center">
            <p className="text-xs text-slate-500 mb-1">Salário</p>
            <p className="font-bold text-slate-700">{fmt(func.salario)}</p>
          </div>
          <div className="bg-green-50 rounded-xl p-3 text-center">
            <p className="text-xs text-slate-500 mb-1">Comissões</p>
            <p className="font-bold text-green-700">{fmt(totalComissao)}</p>
          </div>
          <div className="bg-blue-50 rounded-xl p-3 text-center">
            <p className="text-xs text-slate-500 mb-1">Total</p>
            <p className="font-bold text-blue-700">{fmt((func.salario || 0) + totalComissao)}</p>
          </div>
        </div>

        {/* Lista de OSs / comissões */}
        <div className="p-5">
          <h4 className="text-sm font-semibold text-slate-700 mb-3">
            Atendimentos do mês — {comissoes.length} OS{comissoes.length !== 1 ? 's' : ''}
          </h4>

          {loading ? (
            <p className="text-slate-400 text-sm text-center py-4">Carregando...</p>
          ) : comissoes.length === 0 ? (
            <p className="text-slate-400 text-sm text-center py-4">Nenhum atendimento com comissão neste mês.</p>
          ) : (
            <div className="space-y-2">
              {comissoes.map(c => (
                <div key={c.id} className="flex items-center justify-between bg-slate-50 rounded-lg px-3 py-2 text-sm">
                  <div>
                    <span className="font-medium text-slate-700">OS</span>
                    <span className="text-slate-400 text-xs ml-2">{new Date(c.criadoEm).toLocaleDateString('pt-BR')}</span>
                  </div>
                  <div className="text-right">
                    <p className="text-slate-600">{fmt(c.valorBase)} × {c.percentual}%</p>
                    <p className="font-semibold text-green-600">{fmt(c.valorComissao)}</p>
                  </div>
                </div>
              ))}

              <div className="flex justify-between items-center pt-2 border-t font-semibold text-sm">
                <span className="text-slate-700">Total comissões</span>
                <span className="text-green-600">{fmt(totalComissao)}</span>
              </div>
            </div>
          )}
        </div>

        {/* Ações */}
        <div className="p-5 border-t flex gap-3">
          {!fechamento && (
            <button onClick={fechar} disabled={saving}
              className="flex-1 bg-blue-600 text-white py-2 rounded-lg text-sm hover:bg-blue-700 disabled:opacity-50">
              {saving ? 'Fechando...' : 'Fechar Mês'}
            </button>
          )}
          {fechamento?.status === 'fechado' && (
            <button onClick={marcarPago} disabled={saving}
              className="flex-1 bg-green-600 text-white py-2 rounded-lg text-sm hover:bg-green-700 disabled:opacity-50">
              {saving ? '...' : 'Marcar como Pago'}
            </button>
          )}
          {fechamento?.status === 'pago' && (
            <div className="flex-1 text-center text-green-600 font-medium text-sm py-2">
              ✓ Pagamento realizado
            </div>
          )}
          <button onClick={onClose}
            className="px-4 py-2 rounded-lg border text-sm text-slate-500 hover:bg-slate-50">
            Fechar
          </button>
        </div>
      </div>
    </div>
  )
}

// ─── Página principal ─────────────────────────────────────────────────────────
export default function Fechamento() {
  const [competencia, setCompetencia] = useState(mesAtual())
  const [funcionarios, setFuncionarios] = useState([])
  const [fechamentos, setFechamentos]   = useState([])
  const [loading, setLoading]   = useState(true)
  const [detalhe, setDetalhe]   = useState(null)

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
    await api.post('/rh/fechamento', {
      funcionarioId,
      competencia: `${competencia}-01`,
      observacoes: null
    })
    await carregar()
  }

  async function marcarPago(fechamentoId) {
    await api.patch(`/rh/fechamento/${fechamentoId}/pago`)
    await carregar()
  }

  const totalSalarios  = fechamentos.reduce((s, f) => s + f.salario, 0)
  const totalComissoes = fechamentos.reduce((s, f) => s + f.totalComissoes, 0)
  const totalGeral     = fechamentos.reduce((s, f) => s + f.totalPagar, 0)

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold">Fechamento Mensal</h1>
          <p className="text-sm text-slate-500">Clique no funcionário para ver detalhes</p>
        </div>
        <input type="month" value={competencia}
          onChange={e => setCompetencia(e.target.value)}
          className="border rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
      </div>

      {/* Totais */}
      {fechamentos.length > 0 && (
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
              <div key={func.id}
                onClick={() => setDetalhe(func)}
                className="flex items-center justify-between px-4 py-4 hover:bg-slate-50 cursor-pointer transition">
                <div className="flex items-center gap-3">
                  <div className="w-9 h-9 rounded-full bg-blue-100 flex items-center justify-center text-blue-700 font-semibold text-sm">
                    {func.nome.charAt(0).toUpperCase()}
                  </div>
                  <div>
                    <p className="font-medium text-slate-800 text-sm">{func.nome}</p>
                    <p className="text-xs text-slate-500">{func.cargo || '—'} · {func.percentualComissao}% comissão</p>
                  </div>
                </div>

                <div className="flex items-center gap-4">
                  {fech ? (
                    <div className="text-right text-sm">
                      <p className="text-slate-500">
                        {fmt(fech.salario)} + <span className="text-green-600">{fmt(fech.totalComissoes)}</span>
                      </p>
                      <p className="font-semibold text-blue-700">{fmt(fech.totalPagar)}</p>
                    </div>
                  ) : (
                    <p className="text-xs text-slate-400">Sem fechamento</p>
                  )}
                  <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${cfg.cls}`}>{cfg.label}</span>
                  <span className="text-slate-300">›</span>
                </div>
              </div>
            )
          })}
        </div>
      )}

      {detalhe && (
        <ModalDetalhe
          func={detalhe}
          competencia={competencia}
          fechamento={getFechamento(detalhe.id)}
          onFechar={fechar}
          onPago={marcarPago}
          onClose={() => { setDetalhe(null); carregar() }}
        />
      )}
    </div>
  )
}
