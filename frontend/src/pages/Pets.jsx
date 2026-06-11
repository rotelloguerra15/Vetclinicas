import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import api from '../api/client'

const ESPECIES = { cao: '🐕 Cão', gato: '🐈 Gato', ave: '🦜 Ave', roedor: '🐹 Roedor', reptil: '🦎 Réptil', outro: 'Outro' }

const FORM_VAZIO = {
  tutorId: '', nome: '', especie: 'cao', raca: '', sexo: 'indefinido',
  dataNascimento: '', pesoKg: '', castrado: '', pelagem: '',
  temMicrochip: false, microchipNum: '',
  temPlanoSaude: false,
  // M5: campos do vínculo
  planoId: '', numCarteirinha: '', validade: '', descontoPercent: ''
}

export default function Pets() {
  const nav = useNavigate()
  const [pets, setPets] = useState([])
  const [tutores, setTutores] = useState([])
  const [pelagens, setPelagens] = useState([])
  const [planos, setPlanos] = useState([])         // M5
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
    api.get('/cadastros/pelagens').then(r => setPelagens(r.data)).catch(() => {})
    api.get('/planos-saude').then(r => setPlanos(r.data)).catch(() => {})   // M5
  }, [])

  // Pré-seleciona tutor se vier por URL (?tutorId=...)
  useEffect(() => {
    const params = new URLSearchParams(window.location.search)
    const tid = params.get('tutorId')
    if (tid) {
      setForm(f => ({ ...f, tutorId: tid }))
      setMostrarForm(true)
    }
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

      // M5: busca vínculo ativo de plano do pet
      let planoAtivo = null
      try {
        const rp = await api.get(`/planos-saude/pet/${id}`)
        planoAtivo = rp.data.find(v => v.ativo) || null
      } catch { /* sem plano */ }

      setForm({
        tutorId: data.tutorId || '',
        nome: data.nome || '',
        especie: data.especie || 'cao',
        raca: data.raca || '',
        sexo: data.sexo || 'indefinido',
        dataNascimento: data.dataNascimento || '',
        pesoKg: data.pesoKg ?? '',
        castrado: data.castrado === true ? 'true' : data.castrado === false ? 'false' : '',
        pelagem: data.pelagem || '',
        temMicrochip: data.temMicrochip || false,
        microchipNum: data.microchipNum || '',
        temPlanoSaude: !!planoAtivo,
        planoId: planoAtivo?.planoId || '',
        numCarteirinha: planoAtivo?.numCarteirinha || '',
        validade: planoAtivo?.validade || '',
        descontoPercent: planoAtivo?.descontoEfetivo != null ? String(planoAtivo.descontoEfetivo) : ''
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
      tutorId: form.tutorId,
      nome: form.nome,
      especie: form.especie,
      raca: form.raca || null,
      sexo: form.sexo,
      dataNascimento: form.dataNascimento || null,
      pesoKg: form.pesoKg ? parseFloat(form.pesoKg) : null,
      castrado: form.castrado === 'true' ? true : form.castrado === 'false' ? false : null,
      pelagem: form.pelagem || null,
      obs: null,
      temMicrochip: form.temMicrochip,
      microchipNum: form.temMicrochip ? form.microchipNum : null,
      // legado — mantém compatibilidade com PetsController existente
      temPlanoSaude: form.temPlanoSaude,
      planoSaudeNome: form.temPlanoSaude && form.planoId
        ? (planos.find(p => p.id === form.planoId)?.nome || null)
        : null,
      planoSaudeCarteira: form.temPlanoSaude ? form.numCarteirinha || null : null
    }

    let petId = editandoId
    if (editandoId) {
      await api.put(`/pets/${editandoId}`, payload)
    } else {
      const r = await api.post('/pets', payload)
      petId = r.data.id
    }

    // M5: vincula plano se selecionado
    if (form.temPlanoSaude && form.planoId && petId) {
      await api.post(`/planos-saude/pet/${petId}/vincular`, {
        planoId: form.planoId,
        numCarteirinha: form.numCarteirinha || null,
        validade: form.validade || null,
        descontoPercent: form.descontoPercent !== '' ? parseFloat(form.descontoPercent) : null
      })
    }

    cancelar()
    carregar()
  }

  const f = (field) => ({
    value: form[field],
    onChange: (e) => setForm({ ...form, [field]: e.target.value })
  })

  // Plano selecionado no dropdown (para mostrar desconto padrão)
  const planoSelecionado = planos.find(p => p.id === form.planoId)

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
              <option value="">Castrado? — Não sei</option>
              <option value="true">Sim, castrado(a)</option>
              <option value="false">Não, inteiro(a)</option>
            </select>
            <select className="border rounded-lg px-3 py-2" {...f('pelagem')}>
              <option value="">Pelagem</option>
              {pelagens.map(p => <option key={p.id} value={p.nome}>{p.nome}</option>)}
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

          {/* Plano de Saúde — M5 */}
          <p className="font-semibold text-slate-700 border-b pb-1 pt-2">🏥 Plano de Saúde</p>
          <div className="space-y-3">
            <label className="flex items-center gap-2 cursor-pointer">
              <input type="checkbox" checked={form.temPlanoSaude}
                onChange={(e) => setForm({ ...form, temPlanoSaude: e.target.checked, planoId: '', numCarteirinha: '', validade: '', descontoPercent: '' })} />
              <span className="text-sm">Possui plano de saúde</span>
            </label>

            {form.temPlanoSaude && (
              <div className="grid grid-cols-2 gap-3">
                {/* Select de planos cadastrados no M5 */}
                <div className="col-span-2">
                  <select className="border rounded-lg px-3 py-2 w-full" value={form.planoId}
                    onChange={(e) => setForm({ ...form, planoId: e.target.value, descontoPercent: '' })}>
                    <option value="">Selecione o plano...</option>
                    {planos.length === 0
                      ? <option disabled>Nenhum plano cadastrado — acesse Planos de Saude</option>
                      : planos.map(p => (
                        <option key={p.id} value={p.id}>
                          {p.nome}{p.operadora ? ` — ${p.operadora}` : ''}
                        </option>
                      ))
                    }
                  </select>
                </div>

                {/* Desconto padrão do plano (informativo) */}
                {planoSelecionado && planoSelecionado.descontoPercent > 0 && (
                  <div className="col-span-2 bg-emerald-50 rounded-lg px-3 py-2 text-sm text-emerald-700">
                    Desconto padrão do plano: <strong>{planoSelecionado.descontoPercent}%</strong>
                    {form.descontoPercent === '' && ' (será aplicado automaticamente)'}
                  </div>
                )}

                <input className="border rounded-lg px-3 py-2" placeholder="Número da carteirinha"
                  value={form.numCarteirinha} onChange={(e) => setForm({ ...form, numCarteirinha: e.target.value })} />

                <div>
                  <label className="text-xs text-slate-500 block mb-1">Validade do plano</label>
                  <input type="date" className="border rounded-lg px-3 py-2 w-full"
                    value={form.validade} onChange={(e) => setForm({ ...form, validade: e.target.value })} />
                </div>

                <div className="col-span-2">
                  <label className="text-xs text-slate-500 block mb-1">Desconto específico (%) — deixe em branco para usar o do plano</label>
                  <input type="number" min="0" max="100" step="0.5"
                    className="border rounded-lg px-3 py-2 w-full"
                    placeholder="Ex: 15"
                    value={form.descontoPercent} onChange={(e) => setForm({ ...form, descontoPercent: e.target.value })} />
                </div>
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
