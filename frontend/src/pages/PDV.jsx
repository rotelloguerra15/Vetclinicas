import { useEffect, useState, useCallback } from 'react'
import api from '../api/client'

const FORMAS = [
  { value: 'PIX',               label: 'PIX' },
  { value: 'Dinheiro',          label: 'Dinheiro' },
  { value: 'Cartão de Débito',  label: 'Cartão Débito' },
  { value: 'Cartão de Crédito', label: 'Cartão Crédito' },
  { value: 'Boleto',            label: 'Boleto' },
]

function fmt(v) {
  return Number(v ?? 0).toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
}

// ─── Abertura de caixa ─────────────────────────────────────────────
function AberturaCaixa({ onAberto }) {
  const [saldo, setSaldo] = useState('')
  const [saving, setSaving] = useState(false)
  const [saldoAnterior, setSaldoAnterior] = useState(null)

  useEffect(() => {
    // Busca saldo final do dia anterior para exibir como referência
    api.get('/caixa/historico?ultimos=1')
      .then(r => { if (r.data[0]?.saldoFinal) setSaldoAnterior(r.data[0].saldoFinal) })
      .catch(() => {})
  }, [])

  async function abrir() {
    setSaving(true)
    try {
      const { data } = await api.post('/caixa/abrir', { saldoInicial: +saldo || 0 })
      onAberto(data.saldoInicial)
    } catch (e) {
      alert(e.response?.data?.erro || 'Erro ao abrir caixa')
    } finally { setSaving(false) }
  }

  return (
    <div className="max-w-sm mx-auto mt-16">
      <div className="bg-white rounded-2xl shadow-lg p-8 text-center">
        <div className="text-4xl mb-4">🏪</div>
        <h2 className="text-xl font-bold mb-2">Abrir Caixa</h2>
        {saldoAnterior !== null ? (
          <p className="text-slate-500 text-sm mb-2">
            Saldo final do dia anterior: <strong>{fmt(saldoAnterior)}</strong>
          </p>
        ) : null}
        <p className="text-slate-400 text-xs mb-4">
          O saldo inicial será preenchido automaticamente com o saldo final do dia anterior.
          Altere se necessário.
        </p>
        <input
          className="border rounded-lg px-3 py-2 w-full mb-4 text-center text-lg"
          placeholder="R$ 0,00"
          type="number" step="0.01"
          value={saldo || saldoAnterior || ''}
          onChange={e => setSaldo(e.target.value)}
        />
        <button onClick={abrir} disabled={saving}
          className="w-full bg-emerald-600 text-white py-3 rounded-lg font-medium disabled:opacity-50">
          {saving ? 'Abrindo...' : 'Abrir Caixa'}
        </button>
      </div>
    </div>
  )
}

// ─── Modal Retirada / Depósito ─────────────────────────────────────
function ModalMovimentacao({ tipo, onClose, onSaved }) {
  const [valor, setValor]       = useState('')
  const [descricao, setDescricao] = useState('')
  const [saving, setSaving]     = useState(false)

  async function salvar() {
    if (!valor || +valor <= 0) return alert('Informe um valor válido.')
    setSaving(true)
    try {
      await api.post('/caixa/movimentacao', { tipo, valor: +valor, descricao })
      onSaved()
      onClose()
    } catch (e) {
      alert(e.response?.data?.erro || 'Erro ao registrar')
    } finally { setSaving(false) }
  }

  const isRetirada = tipo === 'retirada'
  return (
    <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50 p-4" onClick={onClose}>
      <div className="bg-white rounded-2xl shadow-xl p-6 w-full max-w-sm" onClick={e => e.stopPropagation()}>
        <h3 className="font-bold text-lg mb-4">
          {isRetirada ? '💸 Retirada de Caixa' : '💰 Depósito em Caixa'}
        </h3>
        <div className="space-y-3">
          <div>
            <label className="text-xs font-medium text-slate-500 mb-1 block">Valor (R$)</label>
            <input type="number" step="0.01" value={valor} onChange={e => setValor(e.target.value)}
              className="w-full border rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
              placeholder="0,00" autoFocus />
          </div>
          <div>
            <label className="text-xs font-medium text-slate-500 mb-1 block">Descrição (opcional)</label>
            <input type="text" value={descricao} onChange={e => setDescricao(e.target.value)}
              className="w-full border rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
              placeholder={isRetirada ? 'Ex: Pagamento fornecedor' : 'Ex: Reforço de caixa'} />
          </div>
        </div>
        <div className="flex gap-3 mt-5">
          <button onClick={salvar} disabled={saving}
            className={`flex-1 text-white py-2 rounded-lg text-sm font-medium disabled:opacity-50
              ${isRetirada ? 'bg-red-600 hover:bg-red-700' : 'bg-emerald-600 hover:bg-emerald-700'}`}>
            {saving ? '...' : `Confirmar ${isRetirada ? 'Retirada' : 'Depósito'}`}
          </button>
          <button onClick={onClose} className="px-4 py-2 border rounded-lg text-sm text-slate-500 hover:bg-slate-50">
            Cancelar
          </button>
        </div>
      </div>
    </div>
  )
}

