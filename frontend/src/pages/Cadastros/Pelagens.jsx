import { useEffect, useState } from 'react'
import api from '../../api/client'

export default function Pelagens() {
  const [pelagens, setPelagens] = useState([])
  const [novo, setNovo]         = useState('')
  const [erro, setErro]         = useState(null)
  const [sucesso, setSucesso]   = useState(null)

  function carregar() {
    api.get('/cadastros/pelagens').then(r => setPelagens(r.data)).catch(() => {})
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
      await api.post('/cadastros/pelagens', { nome: novo.trim() })
      setNovo('')
      carregar()
      flash('ok', 'Pelagem cadastrada!')
    } catch (err) {
      flash('erro', err.response?.data?.erro || 'Erro ao salvar.')
    }
  }

  async function deletar(id, nome) {
    if (!confirm(`Remover a pelagem "${nome}"?`)) return
    try {
      await api.delete(`/cadastros/pelagens/${id}`)
      carregar()
      flash('ok', `"${nome}" removida.`)
    } catch (err) {
      flash('erro', err.response?.data?.erro || 'Nao foi possivel remover.')
    }
  }

  return (
    <div className="max-w-lg">
      <div className="flex items-center gap-3 mb-6">
        <h2 className="text-2xl font-bold">Pelagens</h2>
      </div>

      {(sucesso || erro) && (
        <div className={`px-4 py-3 rounded-lg text-sm font-medium mb-4 ${
          sucesso ? 'bg-emerald-50 text-emerald-700 border border-emerald-200'
                  : 'bg-red-50 text-red-700 border border-red-200'
        }`}>
          {sucesso ? `✅ ${sucesso}` : `❌ ${erro}`}
        </div>
      )}

      <form onSubmit={salvar} className="bg-white rounded-2xl shadow p-5 mb-6">
        <p className="text-sm font-semibold text-slate-700 mb-3">+ Nova pelagem</p>
        <div className="flex gap-2">
          <input
            className="border rounded-lg px-3 py-2 flex-1"
            placeholder="ex: Bicolor, Tigrada, Longa..."
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

      <div className="bg-white rounded-2xl shadow overflow-hidden">
        <table className="w-full text-sm">
          <thead className="bg-slate-50 text-left">
            <tr>
              <th className="p-3 font-semibold text-slate-600">Pelagem</th>
              <th className="p-3 w-16"></th>
            </tr>
          </thead>
          <tbody>
            {pelagens.map(p => (
              <tr key={p.id} className="border-t hover:bg-slate-50">
                <td className="p-3">{p.nome}</td>
                <td className="p-3 text-right">
                  <button
                    onClick={() => deletar(p.id, p.nome)}
                    className="text-xs text-red-400 hover:text-red-600 hover:underline">
                    Remover
                  </button>
                </td>
              </tr>
            ))}
            {pelagens.length === 0 && (
              <tr>
                <td colSpan="2" className="p-6 text-center text-slate-400">
                  Nenhuma pelagem cadastrada.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      <p className="text-xs text-slate-400 mt-3">
        As pelagens cadastradas aqui aparecem como opcao ao cadastrar um pet.
      </p>
    </div>
  )
}
