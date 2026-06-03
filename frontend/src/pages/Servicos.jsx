import { useEffect, useState } from 'react'
import api from '../api/client'

const CATEGORIAS = { banho_tosa: 'Banho e Tosa', consulta: 'Consulta', vacina: 'Vacina', cirurgia: 'Cirurgia', exame: 'Exame' }

export default function Servicos() {
  const [servicos, setServicos] = useState([])
  const [form, setForm] = useState(null)

  function carregar() {
    api.get('/servicos').then((r) => setServicos(r.data)).catch(() => {})
  }
  useEffect(() => { carregar() }, [])

  function novo() {
    setForm({ id: null, nome: '', categoria: 'banho_tosa', descricao: '', precoBase: '', duracaoMin: '' })
  }
  function editar(s) {
    setForm({ id: s.id, nome: s.nome, categoria: s.categoria || 'banho_tosa', descricao: '', precoBase: s.precoBase, duracaoMin: s.duracaoMin || '' })
  }
  async function salvar(e) {
    e.preventDefault()
    const payload = { ...form, precoBase: +form.precoBase || 0, duracaoMin: form.duracaoMin ? +form.duracaoMin : null }
    if (form.id) await api.put(`/servicos/${form.id}`, payload)
    else await api.post('/servicos', payload)
    setForm(null); carregar()
  }
  async function toggle(s) {
    await api.put(`/servicos/${s.id}/toggle`)
    carregar()
  }

  return (
    <div>
      <div className="flex justify-between items-center mb-6">
        <h2 className="text-2xl font-bold">Serviços e Valores</h2>
        <button onClick={novo} className="bg-slate-900 text-white px-4 py-2 rounded-lg">+ Novo serviço</button>
      </div>

      {form && (
        <form onSubmit={salvar} className="bg-white p-6 rounded-2xl shadow mb-6 grid grid-cols-2 gap-3">
          <div className="col-span-2 font-semibold text-slate-700">{form.id ? '✏️ Editar' : 'Novo serviço'}</div>
          <input className="border rounded-lg px-3 py-2" placeholder="Nome do serviço" required
            value={form.nome} onChange={(e) => setForm({ ...form, nome: e.target.value })} />
          <select className="border rounded-lg px-3 py-2"
            value={form.categoria} onChange={(e) => setForm({ ...form, categoria: e.target.value })}>
            {Object.entries(CATEGORIAS).map(([k, v]) => <option key={k} value={k}>{v}</option>)}
          </select>
          <input className="border rounded-lg px-3 py-2" placeholder="Preço base (R$)" type="number" step="0.01" required
            value={form.precoBase} onChange={(e) => setForm({ ...form, precoBase: e.target.value })} />
          <input className="border rounded-lg px-3 py-2" placeholder="Duração (min)" type="number"
            value={form.duracaoMin} onChange={(e) => setForm({ ...form, duracaoMin: e.target.value })} />
          <div className="col-span-2 flex gap-2">
            <button className="bg-emerald-600 text-white py-2 px-6 rounded-lg">{form.id ? 'Salvar' : 'Criar'}</button>
            <button type="button" onClick={() => setForm(null)} className="bg-slate-200 py-2 px-6 rounded-lg">Cancelar</button>
          </div>
        </form>
      )}

      <div className="bg-white rounded-2xl shadow overflow-hidden">
        <table className="w-full text-sm">
          <thead className="bg-slate-50 text-left">
            <tr>
              <th className="p-3">Serviço</th><th className="p-3">Categoria</th>
              <th className="p-3 text-right">Preço</th><th className="p-3 text-center">Duração</th>
              <th className="p-3 text-center">Ativo</th><th className="p-3"></th>
            </tr>
          </thead>
          <tbody>
            {servicos.map((s) => (
              <tr key={s.id} className={`border-t hover:bg-slate-50 ${!s.ativo ? 'opacity-40' : ''}`}>
                <td className="p-3 font-medium">{s.nome}</td>
                <td className="p-3 text-xs">{CATEGORIAS[s.categoria] || s.categoria || '—'}</td>
                <td className="p-3 text-right">R$ {s.precoBase.toFixed(2)}</td>
                <td className="p-3 text-center">{s.duracaoMin ? `${s.duracaoMin} min` : '—'}</td>
                <td className="p-3 text-center">
                  <button onClick={() => toggle(s)}
                    className={`w-9 h-5 rounded-full relative transition ${s.ativo ? 'bg-emerald-500' : 'bg-slate-300'}`}>
                    <span className={`absolute top-0.5 w-4 h-4 bg-white rounded-full transition ${s.ativo ? 'left-4' : 'left-0.5'}`}></span>
                  </button>
                </td>
                <td className="p-3 text-right">
                  <button onClick={() => editar(s)} className="text-xs text-blue-600 hover:underline">Editar</button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  )
}
