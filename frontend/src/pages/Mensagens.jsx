import { useEffect, useState } from 'react'
import api from '../api/client'

const GATILHOS = {
  aniversario_pet: { label: 'Aniversário do pet', icone: '🎉', desc: 'Enviada no dia do aniversário do pet' },
  aniversario_tutor: { label: 'Aniversário do tutor', icone: '🎂', desc: 'No dia do aniversário do tutor (cadastre em Tutores)' },
  boas_vindas_tutor: { label: 'Boas-vindas ao tutor', icone: '👋', desc: 'Ao cadastrar um novo tutor' },
  boas_vindas_pet: { label: 'Boas-vindas ao pet', icone: '🐾', desc: 'Ao cadastrar um novo pet' },
  lembrete_agendamento: { label: 'Lembrete de agendamento', icone: '⏰', desc: 'Enviada X horas antes do horário marcado' },
  pos_atendimento_nps: { label: 'Pesquisa de satisfação (NPS)', icone: '⭐', desc: 'Após o atendimento, pede nota de 0 a 10' },
  vacina_vencendo: { label: 'Vacina vencendo', icone: '💉', desc: 'Quando a próxima dose está próxima' }
}

const PLACEHOLDERS = ['{tutor}', '{pet}', '{clinica}', '{data}', '{hora}']

export default function Mensagens() {
  const [configs, setConfigs] = useState([])
  const [editando, setEditando] = useState(null)

  function carregar() {
    api.get('/mensagens/config').then((r) => setConfigs(r.data)).catch(() => {})
  }
  useEffect(() => { carregar() }, [])

  async function toggle(c) {
    await api.put(`/mensagens/config/${c.id}`, { ...c, ativo: !c.ativo })
    carregar()
  }

  return (
    <div>
      <h2 className="text-2xl font-bold mb-2">Mensagens automáticas</h2>
      <p className="text-slate-500 text-sm mb-6">
        Configure os disparos automáticos via WhatsApp. Use os marcadores {PLACEHOLDERS.join(', ')} no texto.
      </p>

      <div className="grid gap-3">
        {configs.map((c) => {
          const g = GATILHOS[c.gatilho] || { label: c.gatilho, icone: '📩', desc: '' }
          return (
            <div key={c.id} className="bg-white rounded-2xl shadow p-5">
              <div className="flex items-start justify-between">
                <div className="flex gap-3">
                  <div className="text-2xl">{g.icone}</div>
                  <div>
                    <div className="font-semibold">{g.label}</div>
                    <div className="text-xs text-slate-400">{g.desc}</div>
                  </div>
                </div>
                <button onClick={() => toggle(c)}
                  className={`relative w-11 h-6 rounded-full transition ${c.ativo ? 'bg-emerald-500' : 'bg-slate-300'}`}>
                  <span className={`absolute top-0.5 w-5 h-5 bg-white rounded-full transition ${c.ativo ? 'left-5' : 'left-0.5'}`}></span>
                </button>
              </div>

              <div className="mt-3 bg-slate-50 rounded-lg p-3 text-sm text-slate-600 whitespace-pre-wrap">
                {c.template}
              </div>

              <div className="flex items-center justify-between mt-3">
                {c.gatilho === 'lembrete_agendamento' && (
                  <span className="text-xs text-slate-400">⏱ {c.horasAntes || 24}h antes</span>
                )}
                <button onClick={() => setEditando(c)} className="text-xs text-blue-600 hover:underline ml-auto">
                  Editar texto
                </button>
              </div>
            </div>
          )
        })}
      </div>

      {editando && (
        <ModalEditar config={editando} onClose={() => setEditando(null)}
          onSaved={() => { setEditando(null); carregar() }} />
      )}
    </div>
  )
}

function ModalEditar({ config, onClose, onSaved }) {
  const [template, setTemplate] = useState(config.template)
  const [horasAntes, setHorasAntes] = useState(config.horasAntes || 24)

  async function salvar(e) {
    e.preventDefault()
    await api.put(`/mensagens/config/${config.id}`, {
      canal: config.canal, ativo: config.ativo, template,
      horasAntes: config.gatilho === 'lembrete_agendamento' ? +horasAntes : null
    })
    onSaved()
  }

  return (
    <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50 p-4" onClick={onClose}>
      <div className="bg-white rounded-2xl shadow-xl p-6 w-full max-w-lg" onClick={(e) => e.stopPropagation()}>
        <h3 className="font-bold text-lg mb-4">Editar mensagem</h3>
        <form onSubmit={salvar} className="grid gap-3">
          <textarea className="border rounded-lg px-3 py-2" rows="5" required
            value={template} onChange={(e) => setTemplate(e.target.value)} />
          <div className="flex flex-wrap gap-1">
            {PLACEHOLDERS.map((p) => (
              <button key={p} type="button" onClick={() => setTemplate((t) => t + ' ' + p)}
                className="text-xs bg-slate-100 px-2 py-1 rounded hover:bg-slate-200">{p}</button>
            ))}
          </div>
          {config.gatilho === 'lembrete_agendamento' && (
            <label className="text-sm flex items-center gap-2">
              Enviar
              <input type="number" className="border rounded px-2 py-1 w-20" value={horasAntes}
                onChange={(e) => setHorasAntes(e.target.value)} />
              horas antes
            </label>
          )}
          <button className="bg-emerald-600 text-white py-2 rounded-lg">Salvar</button>
        </form>
      </div>
    </div>
  )
}
