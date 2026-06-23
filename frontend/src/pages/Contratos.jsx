import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import api from '../api/client'

const STATUS_COR = {
  ativo: 'bg-blue-100 text-blue-700',
  encerrado: 'bg-slate-100 text-slate-600',
  cancelado: 'bg-red-100 text-red-700'
}

export default function Contratos() {
  const [lista, setLista] = useState([])
  const [filtroStatus, setFiltroStatus] = useState('')
  const [modalAberto, setModalAberto] = useState(false)

  function carregar() {
    api.get('/contratos', { params: { status: filtroStatus || undefined } })
      .then(r => setLista(r.data)).catch(() => {})
  }
  useEffect(() => { carregar() }, [filtroStatus])

  return (
    <div className="p-6">
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-xl font-bold">Contratos</h1>
          <p className="text-sm text-slate-500">Contratos de fornecimento recorrente — cada parcela precisa de medição e aprovação antes de virar título pagável.</p>
        </div>
        <button onClick={() => setModalAberto(true)}
          className="bg-blue-600 hover:bg-blue-700 text-white font-bold px-4 py-2 rounded-lg text-sm">
          + Novo contrato
        </button>
      </div>

      <div className="mb-4">
        <select value={filtroStatus} onChange={e => setFiltroStatus(e.target.value)}
          className="border rounded-lg px-3 py-2 text-sm">
          <option value="">Todos os status</option>
          <option value="ativo">Ativo</option>
          <option value="encerrado">Encerrado</option>
          <option value="cancelado">Cancelado</option>
        </select>
      </div>

      <div className="bg-white rounded-2xl shadow overflow-hidden">
        <table className="w-full text-sm">
          <thead className="bg-slate-50 text-left">
            <tr>
              <th className="p-3">Fornecedor</th>
              <th className="p-3">Produto</th>
              <th className="p-3">Valor total</th>
              <th className="p-3">Parcelas</th>
              <th className="p-3">Status</th>
              <th className="p-3"></th>
            </tr>
          </thead>
          <tbody className="divide-y">
            {lista.map(c => (
              <tr key={c.id} className="hover:bg-slate-50">
                <td className="p-3">{c.fornecedorNome}</td>
                <td className="p-3">{c.produtoNome}</td>
                <td className="p-3">R$ {c.valorTotal.toFixed(2)}</td>
                <td className="p-3">
                  {c.parcelasAprovadas}/{c.numeroParcelas} aprovadas
                  {c.parcelasPendentes > 0 && <span className="text-amber-600"> · {c.parcelasPendentes} pendente(s)</span>}
                </td>
                <td className="p-3">
                  <span className={`text-xs px-2 py-1 rounded-full ${STATUS_COR[c.status] || 'bg-slate-100'}`}>{c.status}</span>
                </td>
                <td className="p-3 text-right">
                  <Link to={`/contratos/${c.id}`} className="text-blue-600 hover:underline text-xs">Ver parcelas</Link>
                </td>
              </tr>
            ))}
            {lista.length === 0 && <tr><td colSpan="6" className="p-6 text-center text-slate-400">Nenhum contrato ainda</td></tr>}
          </tbody>
        </table>
      </div>

      {modalAberto && <ModalNovoContrato onClose={() => setModalAberto(false)} onCriado={carregar} />}
    </div>
  )
}

