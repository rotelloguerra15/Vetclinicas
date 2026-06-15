import { useEffect, useState } from 'react'
import api from '../api/client'

const PAPEIS = {
  owner:        { label: 'Dono',                  cor: 'bg-purple-100 text-purple-700' },
  admin:        { label: 'Administrador',          cor: 'bg-blue-100 text-blue-700' },
  veterinario:  { label: 'Veterinário(a)',         cor: 'bg-emerald-100 text-emerald-700' },
  tosador:      { label: 'Tosador(a) / Banhista',  cor: 'bg-amber-100 text-amber-700' },
  recepcionista:{ label: 'Recepcionista',          cor: 'bg-slate-100 text-slate-600' },
}

// Permissões por papel — centralizado aqui para ficar visível
const PERMISSOES = {
  owner:        ['Acesso total', 'Reabrir caixa', 'Gerenciar usuários', 'Parâmetros'],
  admin:        ['Acesso total', 'Reabrir caixa', 'Gerenciar usuários'],
  veterinario:  ['Agenda', 'Atendimentos', 'Receituário', 'Prontuário'],
  tosador:      ['Agenda', 'Atendimentos', 'PDV'],
  recepcionista:['Agenda', 'PDV', 'Cadastros'],
}

export default function Usuarios() {
  const [users, setUsers]         = useState([])
  const [form, setForm]           = useState(null)
  const [mostrarPerm, setMostrarPerm] = useState(false)
  const papel     = localStorage.getItem('papel')
  const podeEditar = papel === 'owner' || papel === 'admin'

  function carregar() {
    api.get('/usuarios').then(r => setUsers(r.data)).catch(() => {})
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
        <div className="flex gap-2">
          <button onClick={() => setMostrarPerm(v => !v)}
            className="text-sm text-slate-500 border border-slate-200 px-3 py-2 rounded-lg hover:bg-slate-50">
            {mostrarPerm ? 'Ocultar permissões' : 'Ver permissões por papel'}
          </button>
          {podeEditar && (
            <button onClick={novo} className="bg-slate-900 text-white px-4 py-2 rounded-lg text-sm">
              + Novo profissional
            </button>
          )}
        </div>
      </div>

      {/* Tabela de permissões por papel */}
      {mostrarPerm && (
        <div className="bg-white rounded-2xl shadow p-5 mb-6">
          <h3 className="font-semibold text-slate-700 mb-4">Permissões por Papel</h3>
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="text-left border-b border-slate-100">
                  <th className="pb-2 font-medium text-slate-600 pr-4">Papel</th>
                  <th className="pb-2 font-medium text-slate-600">Permissões</th>
                </tr>
              </thead>
              <tbody>
                {Object.entries(PAPEIS).map(([k, v]) => (
                  <tr key={k} className="border-b border-slate-50 hover:bg-slate-50">
                    <td className="py-3 pr-4">
                      <span className={`text-xs px-2 py-1 rounded-full font-medium ${v.cor}`}>
                        {v.label}
                      </span>
                    </td>
                    <td className="py-3">
                      <div className="flex flex-wrap gap-1">
                        {PERMISSOES[k].map(perm => (
                          <span key={perm}
                            className={`text-xs px-2 py-0.5 rounded-full border ${
                              perm === 'Reabrir caixa'
                                ? 'bg-amber-50 text-amber-700 border-amber-200 font-medium'
                                : perm === 'Acesso total'
                                ? 'bg-purple-50 text-purple-700 border-purple-200'
                                : 'bg-slate-50 text-slate-600 border-slate-200'
                            }`}>
                            {perm === 'Reabrir caixa' ? '🔓 ' : ''}{perm}
                          </span>
                        ))}
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <p className="text-xs text-slate-400 mt-3">
            Apenas <strong>Dono</strong> e <strong>Administrador</strong> podem reabrir o caixa após fechamento.
          </p>
        </div>
      )}

      {/* Form novo/editar */}
      {form && (
        <form onSubmit={salvar} className="bg-white p-6 rounded-2xl shadow mb-6 grid grid-cols-2 gap-3">
          <div className="col-span-2 font-semibold text-slate-700">
            {form.id ? '✏️ Editar profissional' : '👤 Novo profissional'}
          </div>
          <input className="border rounded-lg px-3 py-2" placeholder="Nome completo" required
            value={form.nome} onChange={e => setForm({ ...form, nome: e.target.value })} />
          <input className="border rounded-lg px-3 py-2" placeholder="Email (login)" type="email" required
            value={form.email} onChange={e => setForm({ ...form, email: e.target.value })} />
          <div className="col-span-2">
            <label className="text-xs text-slate-500 block mb-1">Papel</label>
            <select className="border rounded-lg px-3 py-2 w-full"
              value={form.papel} onChange={e => setForm({ ...form, papel: e.target.value })}>
              {Object.entries(PAPEIS).filter(([k]) => k !== 'owner').map(([k, v]) => (
                <option key={k} value={k}>{v.label}</option>
              ))}
            </select>
            {/* Preview de permissões do papel selecionado */}
            <div className="flex flex-wrap gap-1 mt-2">
              {PERMISSOES[form.papel]?.map(perm => (
                <span key={perm}
                  className={`text-xs px-2 py-0.5 rounded-full border ${
                    perm === 'Reabrir caixa'
                      ? 'bg-amber-50 text-amber-700 border-amber-200'
                      : 'bg-slate-50 text-slate-500 border-slate-200'
                  }`}>
                  {perm === 'Reabrir caixa' ? '🔓 ' : ''}{perm}
                </span>
              ))}
            </div>
          </div>
          {!form.id && (
            <input className="border rounded-lg px-3 py-2 col-span-2" placeholder="Senha inicial" type="password" required
              value={form.senha} onChange={e => setForm({ ...form, senha: e.target.value })} />
          )}
          <div className="col-span-2 flex gap-2">
            <button className="bg-emerald-600 text-white py-2 px-6 rounded-lg">
              {form.id ? 'Salvar' : 'Criar'}
            </button>
            <button type="button" onClick={() => setForm(null)} className="bg-slate-200 py-2 px-6 rounded-lg">
              Cancelar
            </button>
          </div>
        </form>
      )}

      {/* Lista de usuários */}
      <div className="grid gap-3">
        {users.map(u => {
          const p = PAPEIS[u.papel] || PAPEIS.recepcionista
          const podeReopenCaixa = u.papel === 'owner' || u.papel === 'admin'
          return (
            <div key={u.id}
              className={`bg-white rounded-2xl shadow p-4 flex items-center justify-between ${!u.ativo ? 'opacity-40' : ''}`}>
              <div className="flex items-center gap-3">
                <div className="w-10 h-10 bg-slate-200 rounded-full flex items-center justify-center font-bold text-slate-500">
                  {u.nome.charAt(0)}
                </div>
                <div>
                  <div className="font-medium">{u.nome}</div>
                  <div className="text-xs text-slate-400">{u.email}</div>
                  {podeReopenCaixa && (
                    <div className="text-xs text-amber-600 mt-0.5">🔓 Pode reabrir caixa</div>
                  )}
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
