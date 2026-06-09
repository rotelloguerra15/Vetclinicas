import { useEffect, useState, useCallback } from 'react'
import api from '../api/client'

const TIPOS_CONTA = { corrente: 'Conta Corrente', poupanca: 'Poupança', caixa: 'Caixa', outro: 'Outro' }
const TIPO_LABEL = {
  entrada:       'Entrada',
  saida:         'Saída',
  transferencia: 'Transferência',
  deposito:      'Depósito'
}

const TIPO_COR = {
  entrada:       'bg-emerald-100 text-emerald-700',
  saida:         'bg-red-100 text-red-700',
  transferencia: 'bg-blue-100 text-blue-700',
  deposito:      'bg-violet-100 text-violet-700'
}

function fmt(v) { return (v ?? 0).toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' }) }
function fmtData(d) { return d ? new Date(d + 'T12:00:00').toLocaleDateString('pt-BR') : '' }
function hoje() { return new Date().toISOString().split('T')[0] }

export default function Bancario() {
  const [aba, setAba] = useState('movimentacoes')

  return (
    <div>
      <h2 className="text-2xl font-bold mb-6">Movimentação Bancária</h2>

      <div className="flex gap-1 border-b mb-6 overflow-x-auto">
        {[
          { k: 'movimentacoes', l: '💳 Movimentações' },
          { k: 'contas',        l: '🏦 Contas' },
          { k: 'conciliacao',   l: '📋 Conciliação Diária' },
          { k: 'clientes',      l: '👥 Histórico por Cliente' },
        ].map(t => (
          <button key={t.k} onClick={() => setAba(t.k)}
            className={`px-4 py-2 text-sm font-medium border-b-2 -mb-px whitespace-nowrap ${
              aba === t.k ? 'border-slate-900 text-slate-900' : 'border-transparent text-slate-400 hover:text-slate-600'
            }`}>
            {t.l}
          </button>
        ))}
      </div>

      {aba === 'movimentacoes' && <AbaMovimentacoes />}
      {aba === 'contas'        && <AbaContas />}
      {aba === 'conciliacao'   && <AbaConciliacao />}
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

  const dataHoje = hoje()
  const [filtros, setFiltros] = useState({
    contaId: '', tipo: '',
    de: new Date(new Date().setDate(1)).toISOString().split('T')[0],
    ate: dataHoje
  })
  const [form, setForm] = useState({
    contaBancariaId: '', tipo: 'entrada', valor: '',
    descricao: '', dataMovimentacao: dataHoje,
    categoriaId: '', contaDestinoId: ''
  })

  const carregar = useCallback(() => {
    api.get('/bancario/movimentacoes', {
      params: { ...filtros, page, pageSize: 30 }
    }).then(r => { setMovs(r.data.items); setTotal(r.data.total) }).catch(() => {})
  }, [filtros, page])

  useEffect(() => {
    api.get('/bancario/contas').then(r => setContas(r.data)).catch(() => {})
    api.get('/financeiro/categorias').then(r => setCats(r.data)).catch(() => {})
  }, [])

  useEffect(() => { carregar() }, [carregar])

  async function salvar(e) {
    e.preventDefault()
    try {
      await api.post('/bancario/movimentacoes', {
        ...form,
        valor: parseFloat(form.valor),
        categoriaId: form.categoriaId || null,
        contaDestinoId: form.contaDestinoId || null
      })
      setMostrarForm(false)
      setForm({ contaBancariaId: '', tipo: 'entrada', valor: '', descricao: '', dataMovimentacao: dataHoje, categoriaId: '', contaDestinoId: '' })
      carregar()
    } catch (err) {
      alert(err.response?.data?.erro || 'Erro ao salvar.')
    }
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

  const totalEntradas = movs.filter(m => m.tipo === 'entrada' || m.tipo === 'deposito').reduce((s, m) => s + m.valor, 0)
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
              <option value="deposito">Depósito</option>
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
          <div className="text-xl font-bold text-emerald-700">{fmt(totalEntradas)}</div>
        </div>
        <div className="bg-red-50 rounded-xl p-4">
          <div className="text-xs text-red-600 mb-1">Saídas</div>
          <div className="text-xl font-bold text-red-700">{fmt(totalSaidas)}</div>
        </div>
        <div className="bg-slate-50 rounded-xl p-4">
          <div className="text-xs text-slate-500 mb-1">Saldo período</div>
          <div className={`text-xl font-bold ${totalEntradas - totalSaidas >= 0 ? 'text-emerald-700' : 'text-red-700'}`}>
            {fmt(totalEntradas - totalSaidas)}
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
              {contas.map(c => <option key={c.id} value={c.id}>{c.nome} — {fmt(c.saldoAtual)}</option>)}
            </select>
            <select className="border rounded-lg px-3 py-2"
              value={form.tipo} onChange={e => setForm({ ...form, tipo: e.target.value })}>
              <option value="entrada">Entrada</option>
              <option value="saida">Saída</option>
              <option value="transferencia">Transferência</option>
              <option value="deposito">Depósito</option>
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
                <td className="p-3 whitespace-nowrap text-slate-500">{fmtData(m.dataMovimentacao)}</td>
                <td className="p-3">
                  {m.descricao}
                  {m.origem === 'baixa_financeiro' && (
                    <span className="ml-1 text-xs text-slate-400">(financeiro)</span>
                  )}
                </td>
                <td className="p-3 text-slate-500 text-xs">{m.contaNome}</td>
                <td className="p-3">
                  <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${TIPO_COR[m.tipo]}`}>
                    {(TIPO_LABEL[m.tipo] ?? m.tipo)}
                  </span>
                </td>
                <td className={`p-3 text-right font-medium ${m.tipo === 'entrada' || m.tipo === 'deposito' ? 'text-emerald-700' : m.tipo === 'saida' ? 'text-red-600' : 'text-blue-600'}`}>
                  {m.tipo === 'saida' ? '-' : '+'} {fmt(m.valor)}
                </td>
                <td className="p-3">
                  <button onClick={() => conciliar(m.id)}
                    className={`text-xs px-2 py-0.5 rounded-full ${m.conciliado ? 'bg-emerald-100 text-emerald-700' : 'bg-slate-100 text-slate-500'}`}>
                    {m.conciliado ? '✓ Sim' : 'Não'}
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

// ── ABA: CONTAS BANCÁRIAS ─────────────────────────────────────────────────────
// FIX: saldoInicial agora é enviado como número (parseFloat), campo obrigatório

function AbaContas() {
  const [contas, setContas] = useState([])
  const [form, setForm]     = useState({ nome: '', banco: '', agencia: '', conta: '', tipo: 'corrente', saldoInicial: '0' })
  const [editId, setEditId] = useState(null)
  const [mostrarForm, setMostrarForm] = useState(false)
  const [salvando, setSalvando] = useState(false)

  function carregar() {
    api.get('/bancario/contas').then(r => setContas(r.data)).catch(() => {})
  }

  useEffect(() => { carregar() }, [])

  async function salvar(e) {
    e.preventDefault()
    setSalvando(true)
    try {
      const payload = {
        nome:         form.nome,
        banco:        form.banco || null,
        agencia:      form.agencia || null,
        conta:        form.conta || null,
        tipo:         form.tipo,
        saldoInicial: parseFloat(form.saldoInicial || '0')
      }
      if (editId) await api.put(`/bancario/contas/${editId}`, payload)
      else await api.post('/bancario/contas', payload)
      setMostrarForm(false)
      setEditId(null)
      setForm({ nome: '', banco: '', agencia: '', conta: '', tipo: 'corrente', saldoInicial: '0' })
      carregar()
    } catch (err) {
      alert(err.response?.data?.erro || 'Erro ao salvar conta.')
    } finally {
      setSalvando(false)
    }
  }

  function editar(c) {
    setForm({
      nome: c.nome, banco: c.banco || '', agencia: c.agencia || '',
      conta: c.conta || '', tipo: c.tipo, saldoInicial: String(c.saldoInicial ?? 0)
    })
    setEditId(c.id)
    setMostrarForm(true)
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
      <button onClick={() => { setMostrarForm(!mostrarForm); setEditId(null); setForm({ nome: '', banco: '', agencia: '', conta: '', tipo: 'corrente', saldoInicial: '0' }) }}
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
            <div>
              <label className="text-xs text-slate-500 block mb-1">Saldo inicial (R$)</label>
              <input className="border rounded-lg px-3 py-2 w-full" type="number" step="0.01" min="0"
                value={form.saldoInicial} onChange={e => setForm({ ...form, saldoInicial: e.target.value })} />
            </div>
          </div>
          <div className="flex gap-2">
            <button type="submit" disabled={salvando}
              className="bg-emerald-600 text-white px-5 py-2 rounded-lg text-sm disabled:opacity-50">
              {salvando ? 'Salvando...' : 'Salvar'}
            </button>
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
                {c.conta ? ` · CC ${c.conta}` : ''}
              </div>
            </div>
            <div className="text-right">
              <div className={`text-lg font-bold ${(c.saldoAtual ?? 0) >= 0 ? 'text-emerald-700' : 'text-red-600'}`}>
                {fmt(c.saldoAtual)}
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

// ── ABA: CONCILIAÇÃO DIÁRIA ───────────────────────────────────────────────────

function AbaConciliacao() {
  const [contas, setContas]       = useState([])
  const [contaId, setContaId]     = useState('')
  const [data, setData]           = useState(hoje())
  const [resumo, setResumo]       = useState(null)
  const [historico, setHistorico] = useState([])
  const [carregando, setCarregando] = useState(false)
  const [saldoExtrato, setSaldoExtrato] = useState('')
  const [observacao, setObservacao]     = useState('')
  const [fechando, setFechando]   = useState(false)
  const [erro, setErro]           = useState('')

  useEffect(() => {
    api.get('/bancario/contas').then(r => {
      setContas(r.data)
      if (r.data.length > 0) setContaId(r.data[0].id)
    }).catch(() => {})
  }, [])

  useEffect(() => {
    if (!contaId) return
    carregarDia()
    carregarHistorico()
  }, [contaId, data])

  async function carregarDia() {
    setCarregando(true)
    setErro('')
    try {
      const r = await api.get(`/bancario/conciliacao/${contaId}/${data}`)
      setResumo(r.data)
    } catch {
      setErro('Erro ao carregar dados do dia.')
      setResumo(null)
    } finally {
      setCarregando(false)
    }
  }

  async function carregarHistorico() {
    try {
      const r = await api.get(`/bancario/conciliacao/historico/${contaId}`)
      setHistorico(r.data)
    } catch { setHistorico([]) }
  }

  async function fecharDia() {
    if (!confirm(`Confirma o fechamento do dia ${fmtData(data)} para a conta selecionada?\n\nEsta ação marcará todas as movimentações do dia como conciliadas.`)) return
    setFechando(true)
    setErro('')
    try {
      await api.post('/bancario/conciliacao/fechar', {
        contaBancariaId: contaId,
        dataConciliacao: data,
        saldoExtrato:    saldoExtrato ? parseFloat(saldoExtrato) : null,
        observacao:      observacao || null
      })
      setSaldoExtrato('')
      setObservacao('')
      await carregarDia()
      await carregarHistorico()
    } catch (err) {
      setErro(err.response?.data?.erro || 'Erro ao fechar o dia.')
    } finally {
      setFechando(false)
    }
  }

  const jaFechado = resumo?.fechamento != null

  return (
    <div className="space-y-5 max-w-3xl">
      {/* Seletor */}
      <div className="bg-white rounded-2xl shadow p-4 flex flex-wrap gap-3 items-end">
        <div>
          <label className="text-xs text-slate-500 block mb-1">Conta</label>
          <select className="border rounded-lg px-3 py-2 text-sm"
            value={contaId} onChange={e => setContaId(e.target.value)}>
            {contas.map(c => <option key={c.id} value={c.id}>{c.nome}</option>)}
          </select>
        </div>
        <div>
          <label className="text-xs text-slate-500 block mb-1">Data</label>
          <input type="date" className="border rounded-lg px-3 py-2 text-sm"
            value={data} onChange={e => setData(e.target.value)} />
        </div>
        <button onClick={carregarDia}
          className="bg-slate-900 text-white px-4 py-2 rounded-lg text-sm">
          Carregar
        </button>
      </div>

      {erro && (
        <div className="bg-red-50 border border-red-200 rounded-xl p-3 text-red-700 text-sm">{erro}</div>
      )}

      {carregando && (
        <div className="text-center text-slate-400 py-8">Carregando...</div>
      )}

      {resumo && !carregando && (
        <>
          {/* Status do fechamento */}
          {jaFechado && (
            <div className="bg-emerald-50 border border-emerald-200 rounded-xl p-4 flex items-center gap-3">
              <span className="text-2xl">✅</span>
              <div>
                <div className="font-semibold text-emerald-800">Dia fechado</div>
                <div className="text-sm text-emerald-700">
                  Fechado em {new Date(resumo.fechamento.fechadoEm).toLocaleString('pt-BR')}
                  {resumo.fechamento.observacao && ` · ${resumo.fechamento.observacao}`}
                </div>
              </div>
            </div>
          )}

          {/* Resumo financeiro do dia */}
          <div className="bg-white rounded-2xl shadow p-5 space-y-4">
            <h3 className="font-bold text-slate-700">
              Resumo do dia — {fmtData(data)}
              {resumo.conta?.nome && <span className="text-slate-400 font-normal ml-2">({resumo.conta.nome})</span>}
            </h3>

            <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
              <div className="bg-slate-50 rounded-xl p-3 text-center">
                <div className="text-xs text-slate-500 mb-1">Saldo Anterior</div>
                <div className="text-lg font-bold text-slate-700">{fmt(resumo.saldoAnterior)}</div>
              </div>
              <div className="bg-emerald-50 rounded-xl p-3 text-center">
                <div className="text-xs text-emerald-600 mb-1">+ Entradas</div>
                <div className="text-lg font-bold text-emerald-700">{fmt(resumo.totalEntradas)}</div>
              </div>
              <div className="bg-red-50 rounded-xl p-3 text-center">
                <div className="text-xs text-red-600 mb-1">- Saídas</div>
                <div className="text-lg font-bold text-red-700">{fmt(resumo.totalSaidas)}</div>
              </div>
              <div className={`rounded-xl p-3 text-center ${resumo.saldoCalculado >= 0 ? 'bg-blue-50' : 'bg-orange-50'}`}>
                <div className={`text-xs mb-1 ${resumo.saldoCalculado >= 0 ? 'text-blue-600' : 'text-orange-600'}`}>
                  = Saldo Final
                </div>
                <div className={`text-lg font-bold ${resumo.saldoCalculado >= 0 ? 'text-blue-700' : 'text-orange-700'}`}>
                  {fmt(resumo.saldoCalculado)}
                </div>
              </div>
            </div>

            {/* Transferências */}
            {resumo.totalTransferencias !== 0 && (
              <div className="text-xs text-slate-400 text-center">
                Transferências líquidas do dia: {fmt(resumo.totalTransferencias)}
              </div>
            )}

            {/* Comparação com extrato (se fechado) */}
            {jaFechado && resumo.fechamento.saldoExtrato != null && (
              <div className={`rounded-xl p-4 ${Math.abs(resumo.fechamento.diferenca) < 0.01 ? 'bg-emerald-50 border border-emerald-200' : 'bg-amber-50 border border-amber-200'}`}>
                <div className="flex justify-between text-sm">
                  <span className="font-medium">Saldo Extrato Banco:</span>
                  <span className="font-bold">{fmt(resumo.fechamento.saldoExtrato)}</span>
                </div>
                <div className="flex justify-between text-sm mt-1">
                  <span className="font-medium">Diferença:</span>
                  <span className={`font-bold ${Math.abs(resumo.fechamento.diferenca) < 0.01 ? 'text-emerald-700' : 'text-amber-700'}`}>
                    {Math.abs(resumo.fechamento.diferenca) < 0.01 ? '✓ Conciliado' : fmt(resumo.fechamento.diferenca)}
                  </span>
                </div>
              </div>
            )}
          </div>

          {/* Movimentações do dia */}
          <div className="bg-white rounded-2xl shadow overflow-hidden">
            <div className="p-4 border-b">
              <h4 className="font-semibold text-slate-700">
                Movimentações do dia ({resumo.movimentacoes?.length ?? 0})
              </h4>
            </div>
            {resumo.movimentacoes?.length > 0 ? (
              <table className="w-full text-sm">
                <thead className="bg-slate-50 text-left">
                  <tr>
                    <th className="p-3">Descrição</th>
                    <th className="p-3">Categoria</th>
                    <th className="p-3">Tipo</th>
                    <th className="p-3 text-right">Valor</th>
                    <th className="p-3 text-center">Conciliado</th>
                  </tr>
                </thead>
                <tbody>
                  {resumo.movimentacoes.map(m => (
                    <tr key={m.id} className="border-t hover:bg-slate-50">
                      <td className="p-3">
                        {m.descricao}
                        {m.origem === 'baixa_financeiro' && (
                          <span className="ml-1 text-xs bg-blue-50 text-blue-600 px-1.5 py-0.5 rounded">financeiro</span>
                        )}
                      </td>
                      <td className="p-3 text-xs text-slate-400">{m.categoriaNome || '—'}</td>
                      <td className="p-3">
                        <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${TIPO_COR[m.tipo]}`}>
                          {(TIPO_LABEL[m.tipo] ?? m.tipo)}
                        </span>
                      </td>
                      <td className={`p-3 text-right font-medium ${m.tipo === 'entrada' || m.tipo === 'deposito' ? 'text-emerald-700' : m.tipo === 'saida' ? 'text-red-600' : 'text-blue-600'}`}>
                        {m.tipo === 'saida' ? '-' : '+'} {fmt(m.valor)}
                      </td>
                      <td className="p-3 text-center">
                        <span className={`text-xs px-2 py-0.5 rounded-full ${m.conciliado ? 'bg-emerald-100 text-emerald-700' : 'bg-slate-100 text-slate-500'}`}>
                          {m.conciliado ? '✓' : '—'}
                        </span>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            ) : (
              <div className="p-6 text-center text-slate-400">Nenhuma movimentação neste dia.</div>
            )}
          </div>

          {/* Formulário de fechamento */}
          {!jaFechado && (
            <div className="bg-white rounded-2xl shadow p-5 space-y-4">
              <h4 className="font-semibold text-slate-700">Fechar o dia</h4>
              <p className="text-sm text-slate-500">
                Informe o saldo do extrato bancário para comparação (opcional). 
                Ao fechar, todas as movimentações do dia serão marcadas como conciliadas.
              </p>
              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="text-xs text-slate-500 block mb-1">Saldo extrato banco (R$)</label>
                  <input type="number" step="0.01" className="border rounded-lg px-3 py-2 w-full text-sm"
                    placeholder="Opcional — informe o saldo do extrato"
                    value={saldoExtrato} onChange={e => setSaldoExtrato(e.target.value)} />
                </div>
                <div>
                  <label className="text-xs text-slate-500 block mb-1">Observação</label>
                  <input type="text" className="border rounded-lg px-3 py-2 w-full text-sm"
                    placeholder="Ex: Conferido com extrato digital"
                    value={observacao} onChange={e => setObservacao(e.target.value)} />
                </div>
              </div>

              {saldoExtrato && (
                <div className={`rounded-xl p-3 text-sm flex justify-between ${Math.abs(parseFloat(saldoExtrato) - resumo.saldoCalculado) < 0.01 ? 'bg-emerald-50 text-emerald-700' : 'bg-amber-50 text-amber-700'}`}>
                  <span>Diferença prevista:</span>
                  <span className="font-bold">
                    {Math.abs(parseFloat(saldoExtrato) - resumo.saldoCalculado) < 0.01
                      ? '✓ Sem diferença'
                      : fmt(resumo.saldoCalculado - parseFloat(saldoExtrato))}
                  </span>
                </div>
              )}

              <button onClick={fecharDia} disabled={fechando}
                className="bg-slate-900 text-white px-6 py-2.5 rounded-xl text-sm font-medium disabled:opacity-50 hover:bg-slate-800 w-full">
                {fechando ? 'Fechando...' : `Fechar dia ${fmtData(data)}`}
              </button>
            </div>
          )}
        </>
      )}

      {/* Histórico de conciliações */}
      {historico.length > 0 && (
        <div className="bg-white rounded-2xl shadow overflow-hidden">
          <div className="p-4 border-b">
            <h4 className="font-semibold text-slate-700">Histórico de Fechamentos</h4>
          </div>
          <table className="w-full text-sm">
            <thead className="bg-slate-50 text-left">
              <tr>
                <th className="p-3">Data</th>
                <th className="p-3 text-right">Saldo Anterior</th>
                <th className="p-3 text-right">Entradas</th>
                <th className="p-3 text-right">Saídas</th>
                <th className="p-3 text-right">Saldo Final</th>
                <th className="p-3 text-center">Extrato</th>
                <th className="p-3 text-center">Diferença</th>
              </tr>
            </thead>
            <tbody>
              {historico.map(h => (
                <tr key={h.id} className="border-t hover:bg-slate-50">
                  <td className="p-3 font-medium">{fmtData(h.dataConciliacao)}</td>
                  <td className="p-3 text-right text-slate-500">{fmt(h.saldoAnterior)}</td>
                  <td className="p-3 text-right text-emerald-700">+{fmt(h.totalEntradas)}</td>
                  <td className="p-3 text-right text-red-600">-{fmt(h.totalSaidas)}</td>
                  <td className={`p-3 text-right font-bold ${h.saldoFinal >= 0 ? 'text-blue-700' : 'text-orange-700'}`}>
                    {fmt(h.saldoFinal)}
                  </td>
                  <td className="p-3 text-center text-slate-500 text-xs">
                    {h.saldoExtrato != null ? fmt(h.saldoExtrato) : '—'}
                  </td>
                  <td className="p-3 text-center">
                    {h.diferenca == null ? (
                      <span className="text-xs text-slate-400">—</span>
                    ) : Math.abs(h.diferenca) < 0.01 ? (
                      <span className="text-xs text-emerald-600 font-medium">✓ OK</span>
                    ) : (
                      <span className="text-xs text-amber-600 font-medium">{fmt(h.diferenca)}</span>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}

// ── ABA: HISTÓRICO DE CLIENTES ────────────────────────────────────────────────

function AbaClientes() {
  const [tutores, setTutores]   = useState([])
  const [total, setTotal]       = useState(0)
  const [page, setPage]         = useState(1)
  const [busca, setBusca]       = useState('')
  const [inputBusca, setInput]  = useState('')
  const [detalhe, setDetalhe]   = useState(null)
  const [loadDetalhe, setLD]    = useState(false)

  const dataHoje = hoje()
  const [de, setDe]   = useState(new Date(new Date().setMonth(new Date().getMonth() - 3)).toISOString().split('T')[0])
  const [ate, setAte] = useState(dataHoje)

  const carregar = useCallback(() => {
    api.get('/bancario/historico-clientes', { params: { busca, de, ate, page, pageSize: 20 } })
      .then(r => { setTutores(r.data.items); setTotal(r.data.total) })
      .catch(() => {})
  }, [busca, de, ate, page])

  useEffect(() => { carregar() }, [carregar])

  async function verDetalhe(tutorId) {
    setLD(true)
    try {
      const r = await api.get(`/bancario/historico-cliente/${tutorId}`, { params: { de, ate } })
      setDetalhe(r.data)
    } catch { setDetalhe(null) }
    finally { setLD(false) }
  }

  return (
    <div className="space-y-4">
      {/* Filtros */}
      <div className="bg-white rounded-2xl shadow p-4 flex flex-wrap gap-3 items-end">
        <div className="flex-1 min-w-48">
          <label className="text-xs text-slate-500 block mb-1">Buscar cliente</label>
          <div className="flex gap-2">
            <input className="border rounded-lg px-3 py-2 text-sm flex-1" placeholder="Nome do tutor..."
              value={inputBusca} onChange={e => setInput(e.target.value)}
              onKeyDown={e => e.key === 'Enter' && setBusca(inputBusca)} />
            <button onClick={() => setBusca(inputBusca)}
              className="bg-slate-900 text-white px-4 py-2 rounded-lg text-sm">Buscar</button>
          </div>
        </div>
        <div>
          <label className="text-xs text-slate-500 block mb-1">De</label>
          <input type="date" className="border rounded-lg px-3 py-2 text-sm" value={de} onChange={e => setDe(e.target.value)} />
        </div>
        <div>
          <label className="text-xs text-slate-500 block mb-1">Até</label>
          <input type="date" className="border rounded-lg px-3 py-2 text-sm" value={ate} onChange={e => setAte(e.target.value)} />
        </div>
      </div>

      <div className="grid md:grid-cols-2 gap-4">
        {/* Lista */}
        <div className="space-y-2">
          {tutores.map(t => (
            <div key={t.id}
              className={`bg-white rounded-2xl shadow p-4 cursor-pointer hover:ring-2 hover:ring-slate-200 transition ${detalhe?.tutor?.id === t.id ? 'ring-2 ring-slate-900' : ''}`}
              onClick={() => verDetalhe(t.id)}>
              <div className="flex justify-between">
                <div>
                  <div className="font-semibold text-sm">{t.nome}</div>
                  <div className="text-xs text-slate-400">{t.telefone} · {t.qtdPets} pet(s)</div>
                </div>
                <div className="text-right">
                  <div className="text-sm font-bold text-emerald-700">{fmt(t.totalPago)}</div>
                  <div className="text-xs text-slate-400">pago no período</div>
                </div>
              </div>
            </div>
          ))}
          {tutores.length === 0 && <p className="text-slate-400 text-sm text-center py-8">Nenhum cliente encontrado.</p>}
          {total > 20 && (
            <div className="flex justify-center gap-2 text-sm pt-2">
              <button disabled={page === 1} onClick={() => setPage(p => p - 1)}
                className="px-3 py-1.5 border rounded-lg disabled:opacity-40">← Ant</button>
              <span className="px-3 py-1.5 text-slate-500">Pág {page}</span>
              <button disabled={page * 20 >= total} onClick={() => setPage(p => p + 1)}
                className="px-3 py-1.5 border rounded-lg disabled:opacity-40">Próx →</button>
            </div>
          )}
        </div>

        {/* Detalhe */}
        <div>
          {loadDetalhe && <div className="text-center text-slate-400 py-12">Carregando...</div>}
          {detalhe && !loadDetalhe && (
            <div className="bg-white rounded-2xl shadow p-5 space-y-4">
              <div>
                <div className="font-bold text-lg">{detalhe.tutor.nome}</div>
                <div className="text-sm text-slate-400">{detalhe.tutor.telefone} · {detalhe.tutor.email}</div>
              </div>
              <div className="grid grid-cols-3 gap-2 text-center">
                <div className="bg-emerald-50 rounded-xl p-3">
                  <div className="text-xs text-emerald-600">Total pago</div>
                  <div className="font-bold text-emerald-700">{fmt(detalhe.resumo.totalGasto)}</div>
                </div>
                <div className="bg-amber-50 rounded-xl p-3">
                  <div className="text-xs text-amber-600">Em aberto</div>
                  <div className="font-bold text-amber-700">{fmt(detalhe.resumo.totalAberto)}</div>
                </div>
                <div className="bg-slate-50 rounded-xl p-3">
                  <div className="text-xs text-slate-500">Atendimentos</div>
                  <div className="font-bold">{detalhe.resumo.totalServicos}</div>
                </div>
              </div>
              <div className="space-y-2 max-h-80 overflow-y-auto">
                {detalhe.contas.map(c => (
                  <div key={c.id} className="flex items-center justify-between p-2 rounded-lg bg-slate-50 text-sm">
                    <div>
                      <div>{c.descricao}</div>
                      <div className="text-xs text-slate-400">
                        {fmtData(c.dataCompetencia)} · {c.formaPagamento || 'N/I'}
                      </div>
                    </div>
                    <div className="text-right">
                      <div className={`font-bold ${c.status === 'aberta' ? 'text-amber-600' : 'text-emerald-700'}`}>
                        {fmt(c.valorPago ?? c.valor)}
                      </div>
                      <div className="text-xs text-slate-400">{c.status}</div>
                    </div>
                  </div>
                ))}
                {detalhe.contas.length === 0 && (
                  <p className="text-center text-slate-400 py-4">Nenhum lançamento no período.</p>
                )}
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  )
}
