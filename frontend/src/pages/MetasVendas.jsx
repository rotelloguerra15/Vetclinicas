import { useEffect, useState } from 'react'
import api from '../api/client'

const MESES = ['Jan','Fev','Mar','Abr','Mai','Jun','Jul','Ago','Set','Out','Nov','Dez']
const TIPO_LABEL = { produto: 'Produto', servico: 'Serviço', ambos: 'Produto + Serviço' }
const TIPO_COR = { produto: 'bg-blue-100 text-blue-700', servico: 'bg-purple-100 text-purple-700', ambos: 'bg-slate-100 text-slate-700' }

export default function MetasVendas() {
  const anoAtual = new Date().getFullYear()
  const [ano, setAno] = useState(anoAtual)
  const [metas, setMetas] = useState([])
  const [modalAberto, setModalAberto] = useState(false)
  const [loading, setLoading] = useState(true)

  function carregar() {
    setLoading(true)
    api.get('/metas-vendas', { params: { ano } })
      .then(r => setMetas(r.data))
      .catch(() => {})
      .finally(() => setLoading(false))
  }
  useEffect(() => { carregar() }, [ano])

  async function remover(id) {
    if (!confirm('Remover essa meta?')) return
    await api.delete(`/metas-vendas/${id}`)
    carregar()
  }

  const fmt = (v) => `R$ ${v.toLocaleString('pt-BR', { minimumFractionDigits: 2 })}`

  return (
    <div className="p-6 max-w-4xl">
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-xl font-bold">Metas de Vendas</h1>
          <p className="text-sm text-slate-500">Defina metas mensais por produto, serviço, ou as duas juntas. O realizado é calculado a partir das vendas do PDV e das ordens de serviço entregues.</p>
        </div>
        <div className="flex gap-2">
          <select value={ano} onChange={e => setAno(Number(e.target.value))}
            className="border rounded-lg px-3 py-2 text-sm">
            {[anoAtual - 1, anoAtual, anoAtual + 1].map(a => <option key={a} value={a}>{a}</option>)}
          </select>
          <button onClick={() => setModalAberto(true)}
            className="bg-blue-600 hover:bg-blue-700 text-white font-bold px-4 py-2 rounded-lg text-sm">
            + Nova meta
          </button>
        </div>
      </div>

      <div className="bg-white rounded-2xl shadow overflow-hidden">
        <table className="w-full text-sm">
          <thead className="bg-slate-50 text-left">
            <tr>
              <th className="p-3">Mês</th>
              <th className="p-3">Tipo</th>
              <th className="p-3 text-right">Meta</th>
              <th className="p-3 text-right">Realizado</th>
              <th className="p-3 text-right">%</th>
              <th className="p-3"></th>
            </tr>
          </thead>
          <tbody className="divide-y">
            {metas.map(m => (
              <tr key={m.id} className="hover:bg-slate-50">
                <td className="p-3">{MESES[m.mes - 1]}/{m.ano}</td>
                <td className="p-3"><span className={`text-xs px-2 py-1 rounded-full ${TIPO_COR[m.tipo]}`}>{TIPO_LABEL[m.tipo]}</span></td>
                <td className="p-3 text-right">{fmt(m.valorMeta)}</td>
                <td className="p-3 text-right font-semibold">{fmt(m.realizado)}</td>
                <td className="p-3 text-right">
                  <span className={m.atingido ? 'text-emerald-600 font-semibold' : m.percentual > 75 ? 'text-amber-600' : 'text-red-600'}>
                    {m.percentual.toFixed(1)}%
                  </span>
                </td>
                <td className="p-3 text-right">
                  <button onClick={() => remover(m.id)} className="text-xs text-red-500 hover:underline">Remover</button>
                </td>
              </tr>
            ))}
            {!loading && metas.length === 0 && (
              <tr><td colSpan="6" className="p-6 text-center text-slate-400">Nenhuma meta cadastrada para {ano}</td></tr>
            )}
          </tbody>
        </table>
      </div>

      {modalAberto && (
        <ModalNovaMeta ano={ano} onClose={() => setModalAberto(false)} onSalvo={carregar} />
      )}
    </div>
  )
}

function ModalNovaMeta({ ano, onClose, onSalvo }) {
  const [mes, setMes] = useState(new Date().getMonth() + 1)
  const [tipo, setTipo] = useState('ambos')
  const [valor, setValor] = useState('')
  const [erro, setErro] = useState('')
  const [loading, setLoading] = useState(false)

  async function salvar(e) {
    e.preventDefault()
    setErro('')
    if (!valor || Number(valor) < 0) return setErro('Informe o valor da meta.')

    setLoading(true)
    try {
      await api.post('/metas-vendas', { ano, mes: Number(mes), tipo, valorMeta: Number(valor) })
      onSalvo()
      onClose()
    } catch (err) {
      setErro(err.response?.data?.erro || 'Erro ao salvar meta.')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50 p-4" onClick={onClose}>
      <div className="bg-white rounded-2xl shadow-xl p-6 w-full max-w-sm" onClick={e => e.stopPropagation()}>
        <h3 className="font-bold text-lg mb-4">Nova meta — {ano}</h3>
        <form onSubmit={salvar} className="space-y-3">
          <div>
            <label className="block text-xs font-medium text-slate-600 mb-1">Mês</label>
            <select value={mes} onChange={e => setMes(e.target.value)} className="w-full border rounded-lg px-3 py-2 text-sm">
              {MESES.map((m, i) => <option key={i} value={i + 1}>{m}</option>)}
            </select>
          </div>
          <div>
            <label className="block text-xs font-medium text-slate-600 mb-1">Tipo de meta</label>
            <select value={tipo} onChange={e => setTipo(e.target.value)} className="w-full border rounded-lg px-3 py-2 text-sm">
              <option value="produto">Produto</option>
              <option value="servico">Serviço</option>
              <option value="ambos">Produto + Serviço (ambos)</option>
            </select>
          </div>
          <div>
            <label className="block text-xs font-medium text-slate-600 mb-1">Valor da meta</label>
            <input type="number" step="0.01" value={valor} onChange={e => setValor(e.target.value)}
              className="w-full border rounded-lg px-3 py-2 text-sm" />
          </div>

          {erro && <div className="text-sm px-3 py-2 rounded-lg bg-red-50 text-red-700">{erro}</div>}

          <div className="flex gap-2 pt-2">
            <button type="button" onClick={onClose} className="flex-1 border rounded-lg py-2 text-sm">Cancelar</button>
            <button type="submit" disabled={loading}
              className="flex-1 bg-blue-600 hover:bg-blue-700 disabled:bg-blue-400 text-white font-bold py-2 rounded-lg text-sm">
              {loading ? 'Salvando...' : 'Salvar'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
