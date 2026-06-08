import { useEffect, useState } from 'react'
import api from '../api/client'

const STATUS_COR = {
  rascunho:  'bg-slate-100 text-slate-600',
  enviado:   'bg-blue-100 text-blue-700',
  recebido:  'bg-emerald-100 text-emerald-700',
  cancelado: 'bg-red-100 text-red-600'
}
const STATUS_LABEL = { rascunho: 'Rascunho', enviado: 'Enviado', recebido: 'Recebido', cancelado: 'Cancelado' }

export default function Compras() {
  const [pedidos, setPedidos] = useState([])
  const [total, setTotal]     = useState(0)
  const [page, setPage]       = useState(1)
  const [filtroStatus, setFiltroStatus] = useState('')
  const [mostrarForm, setMostrarForm]   = useState(false)
  const [detalhe, setDetalhe]           = useState(null)

  function carregar() {
    api.get('/compras', { params: { status: filtroStatus || undefined, page, pageSize: 20 } })
      .then(r => { setPedidos(r.data.items); setTotal(r.data.total) })
      .catch(() => {})
  }

  useEffect(() => { carregar() }, [page, filtroStatus])

  async function verDetalhe(id) {
    const { data } = await api.get(`/compras/${id}`)
    setDetalhe(data)
  }

  async function confirmar(id) {
    if (!confirm('Confirmar pedido e gerar titulos a pagar no financeiro?')) return
    try {
      const { data } = await api.put(`/compras/${id}/confirmar`)
      alert(`Pedido confirmado!\n${data.titulos} titulo(s) a pagar gerado(s).\nRecebimento criado em Estoque.`)
      setDetalhe(null)
      carregar()
    } catch (err) {
      alert(err.response?.data?.erro || 'Erro.')
    }
  }

  async function cancelar(id) {
    if (!confirm('Cancelar este pedido? Os titulos a pagar serao cancelados.')) return
    try {
      await api.put(`/compras/${id}/cancelar`)
      setDetalhe(null)
      carregar()
    } catch (err) {
      alert(err.response?.data?.erro || 'Erro.')
    }
  }

  return (
    <div>
      <div className="flex justify-between items-center mb-6">
        <h2 className="text-2xl font-bold">Pedidos de Compra</h2>
        <button onClick={() => setMostrarForm(true)}
          className="bg-slate-900 text-white px-4 py-2 rounded-lg text-sm">
          + Novo pedido
        </button>
      </div>

      <div className="flex gap-2 mb-4 flex-wrap">
        {['', 'rascunho', 'enviado', 'recebido', 'cancelado'].map(s => (
          <button key={s} onClick={() => { setFiltroStatus(s); setPage(1) }}
            className={`px-3 py-1.5 rounded-lg text-sm border ${filtroStatus === s ? 'bg-slate-900 text-white border-slate-900' : 'border-slate-200 text-slate-600 hover:bg-slate-50'}`}>
            {s === '' ? 'Todos' : STATUS_LABEL[s]}
          </button>
        ))}
      </div>

      <div className="bg-white rounded-2xl shadow overflow-hidden">
        <table className="w-full text-sm">
          <thead className="bg-slate-50 text-left">
            <tr>
              <th className="p-3">N.</th>
              <th className="p-3">Fornecedor</th>
              <th className="p-3">Data</th>
              <th className="p-3">Cond. Pgto</th>
              <th className="p-3">Status</th>
              <th className="p-3 text-right">Total</th>
              <th className="p-3"></th>
            </tr>
          </thead>
          <tbody>
            {pedidos.map(p => (
              <tr key={p.id} className="border-t hover:bg-slate-50">
                <td className="p-3 font-mono text-slate-500">#{p.numero}</td>
                <td className="p-3 font-medium">{p.fornecedorNome || '—'}</td>
                <td className="p-3 text-slate-500">
                  {new Date(p.dataPedido + 'T12:00:00').toLocaleDateString('pt-BR')}
                </td>
                <td className="p-3 text-slate-500 text-xs">{p.condicaoNome || '—'}</td>
                <td className="p-3">
                  <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${STATUS_COR[p.status]}`}>
                    {STATUS_LABEL[p.status]}
                  </span>
                </td>
                <td className="p-3 text-right font-medium">R$ {p.valorTotal?.toFixed(2)}</td>
                <td className="p-3 text-right">
                  <button onClick={() => verDetalhe(p.id)} className="text-xs text-blue-600 hover:underline">Ver</button>
                </td>
              </tr>
            ))}
            {pedidos.length === 0 && (
              <tr><td colSpan="7" className="p-6 text-center text-slate-400">Nenhum pedido encontrado</td></tr>
            )}
          </tbody>
        </table>
      </div>

      {detalhe && (
        <ModalDetalhe pedido={detalhe} onClose={() => setDetalhe(null)}
          onConfirmar={confirmar} onCancelar={cancelar} />
      )}

      {mostrarForm && (
        <ModalNovoPedido onClose={() => setMostrarForm(false)}
          onSaved={() => { setMostrarForm(false); carregar() }} />
      )}
    </div>
  )
}

function ModalDetalhe({ pedido, onClose, onConfirmar, onCancelar }) {
  return (
    <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50 p-4" onClick={onClose}>
      <div className="bg-white rounded-2xl shadow-xl p-6 w-full max-w-2xl max-h-[90vh] overflow-auto"
        onClick={e => e.stopPropagation()}>
        <div className="flex justify-between items-start mb-4">
          <div>
            <h3 className="font-bold text-lg">Pedido #{pedido.numero}</h3>
            <p className="text-sm text-slate-500">{pedido.fornecedor?.nome || 'Sem fornecedor'}</p>
            {pedido.condicaoPagamento && (
              <p className="text-xs text-slate-400 mt-0.5">
                {pedido.condicaoPagamento.nome} · {pedido.formaPagamento || '—'}
              </p>
            )}
          </div>
          <span className={`text-xs px-3 py-1 rounded-full font-medium ${STATUS_COR[pedido.status]}`}>
            {STATUS_LABEL[pedido.status]}
          </span>
        </div>

        <table className="w-full text-sm mb-4">
          <thead className="bg-slate-50">
            <tr>
              <th className="p-2 text-left">Produto</th>
              <th className="p-2 text-center">Qtd</th>
              <th className="p-2 text-center">Un</th>
              <th className="p-2 text-right">V. Unit.</th>
              <th className="p-2 text-right">Total</th>
              <th className="p-2 text-center">Uso</th>
            </tr>
          </thead>
          <tbody>
            {pedido.itens?.map(item => (
              <tr key={item.id} className="border-t">
                <td className="p-2">{item.nomeProduto}</td>
                <td className="p-2 text-center">{item.quantidade}</td>
                <td className="p-2 text-center text-slate-500">{item.unidade}</td>
                <td className="p-2 text-right">R$ {item.valorUnitario?.toFixed(2)}</td>
                <td className="p-2 text-right font-medium">R$ {item.valorTotal?.toFixed(2)}</td>
                <td className="p-2 text-center">
                  <span className={`text-xs px-2 py-0.5 rounded-full ${item.uso === 'venda' ? 'bg-blue-100 text-blue-700' : 'bg-amber-100 text-amber-700'}`}>
                    {item.uso === 'venda' ? 'Venda' : 'Interno'}
                  </span>
                </td>
              </tr>
            ))}
          </tbody>
          <tfoot>
            <tr className="border-t font-semibold">
              <td colSpan="4" className="p-2 text-right">Total:</td>
              <td className="p-2 text-right">R$ {pedido.valorTotal?.toFixed(2)}</td>
              <td></td>
            </tr>
          </tfoot>
        </table>

        {pedido.obs && <p className="text-sm text-slate-500 mb-4">Obs: {pedido.obs}</p>}

        <div className="flex gap-2 flex-wrap">
          {pedido.status === 'rascunho' && (
            <button onClick={() => onConfirmar(pedido.id)}
              className="bg-emerald-600 text-white px-4 py-2 rounded-lg text-sm font-medium">
              Confirmar e gerar titulos
            </button>
          )}
          {(pedido.status === 'rascunho' || pedido.status === 'enviado') && (
            <button onClick={() => onCancelar(pedido.id)}
              className="border border-red-200 text-red-600 px-4 py-2 rounded-lg text-sm">
              Cancelar pedido
            </button>
          )}
          <button onClick={onClose} className="ml-auto border px-4 py-2 rounded-lg text-sm">Fechar</button>
        </div>
      </div>
    </div>
  )
}

function ModalNovoPedido({ onClose, onSaved }) {
  const [fornecedores, setFornecedores]     = useState([])
  const [produtos, setProdutos]             = useState([])
  const [condicoes, setCondicoes]           = useState([])
  const [fornecedorId, setFornecedorId]     = useState('')
  const [condicaoId, setCondicaoId]         = useState('')
  const [formaPagamento, setFormaPagamento] = useState('boleto')
  const [dataPedido, setDataPedido]         = useState(new Date().toISOString().split('T')[0])
  const [obs, setObs]                       = useState('')
  const [itens, setItens]                   = useState([
    { produtoId: '', nomeProduto: '', quantidade: 1, unidade: 'un', valorUnitario: '', uso: 'venda' }
  ])

  useEffect(() => {
    api.get('/fornecedores', { params: { pageSize: 100 } }).then(r => setFornecedores(r.data.items)).catch(() => {})
    api.get('/produtos', { params: { pageSize: 200 } }).then(r => setProdutos(r.data.items || r.data)).catch(() => {})
    api.get('/compras/condicoes').then(r => setCondicoes(r.data)).catch(() => {})
  }, [])

  function addItem() {
    setItens([...itens, { produtoId: '', nomeProduto: '', quantidade: 1, unidade: 'un', valorUnitario: '', uso: 'venda' }])
  }

  function updateItem(i, field, val) {
    const novos = itens.map((item, idx) => {
      if (idx !== i) return item
      const updated = { ...item, [field]: val }
      if (field === 'produtoId' && val) {
        const prod = produtos.find(p => p.id === val)
        if (prod) {
          updated.nomeProduto   = prod.nome
          updated.valorUnitario = prod.precoCusto || prod.precoVenda || ''
          updated.unidade       = prod.unidade || 'un'
        }
      }
      return updated
    })
    setItens(novos)
  }

  function removeItem(i) { setItens(itens.filter((_, idx) => idx !== i)) }

  const totalGeral = itens.reduce((s, i) =>
    s + (parseFloat(i.quantidade || 0) * parseFloat(i.valorUnitario || 0)), 0)

  const condicaoSelecionada = condicoes.find(c => c.id === condicaoId)

  async function salvar(e) {
    e.preventDefault()
    const itensFiltrados = itens.filter(i => i.nomeProduto || i.produtoId)
    if (itensFiltrados.length === 0) {
      alert('Adicione pelo menos um item ao pedido.')
      return
    }
    await api.post('/compras', {
      fornecedorId: fornecedorId || null,
      condicaoPagamentoId: condicaoId || null,
      formaPagamento: formaPagamento || null,
      dataPedido,
      obs: obs || null,
      itens: itensFiltrados.map(i => ({
        produtoId:     i.produtoId || null,
        nomeProduto:   i.nomeProduto,
        quantidade:    parseFloat(i.quantidade),
        unidade:       i.unidade,
        valorUnitario: parseFloat(i.valorUnitario || 0),
        uso:           i.uso
      }))
    })
    onSaved()
  }

  return (
    <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50 p-4" onClick={onClose}>
      <div className="bg-white rounded-2xl shadow-xl p-6 w-full max-w-3xl max-h-[90vh] overflow-auto"
        onClick={e => e.stopPropagation()}>
        <h3 className="font-bold text-lg mb-4">Novo Pedido de Compra</h3>

        <form onSubmit={salvar} className="space-y-4">
          {/* Cabecalho */}
          <div className="grid grid-cols-2 gap-3">
            <select className="border rounded-lg px-3 py-2 col-span-2"
              value={fornecedorId} onChange={e => setFornecedorId(e.target.value)}>
              <option value="">Fornecedor (opcional)</option>
              {fornecedores.map(f => <option key={f.id} value={f.id}>{f.nome}</option>)}
            </select>

            <select className="border rounded-lg px-3 py-2"
              value={condicaoId} onChange={e => setCondicaoId(e.target.value)}>
              <option value="">Condicao de pagamento</option>
              {condicoes.map(c => <option key={c.id} value={c.id}>{c.nome} ({c.parcelas}x)</option>)}
            </select>

            <select className="border rounded-lg px-3 py-2"
              value={formaPagamento} onChange={e => setFormaPagamento(e.target.value)}>
              <option value="boleto">Boleto</option>
              <option value="pix">PIX</option>
              <option value="transferencia">Transferencia</option>
              <option value="dinheiro">Dinheiro</option>
              <option value="cartao">Cartao</option>
              <option value="cheque">Cheque</option>
            </select>

            <div>
              <label className="text-xs text-slate-500 block mb-1">Data do pedido</label>
              <input type="date" className="border rounded-lg px-3 py-2 w-full"
                value={dataPedido} onChange={e => setDataPedido(e.target.value)} />
            </div>

            {condicaoSelecionada && (
              <div className="flex items-center bg-blue-50 rounded-lg px-3 py-2 text-sm text-blue-700">
                {condicaoSelecionada.parcelas}x de R$ {(totalGeral / condicaoSelecionada.parcelas).toFixed(2)} — vence a cada {condicaoSelecionada.intervaloDias} dias
              </div>
            )}
          </div>

          {/* Itens */}
          <div>
            <div className="flex justify-between items-center mb-2">
              <span className="font-semibold text-sm">Itens do pedido</span>
              <button type="button" onClick={addItem}
                className="text-xs text-blue-600 hover:underline">+ Adicionar item</button>
            </div>
            <div className="space-y-2">
              {itens.map((item, i) => (
                <div key={i} className="grid grid-cols-12 gap-2 items-center bg-slate-50 p-2 rounded-xl">
                  <select className="border rounded-lg px-2 py-1.5 text-sm col-span-3"
                    value={item.produtoId} onChange={e => updateItem(i, 'produtoId', e.target.value)}>
                    <option value="">Selecionar produto</option>
                    {produtos.map(p => <option key={p.id} value={p.id}>{p.nome}</option>)}
                  </select>
                  <input className="border rounded-lg px-2 py-1.5 text-sm col-span-3"
                    placeholder="Nome *" required
                    value={item.nomeProduto} onChange={e => updateItem(i, 'nomeProduto', e.target.value)} />
                  <input className="border rounded-lg px-2 py-1.5 text-sm col-span-1 text-center"
                    type="number" placeholder="Qtd" min="0.001" step="0.001"
                    value={item.quantidade} onChange={e => updateItem(i, 'quantidade', e.target.value)} />
                  <input className="border rounded-lg px-2 py-1.5 text-sm col-span-1"
                    placeholder="Un"
                    value={item.unidade} onChange={e => updateItem(i, 'unidade', e.target.value)} />
                  <input className="border rounded-lg px-2 py-1.5 text-sm col-span-2"
                    type="number" placeholder="V. unit." step="0.01"
                    value={item.valorUnitario} onChange={e => updateItem(i, 'valorUnitario', e.target.value)} />
                  <select className="border rounded-lg px-2 py-1.5 text-sm col-span-1"
                    value={item.uso} onChange={e => updateItem(i, 'uso', e.target.value)}>
                    <option value="venda">Venda</option>
                    <option value="interno">Interno</option>
                  </select>
                  {itens.length > 1 && (
                    <button type="button" onClick={() => removeItem(i)}
                      className="text-red-400 text-xs hover:underline">X</button>
                  )}
                </div>
              ))}
            </div>
            <div className="text-right text-sm font-semibold mt-2 text-slate-700">
              Total: R$ {totalGeral.toFixed(2)}
            </div>
          </div>

          <textarea className="border rounded-lg px-3 py-2 w-full text-sm" rows={2}
            placeholder="Observacoes" value={obs} onChange={e => setObs(e.target.value)} />

          <div className="bg-amber-50 border border-amber-200 rounded-xl p-3 text-xs text-amber-800">
            O pedido sera salvo como Rascunho. Para gerar os titulos a pagar e o recebimento, abra o pedido e clique em "Confirmar".
          </div>

          <div className="flex gap-2 justify-end">
            <button type="button" onClick={onClose} className="border px-4 py-2 rounded-lg text-sm">Cancelar</button>
            <button type="submit" className="bg-emerald-600 text-white px-5 py-2 rounded-lg text-sm font-medium">
              Salvar rascunho
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
