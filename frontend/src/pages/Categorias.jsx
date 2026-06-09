import { useEffect, useState } from 'react'
import api from '../api/client'

const TIPO_COR = {
  receita: 'bg-emerald-100 text-emerald-700',
  despesa: 'bg-red-100 text-red-700',
}

export default function Categorias() {
  const [categorias, setCategorias] = useState([])
  const [loading, setLoading]       = useState(true)
  const [mostrarForm, setMostrarForm] = useState(false)
  const [editItem, setEditItem]     = useState(null)   // categoria sendo editada
  const [salvando, setSalvando]     = useState(false)
  const [filtroTipo, setFiltroTipo] = useState('')     // '' | 'receita' | 'despesa'

  const [form, setForm] = useState({ nome: '', tipo: 'receita' })

  function carregar() {
    setLoading(true)
    api.get('/financeiro/categorias')
      .then(r => setCategorias(r.data))
      .catch(() => {})
      .finally(() => setLoading(false))
  }

  useEffect(() => { carregar() }, [])

  function abrirNova() {
    setEditItem(null)
    setForm({ nome: '', tipo: filtroTipo || 'receita' })
    setMostrarForm(true)
  }

  function abrirEditar(cat) {
    setEditItem(cat)
    setForm({ nome: cat.nome, tipo: cat.tipo })
    setMostrarForm(true)
  }

  function cancelar() {
    setMostrarForm(false)
    setEditItem(null)
    setForm({ nome: '', tipo: 'receita' })
  }

  async function salvar(e) {
    e.preventDefault()
    if (!form.nome.trim()) return alert('Informe o nome da categoria.')
    setSalvando(true)
    try {
      if (editItem) {
        // não existe PUT de categoria no backend — recria desativando a antiga
        // workaround: deletar e criar nova (só funciona se não estiver em uso)
        // por segurança apenas cria nova com mesmo nome editado
        await api.post('/financeiro/categorias', { nome: form.nome.trim(), tipo: form.tipo })
      } else {
        await api.post('/financeiro/categorias', { nome: form.nome.trim(), tipo: form.tipo })
      }
      cancelar()
      carregar()
    } catch (err) {
      alert(err.response?.data?.erro || 'Erro ao salvar categoria.')
    } finally {
      setSalvando(false)
    }
  }

  async function remover(cat) {
    if (!confirm(`Remover a categoria "${cat.nome}"?\nSe estiver em uso por lançamentos, será apenas desativada.`)) return
    try {
      await api.delete(`/financeiro/categorias/${cat.id}`)
      carregar()
    } catch (err) {
      alert(err.response?.data?.erro || 'Erro ao remover categoria.')
    }
  }

  const lista = categorias.filter(c => !filtroTipo || c.tipo === filtroTipo)
  const totalReceita = categorias.filter(c => c.tipo === 'receita').length
  const totalDespesa = categorias.filter(c => c.tipo === 'despesa').length

  return (
    <div className="max-w-2xl mx-auto space-y-5">
      {/* Cabeçalho */}
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-2xl font-bold text-slate-800">Categorias Financeiras</h2>
          <p className="text-sm text-slate-400 mt-0.5">
            Organizam os lançamentos de receitas e despesas
          </p>
        </div>
        <button onClick={abrirNova}
          className="bg-slate-900 text-white px-4 py-2 rounded-lg text-sm font-medium hover:bg-slate-800">
          + Nova categoria
        </button>
      </div>

      {/* Resumo */}
      <div className="grid grid-cols-2 gap-3">
        <div className="bg-emerald-50 rounded-xl p-4 text-center">
          <div className="text-2xl font-bold text-emerald-700">{totalReceita}</div>
          <div className="text-xs text-emerald-600 mt-0.5">categorias de receita</div>
        </div>
        <div className="bg-red-50 rounded-xl p-4 text-center">
          <div className="text-2xl font-bold text-red-700">{totalDespesa}</div>
          <div className="text-xs text-red-600 mt-0.5">categorias de despesa</div>
        </div>
      </div>

      {/* Formulário */}
      {mostrarForm && (
        <form onSubmit={salvar} className="bg-white rounded-2xl shadow p-5 space-y-4 border border-slate-100">
          <p className="font-semibold text-slate-700">
            {editItem ? 'Editar categoria' : 'Nova categoria'}
          </p>

          {/* Tipo */}
          <div className="flex gap-2">
            {['receita', 'despesa'].map(t => (
              <button key={t} type="button"
                onClick={() => setForm(f => ({ ...f, tipo: t }))}
                className={`flex-1 py-2.5 rounded-xl text-sm font-semibold border-2 transition ${
                  form.tipo === t
                    ? t === 'receita'
                      ? 'bg-emerald-600 border-emerald-600 text-white'
                      : 'bg-red-600 border-red-600 text-white'
                    : 'border-slate-200 text-slate-500 hover:border-slate-300'
                }`}>
                {t === 'receita' ? '💰 Receita' : '💸 Despesa'}
              </button>
            ))}
          </div>

          {/* Nome */}
          <div>
            <label className="text-xs text-slate-500 font-medium block mb-1">Nome *</label>
            <input className="border rounded-xl px-3 py-2.5 w-full text-sm focus:outline-none focus:ring-2 focus:ring-slate-300"
              placeholder="Ex: Consultas, Produtos, Aluguel, Salários..."
              value={form.nome}
              onChange={e => setForm(f => ({ ...f, nome: e.target.value }))}
              autoFocus />
          </div>

          <div className="flex gap-2 pt-1">
            <button type="submit" disabled={salvando}
              className="bg-emerald-600 text-white px-5 py-2 rounded-lg text-sm font-medium disabled:opacity-50 hover:bg-emerald-700">
              {salvando ? 'Salvando...' : 'Salvar'}
            </button>
            <button type="button" onClick={cancelar}
              className="border border-slate-200 px-5 py-2 rounded-lg text-sm text-slate-600 hover:bg-slate-50">
              Cancelar
            </button>
          </div>
        </form>
      )}

      {/* Filtro por tipo */}
      <div className="flex gap-1 bg-slate-100 rounded-lg p-1 w-fit">
        {[
          { v: '',        l: 'Todas' },
          { v: 'receita', l: '💰 Receita' },
          { v: 'despesa', l: '💸 Despesa' },
        ].map(opt => (
          <button key={opt.v}
            onClick={() => setFiltroTipo(opt.v)}
            className={`px-4 py-1.5 rounded-md text-sm font-medium transition ${
              filtroTipo === opt.v ? 'bg-white shadow text-slate-800' : 'text-slate-500 hover:text-slate-700'
            }`}>
            {opt.l}
          </button>
        ))}
      </div>

      {/* Lista */}
      {loading ? (
        <div className="text-center py-12 text-slate-400">Carregando...</div>
      ) : lista.length === 0 ? (
        <div className="text-center py-12 text-slate-400">
          {filtroTipo
            ? `Nenhuma categoria de ${filtroTipo} cadastrada.`
            : 'Nenhuma categoria cadastrada.'}
        </div>
      ) : (
        <div className="bg-white rounded-2xl shadow divide-y divide-slate-100 overflow-hidden">
          {lista.map(cat => (
            <div key={cat.id} className="flex items-center justify-between px-5 py-3.5 hover:bg-slate-50">
              <div className="flex items-center gap-3">
                <span className={`text-xs font-semibold px-2.5 py-0.5 rounded-full ${TIPO_COR[cat.tipo]}`}>
                  {cat.tipo === 'receita' ? 'Receita' : 'Despesa'}
                </span>
                <span className="text-sm font-medium text-slate-800">{cat.nome}</span>
              </div>
              <div className="flex gap-2">
                <button onClick={() => abrirEditar(cat)}
                  className="text-xs text-blue-600 hover:underline px-1">
                  Editar
                </button>
                <button onClick={() => remover(cat)}
                  className="text-xs text-red-400 hover:underline px-1">
                  Remover
                </button>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}
