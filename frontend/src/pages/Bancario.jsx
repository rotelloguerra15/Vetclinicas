import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import api from '../api/client'

const TIPOS_CONTA = { corrente: 'Conta Corrente', poupanca: 'Poupança', caixa: 'Caixa', outro: 'Outro' }
const TIPO_COR = {
  entrada:       'bg-emerald-100 text-emerald-700',
  saida:         'bg-red-100 text-red-700',
  transferencia: 'bg-blue-100 text-blue-700'
}

export default function Bancario() {
  const [aba, setAba] = useState('movimentacoes')

  return (
    <div>
      <h2 className="text-2xl font-bold mb-6">Movimentação Bancária</h2>

      <div className="flex gap-1 border-b mb-6">
        {[
          { k: 'movimentacoes', l: '💳 Movimentações' },
          { k: 'contas',        l: '🏦 Contas' },
          { k: 'clientes',      l: '👥 Histórico por Cliente' },
        ].map(t => (
          <button key={t.k} onClick={() => setAba(t.k)}
            className={`px-4 py-2 text-sm font-medium border-b-2 -mb-px ${
              aba === t.k ? 'border-slate-900 text-slate-900' : 'border-transparent text-slate-400 hover:text-slate-600'
            }`}>
            {t.l}
          </button>
        ))}
      </div>

      {aba === 'movimentacoes' && <AbaMovimentacoes />}
      {aba === 'contas'        && <AbaContas />}
      {aba === 'clientes'      && <AbaClientes />}
    </div>
  )
}

// ── ABA: MOVIMENTAÇÕES ────────────────────────────────────────────────────────

