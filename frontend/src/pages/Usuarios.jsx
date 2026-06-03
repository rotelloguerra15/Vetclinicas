import { useEffect, useState } from 'react'
import api from '../api/client'

const PAPEIS = {
  owner: { label: 'Dono', cor: 'bg-purple-100 text-purple-700' },
  admin: { label: 'Administrador', cor: 'bg-blue-100 text-blue-700' },
  veterinario: { label: 'Veterinário(a)', cor: 'bg-emerald-100 text-emerald-700' },
  tosador: { label: 'Tosador(a) / Banhista', cor: 'bg-amber-100 text-amber-700' },
  recepcionista: { label: 'Recepcionista', cor: 'bg-slate-100 text-slate-600' }
}

export default function Usuarios() {
  const [users, setUsers] = useState([])
  const [form, setForm] = useState(null)
  const papel = localStorage.getItem('papel')
  const podeEditar = papel === 'owner' || papel === 'admin'

  function carregar() {
    api.get('/usuarios').then((r) => setUsers(r.data)).catch(() => {})
  }
  useEffect(() => { carregar() }, [])

  function novo() {
    setForm({ id: null, nome: '', email: '', papel: 'recepcionista', senha: '' })
  }
  function editar(u) {
    setForm({ id: u.id, nome: u.nome, email: u.email, papel: u.papel, senha: '' })
  }
  async function salvar(e) {
    e.preventDefault()
    if (form.id) {
      await api.put(`/usuarios/${form.id}`, { nome: form.nome, email: form.email, papel: form.papel })
    } else {
      if (!form.senha) return alert('Defina uma senha para o novo usuário')
      await api.post('/usuarios', form)
    }
    setForm(null); carregar()
  }
  async function toggleAtivo(u) {
    await api.put(`/usuarios/${u.id}/desativar`)
    carregar()
  }

  return (
    <div>
      <div className="flex justify-between items-center mb-6">
        <h2 className="text-2xl font-bold">Equipe</h2>
        {podeEditar && (
          <button onClick={novo} className="bg-slate-900 text-white px-4 py-2 rounded-lg">+ Novo profissional</button>
        )}
      </div>

      {form && (
        <form onSubmit={salvar} className="bg-white p-6 rounded-2xl shadow mb-6 grid grid-cols-2 gap-3">
          <div className="col-span-2 font-semibold text-slate-700">{form.id ? '✏️ Editar' : 'Novo profissional'}</div>
          <input className="border rounded-lg px-3 py-2" placeholder="Nome completo" required
            value={form.nome} onChange={(e) => setForm({ ...form, nome: e.target.value })} />
          <input className="border rounded-lg px-3 py-2" placeholder="Email (login)" type="email" required
            value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} />
          <select className="border rounded-lg px-3 py-2"
            value={form.papel} onChange={(e) => setForm({ ...form, papel: e.target.value })}>
            {Object.entries(PAPEIS).filter(([k]) => k !== 'owner').map(([k, v]) => (
              <option key={k} value={k}>{v.label}</option>
            ))}
          </select>
          {!form.id && (
            <input className="border rounded-lg px-3 py-2" placeholder="Senha inicial" type="password" required
              value={form.senha} onChange={(e) => setForm({ ...form, senha: e.target.value })} />
          )}
          <div className="col-span-2 flex gap-2">
            <button className="bg-emerald-600 text-white py-2 px-6 rounded-lg">{form.id ? 'Salvar' : 'Criar'}</button>
            <button type="button" onClick={() => setForm(null)} className="bg-slate-200 py-2 px-6 rounded-lg">Cancelar</button>
          </div>
        </form>
      )}

      <div className="grid gap-3">
        {users.map((u) => {
          const p = PAPEIS[u.papel] || PAPEIS.recepcionista
          return (
            <div key={u.id} className={`bg-white rounded-2xl shadow p-4 flex items-center justify-between ${!u.ativo ? 'opacity-40' : ''}`}>
              <div className="flex items-center gap-3">
                <div className="w-10 h-10 bg-slate-200 rounded-full flex items-center justify-center font-bold text-slate-500">
                  {u.nome.charAt(0)}
                </div>
                <div>
                  <div className="font-medium">{u.nome}</div>
                  <div className="text-xs text-slate-400">{u.email}</div>
                </div>
              </div>
              <div className="flex items-center gap-3">
                <span className={`text-xs px-2 py-1 rounded-full ${p.cor}`}>{p.label}</span>
                {podeEditar && u.papel !== 'owner' && (
                  <div className="flex gap-2">
                    <button onClick={() => editar(u)} className="text-xs text-blue-600 hover:underline">Editar</button>
                    <button onClick={() => toggleAtivo(u)} className="text-xs text-slate-400 hover:underline">
                      {u.ativo ? 'Desativar' : 'Ativar'}
                    </button>
                  </div>
                )}
              </div>
            </div>
          )
        })}
      </div>
    </div>
  )
}