function ModalNovoContrato({ onClose, onCriado }) {
  const [fornecedores, setFornecedores] = useState([])
  const [produtos, setProdutos] = useState([])
  const [condicoes, setCondicoes] = useState([])
  const [fornecedorId, setFornecedorId] = useState('')
  const [produtoId, setProdutoId] = useState('')
  const [valorTotal, setValorTotal] = useState('')
  const [condicaoPagamentoId, setCondicaoPagamentoId] = useState('')
  const [numeroParcelas, setNumeroParcelas] = useState(1)
  const [dataInicio, setDataInicio] = useState(() => new Date().toISOString().slice(0, 10))
  const [obs, setObs] = useState('')
  const [erro, setErro] = useState('')
  const [loading, setLoading] = useState(false)

  useEffect(() => {
    api.get('/fornecedores', { params: { pageSize: 100 } }).then(r => setFornecedores(r.data.items || r.data)).catch(() => {})
    api.get('/produtos', { params: { pageSize: 200 } }).then(r => setProdutos(r.data.items || r.data)).catch(() => {})
    api.get('/compras/condicoes').then(r => setCondicoes(r.data)).catch(() => {})
  }, [])

  function escolherCondicao(id) {
    setCondicaoPagamentoId(id)
    const cond = condicoes.find(c => c.id === id)
    if (cond) setNumeroParcelas(cond.parcelas)
  }

  async function salvar(e) {
    e.preventDefault()
    setErro('')
    if (!fornecedorId) return setErro('Selecione o fornecedor.')
    if (!produtoId) return setErro('Selecione o produto.')
    if (!valorTotal || Number(valorTotal) <= 0) return setErro('Informe o valor total.')

    setLoading(true)
    try {
      await api.post('/contratos', {
        fornecedorId, produtoId, valorTotal: Number(valorTotal),
        condicaoPagamentoId: condicaoPagamentoId || null,
        numeroParcelas: Number(numeroParcelas), dataInicio, obs: obs || null
      })
      onCriado()
      onClose()
    } catch (err) {
      setErro(err.response?.data?.erro || 'Erro ao criar contrato.')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50 p-4" onClick={onClose}>
      <div className="bg-white rounded-2xl shadow-xl p-6 w-full max-w-md" onClick={e => e.stopPropagation()}>
        <h3 className="font-bold text-lg mb-4">Novo contrato</h3>
        <form onSubmit={salvar} className="space-y-3">
          <div>
            <label className="block text-xs font-medium text-slate-600 mb-1">Fornecedor</label>
            <select value={fornecedorId} onChange={e => setFornecedorId(e.target.value)}
              className="w-full border rounded-lg px-3 py-2 text-sm">
              <option value="">Selecione...</option>
              {fornecedores.map(f => <option key={f.id} value={f.id}>{f.nome}</option>)}
            </select>
          </div>
          <div>
            <label className="block text-xs font-medium text-slate-600 mb-1">Produto</label>
            <select value={produtoId} onChange={e => setProdutoId(e.target.value)}
              className="w-full border rounded-lg px-3 py-2 text-sm">
              <option value="">Selecione...</option>
              {produtos.map(p => <option key={p.id} value={p.id}>{p.nome}</option>)}
            </select>
          </div>
          <div>
            <label className="block text-xs font-medium text-slate-600 mb-1">Valor total do contrato</label>
            <input type="number" step="0.01" value={valorTotal} onChange={e => setValorTotal(e.target.value)}
              className="w-full border rounded-lg px-3 py-2 text-sm" />
          </div>
          <div>
            <label className="block text-xs font-medium text-slate-600 mb-1">Condição de pagamento</label>
            <select value={condicaoPagamentoId} onChange={e => escolherCondicao(e.target.value)}
              className="w-full border rounded-lg px-3 py-2 text-sm">
              <option value="">Sem condição (definir parcelas manualmente)</option>
              {condicoes.map(c => <option key={c.id} value={c.id}>{c.nome} ({c.parcelas}x)</option>)}
            </select>
          </div>
          <div>
            <label className="block text-xs font-medium text-slate-600 mb-1">Número de parcelas (medições)</label>
            <input type="number" min="1" value={numeroParcelas} onChange={e => setNumeroParcelas(e.target.value)}
              className="w-full border rounded-lg px-3 py-2 text-sm" />
          </div>
          <div>
            <label className="block text-xs font-medium text-slate-600 mb-1">Data de início</label>
            <input type="date" value={dataInicio} onChange={e => setDataInicio(e.target.value)}
              className="w-full border rounded-lg px-3 py-2 text-sm" />
          </div>
          <div>
            <label className="block text-xs font-medium text-slate-600 mb-1">Observações (opcional)</label>
            <textarea value={obs} onChange={e => setObs(e.target.value)} rows={2}
              className="w-full border rounded-lg px-3 py-2 text-sm" />
          </div>

          {erro && <div className="text-sm px-3 py-2 rounded-lg bg-red-50 text-red-700">{erro}</div>}

          <div className="flex gap-2 pt-2">
            <button type="button" onClick={onClose} className="flex-1 border rounded-lg py-2 text-sm">Cancelar</button>
            <button type="submit" disabled={loading}
              className="flex-1 bg-blue-600 hover:bg-blue-700 disabled:bg-blue-400 text-white font-bold py-2 rounded-lg text-sm">
              {loading ? 'Criando...' : 'Criar contrato'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
