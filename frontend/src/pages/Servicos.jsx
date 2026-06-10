import { useEffect, useState } from 'react'
import api from '../api/client'

const CATEGORIAS = {
  consulta:   'Consulta',
  banho_tosa: 'Banho e Tosa',
  banho:      'Banho',
  tosa:       'Tosa',
  vacina:     'Vacina',
  cirurgia:   'Cirurgia',
  exame:      'Exame',
  retorno:    'Retorno',
  outro:      'Outro'
}

// Emojis sugeridos por categoria
const ICONES_SUGERIDOS = {
  consulta:   ['🩺','🏥','💊','🩻','📋'],
  banho_tosa: ['🛁','🚿','✂️','🪮','🫧'],
  banho:      ['🛁','🚿','🫧','💧','🐾'],
  tosa:       ['✂️','🪮','💈','🐩','✨'],
  vacina:     ['💉','🧪','🛡️','💊','🩹'],
  cirurgia:   ['⚕️','🔪','🏥','🩺','💉'],
  exame:      ['🔬','🧪','📊','🩻','🧬'],
  retorno:    ['🔄','📅','🩺','✅','🔁'],
  outro:      ['🐾','⭐','🏥','📋','🎯']
}

const ICONES_EXTRAS = ['🐕','🐈','🐇','🦜','🐢','🐟','🦎','🐿️','🦊','🦔',
  '❤️','💙','💚','💛','🧡','💜','🩷','🩶','🤍','🖤',
  '⭐','🌟','✨','💫','🎯','🏆','🎖️','🏅','🎪','🎨']

function fmt(v) {
  return Number(v).toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
}

