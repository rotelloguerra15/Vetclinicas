import { useEffect, useState } from 'react'
import api from '../../api/client'

export default function Cargos() {
  const [cargos, setCargos]         = useState([])
  const [mostrarForm, setMostrarForm] = useState(false)
  const [editId, setEditId]         = useState(null)
  const [form, setForm]             = useState({ nome: '', podeReceituario: false })
  const [salvando, setSalvando]     = useState(false)

  function carregar() {
    api.get('/cargos').then(r => setCargos(r.data)).catch(() => {})
  }
  useEffect(() => { carregar() }, [])

  function abrirNovo() {
    setEditId(null)
    setForm({ nome: '', podeReceituario: false })
    setMostrarForm(true)
  }
  function abrirEditar(c) {
    setEditId(c.id)
    setForm({ nome: c.nome, podeReceituario: c.podeReceituario })
    setMostrarForm(true)
  }
  function cancelar() {
    setMostrarForm(false); setEditId(null)
    setForm({ nome: '', podeReceituario: false })
  }

  async function salvar(e) {
    e.preventDefault()
    if (!form.nome.trim()) return alert('Informe o nome do cargo.')
    setSalvando(true)
    try {
      if (editId) await api.put(`/cargos/${editId}`, form)
      else        await api.post('/cargos', form)
      cancelar(); carregar()
    } catch (err) {
      alert(err.response?.data?.erro || 'Erro ao salvar.')
    } finally { setSalvando(false) }
  }

  async function remover(c) {
    if (!confirm(`Remover o cargo "${c.nome}"?`)) return
    try {
      await api.delete(`/cargos/${c.id}`)
      carregar()
    } catch (err) {
      alert(err.response?.data?.erro || 'Erro ao remover.')
    }
  }

  return (
    <div className="max-w-xl mx-auto space-y-5">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-2xl font-bold">Cargos</h2>
          <p className="text-sm text-slate-400 mt-0.5">Defina quais cargos podem assinar receituário</p>
        </div>
        <button onClick={abrirNovo}
          className="bg-slate-900 text-white px-4 py-2 rounded-lg text-sm font-medium hover:bg-slate-800">
          + Novo cargo
        </button>
      </div>

      {mostrarForm && (
        <form onSubmit={salvar} className="bg-white rounded-2xl shadow p-5 space-y-4 border border-slate-100">
          <p className="font-semibold text-slate-700">{editId ? 'Editar cargo' : 'Novo cargo'}</p>

          <div>
            <label className="text-xs text-slate-500 block mb-1">Nome do cargo *</label>
            <input
              className="border rounded-xl px-3 py-2.5 w-full text-sm focus:outline-none focus:ring-2 focus:ring-slate-300"
              placeholder="Ex: Médico Veterinário, Recepcionista, Tosador..."
              value={form.nome}
              onChange={e => setForm(f => ({ ...f, nome: e.target.value }))}
              autoFocus
            />
          </div>

          {/* Toggle assina receituário */}
          <div className="flex items-center gap-3 p-4 bg-slate-50 rounded-xl border border-slate-100">
            <button
              type="button"
              onClick={() => setForm(f => ({ ...f, podeReceituario: !f.podeReceituario }))}
              className={`relative inline-flex h-6 w-11 flex-shrink-0 items-center rounded-full transition-colors ${form.podeReceituario ? 'bg-emerald-500' : 'bg-slate-300'}`}
            >
              <span className={`inline-block h-4 w-4 transform rounded-full bg-white shadow transition-transform ${form.podeReceituario ? 'translate-x-6' : 'translate-x-1'}`} />
            </button>
            <div>
              <p className="text-sm font-semibold text-slate-700">Pode assinar receituário</p>
              <p className="text-xs text-slate-400">
                Funcionários com este cargo aparecem como veterinário responsável no PDF
              </p>
            </div>
          </div>

          <div className="flex gap-2 pt-1">
            <button type="submit" disabled={salvando}
              className="bg-emerald-600 text-white px-5 py-2 rounded-lg text-sm font-medium disabled:opacity-50 hover:bg-emerald-700">
              {salvando ? 'Salvando...' : 'Salvar'}
            </button>
            <button type="button" onClick={cancelar}
              className="border px-5 py-2 rounded-lg text-sm text-slate-600 hover:bg-slate-50">
              Cancelar
            </button>
          </div>
        </form>
      )}

      {/* Lista */}
      <div className="bg-white rounded-2xl shadow divide-y divide-slate-100 overflow-hidden">
        {cargos.length === 0 ? (
          <div className="p-8 text-center text-slate-400">
            <div className="text-3xl mb-2">💼</div>
            <p className="text-sm">Nenhum cargo cadastrado.</p>
            <p className="text-xs mt-1">Crie cargos para organizar sua equipe.</p>
          </div>
        ) : cargos.map(c => (
          <div key={c.id} className="flex items-center justify-between px-5 py-4 hover:bg-slate-50">
            <div className="flex items-center gap-3">
              <div className={`w-2 h-2 rounded-full ${c.podeReceituario ? 'bg-emerald-500' : 'bg-slate-300'}`} />
              <div>
                <p className="text-sm font-semibold text-slate-800">{c.nome}</p>
                {c.podeReceituario && (
                  <p className="text-xs text-emerald-600 font-medium">✓ Assina receituário</p>
                )}
              </div>
            </div>
            <div className="flex gap-2">
              <button onClick={() => abrirEditar(c)}
                className="text-xs text-blue-600 hover:underline px-2">Editar</button>
              <button onClick={() => remover(c)}
                className="text-xs text-red-400 hover:underline px-2">Remover</button>
            </div>
          </div>
        ))}
      </div>

      <div className="bg-blue-50 border border-blue-200 rounded-xl p-3 text-xs text-blue-700 space-y-1">
        <p>💡 <strong>Exemplos de cargos comuns:</strong></p>
        <p>• Médico Veterinário → Assina receituário ✓</p>
        <p>• Técnico em Veterinária → Assina receituário ✓ (conforme legislação local)</p>
        <p>• Tosador / Banhista → Não assina ✗</p>
        <p>• Recepcionista → Não assina ✗</p>
      </div>
    </div>
  )
}