function AbaMovimentacoes() {
  const [movs, setMovs]       = useState([])
  const [contas, setContas]   = useState([])
  const [categorias, setCats] = useState([])
  const [total, setTotal]     = useState(0)
  const [page, setPage]       = useState(1)
  const [mostrarForm, setMostrarForm] = useState(false)

  const hoje = new Date().toISOString().split('T')[0]
  const [filtros, setFiltros] = useState({
    contaId: '', tipo: '',
    de: new Date(new Date().setDate(1)).toISOString().split('T')[0],
    ate: hoje
  })
  const [form, setForm] = useState({
    contaBancariaId: '', tipo: 'entrada', valor: '',
    descricao: '', dataMovimentacao: hoje,
    categoriaId: '', contaDestinoId: ''
  })

  function carregar() {
    api.get('/bancario/movimentacoes', {
      params: { ...filtros, page, pageSize: 30 }
    }).then(r => { setMovs(r.data.items); setTotal(r.data.total) }).catch(() => {})
  }

  useEffect(() => {
    api.get('/bancario/contas').then(r => setContas(r.data)).catch(() => {})
    api.get('/financeiro/categorias').then(r => setCats(r.data)).catch(() => {})
  }, [])

  useEffect(() => { carregar() }, [filtros, page])

  async function salvar(e) {
    e.preventDefault()
    await api.post('/bancario/movimentacoes', {
      ...form,
      valor: parseFloat(form.valor),
      categoriaId: form.categoriaId || null,
      contaDestinoId: form.contaDestinoId || null
    })
    setMostrarForm(false)
    setForm({ contaBancariaId: '', tipo: 'entrada', valor: '', descricao: '', dataMovimentacao: hoje, categoriaId: '', contaDestinoId: '' })
    carregar()
  }

  async function remover(id) {
    if (!confirm('Remover esta movimentação?')) return
    try {
      await api.delete(`/bancario/movimentacoes/${id}`)
      carregar()
    } catch (err) {
      alert(err.response?.data?.erro || 'Erro ao remover.')
    }
  }

  async function conciliar(id) {
    await api.put(`/bancario/movimentacoes/${id}/conciliar`)
    carregar()
  }

  const totalEntradas = movs.filter(m => m.tipo === 'entrada').reduce((s, m) => s + m.valor, 0)
  const totalSaidas   = movs.filter(m => m.tipo === 'saida').reduce((s, m) => s + m.valor, 0)

  return (
    <div className="space-y-4">
      {/* Filtros */}
      <div className="bg-white rounded-2xl shadow p-4 flex flex-wrap gap-3 items-end">
        <div>
          <label className="text-xs text-slate-500 block mb-1">Conta</label>
          <select className="border rounded-lg px-3 py-2 text-sm"
            value={filtros.contaId} onChange={e => setFiltros({ ...filtros, contaId: e.target.value })}>
            <option value="">Todas</option>
            {contas.map(c => <option key={c.id} value={c.id}>{c.nome}</option>)}
          </select>
        </div>
        <div>
          <label className="text-xs text-slate-500 block mb-1">Tipo</label>
          <select className="border rounded-lg px-3 py-2 text-sm"
            value={filtros.tipo} onChange={e => setFiltros({ ...filtros, tipo: e.target.value })}>
            <option value="">Todos</option>
            <option value="entrada">Entrada</option>
            <option value="saida">Saída</option>
            <option value="transferencia">Transferência</option>
          </select>
        </div>
        <div>
          <label className="text-xs text-slate-500 block mb-1">De</label>
          <input type="date" className="border rounded-lg px-3 py-2 text-sm"
            value={filtros.de} onChange={e => setFiltros({ ...filtros, de: e.target.value })} />
        </div>
        <div>
          <label className="text-xs text-slate-500 block mb-1">Até</label>
          <input type="date" className="border rounded-lg px-3 py-2 text-sm"
            value={filtros.ate} onChange={e => setFiltros({ ...filtros, ate: e.target.value })} />
        </div>
        <button onClick={() => setMostrarForm(!mostrarForm)}
          className="ml-auto bg-slate-900 text-white px-4 py-2 rounded-lg text-sm">
          + Nova movimentação
        </button>
      </div>

      {/* Resumo */}
      <div className="grid grid-cols-3 gap-3">
        <div className="bg-emerald-50 rounded-xl p-4">
          <div className="text-xs text-emerald-600 mb-1">Entradas</div>
          <div className="text-xl font-bold text-emerald-700">R$ {totalEntradas.toFixed(2)}</div>
        </div>
        <div className="bg-red-50 rounded-xl p-4">
          <div className="text-xs text-red-600 mb-1">Saídas</div>
          <div className="text-xl font-bold text-red-700">R$ {totalSaidas.toFixed(2)}</div>
        </div>
        <div className="bg-slate-50 rounded-xl p-4">
          <div className="text-xs text-slate-500 mb-1">Saldo período</div>
          <div className={`text-xl font-bold ${totalEntradas - totalSaidas >= 0 ? 'text-emerald-700' : 'text-red-700'}`}>
            R$ {(totalEntradas - totalSaidas).toFixed(2)}
          </div>
        </div>
      </div>

      {/* Formulário */}
      {mostrarForm && (
        <form onSubmit={salvar} className="bg-white rounded-2xl shadow p-5 space-y-3">
          <p className="font-semibold text-slate-700">Nova movimentação</p>
          <div className="grid grid-cols-2 gap-3">
            <select className="border rounded-lg px-3 py-2" required
              value={form.contaBancariaId} onChange={e => setForm({ ...form, contaBancariaId: e.target.value })}>
              <option value="">Selecione a conta *</option>
              {contas.map(c => <option key={c.id} value={c.id}>{c.nome} — R$ {c.saldoAtual?.toFixed(2)}</option>)}
            </select>
            <select className="border rounded-lg px-3 py-2"
              value={form.tipo} onChange={e => setForm({ ...form, tipo: e.target.value })}>
              <option value="entrada">Entrada</option>
              <option value="saida">Saída</option>
              <option value="transferencia">Transferência</option>
            </select>
            <input className="border rounded-lg px-3 py-2" placeholder="Descrição *" required
              value={form.descricao} onChange={e => setForm({ ...form, descricao: e.target.value })} />
            <input className="border rounded-lg px-3 py-2" placeholder="Valor *" type="number" step="0.01" required
              value={form.valor} onChange={e => setForm({ ...form, valor: e.target.value })} />
            <div>
              <label className="text-xs text-slate-500 block mb-1">Data</label>
              <input type="date" className="border rounded-lg px-3 py-2 w-full"
                value={form.dataMovimentacao} onChange={e => setForm({ ...form, dataMovimentacao: e.target.value })} />
            </div>
            <select className="border rounded-lg px-3 py-2"
              value={form.categoriaId} onChange={e => setForm({ ...form, categoriaId: e.target.value })}>
              <option value="">Categoria (opcional)</option>
              {categorias.map(c => <option key={c.id} value={c.id}>{c.nome}</option>)}
            </select>
            {form.tipo === 'transferencia' && (
              <select className="border rounded-lg px-3 py-2 col-span-2"
                value={form.contaDestinoId} onChange={e => setForm({ ...form, contaDestinoId: e.target.value })}>
                <option value="">Conta destino *</option>
                {contas.filter(c => c.id !== form.contaBancariaId).map(c => <option key={c.id} value={c.id}>{c.nome}</option>)}
              </select>
            )}
          </div>
          <div className="flex gap-2">
            <button type="submit" className="bg-emerald-600 text-white px-5 py-2 rounded-lg text-sm">Salvar</button>
            <button type="button" onClick={() => setMostrarForm(false)} className="border px-5 py-2 rounded-lg text-sm">Cancelar</button>
          </div>
        </form>
      )}

      {/* Tabela */}
      <div className="bg-white rounded-2xl shadow overflow-hidden">
        <table className="w-full text-sm">
          <thead className="bg-slate-50 text-left">
            <tr>
              <th className="p-3">Data</th>
              <th className="p-3">Descrição</th>
              <th className="p-3">Conta</th>
              <th className="p-3">Tipo</th>
              <th className="p-3 text-right">Valor</th>
              <th className="p-3">Conciliado</th>
              <th className="p-3"></th>
            </tr>
          </thead>
          <tbody>
            {movs.map(m => (
              <tr key={m.id} className="border-t hover:bg-slate-50">
                <td className="p-3 whitespace-nowrap text-slate-500">
                  {new Date(m.dataMovimentacao + 'T12:00:00').toLocaleDateString('pt-BR')}
                </td>
                <td className="p-3">{m.descricao}</td>
                <td className="p-3 text-slate-500 text-xs">{m.contaNome}</td>
                <td className="p-3">
                  <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${TIPO_COR[m.tipo]}`}>
                    {m.tipo}
                  </span>
                </td>
                <td className={`p-3 text-right font-medium ${m.tipo === 'entrada' ? 'text-emerald-700' : m.tipo === 'saida' ? 'text-red-600' : 'text-blue-600'}`}>
                  {m.tipo === 'saida' ? '-' : '+'} R$ {m.valor.toFixed(2)}
                </td>
                <td className="p-3">
                  <button onClick={() => conciliar(m.id)}
                    className={`text-xs px-2 py-0.5 rounded-full ${m.conciliado ? 'bg-emerald-100 text-emerald-700' : 'bg-slate-100 text-slate-500'}`}>
                    {m.conciliado ? 'Sim' : 'Não'}
                  </button>
                </td>
                <td className="p-3 text-right">
                  {!m.conciliado && (
                    <button onClick={() => remover(m.id)} className="text-xs text-red-400 hover:underline">Remover</button>
                  )}
                </td>
              </tr>
            ))}
            {movs.length === 0 && (
              <tr><td colSpan="7" className="p-6 text-center text-slate-400">Nenhuma movimentação no período</td></tr>
            )}
          </tbody>
        </table>
      </div>

      {total > 30 && (
        <div className="flex justify-between items-center text-sm">
          <span className="text-slate-400">{total} registros</span>
          <div className="flex gap-2">
            <button disabled={page === 1} onClick={() => setPage(p => p - 1)}
              className="px-3 py-1.5 border rounded-lg disabled:opacity-40">← Anterior</button>
            <span className="px-3 py-1.5 text-slate-600">Pág {page}</span>
            <button disabled={page * 30 >= total} onClick={() => setPage(p => p + 1)}
              className="px-3 py-1.5 border rounded-lg disabled:opacity-40">Próxima →</button>
          </div>
        </div>
      )}
    </div>
  )
}

// ── ABA: CONTAS ───────────────────────────────────────────────────────────────

function AbaContas() {
  const [contas, setContas] = useState([])
  const [form, setForm]     = useState({ nome: '', banco: '', agencia: '', conta: '', tipo: 'corrente', saldoInicial: '' })
  const [editId, setEditId] = useState(null)
  const [mostrarForm, setMostrarForm] = useState(false)

  function carregar() {
    api.get('/bancario/contas').then(r => setContas(r.data)).catch(() => {})
  }

  useEffect(() => { carregar() }, [])

  async function salvar(e) {
    e.preventDefault()
    const payload = { ...form, saldoInicial: parseFloat(form.saldoInicial || 0) }
    if (editId) await api.put(`/bancario/contas/${editId}`, payload)
    else await api.post('/bancario/contas', payload)
    setMostrarForm(false); setEditId(null)
    setForm({ nome: '', banco: '', agencia: '', conta: '', tipo: 'corrente', saldoInicial: '' })
    carregar()
  }

  function editar(c) {
    setForm({ nome: c.nome, banco: c.banco || '', agencia: c.agencia || '', conta: c.conta || '', tipo: c.tipo, saldoInicial: c.saldoInicial })
    setEditId(c.id); setMostrarForm(true)
  }

  async function remover(id) {
    if (!confirm('Remover conta?')) return
    try {
      await api.delete(`/bancario/contas/${id}`)
      carregar()
    } catch (err) {
      alert(err.response?.data?.erro || 'Erro.')
    }
  }

  return (
    <div className="space-y-4 max-w-2xl">
      <button onClick={() => { setMostrarForm(!mostrarForm); setEditId(null) }}
        className="bg-slate-900 text-white px-4 py-2 rounded-lg text-sm">
        + Nova conta bancária
      </button>

      {mostrarForm && (
        <form onSubmit={salvar} className="bg-white rounded-2xl shadow p-5 space-y-3">
          <p className="font-semibold">{editId ? 'Editar conta' : 'Nova conta'}</p>
          <div className="grid grid-cols-2 gap-3">
            <input className="border rounded-lg px-3 py-2" placeholder="Nome *" required
              value={form.nome} onChange={e => setForm({ ...form, nome: e.target.value })} />
            <select className="border rounded-lg px-3 py-2"
              value={form.tipo} onChange={e => setForm({ ...form, tipo: e.target.value })}>
              {Object.entries(TIPOS_CONTA).map(([k, v]) => <option key={k} value={k}>{v}</option>)}
            </select>
            <input className="border rounded-lg px-3 py-2" placeholder="Banco"
              value={form.banco} onChange={e => setForm({ ...form, banco: e.target.value })} />
            <input className="border rounded-lg px-3 py-2" placeholder="Agência"
              value={form.agencia} onChange={e => setForm({ ...form, agencia: e.target.value })} />
            <input className="border rounded-lg px-3 py-2" placeholder="Nº conta"
              value={form.conta} onChange={e => setForm({ ...form, conta: e.target.value })} />
            <input className="border rounded-lg px-3 py-2" placeholder="Saldo inicial (R$)" type="number" step="0.01"
              value={form.saldoInicial} onChange={e => setForm({ ...form, saldoInicial: e.target.value })} />
          </div>
          <div className="flex gap-2">
            <button type="submit" className="bg-emerald-600 text-white px-5 py-2 rounded-lg text-sm">Salvar</button>
            <button type="button" onClick={() => setMostrarForm(false)} className="border px-5 py-2 rounded-lg text-sm">Cancelar</button>
          </div>
        </form>
      )}

      <div className="space-y-3">
        {contas.map(c => (
          <div key={c.id} className="bg-white rounded-2xl shadow p-5 flex items-center justify-between">
            <div>
              <div className="font-semibold">{c.nome}</div>
              <div className="text-xs text-slate-400 mt-0.5">
                {TIPOS_CONTA[c.tipo]}{c.banco ? ` · ${c.banco}` : ''}{c.agencia ? ` · Ag ${c.agencia}` : ''}
              </div>
            </div>
            <div className="text-right">
              <div className={`text-lg font-bold ${c.saldoAtual >= 0 ? 'text-emerald-700' : 'text-red-600'}`}>
                R$ {c.saldoAtual?.toFixed(2)}
              </div>
              <div className="text-xs text-slate-400">saldo atual</div>
            </div>
            <div className="flex gap-2 ml-4">
              <button onClick={() => editar(c)} className="text-xs text-blue-600 hover:underline">Editar</button>
              <button onClick={() => remover(c.id)} className="text-xs text-red-400 hover:underline">Remover</button>
            </div>
          </div>
        ))}
        {contas.length === 0 && <p className="text-slate-400 text-sm">Nenhuma conta cadastrada.</p>}
      </div>
    </div>
  )
}

// ── ABA: HISTÓRICO DE CLIENTES ────────────────────────────────────────────────

function AbaClientes() {
  const nav = useNavigate()
  const [tutores, setTutores] = useState([])
  const [busca, setBusca]     = useState('')
  const [total, setTotal]     = useState(0)
  const [page, setPage]       = useState(1)

  const hoje = new Date().toISOString().split('T')[0]
  const [de, setDe]   = useState(new Date(new Date().setMonth(new Date().getMonth() - 3)).toISOString().split('T')[0])
  const [ate, setAte] = useState(hoje)

  function carregar() {
    api.get('/bancario/historico-clientes', { params: { busca, de, ate, page, pageSize: 20 } })
      .then(r => { setTutores(r.data.items); setTotal(r.data.total) })
      .catch(() => {})
  }

  useEffect(() => { carregar() }, [page, de, ate])

  return (
    <div className="space-y-4">
      <div className="bg-white rounded-2xl shadow p-4 flex flex-wrap gap-3 items-end">
        <input className="border rounded-lg px-3 py-2 flex-1 min-w-40 text-sm" placeholder="Buscar tutor..."
          value={busca} onChange={e => setBusca(e.target.value)}
          onKeyDown={e => e.key === 'Enter' && carregar()} />
        <div>
          <label className="text-xs text-slate-500 block mb-1">De</label>
          <input type="date" className="border rounded-lg px-3 py-2 text-sm"
            value={de} onChange={e => setDe(e.target.value)} />
        </div>
        <div>
          <label className="text-xs text-slate-500 block mb-1">Até</label>
          <input type="date" className="border rounded-lg px-3 py-2 text-sm"
            value={ate} onChange={e => setAte(e.target.value)} />
        </div>
        <button onClick={carregar} className="bg-slate-200 px-4 py-2 rounded-lg text-sm hover:bg-slate-300">
          Buscar
        </button>
      </div>

      <div className="bg-white rounded-2xl shadow overflow-hidden">
        <table className="w-full text-sm">
          <thead className="bg-slate-50 text-left">
            <tr>
              <th className="p-3">Tutor</th>
              <th className="p-3">Telefone</th>
              <th className="p-3 text-center">Pets</th>
              <th className="p-3 text-right">Total no período</th>
              <th className="p-3"></th>
            </tr>
          </thead>
          <tbody>
            {tutores.map(t => (
              <tr key={t.id} className="border-t hover:bg-slate-50">
                <td className="p-3 font-medium">{t.nome}</td>
                <td className="p-3 text-slate-500">{t.telefone || '—'}</td>
                <td className="p-3 text-center">{t.qtdPets}</td>
                <td className="p-3 text-right font-semibold text-emerald-700">
                  R$ {t.totalPago?.toFixed(2)}
                </td>
                <td className="p-3 text-right">
                  <button onClick={() => nav(`/financeiro/cliente/${t.id}`)}
                    className="text-xs text-blue-600 hover:underline">
                    Ver histórico
                  </button>
                </td>
              </tr>
            ))}
            {tutores.length === 0 && (
              <tr><td colSpan="5" className="p-6 text-center text-slate-400">Nenhum cliente encontrado</td></tr>
            )}
          </tbody>
        </table>
      </div>

      {total > 20 && (
        <div className="flex justify-between items-center text-sm">
          <span className="text-slate-400">{total} clientes</span>
          <div className="flex gap-2">
            <button disabled={page === 1} onClick={() => setPage(p => p - 1)}
              className="px-3 py-1.5 border rounded-lg disabled:opacity-40">← Anterior</button>
            <span className="px-3 py-1.5 text-slate-600">Pág {page}</span>
            <button disabled={page * 20 >= total} onClick={() => setPage(p => p + 1)}
              className="px-3 py-1.5 border rounded-lg disabled:opacity-40">Próxima →</button>
          </div>
        </div>
      )}
    </div>
  )
}
