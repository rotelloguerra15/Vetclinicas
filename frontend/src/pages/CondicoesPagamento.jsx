import { useEffect, useState } from 'react'
import api from '../api/client'

export default function CondicoesPagamento() {
  const [condicoes, setCondicoes] = useState([])
  const [form, setForm] = useState({ nome: '', parcelas: 1, intervaloDias: 30 })
  const [mostrarForm, setMostrarForm] = useState(false)

  function carregar() {
    api.get('/compras/condicoes').then(r => setCondicoes(r.data)).catch(() => {})
  }

  useEffect(() => { carregar() }, [])

  async function salvar(e) {
    e.preventDefault()
    await api.post('/compras/condicoes', {
      nome: form.nome,
      parcelas: parseInt(form.parcelas),
      intervaloDias: parseInt(form.intervaloDias)
    })
    setForm({ nome: '', parcelas: 1, intervaloDias: 30 })
    setMostrarForm(false)
    carregar()
  }

  async function remover(id) {
    if (!confirm('Remover esta condicao?')) return
    await api.delete(`/compras/condicoes/${id}`)
    carregar()
  }

  return (
    <div className="max-w-xl">
      <div className="flex justify-between items-center mb-6">
        <h2 className="text-2xl font-bold">Condicoes de Pagamento</h2>
        <button onClick={() => setMostrarForm(!mostrarForm)}
          className="bg-slate-900 text-white px-4 py-2 rounded-lg text-sm">
          {mostrarForm ? 'Cancelar' : '+ Nova condicao'}
        </button>
      </div>

      {mostrarForm && (
        <form onSubmit={salvar} className="bg-white rounded-2xl shadow p-5 mb-6 space-y-3">
          <p className="font-semibold text-slate-700">Nova condicao de pagamento</p>
          <input className="border rounded-lg px-3 py-2 w-full" placeholder="Nome (ex: 30/60/90)" required
            value={form.nome} onChange={e => setForm({ ...form, nome: e.target.value })} />
          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="text-xs text-slate-500 block mb-1">Numero de parcelas</label>
              <input type="number" min="1" max="60" className="border rounded-lg px-3 py-2 w-full"
                value={form.parcelas} onChange={e => setForm({ ...form, parcelas: e.target.value })} />
            </div>
            <div>
              <label className="text-xs text-slate-500 block mb-1">Intervalo entre parcelas (dias)</label>
              <input type="number" min="1" max="365" className="border rounded-lg px-3 py-2 w-full"
                value={form.intervaloDias} onChange={e => setForm({ ...form, intervaloDias: e.target.value })} />
            </div>
          </div>
          <p className="text-xs text-slate-400">
            Ex: 3 parcelas com intervalo de 30 dias = vence em 30, 60 e 90 dias.
          </p>
          <button type="submit" className="bg-emerald-600 text-white py-2 rounded-lg w-full font-medium">
            Salvar
          </button>
        </form>
      )}

      <div className="bg-white rounded-2xl shadow overflow-hidden">
        <table className="w-full text-sm">
          <thead className="bg-slate-50 text-left">
            <tr>
              <th className="p-3">Nome</th>
              <th className="p-3 text-center">Parcelas</th>
              <th className="p-3 text-center">Intervalo</th>
              <th className="p-3"></th>
            </tr>
          </thead>
          <tbody>
            {condicoes.map(c => (
              <tr key={c.id} className="border-t hover:bg-slate-50">
                <td className="p-3 font-medium">{c.nome}</td>
                <td className="p-3 text-center">{c.parcelas}x</td>
                <td className="p-3 text-center text-slate-500">{c.intervaloDias} dias</td>
                <td className="p-3 text-right">
                  <button onClick={() => remover(c.id)}
                    className="text-xs text-red-400 hover:underline">Remover</button>
                </td>
              </tr>
            ))}
            {condicoes.length === 0 && (
              <tr><td colSpan="4" className="p-6 text-center text-slate-400">Nenhuma condicao cadastrada</td></tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  )
}
