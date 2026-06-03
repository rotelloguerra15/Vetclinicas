import { useEffect, useState, useCallback } from 'react'
import api from '../api/client'

const STATUS_LABEL = {
  aberta: { label: 'Em aberto', cls: 'bg-yellow-100 text-yellow-800' },
  paga: { label: 'Pago', cls: 'bg-green-100 text-green-700' },
  recebida: { label: 'Recebido', cls: 'bg-green-100 text-green-700' },
  cancelada: { label: 'Cancelada', cls: 'bg-slate-100 text-slate-500' },
}

const FORMAS = ['Dinheiro', 'PIX', 'Cartão de Débito', 'Cartão de Crédito', 'Boleto', 'Cheque', 'Transferência']

function fmt(v) {
  return Number(v ?? 0).toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
}

function fmtDate(d) {
  if (!d) return '—'
  const [y, m, dd] = (typeof d === 'string' ? d : d.toString()).split('T')[0].split('-')
  return `${dd}/${m}/${y}`
}

function hoje() {
  return new Date().toISOString().split('T')[0]
}

// ─── Badge de status ───────────────────────────────────────────────
function Badge({ status, diasAtraso }) {
  const s = STATUS_LABEL[status] ?? { label: status, cls: 'bg-slate-100 text-slate-600' }
  return (
    <span className={`text-xs font-medium px-2 py-0.5 rounded-full ${s.cls}`}>
      {s.label}
      {status === 'aberta' && diasAtraso > 0 ? ` (${diasAtraso}d atraso)` : ''}
    </span>
  )
}