// ─── Modal Fechamento de Caixa ─────────────────────────────────────
function ModalFechamento({ caixa, onClose, onFechado }) {
  const [saving, setSaving] = useState(false)

  const retiradas = caixa.movimentacoes?.filter(m => m.tipo === 'retirada').reduce((s,m) => s+m.valor, 0) || 0
  const depositos = caixa.movimentacoes?.filter(m => m.tipo === 'deposito').reduce((s,m) => s+m.valor, 0) || 0

  async function fechar() {
    setSaving(true)
    try {
      const { data } = await api.put('/caixa/fechar', { saldoFinal: null, obs: null })
      onFechado(data)
      onClose()
    } catch (e) {
      alert(e.response?.data?.erro || 'Erro ao fechar caixa')
    } finally { setSaving(false) }
  }

  return (
    <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50 p-4" onClick={onClose}>
      <div className="bg-white rounded-2xl shadow-xl p-6 w-full max-w-md" onClick={e => e.stopPropagation()}>
        <h3 className="font-bold text-lg mb-5">📊 Fechamento de Caixa</h3>

        <div className="space-y-2 text-sm mb-5">
          <div className="flex justify-between py-2 border-b">
            <span className="text-slate-600">Saldo inicial</span>
            <span className="font-medium">{fmt(caixa.saldoInicial)}</span>
          </div>
          <div className="flex justify-between py-2 border-b">
            <span className="text-slate-600">Entradas (dinheiro)</span>
            <span className="font-medium text-emerald-600">+ {fmt(caixa.totalVendas + caixa.totalServicos)}</span>
          </div>
          {depositos > 0 && (
            <div className="flex justify-between py-2 border-b">
              <span className="text-slate-600">Depósitos</span>
              <span className="font-medium text-emerald-600">+ {fmt(depositos)}</span>
            </div>
          )}
          {retiradas > 0 && (
            <div className="flex justify-between py-2 border-b">
              <span className="text-slate-600">Retiradas</span>
              <span className="font-medium text-red-500">− {fmt(retiradas)}</span>
            </div>
          )}
          <div className="flex justify-between py-3 font-bold text-base">
            <span>Saldo Final Esperado</span>
            <span className="text-blue-700">
              {fmt(caixa.saldoInicial + caixa.totalVendas + caixa.totalServicos + depositos - retiradas)}
            </span>
          </div>
        </div>

        {caixa.movimentacoes?.length > 0 && (
          <div className="bg-slate-50 rounded-xl p-3 mb-5">
            <p className="text-xs font-semibold text-slate-500 mb-2">Movimentações do dia</p>
            {caixa.movimentacoes.map(m => (
              <div key={m.id} className="flex justify-between text-xs py-1">
                <span className={m.tipo === 'retirada' ? 'text-red-500' : 'text-emerald-600'}>
                  {m.tipo === 'retirada' ? '↓' : '↑'} {m.descricao || m.tipo}
                </span>
                <span className="font-medium">{fmt(m.valor)}</span>
              </div>
            ))}
          </div>
        )}

        <p className="text-xs text-slate-400 mb-4">
          ⚠️ Após fechar, o caixa só poderá ser reaberto com aprovação de administrador.
          O saldo final será registrado no Financeiro automaticamente.
        </p>

        <div className="flex gap-3">
          <button onClick={fechar} disabled={saving}
            className="flex-1 bg-red-600 text-white py-2 rounded-lg text-sm font-medium hover:bg-red-700 disabled:opacity-50">
            {saving ? 'Fechando...' : 'Confirmar Fechamento'}
          </button>
          <button onClick={onClose} className="px-4 py-2 border rounded-lg text-sm text-slate-500 hover:bg-slate-50">
            Cancelar
          </button>
        </div>
      </div>
    </div>
  )
}

