import { useEffect, useState } from 'react'
import api from '../api/client'

const CAT = { racao: '🦴 Ração', medicamento: '💊 Medicamento', higiene: '🧴 Higiene', acessorio: '🦮 Acessório' }

export default function Estoque() {
  const [produtos, setProdutos] = useState([])
  const [sugestoes, setSugestoes] = useState([])
  const [aba, setAba] = useState('lista')
  const [modal, setModal] = useState(false)
  const [ajuste, setAjuste] = useState(null)

  function carregar() {
    api.get('/produtos').then((r) => setProdutos(r.data)).catch(() => {})
    api.get('/produtos/sugestao-compra').then((r) => setSugestoes(r.data)).catch(() => {})
  }
  useEffect(() => { carregar() }, [])

  return (
    <div>
      <div className="flex justify-between items-center mb-6">
        <h2 className="text-2xl font-bold">Estoque</h2>
        <button onClick={() => setModal(true)} className="bg-slate-900 text-white px-4 py-2 rounded-lg">
          + Novo produto
        </button>
      </div>

      <div className="flex gap-2 mb-4 border-b">
        <button onClick={() => setAba('lista')}
          className={`px-4 py-2 text-sm font-medium border-b-2 -mb-px ${aba === 'lista' ? 'border-slate-900' : 'border-transparent text-slate-400'}`}>
          Produtos ({produtos.length})
        </button>
        <button onClick={() => setAba('compra')}
          className={`px-4 py-2 text-sm font-medium border-b-2 -mb-px ${aba === 'compra' ? 'border-slate-900' : 'border-transparent text-slate-400'}`}>
          Sugestão de compra {sugestoes.length > 0 && <span className="ml-1 bg-red-100 text-red-700 text-xs px-2 py-0.5 rounded-full">{sugestoes.length}</span>}
        </button>
      </div>

      {aba === 'lista' && (
        <div className="bg-white rounded-2xl shadow overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-slate-50 text-left">
              <tr>
                <th className="p-3">Produto</th><th className="p-3">Categoria</th>
                <th className="p-3 text-right">Venda</th><th className="p-3 text-center">Estoque</th>
                <th className="p-3"></th>
              </tr>
            </thead>
            <tbody>
              {produtos.map((p) => (
                <tr key={p.id} className="border-t hover:bg-slate-50">
                  <td className="p-3 font-medium">{p.nome}</td>
                  <td className="p-3 text-xs">{CAT[p.categoria] || p.categoria || '—'}</td>
                  <td className="p-3 text-right">R$ {p.precoVenda.toFixed(2)}</td>
                  <td className="p-3 text-center">
                    <span className={`px-2 py-1 rounded-full text-xs ${p.abaixoMinimo ? 'bg-red-100 text-red-700' : 'bg-emerald-100 text-emerald-700'}`}>
                      {p.estoqueAtual} {p.unidade}
                    </span>
                  </td>
                  <td className="p-3 text-right">
                    <button onClick={() => setAjuste(p)} className="text-xs text-blue-600 hover:underline">Ajustar</button>
                  </td>
                </tr>
              ))}
              {produtos.length === 0 && <tr><td colSpan="5" className="p-6 text-center text-slate-400">Nenhum produto</td></tr>}
            </tbody>
          </table>
        </div>
      )}

      {aba === 'compra' && (
        <div>
          {sugestoes.length === 0 ? (
            <div className="bg-emerald-50 rounded-2xl p-8 text-center text-emerald-700">
              ✅ Estoque saudável! Nenhum produto abaixo do mínimo.
            </div>
          ) : (
            <div className="grid gap-3">
              {sugestoes.map((s) => (
                <div key={s.produtoId} className="bg-white rounded-xl shadow p-4 flex items-center justify-between">
                  <div>
                    <div className="font-medium">{s.nome}</div>
                    <div className="text-xs text-slate-400 mt-1">
                      Tem {s.estoqueAtual} · mínimo {s.estoqueMinimo}
                    </div>
                  </div>
                  <div className="text-right">
                    <div className="text-xs text-slate-400">Comprar</div>
                    <div className="text-lg font-bold text-amber-600">{s.quantidadeSugerida}</div>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      )}

      {modal && <ModalProduto onClose={() => setModal(false)} onSaved={() => { setModal(false); carregar() }} />}
      {ajuste && <ModalAjuste produto={ajuste} onClose={() => setAjuste(null)} onSaved={() => { setAjuste(null); carregar() }} />}
    </div>
  )
}

function ModalProduto({ onClose, onSaved }) {
  const [f, setF] = useState({ nome: '', categoria: 'racao', unidade: 'un', precoCusto: '', precoVenda: '', estoqueAtual: '', estoqueMinimo: '', estoqueIdeal: '' })
  async function salvar(e) {
    e.preventDefault()
    await api.post('/produtos', {
      ...f, codigoBarras: null, controlaValidade: false,
      precoCusto: +f.precoCusto || 0, precoVenda: +f.precoVenda || 0,
      estoqueAtual: +f.estoqueAtual || 0, estoqueMinimo: +f.estoqueMinimo || 0,
      estoqueIdeal: f.estoqueIdeal ? +f.estoqueIdeal : null
    })
    onSaved()
  }
  return (
    <Overlay titulo="Novo produto" onClose={onClose}>
      <form onSubmit={salvar} className="grid grid-cols-2 gap-3">
        <input className="border rounded-lg px-3 py-2 col-span-2" placeholder="Nome do produto" required
          value={f.nome} onChange={(e) => setF({ ...f, nome: e.target.value })} />
        <select className="border rounded-lg px-3 py-2" value={f.categoria} onChange={(e) => setF({ ...f, categoria: e.target.value })}>
          {Object.entries(CAT).map(([k, v]) => <option key={k} value={k}>{v}</option>)}
        </select>
        <input className="border rounded-lg px-3 py-2" placeholder="Unidade (un, kg)" value={f.unidade} onChange={(e) => setF({ ...f, unidade: e.target.value })} />
        <input className="border rounded-lg px-3 py-2" placeholder="Preço custo" type="number" step="0.01" value={f.precoCusto} onChange={(e) => setF({ ...f, precoCusto: e.target.value })} />
        <input className="border rounded-lg px-3 py-2" placeholder="Preço venda" type="number" step="0.01" value={f.precoVenda} onChange={(e) => setF({ ...f, precoVenda: e.target.value })} />
        <input className="border rounded-lg px-3 py-2" placeholder="Estoque atual" type="number" step="0.01" value={f.estoqueAtual} onChange={(e) => setF({ ...f, estoqueAtual: e.target.value })} />
        <input className="border rounded-lg px-3 py-2" placeholder="Estoque mínimo" type="number" step="0.01" value={f.estoqueMinimo} onChange={(e) => setF({ ...f, estoqueMinimo: e.target.value })} />
        <input className="border rounded-lg px-3 py-2 col-span-2" placeholder="Estoque ideal (p/ sugestão de compra)" type="number" step="0.01" value={f.estoqueIdeal} onChange={(e) => setF({ ...f, estoqueIdeal: e.target.value })} />
        <button className="bg-emerald-600 text-white py-2 rounded-lg col-span-2">Salvar</button>
      </form>
    </Overlay>
  )
}

function ModalAjuste({ produto, onClose, onSaved }) {
  const [f, setF] = useState({ tipo: 'entrada', quantidade: '', motivo: '' })
  async function salvar(e) {
    e.preventDefault()
    await api.post(`/produtos/${produto.id}/ajuste`, { ...f, quantidade: +f.quantidade || 0 })
    onSaved()
  }
  return (
    <Overlay titulo={`Ajustar: ${produto.nome}`} onClose={onClose}>
      <form onSubmit={salvar} className="grid gap-3">
        <div className="text-sm text-slate-500">Estoque atual: <b>{produto.estoqueAtual} {produto.unidade}</b></div>
        <select className="border rounded-lg px-3 py-2" value={f.tipo} onChange={(e) => setF({ ...f, tipo: e.target.value })}>
          <option value="entrada">Entrada (compra)</option>
          <option value="saida_consumo">Consumo interno</option>
          <option value="saida_avaria">Avaria</option>
          <option value="saida_validade">Perda de validade</option>
          <option value="saida_doacao">Doação</option>
          <option value="ajuste">Ajuste / inventário</option>
        </select>
        <input className="border rounded-lg px-3 py-2" placeholder="Quantidade" type="number" step="0.01" required
          value={f.quantidade} onChange={(e) => setF({ ...f, quantidade: e.target.value })} />
        <input className="border rounded-lg px-3 py-2" placeholder="Motivo (opcional)"
          value={f.motivo} onChange={(e) => setF({ ...f, motivo: e.target.value })} />
        <button className="bg-emerald-600 text-white py-2 rounded-lg">Confirmar</button>
      </form>
    </Overlay>
  )
}

function Overlay({ titulo, children, onClose }) {
  return (
    <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50 p-4" onClick={onClose}>
      <div className="bg-white rounded-2xl shadow-xl p-6 w-full max-w-md" onClick={(e) => e.stopPropagation()}>
        <div className="flex justify-between items-center mb-4">
          <h3 className="font-bold text-lg">{titulo}</h3>
          <button onClick={onClose} className="text-slate-400 hover:text-slate-600 text-xl">×</button>
        </div>
        {children}
      </div>
    </div>
  )
}
