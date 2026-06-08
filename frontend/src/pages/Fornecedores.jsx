import { useEffect, useState } from 'react'
import api from '../api/client'

const FORM_VAZIO = {
  nome: '', cnpj: '', telefone: '', email: '', contato: '',
  logradouro: '', numEnd: '', bairro: '', cidade: '', estado: '', cep: '', obs: ''
}

const ESTADOS = ['AC','AL','AP','AM','BA','CE','DF','ES','GO','MA','MT','MS','MG',
  'PA','PB','PR','PE','PI','RJ','RN','RS','RO','RR','SC','SP','SE','TO']

export default function Fornecedores() {
  const [fornecedores, setFornecedores] = useState([])
  const [total, setTotal] = useState(0)
  const [page, setPage] = useState(1)
  const [busca, setBusca] = useState('')
  const [form, setForm] = useState(FORM_VAZIO)
  const [editId, setEditId] = useState(null)
  const [mostrarForm, setMostrarForm] = useState(false)

  function carregar() {
    api.get('/fornecedores', { params: { busca, page, pageSize: 20 } })
      .then(r => { setFornecedores(r.data.items); setTotal(r.data.total) })
      .catch(() => {})
  }

  useEffect(() => { carregar() }, [page])

  async function editar(id) {
    const { data } = await api.get(`/fornecedores/${id}`)
    setForm({
      nome: data.nome || '', cnpj: data.cnpj || '', telefone: data.telefone || '',
      email: data.email || '', contato: data.contato || '',
      logradouro: data.logradouro || '', numEnd: data.numEnd || '',
      bairro: data.bairro || '', cidade: data.cidade || '',
      estado: data.estado || '', cep: data.cep || '', obs: data.obs || ''
    })
    setEditId(id)
    setMostrarForm(true)
  }

  function cancelar() {
    setMostrarForm(false); setEditId(null); setForm(FORM_VAZIO)
  }

  async function salvar(e) {
    e.preventDefault()
    if (editId) await api.put(`/fornecedores/${editId}`, form)
    else await api.post('/fornecedores', form)
    cancelar(); carregar()
  }

  async function remover(id) {
    if (!confirm('Remover fornecedor?')) return
    try {
      await api.delete(`/fornecedores/${id}`)
      carregar()
    } catch (err) {
      alert(err.response?.data?.erro || 'Erro ao remover.')
    }
  }

  const f = field => ({
    value: form[field],
    onChange: e => setForm({ ...form, [field]: e.target.value })
  })

  return (
    <div>
      <div className="flex justify-between items-center mb-6">
        <h2 className="text-2xl font-bold">Fornecedores</h2>
        <button onClick={mostrarForm ? cancelar : () => setMostrarForm(true)}
          className="bg-slate-900 text-white px-4 py-2 rounded-lg text-sm">
          {mostrarForm ? 'Cancelar' : '+ Novo fornecedor'}
        </button>
      </div>

      {mostrarForm && (
        <form onSubmit={salvar} className="bg-white rounded-2xl shadow p-6 mb-6 space-y-4">
          <p className="font-semibold text-slate-700 border-b pb-2">
            {editId ? 'Editar fornecedor' : 'Novo fornecedor'}
          </p>

          <div className="grid grid-cols-2 gap-3">
            <input className="border rounded-lg px-3 py-2 col-span-2" placeholder="Nome *" required {...f('nome')} />
            <input className="border rounded-lg px-3 py-2" placeholder="CNPJ" {...f('cnpj')} />
            <input className="border rounded-lg px-3 py-2" placeholder="Nome do contato" {...f('contato')} />
            <input className="border rounded-lg px-3 py-2" placeholder="Telefone" {...f('telefone')} />
            <input className="border rounded-lg px-3 py-2" placeholder="Email" {...f('email')} />
          </div>

          <p className="text-xs font-semibold text-slate-500 border-b pb-1 pt-1">Endereco</p>
          <div className="grid grid-cols-3 gap-3">
            <input className="border rounded-lg px-3 py-2 col-span-2" placeholder="Logradouro" {...f('logradouro')} />
            <input className="border rounded-lg px-3 py-2" placeholder="Numero" {...f('numEnd')} />
            <input className="border rounded-lg px-3 py-2" placeholder="Bairro" {...f('bairro')} />
            <input className="border rounded-lg px-3 py-2" placeholder="CEP" {...f('cep')} />
            <input className="border rounded-lg px-3 py-2 col-span-2" placeholder="Cidade" {...f('cidade')} />
            <select className="border rounded-lg px-3 py-2" {...f('estado')}>
              <option value="">Estado</option>
              {ESTADOS.map(uf => <option key={uf} value={uf}>{uf}</option>)}
            </select>
          </div>

          <textarea className="border rounded-lg px-3 py-2 w-full text-sm" rows={2}
            placeholder="Observacoes" {...f('obs')} />

          <button className="bg-emerald-600 text-white py-2 rounded-lg w-full font-medium">
            {editId ? 'Salvar alteracoes' : 'Cadastrar fornecedor'}
          </button>
        </form>
      )}

      <div className="flex gap-2 mb-4">
        <input className="border rounded-lg px-3 py-2 flex-1" placeholder="Buscar por nome ou CNPJ..."
          value={busca} onChange={e => setBusca(e.target.value)}
          onKeyDown={e => e.key === 'Enter' && carregar()} />
        <button onClick={carregar} className="bg-slate-200 px-4 rounded-lg hover:bg-slate-300">Buscar</button>
      </div>

      <div className="bg-white rounded-2xl shadow overflow-hidden">
        <table className="w-full text-sm">
          <thead className="bg-slate-50 text-left">
            <tr>
              <th className="p-3">Nome</th>
              <th className="p-3">CNPJ</th>
              <th className="p-3">Contato</th>
              <th className="p-3">Telefone</th>
              <th className="p-3">Cidade</th>
              <th className="p-3"></th>
            </tr>
          </thead>
          <tbody>
            {fornecedores.map(f => (
              <tr key={f.id} className="border-t hover:bg-slate-50">
                <td className="p-3 font-medium">{f.nome}</td>
                <td className="p-3 text-slate-500">{f.cnpj || '—'}</td>
                <td className="p-3 text-slate-500">{f.contato || '—'}</td>
                <td className="p-3">{f.telefone || '—'}</td>
                <td className="p-3 text-slate-500">{f.cidade ? `${f.cidade}/${f.estado}` : '—'}</td>
                <td className="p-3 text-right space-x-3">
                  <button onClick={() => editar(f.id)} className="text-xs text-blue-600 hover:underline">Editar</button>
                  <button onClick={() => remover(f.id)} className="text-xs text-red-400 hover:underline">Remover</button>
                </td>
              </tr>
            ))}
            {fornecedores.length === 0 && (
              <tr><td colSpan="6" className="p-6 text-center text-slate-400">Nenhum fornecedor cadastrado</td></tr>
            )}
          </tbody>
        </table>
      </div>

      {total > 20 && (
        <div className="flex justify-between items-center text-sm mt-4">
          <span className="text-slate-400">{total} fornecedores</span>
          <div className="flex gap-2">
            <button disabled={page === 1} onClick={() => setPage(p => p - 1)}
              className="px-3 py-1.5 border rounded-lg disabled:opacity-40">Anterior</button>
            <span className="px-3 py-1.5 text-slate-600">Pag {page}</span>
            <button disabled={page * 20 >= total} onClick={() => setPage(p => p + 1)}
              className="px-3 py-1.5 border rounded-lg disabled:opacity-40">Proxima</button>
          </div>
        </div>
      )}
    </div>
  )
}