// ─── Card OS ───────────────────────────────────────────────────────
function CardOS({ os, selecionada, onClick }) {
  return (
    <button onClick={onClick}
      className={`text-left w-full rounded-xl border-2 p-3 transition
        ${selecionada ? 'border-emerald-500 bg-emerald-50' : 'border-slate-200 bg-white hover:border-slate-300'}`}>
      <div className="font-bold text-slate-800">{os.tutorNome ?? '—'}</div>
      <div className="text-sm text-slate-500">🐾 {os.petNome}</div>
      {os.funcionarioNome && <div className="text-xs text-blue-500">👤 {os.funcionarioNome}</div>}
      <div className="text-emerald-700 font-semibold mt-1">{fmt(os.valorTotal)}</div>
      {selecionada && <div className="text-xs text-emerald-600 mt-1 font-medium">✓ Selecionada</div>}
    </button>
  )
}

// ─── PDV principal ─────────────────────────────────────────────────
export default function PDV() {
  const [caixa, setCaixa]             = useState(null)
  const [caixaLoading, setCaixaLoading] = useState(true)
  const [produtos, setProdutos]       = useState([])
  const [osProntas, setOsProntas]     = useState([])
  const [busca, setBusca]             = useState('')
  const [modalMov, setModalMov]       = useState(null) // 'retirada' | 'deposito' | null
  const [modalFechamento, setModalFechamento] = useState(false)

  const [osId, setOsId]               = useState(null)
  const [tutorId, setTutorId]         = useState(null)
  const [tutorNome, setTutorNome]     = useState('')
  const [petNome, setPetNome]         = useState('')
  const [carrinho, setCarrinho]       = useState([])
  const [pagamento, setPagamento]     = useState('PIX')
  const [mostrarDesconto, setMostrarDesconto] = useState(false)
  const [desconto, setDesconto]       = useState('')
  const [saving, setSaving]           = useState(false)
  const [msg, setMsg]                 = useState(null)

  const carregarDados = useCallback(() => {
    api.get('/produtos').then(r => setProdutos(r.data)).catch(() => {})
    api.get('/ordens-servico/abertas')
      .then(r => setOsProntas(r.data.filter(o => o.status === 'entregue')))
      .catch(() => {})
  }, [])

  async function carregarCaixa() {
    setCaixaLoading(true)
    try {
      const { data } = await api.get('/caixa/hoje')
      setCaixa(data)
    } catch {
      setCaixa({ aberto: false })
    } finally { setCaixaLoading(false) }
  }

  useEffect(() => {
    carregarCaixa()
    carregarDados()
  }, [carregarDados])

  async function selecionarOS(os) {
    if (osId === os.id) {
      setOsId(null); setTutorId(null); setTutorNome(''); setPetNome('')
      setCarrinho(c => c.filter(i => i.tipo !== 'servico'))
      return
    }
    setOsId(os.id); setPetNome(os.petNome)
    try {
      const { data } = await api.get(`/ordens-servico/${os.id}/servicos`)
      const itens = data.map(s => ({
        tipo: 'servico', id: s.servicoId, osServicoId: s.id,
        nome: s.servicoNome, preco: s.precoCobrado, qtd: 1
      }))
      setCarrinho(c => [...c.filter(i => i.tipo !== 'servico'), ...itens])
    } catch {
      setCarrinho(c => [...c.filter(i => i.tipo !== 'servico'),
        { tipo: 'servico', id: os.id, nome: `Serviços — ${os.petNome}`, preco: os.valorTotal ?? 0, qtd: 1 }
      ])
    }
    setTutorId(os.tutorId ?? null)
    setTutorNome(os.tutorNome ?? '')
  }

  function addProduto(p) {
    setCarrinho(c => {
      const existe = c.find(i => i.tipo === 'produto' && i.id === p.id)
      if (existe) return c.map(i => i.tipo === 'produto' && i.id === p.id ? { ...i, qtd: i.qtd + 1 } : i)
      return [...c, { tipo: 'produto', id: p.id, nome: p.nome, preco: p.precoVenda, qtd: 1 }]
    })
    setMsg(null)
  }

  function mudarQtd(tipo, id, delta) {
    setCarrinho(c => c.map(i => i.tipo === tipo && i.id === id ? { ...i, qtd: Math.max(1, i.qtd + delta) } : i))
  }

  function removerItem(tipo, id) {
    setCarrinho(c => c.filter(i => !(i.tipo === tipo && i.id === id)))
    if (tipo === 'servico') { setOsId(null); setTutorId(null); setTutorNome(''); setPetNome('') }
  }

  const subtotal    = carrinho.reduce((s, i) => s + i.preco * i.qtd, 0)
  const descontoVal = Math.min(+desconto || 0, subtotal)
  const total       = subtotal - descontoVal

  async function finalizar() {
    if (carrinho.length === 0) return
    setSaving(true); setMsg(null)
    try {
      const produtosCarrinho = carrinho.filter(i => i.tipo === 'produto')

      if (produtosCarrinho.length > 0) {
        // Tem produtos — cria venda normalmente (já marca OS como paga no backend)
        await api.post('/vendas', {
          tutorId: tutorId || null,
          osId: osId || null,
          itens: produtosCarrinho.map(i => ({ produtoId: i.id, quantidade: i.qtd, precoUnitario: i.preco })),
          desconto: descontoVal,
          formaPagamento: pagamento
        })
      } else if (osId) {
        // Só serviços — marca a OS como paga diretamente
        await api.put(`/ordens-servico/${osId}/status`, { status: 'pago' })
      }

      setMsg({ tipo: 'ok', texto: `✅ Venda finalizada! ${fmt(total)}` })
      setCarrinho([]); setOsId(null); setTutorId(null); setTutorNome(''); setPetNome('')
      setDesconto(''); setMostrarDesconto(false)
      carregarDados()
    } catch (e) {
      setMsg({ tipo: 'erro', texto: '❌ ' + (e.response?.data?.erro || 'Erro ao finalizar') })
    } finally { setSaving(false) }
  }

  const filtrados = produtos.filter(p => p.nome.toLowerCase().includes(busca.toLowerCase()) && p.estoqueAtual > 0)

  if (caixaLoading) return <div className="text-center py-20 text-slate-400">Carregando...</div>

  if (!caixa?.aberto && !caixa?.id)
    return <AberturaCaixa onAberto={() => carregarCaixa()} />

  return (
    <div className="h-full">
      {/* Header */}
      <div className="flex items-center justify-between mb-4">
        <div>
          <h2 className="text-2xl font-bold">PDV</h2>
          {caixa?.aberto && (
            <p className="text-xs text-slate-400 mt-0.5">
              Caixa aberto · Saldo inicial: {fmt(caixa.saldoInicial)}
            </p>
          )}
        </div>

        {caixa?.aberto && (
          <div className="flex gap-2">
            {/* Retirada e Depósito */}
            <button onClick={() => setModalMov('deposito')}
              className="bg-emerald-50 text-emerald-700 border border-emerald-200 px-3 py-2 rounded-lg text-sm font-medium hover:bg-emerald-100 transition">
              + Depósito
            </button>
            <button onClick={() => setModalMov('retirada')}
              className="bg-orange-50 text-orange-700 border border-orange-200 px-3 py-2 rounded-lg text-sm font-medium hover:bg-orange-100 transition">
              − Retirada
            </button>
            <button onClick={() => setModalFechamento(true)}
              className="bg-red-50 text-red-600 border border-red-200 px-4 py-2 rounded-lg text-sm font-medium hover:bg-red-100 transition">
              Fechar Caixa
            </button>
          </div>
        )}
      </div>

      {/* Movimentações do dia */}
      {caixa?.movimentacoes?.length > 0 && (
        <div className="flex gap-2 mb-4 flex-wrap">
          {caixa.movimentacoes.map(m => (
            <span key={m.id} className={`text-xs px-2 py-1 rounded-full font-medium
              ${m.tipo === 'retirada' ? 'bg-red-50 text-red-600' : 'bg-emerald-50 text-emerald-700'}`}>
              {m.tipo === 'retirada' ? '↓' : '↑'} {m.descricao || m.tipo}: {fmt(m.valor)}
            </span>
          ))}
        </div>
      )}

      {/* OS prontas */}
      {osProntas.length > 0 && (
        <div className="mb-5">
          <div className="flex items-center gap-2 mb-2">
            <span className="text-sm font-semibold text-slate-700">🐾 Atendimentos aguardando cobrança</span>
            <span className="bg-yellow-400 text-yellow-900 text-xs font-bold px-2 py-0.5 rounded-full">{osProntas.length}</span>
          </div>
          <div className="grid grid-cols-2 md:grid-cols-4 lg:grid-cols-6 gap-2">
            {osProntas.map(os => (
              <CardOS key={os.id} os={os} selecionada={osId === os.id} onClick={() => selecionarOS(os)} />
            ))}
          </div>
        </div>
      )}

      {/* Corpo */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-5">
        {/* Produtos */}
        <div className="lg:col-span-2">
          <input className="border rounded-lg px-3 py-2 w-full mb-3 text-sm"
            placeholder="🔍 Buscar produto..."
            value={busca} onChange={e => setBusca(e.target.value)} />
          {filtrados.length === 0 ? (
            <p className="text-center text-slate-400 py-8 text-sm">Nenhum produto encontrado.</p>
          ) : (
            <div className="grid grid-cols-2 md:grid-cols-3 gap-3">
              {filtrados.map(p => (
                <button key={p.id} onClick={() => addProduto(p)}
                  className="bg-white rounded-xl border border-slate-200 p-3 text-left hover:border-emerald-400 hover:shadow-sm transition">
                  <div className="font-medium text-sm text-slate-800 leading-tight">{p.nome}</div>
                  <div className="text-emerald-600 font-bold mt-1">{fmt(p.precoVenda)}</div>
                  <div className="text-xs text-slate-400 mt-0.5">{p.estoqueAtual} em estoque</div>
                </button>
              ))}
            </div>
          )}
        </div>

        {/* Carrinho */}
        <div className="bg-white rounded-2xl border border-slate-200 p-4 flex flex-col gap-3 h-fit sticky top-4">
          {(tutorNome || petNome) && (
            <div className="bg-emerald-50 rounded-lg px-3 py-2 text-sm">
              {tutorNome && <div className="font-semibold text-slate-800">👤 {tutorNome}</div>}
              {petNome  && <div className="text-slate-500 text-xs">🐾 {petNome}</div>}
            </div>
          )}

          <h3 className="font-bold text-slate-800">Carrinho</h3>

          {carrinho.length === 0 ? (
            <p className="text-slate-400 text-sm text-center py-4">Selecione uma OS ou adicione produtos</p>
          ) : (
            <div className="space-y-2">
              {carrinho.map(i => (
                <div key={`${i.tipo}-${i.id}`}
                  className={`flex items-center gap-2 text-sm rounded-lg px-2 py-1 ${i.tipo === 'servico' ? 'bg-blue-50' : ''}`}>
                  <div className="flex-1 min-w-0">
                    <div className="font-medium truncate">{i.nome}</div>
                    <div className="text-xs text-slate-400">
                      {i.tipo === 'servico' ? '🔧 Serviço' : '📦 Produto'} · {fmt(i.preco)}
                    </div>
                  </div>
                  {i.tipo === 'produto' ? (
                    <div className="flex items-center gap-1">
                      <button onClick={() => mudarQtd(i.tipo, i.id, -1)} className="w-6 h-6 bg-slate-100 rounded text-slate-600 hover:bg-slate-200">−</button>
                      <span className="w-5 text-center text-sm">{i.qtd}</span>
                      <button onClick={() => mudarQtd(i.tipo, i.id, 1)} className="w-6 h-6 bg-slate-100 rounded text-slate-600 hover:bg-slate-200">+</button>
                    </div>
                  ) : (
                    <span className="text-xs text-blue-500 px-1">×1</span>
                  )}
                  <button onClick={() => removerItem(i.tipo, i.id)} className="text-red-400 hover:text-red-600 ml-1">×</button>
                </div>
              ))}
            </div>
          )}

          <div className="border-t pt-3 space-y-2 text-sm">
            <div className="flex justify-between text-slate-500">
              <span>Subtotal</span><span>{fmt(subtotal)}</span>
            </div>
            {!mostrarDesconto ? (
              <button onClick={() => setMostrarDesconto(true)} className="text-xs text-slate-400 hover:text-slate-600 underline">
                + Aplicar desconto
              </button>
            ) : (
              <div className="flex justify-between items-center">
                <span className="text-slate-500">Desconto (R$)</span>
                <input className="border rounded px-2 py-1 w-28 text-right text-sm"
                  type="number" step="0.01" min="0" max={subtotal}
                  placeholder="0,00" value={desconto}
                  onChange={e => setDesconto(e.target.value)} autoFocus />
              </div>
            )}
            {descontoVal > 0 && (
              <div className="flex justify-between text-red-500 text-xs">
                <span>Desconto</span><span>− {fmt(descontoVal)}</span>
              </div>
            )}
            <div className="flex justify-between font-bold text-lg text-slate-800">
              <span>Total</span><span className="text-emerald-600">{fmt(total)}</span>
            </div>
          </div>

          <div className="grid grid-cols-2 gap-1">
            {FORMAS.map(f => (
              <button key={f.value} onClick={() => setPagamento(f.value)}
                className={`py-2 rounded-lg text-xs font-medium border transition
                  ${pagamento === f.value ? 'bg-slate-900 text-white border-slate-900' : 'border-slate-200 text-slate-600 hover:bg-slate-50'}`}>
                {f.label}
              </button>
            ))}
          </div>

          <button onClick={finalizar} disabled={carrinho.length === 0 || saving}
            className="w-full bg-emerald-600 text-white py-3 rounded-xl font-bold text-base hover:bg-emerald-700 transition disabled:bg-slate-300 disabled:cursor-not-allowed">
            {saving ? 'Finalizando...' : `Finalizar · ${fmt(total)}`}
          </button>

          {msg && (
            <div className={`text-sm text-center rounded-lg py-2 px-3
              ${msg.tipo === 'ok' ? 'bg-emerald-50 text-emerald-700' : 'bg-red-50 text-red-600'}`}>
              {msg.texto}
            </div>
          )}
        </div>
      </div>

      {/* Modais */}
      {modalMov && (
        <ModalMovimentacao
          tipo={modalMov}
          onClose={() => setModalMov(null)}
          onSaved={() => carregarCaixa()}
        />
      )}
      {modalFechamento && (
        <ModalFechamento
          caixa={caixa}
          onClose={() => setModalFechamento(false)}
          onFechado={() => carregarCaixa()}
        />
      )}
    </div>
  )
}
