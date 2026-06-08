import { useEffect, useState } from 'react'
import api from '../api/client'

const STATUS_COR = {
  pendente:   'bg-amber-100 text-amber-700',
  conferido:  'bg-blue-100 text-blue-700',
  finalizado: 'bg-emerald-100 text-emerald-700',
  cancelado:  'bg-red-100 text-red-600'
}

export default function Recebimentos() {
  const [recebimentos, setRecebimentos] = useState([])
  const [filtroStatus, setFiltroStatus] = useState('')
  const [detalhe, setDetalhe]           = useState(null)

  function carregar() {
    api.get('/recebimentos', { params: { status: filtroStatus || undefined } })
      .then(r => setRecebimentos(r.data))
      .catch(() => {})
  }

  useEffect(() => { carregar() }, [filtroStatus])

  async function verDetalhe(id) {
    const { data } = await api.get(`/recebimentos/${id}`)
    setDetalhe(data)
  }

  const pendentes = recebimentos.filter(r => r.status === 'pendente').length

  return (
    <div>
      <div className="flex justify-between items-center mb-6">
        <div>
          <h2 className="text-2xl font-bold">Recebimento de Mercadoria</h2>
          {pendentes > 0 && (
            <p className="text-sm text-amber-600 font-medium mt-0.5">
              {pendentes} recebimento(s) aguardando conferencia
            </p>
          )}
        </div>
      </div>

      {/* Filtros */}
      <div className="flex gap-2 mb-4 flex-wrap">
        {['', 'pendente', 'conferido', 'finalizado', 'cancelado'].map(s => (
          <button key={s} onClick={() => setFiltroStatus(s)}
            className={`px-3 py-1.5 rounded-lg text-sm border ${
              filtroStatus === s
                ? 'bg-slate-900 text-white border-slate-900'
                : 'border-slate-200 text-slate-600 hover:bg-slate-50'
            }`}>
            {s === '' ? 'Todos' : s.charAt(0).toUpperCase() + s.slice(1)}
          </button>
        ))}
      </div>

      <div className="bg-white rounded-2xl shadow overflow-hidden">
        <table className="w-full text-sm">
          <thead className="bg-slate-50 text-left">
            <tr>
              <th className="p-3">Pedido</th>
              <th className="p-3">Fornecedor</th>
              <th className="p-3">Data estimada</th>
              <th className="p-3">Status</th>
              <th className="p-3"></th>
            </tr>
          </thead>
          <tbody>
            {recebimentos.map(r => (
              <tr key={r.id} className="border-t hover:bg-slate-50">
                <td className="p-3 font-mono text-slate-500">
                  {r.pedidoNumero ? `#${r.pedidoNumero}` : '—'}
                </td>
                <td className="p-3 font-medium">{r.fornecedorNome || '—'}</td>
                <td className="p-3 text-slate-500">
                  {new Date(r.dataRecebimento + 'T12:00:00').toLocaleDateString('pt-BR')}
                </td>
                <td className="p-3">
                  <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${STATUS_COR[r.status]}`}>
                    {r.status}
                  </span>
                </td>
                <td className="p-3 text-right">
                  {r.status !== 'finalizado' && r.status !== 'cancelado' && (
                    <button onClick={() => verDetalhe(r.id)}
                      className="text-xs text-blue-600 hover:underline font-medium">
                      Conferir
                    </button>
                  )}
                  {r.status === 'finalizado' && (
                    <button onClick={() => verDetalhe(r.id)}
                      className="text-xs text-slate-400 hover:underline">
                      Ver detalhe
                    </button>
                  )}
                </td>
              </tr>
            ))}
            {recebimentos.length === 0 && (
              <tr>
                <td colSpan="5" className="p-6 text-center text-slate-400">
                  Nenhum recebimento encontrado
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      {detalhe && (
        <ModalConferencia
          recebimento={detalhe}
          onClose={() => setDetalhe(null)}
          onFinalizado={() => { setDetalhe(null); carregar() }}
        />
      )}
    </div>
  )
}

function ModalConferencia({ recebimento, onClose, onFinalizado }) {
  const [itens, setItens]     = useState(
    recebimento.itens.map(i => ({ ...i, qtdRecebida: i.quantidadeRecebida || i.quantidadePedida }))
  )
  const [salvando, setSalvando] = useState(false)

  async function conferir() {
    setSalvando(true)
    try {
      await api.put(`/recebimentos/${recebimento.id}/itens`,
        itens.map(i => ({ itemId: i.id, quantidadeRecebida: parseFloat(i.qtdRecebida) }))
      )
      alert('Conferencia salva! Clique em Finalizar para atualizar o estoque.')
    } catch { alert('Erro ao salvar conferencia.') }
    finally { setSalvando(false) }
  }

  async function finalizar() {
    if (!confirm('Finalizar recebimento e atualizar o estoque agora?')) return
    setSalvando(true)
    try {
      await api.put(`/recebimentos/${recebimento.id}/itens`,
        itens.map(i => ({ itemId: i.id, quantidadeRecebida: parseFloat(i.qtdRecebida) }))
      )
      await api.put(`/recebimentos/${recebimento.id}/finalizar`)
      alert('Estoque atualizado com sucesso!')
      onFinalizado()
    } catch (err) {
      alert(err.response?.data?.erro || 'Erro ao finalizar.')
    } finally { setSalvando(false) }
  }

  return (
    <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50 p-4" onClick={onClose}>
      <div className="bg-white rounded-2xl shadow-xl p-6 w-full max-w-2xl max-h-[90vh] overflow-auto"
        onClick={e => e.stopPropagation()}>

        <h3 className="font-bold text-lg mb-1">Conferencia de Recebimento</h3>
        <p className="text-sm text-slate-500 mb-1">
          Pedido #{recebimento.pedido?.numero} · {recebimento.pedido?.fornecedor?.nome || 'Sem fornecedor'}
        </p>
        <p className="text-xs text-slate-400 mb-4">
          Confira a quantidade fisicamente recebida. Ao finalizar, o estoque sera atualizado automaticamente.
        </p>

        <table className="w-full text-sm mb-4">
          <thead className="bg-slate-50">
            <tr>
              <th className="p-2 text-left">Produto</th>
              <th className="p-2 text-center">Pedido</th>
              <th className="p-2 text-center">Qtd Recebida</th>
              <th className="p-2 text-center">Uso</th>
            </tr>
          </thead>
          <tbody>
            {itens.map((item, i) => (
              <tr key={item.id} className={`border-t ${
                parseFloat(item.qtdRecebida) < item.quantidadePedida ? 'bg-amber-50' : ''
              }`}>
                <td className="p-2 font-medium">{item.nomeProduto}</td>
                <td className="p-2 text-center text-slate-500">{item.quantidadePedida} {item.unidade}</td>
                <td className="p-2 text-center">
                  <input type="number" min="0" step="0.001"
                    className="border rounded-lg px-2 py-1 w-24 text-center text-sm"
                    value={item.qtdRecebida}
                    onChange={e => setItens(itens.map((it, idx) =>
                      idx === i ? { ...it, qtdRecebida: e.target.value } : it
                    ))} />
                </td>
                <td className="p-2 text-center">
                  <span className={`text-xs px-2 py-0.5 rounded-full ${
                    item.uso === 'venda' ? 'bg-blue-100 text-blue-700' : 'bg-amber-100 text-amber-700'
                  }`}>
                    {item.uso === 'venda' ? 'Venda' : 'Interno'}
                  </span>
                </td>
              </tr>
            ))}
          </tbody>
        </table>

        {recebimento.status === 'finalizado' ? (
          <div className="text-center py-2">
            <span className="text-emerald-600 font-medium text-sm">Recebimento ja finalizado</span>
          </div>
        ) : (
          <div className="flex gap-2 justify-end">
            <button onClick={onClose} className="border px-4 py-2 rounded-lg text-sm">Fechar</button>
            <button onClick={conferir} disabled={salvando}
              className="border border-blue-300 text-blue-600 px-4 py-2 rounded-lg text-sm disabled:opacity-40">
              Salvar conferencia
            </button>
            <button onClick={finalizar} disabled={salvando}
              className="bg-emerald-600 text-white px-4 py-2 rounded-lg text-sm font-medium disabled:opacity-40">
              Finalizar e atualizar estoque
            </button>
          </div>
        )}
      </div>
    </div>
  )
}
