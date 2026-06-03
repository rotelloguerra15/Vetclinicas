import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import api from '../api/client'

const ESPECIES = { cao: '🐕 Cão', gato: '🐈 Gato', ave: '🦜 Ave', roedor: '🐹 Roedor', reptil: '🦎 Réptil', outro: 'Outro' }

const FORM_VAZIO = {
  tutorId: '', nome: '', especie: 'cao', raca: '', sexo: 'indefinido',
  dataNascimento: '', pesoKg: '', castrado: '',
  temMicrochip: false, microchipNum: '',
  temPlanoSaude: false, planoSaudeNome: '', planoSaudeCarteira: ''
}

export default function Pets() {
  const nav = useNavigate()
  const [pets, setPets] = useState([])
  const [tutores, setTutores] = useState([])
  const [busca, setBusca] = useState('')
  const [mostrarForm, setMostrarForm] = useState(false)
  const [editandoId, setEditandoId] = useState(null)
  const [form, setForm] = useState(FORM_VAZIO)

  function carregar() {
    api.get('/pets', { params: { busca } }).then((r) => setPets(r.data.items)).catch(() => {})
  }

  useEffect(() => {
    carregar()
    api.get('/tutores', { params: { pageSize: 100 } }).then((r) => setTutores(r.data.items)).catch(() => {})
  }, [])

  function novoForm() {
    setEditandoId(null)
    setForm(FORM_VAZIO)
    setMostrarForm(true)
  }

  async function editarPet(id, e) {
    e.stopPropagation()
    try {
      const { data } = await api.get(`/pets/${id}`)
      setForm({
        tutorId: data.tutorId || '',
        nome: data.nome || '',
        especie: data.especie || 'cao',
        raca: data.raca || '',
        sexo: data.sexo || 'indefinido',
        dataNascimento: data.dataNascimento || '',
        pesoKg: data.pesoKg ?? '',
        castrado: data.castrado === true ? 'true' : data.castrado === false ? 'false' : '',
        temMicrochip: data.temMicrochip || false,
        microchipNum: data.microchipNum || '',
        temPlanoSaude: data.temPlanoSaude || false,
        planoSaudeNome: data.planoSaudeNome || '',
        planoSaudeCarteira: data.planoSaudeCarteira || ''
      })
      setEditandoId(id)
      setMostrarForm(true)
    } catch { /* ignora */ }
  }

  function cancelar() {
    setMostrarForm(false)
    setEditandoId(null)
    setForm(FORM_VAZIO)
  }

  async function salvar(e) {
    e.preventDefault()
    const payload = {
      ...form,
      pesoKg: form.pesoKg ? parseFloat(form.pesoKg) : null,
      castrado: form.castrado === 'true' ? true : form.castrado === 'false' ? false : null,
      dataNascimento: form.dataNascimento || null,
      microchipNum: form.temMicrochip ? form.microchipNum : null,
      planoSaudeNome: form.temPlanoSaude ? form.planoSaudeNome : null,
      planoSaudeCarteira: form.temPlanoSaude ? form.planoSaudeCarteira : null
    }
    if (editandoId) await api.put(`/pets/${editandoId}`, payload)
    else await api.post('/pets', payload)
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
        <h2 className="text-2xl font-bold">Pets</h2>
        <button onClick={mostrarForm ? cancelar : novoForm}
          className="bg-slate-900 text-white px-4 py-2 rounded-lg">
          {mostrarForm ? 'Cancelar' : '+ Novo pet'}
        </button>
      </div>

      {mostrarForm && (
        <form onSubmit={salvar} className="bg-white p-6 rounded-2xl shadow mb-6 space-y-4">
          {/* Identificação */}
          <p className="font-semibold text-slate-700 border-b pb-1">
            {editandoId ? '✏️ Editar pet' : '🐾 Novo pet'} — Identificação
          </p>
          <div className="grid grid-cols-2 gap-3">
            <select className="border rounded-lg px-3 py-2 col-span-2" required {...f('tutorId')}>
              <option value="">Selecione o tutor *</option>
              {tutores.map((t) => <option key={t.id} value={t.id}>{t.nome}</option>)}
            </select>
            <input className="border rounded-lg px-3 py-2" placeholder="Nome do pet *" required {...f('nome')} />
            <select className="border rounded-lg px-3 py-2" {...f('especie')}>
              {Object.entries(ESPECIES).map(([k, v]) => <option key={k} value={k}>{v}</option>)}
            </select>
            <input className="border rounded-lg px-3 py-2" placeholder="Raça" {...f('raca')} />
            <select className="border rounded-lg px-3 py-2" {...f('sexo')}>
              <option value="indefinido">Sexo</option>
              <option value="macho">Macho</option>
              <option value="femea">Fêmea</option>
            </select>
            <div>
              <label className="text-xs text-slate-500 block mb-1">Data de nascimento</label>
              <input className="border rounded-lg px-3 py-2 w-full" type="date" {...f('dataNascimento')} />
            </div>
            <input className="border rounded-lg px-3 py-2" placeholder="Peso (kg)" type="number" step="0.1" {...f('pesoKg')} />
            <select className="border rounded-lg px-3 py-2" {...f('castrado')}>
              <option value="">Castrado?</option>
              <option value="true">Sim</option>
              <option value="false">Não</option>
            </select>
          </div>

          {/* Microchip */}
          <p className="font-semibold text-slate-700 border-b pb-1 pt-2">📡 Microchip</p>
          <div className="flex items-center gap-4">
            <label className="flex items-center gap-2 cursor-pointer">
              <input type="checkbox" checked={form.temMicrochip}
                onChange={(e) => setForm({ ...form, temMicrochip: e.target.checked })} />
              <span className="text-sm">Possui microchip</span>
            </label>
            {form.temMicrochip && (
              <input className="border rounded-lg px-3 py-2 flex-1" placeholder="Número do microchip"
                value={form.microchipNum} onChange={(e) => setForm({ ...form, microchipNum: e.target.value })} />
            )}
          </div>

          {/* Plano de saúde */}
          <p className="font-semibold text-slate-700 border-b pb-1 pt-2">🏥 Plano de Saúde</p>
          <div className="space-y-2">
            <label className="flex items-center gap-2 cursor-pointer">
              <input type="checkbox" checked={form.temPlanoSaude}
                onChange={(e) => setForm({ ...form, temPlanoSaude: e.target.checked })} />
              <span className="text-sm">Possui plano de saúde</span>
            </label>
            {form.temPlanoSaude && (
              <div className="grid grid-cols-2 gap-3">
                <input className="border rounded-lg px-3 py-2" placeholder="Nome do plano"
                  value={form.planoSaudeNome} onChange={(e) => setForm({ ...form, planoSaudeNome: e.target.value })} />
                <input className="border rounded-lg px-3 py-2" placeholder="Número da carteirinha"
                  value={form.planoSaudeCarteira} onChange={(e) => setForm({ ...form, planoSaudeCarteira: e.target.value })} />
              </div>
            )}
          </div>

          <button className="bg-emerald-600 text-white py-2 rounded-lg w-full font-medium">
            {editandoId ? 'Salvar alterações' : 'Cadastrar pet'}
          </button>
        </form>
      )}

      <div className="flex gap-2 mb-4">
        <input className="border rounded-lg px-3 py-2 flex-1" placeholder="Buscar pet..."
          value={busca} onChange={(e) => setBusca(e.target.value)} />
        <button onClick={carregar} className="bg-slate-200 px-4 rounded-lg">Buscar</button>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        {pets.map((p) => (
          <div key={p.id} onClick={() => nav(`/pets/${p.id}`)}
            className="bg-white rounded-2xl shadow p-4 cursor-pointer hover:shadow-md hover:ring-2 hover:ring-slate-200 transition">
            <div className="flex items-start gap-3">
              <div className="text-3xl">{ESPECIES[p.especie]?.split(' ')[0] || '🐾'}</div>
              <div className="flex-1 min-w-0">
                <div className="font-bold truncate">{p.nome}</div>
                <div className="text-sm text-slate-500">{p.raca || '—'}</div>
                {p.idadeFormatada && (
                  <div className="text-xs text-slate-400 mt-0.5">🎂 {p.idadeFormatada}</div>
                )}
              </div>
              <button onClick={(e) => editarPet(p.id, e)}
                className="text-xs text-blue-600 hover:underline shrink-0">
                Editar
              </button>
            </div>
            <div className="text-xs text-slate-400 mt-3 flex items-center gap-3">
              <span>👤 {p.tutorNome}</span>
              {p.temMicrochip && <span title={p.microchipNum}>📡 Chip</span>}
              {p.temPlanoSaude && <span title={p.planoSaudeNome}>🏥 Plano</span>}
            </div>
          </div>
        ))}
        {pets.length === 0 && <p className="text-slate-400">Nenhum pet cadastrado</p>}
      </div>
    </div>
  )
}
