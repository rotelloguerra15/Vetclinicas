import { useEffect, useState } from 'react'
import api from '../../api/client'

const ESPECIES = [
  { value: 'cao',    label: 'Cão' },
  { value: 'gato',   label: 'Gato' },
  { value: 'ave',    label: 'Ave' },
  { value: 'roedor', label: 'Roedor' },
  { value: 'reptil', label: 'Réptil' },
  { value: 'outro',  label: 'Outro' },
]

export default function Racas() {
  const [racas, setRacas]       = useState([])
  const [filtroEsp, setFiltroEsp] = useState('cao')
  const [novo, setNovo]         = useState('')
  const [novaEsp, setNovaEsp]   = useState('cao')
  const [erro, setErro]         = useState(null)
  const [sucesso, setSucesso]   = useState(null)

  function carregar() {
    api.get('/cadastros/racas').then(r => setRacas(r.data)).catch(() => {})
  }
  useEffect(() => { carregar() }, [])

  function flash(tipo, msg) {
    if (tipo === 'ok') { setSucesso(msg); setErro(null) }
    else { setErro(msg); setSucesso(null) }
    setTimeout(() => { setSucesso(null); setErro(null) }, 3000)
  }

  async function salvar(e) {
    e.preventDefault()
    if (!novo.trim()) return
    try {
      await api.post('/cadastros/racas', { nome: novo.trim(), especie: novaEsp })
      setNovo('')
      carregar()
      flash('ok', 'Raça cadastrada!')
    } catch (err) {
      flash('erro', err.response?.data?.erro || 'Erro ao salvar.')
    }
  }

  async function deletar(id, nome) {
    if (!confirm(`Remover a raça "${nome}"?`)) return
    try {
      await api.delete(`/cadastros/racas/${id}`)
      carregar()
      flash('ok', `"${nome}" removida.`)
    } catch (err) {
      flash('erro', err.response?.data?.erro || 'Não foi possível remover.')
    }
  }

  const racasFiltradas = racas.filter(r => r.especie === filtroEsp)

  return (
    <div className="max-w-lg">
      <div className="flex items-center gap-3 mb-6">
        <h2 className="text-2xl font-bold">Raças</h2>
      </div>

      {(sucesso || erro) && (
        <div className={`px-4 py-3 rounded-lg text-sm font-medium mb-4 ${
          sucesso ? 'bg-emerald-50 text-emerald-700 border border-emerald-200'
                  : 'bg-red-50 text-red-700 border border-red-200'
        }`}>
          {sucesso ? `✅ ${sucesso}` : `❌ ${erro}`}
        </div>
      )}

      {/* Form nova raça */}
      <form onSubmit={salvar} className="bg-white rounded-2xl shadow p-5 mb-6">
        <p className="text-sm font-semibold text-slate-700 mb-3">+ Nova raça</p>
        <div className="flex gap-2">
          <select
            className="border rounded-lg px-3 py-2 text-sm"
            value={novaEsp}
            onChange={e => setNovaEsp(e.target.value)}>
            {ESPECIES.map(e => <option key={e.value} value={e.value}>{e.label}</option>)}
          </select>
          <input
            className="border rounded-lg px-3 py-2 flex-1 text-sm"
            placeholder="Ex: Labrador Retriever..."
            value={novo}
            onChange={e => setNovo(e.target.value)}
          />
          <button
            type="submit"
            disabled={!novo.trim()}
            className="bg-slate-900 text-white px-5 py-2 rounded-lg font-medium text-sm disabled:opacity-40">
            Salvar
          </button>
        </div>
      </form>

      {/* Filtro por espécie */}
      <div className="flex gap-1 flex-wrap mb-4">
        {ESPECIES.map(e => (
          <button key={e.value}
            onClick={() => setFiltroEsp(e.value)}
            className={`px-3 py-1.5 rounded-full text-xs font-medium transition-colors ${
              filtroEsp === e.value
                ? 'bg-slate-900 text-white'
                : 'bg-white border border-slate-200 text-slate-600 hover:bg-slate-50'
            }`}>
            {e.label} ({racas.filter(r => r.especie === e.value).length})
          </button>
        ))}
      </div>

      {/* Tabela */}
      <div className="bg-white rounded-2xl shadow overflow-hidden">
        <table className="w-full text-sm">
          <thead className="bg-slate-50 text-left">
            <tr>
              <th className="p-3 font-semibold text-slate-600">Raça</th>
              <th className="p-3 w-16"></th>
            </tr>
          </thead>
          <tbody>
            {racasFiltradas.map(r => (
              <tr key={r.id} className="border-t hover:bg-slate-50">
                <td className="p-3">{r.nome}</td>
                <td className="p-3 text-right">
                  <button
                    onClick={() => deletar(r.id, r.nome)}
                    className="text-xs text-red-400 hover:text-red-600 hover:underline">
                    Remover
                  </button>
                </td>
              </tr>
            ))}
            {racasFiltradas.length === 0 && (
              <tr>
                <td colSpan="2" className="p-6 text-center text-slate-400">
                  Nenhuma raça cadastrada para esta espécie.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      <p className="text-xs text-slate-400 mt-3">
        As raças cadastradas aparecem como opção ao cadastrar um pet (filtradas pela espécie selecionada).
      </p>
    </div>
  )
}
