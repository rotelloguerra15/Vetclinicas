import { useEffect, useState } from 'react'
import api from '../api/client'

const VAZIO = {
  nome: '', telefone: '', email: '', cpfCnpj: '', obs: '',
  aniversarioDia: '', aniversarioMes: '',
  logradouro: '', numEnd: '', complemento: '', bairro: '', cidade: '', estado: '', cep: ''
}
const MESES = ['Jan', 'Fev', 'Mar', 'Abr', 'Mai', 'Jun', 'Jul', 'Ago', 'Set', 'Out', 'Nov', 'Dez']
const ESTADOS = ['AC','AL','AP','AM','BA','CE','DF','ES','GO','MA','MT','MS','MG','PA','PB','PR','PE','PI','RJ','RN','RS','RO','RR','SC','SP','SE','TO']

export default function Tutores() {
  const [tutores, setTutores] = useState([])
  const [busca, setBusca] = useState('')
  const [form, setForm] = useState(VAZIO)
  const [editandoId, setEditandoId] = useState(null)
  const [mostrarForm, setMostrarForm] = useState(false)

  function carregar() {
    api.get('/tutores', { params: { busca } })
      .then((r) => setTutores(r.data.items)).catch(() => {})
  }

  useEffect(() => { carregar() }, [])

  function novoTutor() {
    setEditandoId(null)
    setForm(VAZIO)
    setMostrarForm(true)
  }

  async function editarTutor(id) {
    try {
      const { data } = await api.get(`/tutores/${id}`)
      setForm({
        nome: data.nome || '', telefone: data.telefone || '', email: data.email || '',
        cpfCnpj: data.cpfCnpj || '', obs: data.obs || '',
        aniversarioDia: data.aniversarioDia || '', aniversarioMes: data.aniversarioMes || '',
        logradouro: data.logradouro || '', numEnd: data.numEnd || '',
        complemento: data.complemento || '', bairro: data.bairro || '',
        cidade: data.cidade || '', estado: data.estado || '', cep: data.cep || ''
      })
      setEditandoId(id)
      setMostrarForm(true)
    } catch { /* ignora */ }
  }

  function cancelar() {
    setMostrarForm(false)
    setEditandoId(null)
    setForm(VAZIO)
  }

  async function salvar(e) {
    e.preventDefault()
    const payload = {
      ...form,
      aniversarioDia: form.aniversarioDia ? +form.aniversarioDia : null,
      aniversarioMes: form.aniversarioMes ? +form.aniversarioMes : null
    }
    if (editandoId) await api.put(`/tutores/${editandoId}`, payload)
    else await api.post('/tutores', payload)
    cancelar()
    carregar()
  }

  const f = (field) => ({
    value: form[field],
    onChange: (e) => setForm({ ...form, [field]: e.target.value })
  })

  return (
    <div>
      <div className="flex justify-between items-center mb-6">
        <h2 className="text-2xl font-bold">Tutores</h2>
        <button onClick={mostrarForm ? cancelar : novoTutor}
          className="bg-slate-900 text-white px-4 py-2 rounded-lg">
          {mostrarForm ? 'Cancelar' : '+ Novo tutor'}
        </button>
      </div>

      {mostrarForm && (
        <form onSubmit={salvar} className="bg-white p-6 rounded-2xl shadow mb-6 space-y-4">
          {/* Dados pessoais */}
          <p className="font-semibold text-slate-700 border-b pb-1">
            {editandoId ? '✏️ Editar tutor' : '✨ Novo tutor'} — Dados pessoais
          </p>
          <div className="grid grid-cols-2 gap-3">
            <input className="border rounded-lg px-3 py-2" placeholder="Nome *" required {...f('nome')} />
            <input className="border rounded-lg px-3 py-2" placeholder="Telefone / WhatsApp" {...f('telefone')} />
            <input className="border rounded-lg px-3 py-2" placeholder="Email" {...f('email')} />
            <input className="border rounded-lg px-3 py-2" placeholder="CPF / CNPJ" {...f('cpfCnpj')} />
          </div>

          {/* Endereço desmembrado */}
          <p className="font-semibold text-slate-700 border-b pb-1 pt-2">📍 Endereço</p>
          <div className="grid grid-cols-3 gap-3">
            <input className="border rounded-lg px-3 py-2 col-span-2" placeholder="Logradouro (rua / av.)" {...f('logradouro')} />
            <input className="border rounded-lg px-3 py-2" placeholder="Número" {...f('numEnd')} />
            <input className="border rounded-lg px-3 py-2" placeholder="Complemento (apto, bloco...)" {...f('complemento')} />
            <input className="border rounded-lg px-3 py-2" placeholder="Bairro" {...f('bairro')} />
            <input className="border rounded-lg px-3 py-2" placeholder="CEP" {...f('cep')} />
            <input className="border rounded-lg px-3 py-2 col-span-2" placeholder="Cidade" {...f('cidade')} />
            <select className="border rounded-lg px-3 py-2" {...f('estado')}>
              <option value="">Estado</option>
              {ESTADOS.map((uf) => <option key={uf} value={uf}>{uf}</option>)}
            </select>
          </div>

          {/* Aniversário + obs */}
          <p className="font-semibold text-slate-700 border-b pb-1 pt-2">📅 Outras informações</p>
          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="text-xs text-slate-500 block mb-1">Aniversário (opcional)</label>
              <div className="flex gap-2 items-center">
                <input className="border rounded-lg px-3 py-2 w-20" placeholder="Dia" type="number" min="1" max="31" {...f('aniversarioDia')} />
                <span className="text-slate-400">/</span>
                <select className="border rounded-lg px-3 py-2 flex-1" {...f('aniversarioMes')}>
                  <option value="">Mês</option>
                  {MESES.map((m, i) => <option key={i} value={i + 1}>{m}</option>)}
                </select>
              </div>
            </div>
            <input className="border rounded-lg px-3 py-2" placeholder="Observações" {...f('obs')} />
          </div>

          <button className="bg-emerald-600 text-white py-2 rounded-lg w-full font-medium">
            {editandoId ? 'Salvar alterações' : 'Cadastrar tutor'}
          </button>
        </form>
      )}

      <div className="flex gap-2 mb-4">
        <input className="border rounded-lg px-3 py-2 flex-1" placeholder="Buscar por nome ou telefone..."
          value={busca} onChange={(e) => setBusca(e.target.value)} />
        <button onClick={carregar} className="bg-slate-200 px-4 rounded-lg">Buscar</button>
      </div>

      <div className="bg-white rounded-2xl shadow overflow-hidden">
        <table className="w-full text-sm">
          <thead className="bg-slate-50 text-left">
            <tr>
              <th className="p-3">Nome</th>
              <th className="p-3">Telefone</th>
              <th className="p-3">Cidade / Bairro</th>
              <th className="p-3">Pets</th>
              <th className="p-3"></th>
            </tr>
          </thead>
          <tbody>
            {tutores.map((t) => (
              <tr key={t.id} className="border-t hover:bg-slate-50">
                <td className="p-3 font-medium">{t.nome}</td>
                <td className="p-3">{t.telefone}</td>
                <td className="p-3 text-slate-500 text-xs">
                  {[t.cidade, t.bairro].filter(Boolean).join(' — ') || t.endereco || '—'}
                </td>
                <td className="p-3">{t.qtdPets}</td>
                <td className="p-3 text-right">
                  <button onClick={() => editarTutor(t.id)} className="text-xs text-blue-600 hover:underline">
                    Editar
                  </button>
                </td>
              </tr>
            ))}
            {tutores.length === 0 && (
              <tr><td colSpan="5" className="p-6 text-center text-slate-400">Nenhum tutor encontrado</td></tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  )
}
