import { useEffect, useState } from 'react'
import api from '../../api/client'

export default function Vias() {
  const [vias, setVias] = useState([])
  const [novo, setNovo] = useState('')
  const [erro, setErro] = useState(null)
  const [sucesso, setSucesso] = useState(null)

  function carregar() {
    api.get('/cadastros/vias').then(r => setVias(r.data)).catch(() => {})
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
      await api.post('/cadastros/vias', { nome: novo.trim() })
      setNovo('')
      carregar()
      flash('ok', 'Via cadastrada!')
    } catch (err) {
      flash('erro', err.response?.data?.erro || 'Erro ao salvar.')
    }
  }

  async function deletar(id, nome) {
    if (!confirm(`Remover a via "${nome}"?`)) return
    try {
      await api.delete(`/cadastros/vias/${id}`)
      carregar()
      flash('ok', `"${nome}" removida.`)
    } catch (err) {
      flash('erro', err.response?.data?.erro || 'Não foi possível remover.')
    }
  }

  return (
    <div className="max-w-lg">
      <div className="flex items-center gap-3 mb-6">
        <h2 className="text-2xl font-bold">Vias de Administração</h2>
      </div>

      {(sucesso || erro) && (
        <div className={`px-4 py-3 rounded-lg text-sm font-medium mb-4 ${
          sucesso ? 'bg-emerald-50 text-emerald-700 border border-emerald-200'
                  : 'bg-red-50 text-red-700 border border-red-200'
        }`}>
          {sucesso ? `✅ ${sucesso}` : `❌ ${erro}`}
        </div>
      )}

      {/* Formulário de novo */}
      <form onSubmit={salvar} className="bg-white rounded-2xl shadow p-5 mb-6">
        <p className="text-sm font-semibold text-slate-700 mb-3">+ Nova via</p>
        <div className="flex gap-2">
          <input
            className="border rounded-lg px-3 py-2 flex-1"
            placeholder="ex: Subcutâneo"
            value={novo}
            onChange={e => setNovo(e.target.value)}
          />
          <button
            type="submit"
            disabled={!novo.trim()}
            className="bg-slate-900 text-white px-5 py-2 rounded-lg font-medium disabled:opacity-40">
            Salvar
          </button>
        </div>
      </form>

      {/* Lista */}
      <div className="bg-white rounded-2xl shadow overflow-hidden">
        <table className="w-full text-sm">
          <thead className="bg-slate-50 text-left">
            <tr>
              <th className="p-3 font-semibold text-slate-600">Via de administração</th>
              <th className="p-3 w-16"></th>
            </tr>
          </thead>
          <tbody>
            {vias.map(v => (
              <tr key={v.id} className="border-t hover:bg-slate-50">
                <td className="p-3">{v.nome}</td>
                <td className="p-3 text-right">
                  <button
                    onClick={() => deletar(v.id, v.nome)}
                    className="text-xs text-red-400 hover:text-red-600 hover:underline">
                    Remover
                  </button>
                </td>
              </tr>
            ))}
            {vias.length === 0 && (
              <tr>
                <td colSpan="2" className="p-6 text-center text-slate-400">
                  Nenhuma via cadastrada.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      <p className="text-xs text-slate-400 mt-3">
        💡 Vias que já foram usadas em receituários não podem ser removidas.
      </p>
    </div>
  )
}