export default function Servicos() {
  const [servicos, setServicos] = useState([])
  const [form, setForm]         = useState(null)
  const [loading, setLoading]   = useState(true)
  const [showPicker, setShowPicker] = useState(false)

  function carregar() {
    setLoading(true)
    api.get('/servicos').then(r => setServicos(r.data)).catch(() => {}).finally(() => setLoading(false))
  }
  useEffect(() => { carregar() }, [])

  function novo() {
    setForm({ id: null, nome: '', categoria: 'consulta', descricao: '', precoBase: '', duracaoMin: '60', icone: '🐾' })
    setShowPicker(false)
  }
  function editar(s) {
    setForm({ id: s.id, nome: s.nome, categoria: s.categoria || 'consulta', descricao: s.descricao || '', precoBase: s.precoBase, duracaoMin: s.duracaoMin || '', icone: s.icone || '🐾' })
    setShowPicker(false)
  }

  function setCategoria(cat) {
    const iconeAtual = form.icone
    const ehPadrao = iconeAtual === '🐾' || Object.values(ICONES_SUGERIDOS).flat().includes(iconeAtual)
    // Auto-muda ícone se ainda é padrão
    const novoIcone = ehPadrao ? (ICONES_SUGERIDOS[cat]?.[0] ?? '🐾') : iconeAtual
    setForm({ ...form, categoria: cat, icone: novoIcone })
  }

  async function salvar(e) {
    e.preventDefault()
    const payload = {
      nome:      form.nome,
      categoria: form.categoria,
      descricao: form.descricao || null,
      precoBase: +form.precoBase || 0,
      duracaoMin: form.duracaoMin ? +form.duracaoMin : null,
      icone:     form.icone || '🐾'
    }
    try {
      if (form.id) await api.put(`/servicos/${form.id}`, payload)
      else         await api.post('/servicos', payload)
      setForm(null); setShowPicker(false); carregar()
    } catch (err) {
      alert(err.response?.data?.erro || 'Erro ao salvar.')
    }
  }

  async function toggle(s) {
    await api.put(`/servicos/${s.id}/toggle`)
    carregar()
  }

  // Agrupa por categoria
  const porCategoria = servicos.reduce((acc, s) => {
    const cat = s.categoria || 'outro'
    if (!acc[cat]) acc[cat] = []
    acc[cat].push(s)
    return acc
  }, {})

  const totalAtivos = servicos.filter(s => s.ativo).length

  return (
    <div className="max-w-4xl mx-auto space-y-5">

      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-2xl font-bold text-slate-800">Serviços e Valores</h2>
          <p className="text-sm text-slate-400 mt-0.5">{totalAtivos} serviço{totalAtivos !== 1 ? 's' : ''} ativo{totalAtivos !== 1 ? 's' : ''}</p>
        </div>
        <button onClick={novo}
          className="bg-slate-900 text-white px-5 py-2.5 rounded-xl text-sm font-semibold hover:bg-slate-800 shadow-sm">
          + Novo serviço
        </button>
      </div>

      {/* Formulário */}
      {form && (
        <div className="bg-white rounded-2xl shadow-sm border border-slate-100 p-6">
          <h3 className="font-bold text-slate-700 mb-4">{form.id ? '✏️ Editar serviço' : '✨ Novo serviço'}</h3>
          <form onSubmit={salvar} className="space-y-4">

            {/* Ícone + Nome */}
            <div className="flex gap-3 items-start">
              <div className="relative">
                <button type="button"
                  onClick={() => setShowPicker(v => !v)}
                  className="w-16 h-16 rounded-2xl bg-slate-100 hover:bg-slate-200 transition text-4xl flex items-center justify-center border-2 border-dashed border-slate-200 hover:border-slate-400">
                  {form.icone}
                </button>
                {showPicker && (
                  <div className="absolute left-0 top-[72px] z-20 bg-white rounded-2xl shadow-xl border border-slate-100 p-4 w-72">
                    <p className="text-xs font-semibold text-slate-500 mb-2 uppercase tracking-wide">Sugeridos para {CATEGORIAS[form.categoria]}</p>
                    <div className="flex gap-2 flex-wrap mb-3">
                      {(ICONES_SUGERIDOS[form.categoria] || ICONES_SUGERIDOS.outro).map(e => (
                        <button key={e} type="button"
                          onClick={() => { setForm(f => ({...f, icone: e})); setShowPicker(false) }}
                          className={`w-10 h-10 rounded-xl text-2xl flex items-center justify-center hover:bg-slate-100 transition ${form.icone === e ? 'bg-blue-50 ring-2 ring-blue-400' : ''}`}>
                          {e}
                        </button>
                      ))}
                    </div>
                    <p className="text-xs font-semibold text-slate-500 mb-2 uppercase tracking-wide">Animais e outros</p>
                    <div className="flex gap-2 flex-wrap">
                      {ICONES_EXTRAS.map(e => (
                        <button key={e} type="button"
                          onClick={() => { setForm(f => ({...f, icone: e})); setShowPicker(false) }}
                          className={`w-10 h-10 rounded-xl text-2xl flex items-center justify-center hover:bg-slate-100 transition ${form.icone === e ? 'bg-blue-50 ring-2 ring-blue-400' : ''}`}>
                          {e}
                        </button>
                      ))}
                    </div>
                  </div>
                )}
              </div>
              <div className="flex-1">
                <label className="text-xs text-slate-500 block mb-1">Nome do serviço *</label>
                <input className="border rounded-xl px-3 py-2.5 w-full text-sm focus:outline-none focus:ring-2 focus:ring-slate-300"
                  placeholder="Ex: Consulta Clínica, Banho Completo..."
                  required value={form.nome} onChange={e => setForm({...form, nome: e.target.value})} autoFocus />
              </div>
            </div>

            <div className="grid grid-cols-3 gap-3">
              <div>
                <label className="text-xs text-slate-500 block mb-1">Categoria</label>
                <select className="border rounded-xl px-3 py-2.5 w-full text-sm"
                  value={form.categoria} onChange={e => setCategoria(e.target.value)}>
                  {Object.entries(CATEGORIAS).map(([k,v]) => (
                    <option key={k} value={k}>{v}</option>
                  ))}
                </select>
              </div>
              <div>
                <label className="text-xs text-slate-500 block mb-1">Preço base (R$) *</label>
                <input type="number" step="0.01" min="0" className="border rounded-xl px-3 py-2.5 w-full text-sm"
                  placeholder="0,00" required
                  value={form.precoBase} onChange={e => setForm({...form, precoBase: e.target.value})} />
              </div>
              <div>
                <label className="text-xs text-slate-500 block mb-1">Duração (min)</label>
                <select className="border rounded-xl px-3 py-2.5 w-full text-sm"
                  value={form.duracaoMin} onChange={e => setForm({...form, duracaoMin: e.target.value})}>
                  <option value="">Não definida</option>
                  {[15,20,30,45,60,90,120,180,240].map(d => (
                    <option key={d} value={d}>{d} min{d >= 60 ? ` (${Math.floor(d/60)}h${d%60 ? d%60+'min' : ''})` : ''}</option>
                  ))}
                </select>
              </div>
            </div>

            <div className="flex gap-3 pt-1">
              <button type="submit" className="bg-emerald-600 text-white px-6 py-2.5 rounded-xl text-sm font-semibold hover:bg-emerald-700">
                {form.id ? 'Salvar alterações' : 'Criar serviço'}
              </button>
              <button type="button" onClick={() => { setForm(null); setShowPicker(false) }}
                className="border border-slate-200 px-6 py-2.5 rounded-xl text-sm text-slate-600 hover:bg-slate-50">
                Cancelar
              </button>
            </div>
          </form>
        </div>
      )}

      {/* Cards por categoria */}
      {loading ? (
        <div className="text-center py-12 text-slate-400">Carregando...</div>
      ) : servicos.length === 0 ? (
        <div className="bg-white rounded-2xl shadow-sm p-12 text-center border border-slate-100">
          <div className="text-5xl mb-3">🐾</div>
          <p className="text-slate-500 font-medium">Nenhum serviço cadastrado</p>
          <p className="text-slate-400 text-sm mt-1">Crie seus serviços para usar na agenda e no PDV</p>
        </div>
      ) : (
        Object.entries(porCategoria).map(([cat, lista]) => (
          <div key={cat}>
            <div className="flex items-center gap-2 mb-2 px-1">
              <span className="text-xs font-bold text-slate-500 uppercase tracking-wider">
                {CATEGORIAS[cat] || cat}
              </span>
              <div className="flex-1 h-px bg-slate-100" />
              <span className="text-xs text-slate-400">{lista.filter(s => s.ativo).length} ativo{lista.filter(s=>s.ativo).length !== 1 ? 's' : ''}</span>
            </div>
            <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-3">
              {lista.map(s => (
                <div key={s.id}
                  className={`bg-white rounded-2xl border shadow-sm transition hover:shadow-md relative ${!s.ativo ? 'opacity-50' : 'border-slate-100'}`}>

                  {/* Toggle ativo */}
                  <div className="absolute top-3 right-3">
                    <button onClick={() => toggle(s)}
                      className={`w-8 h-4 rounded-full relative transition-colors ${s.ativo ? 'bg-emerald-500' : 'bg-slate-300'}`}>
                      <span className={`absolute top-0.5 w-3 h-3 bg-white rounded-full transition-transform shadow ${s.ativo ? 'translate-x-4' : 'translate-x-0.5'}`} />
                    </button>
                  </div>

                  <div className="p-4">
                    {/* Ícone */}
                    <div className="w-12 h-12 rounded-2xl bg-slate-50 flex items-center justify-center text-3xl mb-3 border border-slate-100">
                      {s.icone || '🐾'}
                    </div>

                    {/* Nome */}
                    <div className="font-semibold text-slate-800 text-sm leading-tight mb-1 pr-8">{s.nome}</div>

                    {/* Duração */}
                    {s.duracaoMin && (
                      <div className="text-xs text-slate-400 mb-2">
                        ⏱ {s.duracaoMin >= 60
                          ? `${Math.floor(s.duracaoMin/60)}h${s.duracaoMin%60 ? s.duracaoMin%60+'min' : ''}`
                          : `${s.duracaoMin}min`}
                      </div>
                    )}

                    {/* Preço */}
                    <div className="text-base font-bold text-emerald-600 mb-3">
                      {fmt(s.precoBase)}
                    </div>

                    {/* Botão editar */}
                    <button onClick={() => editar(s)}
                      className="w-full text-xs text-slate-500 border border-slate-200 py-1.5 rounded-lg hover:bg-slate-50 hover:text-slate-700 transition">
                      ✏️ Editar
                    </button>
                  </div>
                </div>
              ))}
            </div>
          </div>
        ))
      )}
    </div>
  )
}