// ─── Modal de baixa ────────────────────────────────────────────────
function ModalBaixa({ conta, onClose, onOk }) {
  const [form, setForm] = useState({
    valorPago: conta.valor,
    dataBaixa: hoje(),
    formaPagamento: conta.formaPagamento || 'PIX',
    contaBancaria: conta.contaBancaria || 'Caixa',
    obsBaixa: ''
  })
  const [saving, setSaving] = useState(false)

  async function salvar() {
    setSaving(true)
    try {
      await api.post(`/financeiro/contas/${conta.id}/baixar`, {
        valorPago: Number(form.valorPago),
        dataBaixa: form.dataBaixa,
        formaPagamento: form.formaPagamento,
        contaBancaria: form.contaBancaria,
        obsBaixa: form.obsBaixa || null
      })
      onOk()
    } catch (e) {
      alert(e.response?.data?.erro || 'Erro ao registrar baixa')
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50">
      <div className="bg-white rounded-xl shadow-xl p-6 w-full max-w-md">
        <h3 className="font-bold text-lg mb-1">
          {conta.tipo === 'receita' ? '💰 Registrar Recebimento' : '💸 Registrar Pagamento'}
        </h3>
        <p className="text-sm text-slate-500 mb-4">{conta.descricao}</p>

        <div className="grid grid-cols-2 gap-3">
          <div className="col-span-2">
            <label className="text-xs text-slate-500 font-medium">Valor pago (R$)</label>
            <input type="number" step="0.01" className="input w-full mt-1"
              value={form.valorPago}
              onChange={e => setForm(f => ({ ...f, valorPago: e.target.value }))} />
          </div>
          <div>
            <label className="text-xs text-slate-500 font-medium">Data da baixa</label>
            <input type="date" className="input w-full mt-1"
              value={form.dataBaixa}
              onChange={e => setForm(f => ({ ...f, dataBaixa: e.target.value }))} />
          </div>
          <div>
            <label className="text-xs text-slate-500 font-medium">Forma de pagamento</label>
            <select className="input w-full mt-1"
              value={form.formaPagamento}
              onChange={e => setForm(f => ({ ...f, formaPagamento: e.target.value }))}>
              {FORMAS.map(f => <option key={f}>{f}</option>)}
            </select>
          </div>
          <div>
            <label className="text-xs text-slate-500 font-medium">Conta/Caixa</label>
            <input className="input w-full mt-1" placeholder="Ex: Caixa, Banco X"
              value={form.contaBancaria}
              onChange={e => setForm(f => ({ ...f, contaBancaria: e.target.value }))} />
          </div>
          <div>
            <label className="text-xs text-slate-500 font-medium">Observação</label>
            <input className="input w-full mt-1" placeholder="Opcional"
              value={form.obsBaixa}
              onChange={e => setForm(f => ({ ...f, obsBaixa: e.target.value }))} />
          </div>
        </div>

        <div className="flex gap-2 mt-5 justify-end">
          <button className="btn-secondary" onClick={onClose}>Cancelar</button>
          <button className={`btn-primary ${conta.tipo === 'despesa' ? 'bg-red-600 hover:bg-red-700' : ''}`}
            onClick={salvar} disabled={saving}>
            {saving ? 'Salvando...' : conta.tipo === 'receita' ? 'Confirmar Recebimento' : 'Confirmar Pagamento'}
          </button>
        </div>
      </div>
    </div>
  )
}

// ─── Modal nova conta ──────────────────────────────────────────────
function ModalConta({ categorias, onClose, onOk }) {
  const [form, setForm] = useState({
    tipo: 'receita',
    descricao: '',
    valor: '',
    dataCompetencia: hoje(),
    dataVencimento: hoje(),
    formaPagamento: 'PIX',
    categoriaId: '',
    contaBancaria: 'Caixa'
  })
  const [saving, setSaving] = useState(false)

  const cats = categorias.filter(c => c.tipo === form.tipo)

  async function salvar() {
    if (!form.descricao.trim()) return alert('Informe uma descrição')
    if (!form.valor || Number(form.valor) <= 0) return alert('Valor inválido')
    setSaving(true)
    try {
      await api.post('/financeiro/contas', {
        tipo: form.tipo,
        descricao: form.descricao,
        valor: Number(form.valor),
        dataCompetencia: form.dataCompetencia,
        dataVencimento: form.dataVencimento,
        formaPagamento: form.formaPagamento,
        categoriaId: form.categoriaId || null,
        contaBancaria: form.contaBancaria
      })
      onOk()
    } catch (e) {
      alert(e.response?.data?.erro || 'Erro ao criar conta')
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50">
      <div className="bg-white rounded-xl shadow-xl p-6 w-full max-w-lg">
        <h3 className="font-bold text-lg mb-4">Nova Conta</h3>

        <div className="grid grid-cols-2 gap-3">
          <div className="col-span-2">
            <label className="text-xs text-slate-500 font-medium">Tipo</label>
            <div className="flex gap-2 mt-1">
              {['receita', 'despesa'].map(t => (
                <button key={t}
                  className={`flex-1 py-2 rounded-lg text-sm font-medium border transition
                    ${form.tipo === t
                      ? t === 'receita' ? 'bg-green-600 text-white border-green-600' : 'bg-red-600 text-white border-red-600'
                      : 'border-slate-200 text-slate-600 hover:bg-slate-50'}`}
                  onClick={() => setForm(f => ({ ...f, tipo: t, categoriaId: '' }))}>
                  {t === 'receita' ? '💰 Receita' : '💸 Despesa'}
                </button>
              ))}
            </div>
          </div>

          <div className="col-span-2">
            <label className="text-xs text-slate-500 font-medium">Descrição *</label>
            <input className="input w-full mt-1" placeholder="Ex: Consulta Dr. João, Conta de luz..."
              value={form.descricao}
              onChange={e => setForm(f => ({ ...f, descricao: e.target.value }))} />
          </div>

          <div>
            <label className="text-xs text-slate-500 font-medium">Valor (R$) *</label>
            <input type="number" step="0.01" className="input w-full mt-1" placeholder="0,00"
              value={form.valor}
              onChange={e => setForm(f => ({ ...f, valor: e.target.value }))} />
          </div>

          <div>
            <label className="text-xs text-slate-500 font-medium">Categoria</label>
            <select className="input w-full mt-1"
              value={form.categoriaId}
              onChange={e => setForm(f => ({ ...f, categoriaId: e.target.value }))}>
              <option value="">Sem categoria</option>
              {cats.map(c => <option key={c.id} value={c.id}>{c.nome}</option>)}
            </select>
          </div>

          <div>
            <label className="text-xs text-slate-500 font-medium">Competência</label>
            <input type="date" className="input w-full mt-1"
              value={form.dataCompetencia}
              onChange={e => setForm(f => ({ ...f, dataCompetencia: e.target.value }))} />
          </div>

          <div>
            <label className="text-xs text-slate-500 font-medium">Vencimento *</label>
            <input type="date" className="input w-full mt-1"
              value={form.dataVencimento}
              onChange={e => setForm(f => ({ ...f, dataVencimento: e.target.value }))} />
          </div>

          <div>
            <label className="text-xs text-slate-500 font-medium">Forma de pagamento</label>
            <select className="input w-full mt-1"
              value={form.formaPagamento}
              onChange={e => setForm(f => ({ ...f, formaPagamento: e.target.value }))}>
              {FORMAS.map(f => <option key={f}>{f}</option>)}
            </select>
          </div>

          <div>
            <label className="text-xs text-slate-500 font-medium">Conta/Caixa</label>
            <input className="input w-full mt-1" placeholder="Ex: Caixa, Banco X"
              value={form.contaBancaria}
              onChange={e => setForm(f => ({ ...f, contaBancaria: e.target.value }))} />
          </div>
        </div>

        <div className="flex gap-2 mt-5 justify-end">
          <button className="btn-secondary" onClick={onClose}>Cancelar</button>
          <button className="btn-primary" onClick={salvar} disabled={saving}>
            {saving ? 'Salvando...' : 'Criar Conta'}
          </button>
        </div>
      </div>
    </div>
  )
}

// ─── Página principal ──────────────────────────────────────────────
export default function Financeiro() {
  const [contas, setContas] = useState([])
  const [resumo, setResumo] = useState(null)
  const [categorias, setCategorias] = useState([])
  const [loading, setLoading] = useState(true)

  // Filtros
  const [aba, setAba] = useState('todas')         // todas | receber | pagar | vencidas
  const [statusFiltro, setStatusFiltro] = useState('aberta')
  const [busca, setBusca] = useState('')

  // Modais
  const [modalBaixa, setModalBaixa] = useState(null)   // { conta }
  const [modalNova, setModalNova] = useState(false)

  const carregar = useCallback(async () => {
    setLoading(true)
    try {
      const params = {}
      if (aba === 'receber') params.tipo = 'receita'
      if (aba === 'pagar') params.tipo = 'despesa'
      if (aba === 'vencidas') params.vencidas = true
      else if (statusFiltro) params.status = statusFiltro

      const [rContas, rResumo, rCats] = await Promise.all([
        api.get('/financeiro/contas', { params }),
        api.get('/financeiro/resumo'),
        api.get('/financeiro/categorias')
      ])
      setContas(rContas.data)
      setResumo(rResumo.data)
      setCategorias(rCats.data)
    } catch (e) {
      console.error(e)
    } finally {
      setLoading(false)
    }
  }, [aba, statusFiltro])

  useEffect(() => { carregar() }, [carregar])

  async function estornar(id) {
    if (!confirm('Estornar esta baixa?')) return
    try {
      await api.post(`/financeiro/contas/${id}/estornar`)
      carregar()
    } catch (e) {
      alert(e.response?.data?.erro || 'Erro ao estornar')
    }
  }

  async function cancelar(id) {
    if (!confirm('Cancelar esta conta?')) return
    try {
      await api.delete(`/financeiro/contas/${id}`)
      carregar()
    } catch (e) {
      alert(e.response?.data?.erro || 'Erro ao cancelar')
    }
  }

  const contasFiltradas = contas.filter(c =>
    !busca || c.descricao.toLowerCase().includes(busca.toLowerCase()) ||
    (c.categoriaNome || '').toLowerCase().includes(busca.toLowerCase())
  )

  const abas = [
    { id: 'todas', label: 'Todas' },
    { id: 'receber', label: '💰 A Receber' },
    { id: 'pagar', label: '💸 A Pagar' },
    { id: 'vencidas', label: `⚠️ Vencidas${resumo?.contasVencidas > 0 ? ` (${resumo.contasVencidas})` : ''}` },
  ]

  return (
    <div className="p-6 max-w-6xl mx-auto">
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-bold text-slate-800">💼 Financeiro</h1>
        <button className="btn-primary" onClick={() => setModalNova(true)}>
          + Nova Conta
        </button>
      </div>

      {/* Cards de resumo */}
      {resumo && (
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
          <div className="card">
            <p className="text-xs text-slate-500 font-medium uppercase tracking-wide mb-1">Receitas (mês)</p>
            <p className="text-xl font-bold text-green-600">{fmt(resumo.totalReceitas)}</p>
          </div>
          <div className="card">
            <p className="text-xs text-slate-500 font-medium uppercase tracking-wide mb-1">Despesas (mês)</p>
            <p className="text-xl font-bold text-red-600">{fmt(resumo.totalDespesas)}</p>
          </div>
          <div className="card">
            <p className="text-xs text-slate-500 font-medium uppercase tracking-wide mb-1">Saldo</p>
            <p className={`text-xl font-bold ${resumo.saldo >= 0 ? 'text-green-700' : 'text-red-700'}`}>
              {fmt(resumo.saldo)}
            </p>
          </div>
          <div className={`card ${resumo.contasVencidas > 0 ? 'border-red-300 bg-red-50' : ''}`}>
            <p className="text-xs text-slate-500 font-medium uppercase tracking-wide mb-1">Vencidas</p>
            <p className={`text-xl font-bold ${resumo.contasVencidas > 0 ? 'text-red-600' : 'text-slate-700'}`}>
              {resumo.contasVencidas > 0 ? `${resumo.contasVencidas} • ${fmt(resumo.valorVencido)}` : '—'}
            </p>
          </div>
        </div>
      )}

      {/* Abas e filtros */}
      <div className="flex flex-wrap gap-2 items-center mb-4">
        <div className="flex gap-1 bg-slate-100 rounded-lg p-1">
          {abas.map(a => (
            <button key={a.id}
              className={`px-3 py-1.5 rounded-md text-sm font-medium transition
                ${aba === a.id ? 'bg-white shadow text-slate-800' : 'text-slate-500 hover:text-slate-700'}`}
              onClick={() => { setAba(a.id); setStatusFiltro(a.id === 'vencidas' ? '' : 'aberta') }}>
              {a.label}
            </button>
          ))}
        </div>

        {aba !== 'vencidas' && (
          <select className="input text-sm"
            value={statusFiltro}
            onChange={e => setStatusFiltro(e.target.value)}>
            <option value="">Todos os status</option>
            <option value="aberta">Em aberto</option>
            <option value="paga">Pagas</option>
            <option value="recebida">Recebidas</option>
            <option value="cancelada">Canceladas</option>
          </select>
        )}

        <input className="input text-sm flex-1 min-w-32"
          placeholder="Buscar por descrição ou categoria..."
          value={busca}
          onChange={e => setBusca(e.target.value)} />
      </div>

      {/* Resumo a receber / a pagar em aberto */}
      {resumo && (
        <div className="flex gap-3 mb-4 text-sm">
          <span className="text-slate-500">
            A receber em aberto: <strong className="text-green-700">{fmt(resumo.contasReceberAberto)}</strong>
          </span>
          <span className="text-slate-300">|</span>
          <span className="text-slate-500">
            A pagar em aberto: <strong className="text-red-700">{fmt(resumo.contasPagarAberto)}</strong>
          </span>
        </div>
      )}

      {/* Tabela */}
      {loading ? (
        <div className="text-center py-12 text-slate-400">Carregando...</div>
      ) : contasFiltradas.length === 0 ? (
        <div className="text-center py-12 text-slate-400">Nenhuma conta encontrada.</div>
      ) : (
        <div className="bg-white rounded-xl border border-slate-200 overflow-hidden">
          <table className="w-full text-sm">
            <thead>
              <tr className="bg-slate-50 text-slate-500 text-xs uppercase tracking-wide">
                <th className="px-4 py-3 text-left">Descrição</th>
                <th className="px-4 py-3 text-left">Categoria</th>
                <th className="px-4 py-3 text-left">Vencimento</th>
                <th className="px-4 py-3 text-right">Valor</th>
                <th className="px-4 py-3 text-center">Status</th>
                <th className="px-4 py-3 text-left">Baixa</th>
                <th className="px-4 py-3 text-left">Origem</th>
                <th className="px-4 py-3"></th>
              </tr>
            </thead>
            <tbody>
              {contasFiltradas.map(c => (
                <tr key={c.id}
                  className={`border-t border-slate-100 hover:bg-slate-50 transition
                    ${c.status === 'aberta' && c.diasAtraso > 0 ? 'bg-red-50 hover:bg-red-50' : ''}`}>
                  <td className="px-4 py-3">
                    <div className="flex items-center gap-2">
                      <span className={`w-2 h-2 rounded-full flex-shrink-0
                        ${c.tipo === 'receita' ? 'bg-green-400' : 'bg-red-400'}`} />
                      <span className="font-medium text-slate-800">{c.descricao}</span>
                    </div>
                  </td>
                  <td className="px-4 py-3 text-slate-500">{c.categoriaNome || '—'}</td>
                  <td className="px-4 py-3 text-slate-600">{fmtDate(c.dataVencimento)}</td>
                  <td className="px-4 py-3 text-right font-semibold">
                    <span className={c.tipo === 'receita' ? 'text-green-700' : 'text-red-700'}>
                      {fmt(c.valor)}
                    </span>
                  </td>
                  <td className="px-4 py-3 text-center">
                    <Badge status={c.status} diasAtraso={c.diasAtraso} />
                  </td>
                  <td className="px-4 py-3 text-slate-500 text-xs">
                    {c.dataBaixa ? (
                      <span>
                        {fmtDate(c.dataBaixa)}<br />
                        <span className="text-slate-400">{c.formaPagamento}</span>
                      </span>
                    ) : '—'}
                  </td>
                  <td className="px-4 py-3 text-xs text-slate-400">
                    {c.osId ? '🔧 OS' : c.vendaId ? '🛒 PDV' : '✍️ Manual'}
                  </td>
                  <td className="px-4 py-3">
                    <div className="flex gap-1 justify-end">
                      {c.status === 'aberta' && (
                        <button
                          className={`text-xs px-2 py-1 rounded font-medium transition
                            ${c.tipo === 'receita'
                              ? 'bg-green-100 text-green-700 hover:bg-green-200'
                              : 'bg-red-100 text-red-700 hover:bg-red-200'}`}
                          onClick={() => setModalBaixa(c)}>
                          {c.tipo === 'receita' ? 'Receber' : 'Pagar'}
                        </button>
                      )}
                      {(c.status === 'paga' || c.status === 'recebida') && (
                        <button
                          className="text-xs px-2 py-1 rounded bg-slate-100 text-slate-600 hover:bg-slate-200 transition"
                          onClick={() => estornar(c.id)}>
                          Estornar
                        </button>
                      )}
                      {c.status === 'aberta' && (
                        <button
                          className="text-xs px-2 py-1 rounded bg-slate-100 text-slate-500 hover:bg-slate-200 transition"
                          onClick={() => cancelar(c.id)}>
                          ✕
                        </button>
                      )}
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {/* Modais */}
      {modalBaixa && (
        <ModalBaixa
          conta={modalBaixa}
          onClose={() => setModalBaixa(null)}
          onOk={() => { setModalBaixa(null); carregar() }}
        />
      )}

      {modalNova && (
        <ModalConta
          categorias={categorias}
          onClose={() => setModalNova(false)}
          onOk={() => { setModalNova(false); carregar() }}
        />
      )}
    </div>
  )
}
