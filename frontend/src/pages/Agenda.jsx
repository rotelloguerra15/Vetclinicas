import { useEffect, useState } from 'react'
import api from '../api/client'

const STATUS_COR = {
  pendente: 'bg-amber-100 text-amber-700',
  confirmado: 'bg-blue-100 text-blue-700',
  em_atendimento: 'bg-purple-100 text-purple-700',
  concluido: 'bg-emerald-100 text-emerald-700',
  cancelado: 'bg-red-100 text-red-700',
  faltou: 'bg-slate-100 text-slate-500'
}
const TIPOS = ['consulta','banho','tosa','banho_tosa','vacina','retorno','cirurgia','exame']

export default function Agenda() {
  const [ags, setAgs] = useState([])
  const [modal, setModal] = useState(false)

  function carregar() {
    api.get('/agendamentos').then((r) => setAgs(r.data)).catch(() => {})
  }
  useEffect(() => { carregar() }, [])

  async function confirmar(id) {
    await api.put(`/agendamentos/${id}/confirmar`)
    carregar()
  }
  async function cancelar(id) {
    await api.put(`/agendamentos/${id}/status`, { status: 'cancelado' })
    carregar()
  }

  return (
    <div>
      <div className="flex justify-between items-center mb-6">
        <h2 className="text-2xl font-bold">Agenda — próximos 7 dias</h2>
        <button onClick={() => setModal(true)} className="bg-slate-900 text-white px-4 py-2 rounded-lg">
          + Novo agendamento
        </button>
      </div>

      <div className="bg-white rounded-2xl shadow overflow-hidden">
        <table className="w-full text-sm">
          <thead className="bg-slate-50 text-left">
            <tr>
              <th className="p-3">Data/Hora</th><th className="p-3">Pet</th>
              <th className="p-3">Tipo</th><th className="p-3">Origem</th>
              <th className="p-3">Status</th><th className="p-3"></th>
            </tr>
          </thead>
          <tbody>
            {ags.map((a) => (
              <tr key={a.id} className="border-t hover:bg-slate-50">
                <td className="p-3">{new Date(a.dataHora).toLocaleString('pt-BR')}</td>
                <td className="p-3 font-medium">{a.petNome}</td>
                <td className="p-3">{a.tipo}</td>
                <td className="p-3">
                  {a.origem === 'tutor_self_service'
                    ? <span className="text-xs bg-blue-50 text-blue-600 px-2 py-0.5 rounded-full">📱 Tutor</span>
                    : <span className="text-xs text-slate-400">Interno</span>}
                </td>
                <td className="p-3">
                  <span className={`text-xs px-2 py-1 rounded-full ${STATUS_COR[a.status] || ''}`}>
                    {a.status}
                  </span>
                </td>
                <td className="p-3 text-right space-x-2">
                  {a.status === 'pendente' && (
                    <>
                      <button onClick={() => confirmar(a.id)}
                        className="text-xs bg-blue-600 text-white px-3 py-1 rounded-lg">Confirmar</button>
                      <button onClick={() => cancelar(a.id)}
                        className="text-xs bg-slate-200 text-slate-600 px-3 py-1 rounded-lg">Recusar</button>
                    </>
                  )}
                </td>
              </tr>
            ))}
            {ags.length === 0 && (
              <tr><td colSpan="6" className="p-6 text-center text-slate-400">Sem agendamentos para os próximos 7 dias</td></tr>
            )}
          </tbody>
        </table>
      </div>

      {modal && <ModalAgendar onClose={() => setModal(false)} onSaved={() => { setModal(false); carregar() }} />}
    </div>
  )
}

function ModalAgendar({ onClose, onSaved }) {
  const [pets, setPets] = useState([])
  const [f, setF] = useState({ petId: '', tipo: 'consulta', data: '', hora: '09:00', duracaoMin: 60, obs: '' })

  useEffect(() => {
    api.get('/pets', { params: { pageSize: 200 } }).then((r) => setPets(r.data.items)).catch(() => {})
  }, [])

  async function salvar(e) {
    e.preventDefault()
    const dataHora = new Date(`${f.data}T${f.hora}:00`).toISOString()
    await api.post('/agendamentos', { petId: f.petId, tipo: f.tipo, dataHora, duracaoMin: +f.duracaoMin, obs: f.obs || null })
    onSaved()
  }

  return (
    <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50 p-4" onClick={onClose}>
      <div className="bg-white rounded-2xl shadow-xl p-6 w-full max-w-md" onClick={(e) => e.stopPropagation()}>
        <h3 className="font-bold text-lg mb-4">Novo agendamento</h3>
        <form onSubmit={salvar} className="grid gap-3">
          <select className="border rounded-lg px-3 py-2" required
            value={f.petId} onChange={(e) => setF({ ...f, petId: e.target.value })}>
            <option value="">Selecione o pet...</option>
            {pets.map((p) => <option key={p.id} value={p.id}>{p.nome} ({p.tutorNome})</option>)}
          </select>
          <select className="border rounded-lg px-3 py-2"
            value={f.tipo} onChange={(e) => setF({ ...f, tipo: e.target.value })}>
            {TIPOS.map((t) => <option key={t} value={t}>{t}</option>)}
          </select>
          <div className="grid grid-cols-2 gap-2">
            <div>
              <label className="text-xs text-slate-500">Data</label>
              <input type="date" className="border rounded-lg px-3 py-2 w-full" required
                value={f.data} onChange={(e) => setF({ ...f, data: e.target.value })} />
            </div>
            <div>
              <label className="text-xs text-slate-500">Hora</label>
              <input type="time" className="border rounded-lg px-3 py-2 w-full" required
                value={f.hora} onChange={(e) => setF({ ...f, hora: e.target.value })} />
            </div>
          </div>
          <input className="border rounded-lg px-3 py-2" placeholder="Duração (min)" type="number"
            value={f.duracaoMin} onChange={(e) => setF({ ...f, duracaoMin: e.target.value })} />
          <input className="border rounded-lg px-3 py-2" placeholder="Observação (opcional)"
            value={f.obs} onChange={(e) => setF({ ...f, obs: e.target.value })} />
          <button className="bg-emerald-600 text-white py-2 rounded-lg">Agendar</button>
        </form>
      </div>
    </div>
  )
}
