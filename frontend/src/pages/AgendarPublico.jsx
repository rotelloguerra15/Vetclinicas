import { useEffect, useState } from 'react'
import { useParams } from 'react-router-dom'
import api from '../api/client'

export default function AgendarPublico() {
  const { token } = useParams()
  const [dados, setDados] = useState(null)
  const [erro, setErro] = useState('')
  const [selecionado, setSelecionado] = useState(null)
  const [tipo, setTipo] = useState('banho')
  const [enviado, setEnviado] = useState(false)

  useEffect(() => {
    api.get(`/publico/agendar/${token}`)
      .then((r) => setDados(r.data))
      .catch(() => setErro('Este link expirou ou já foi utilizado.'))
  }, [token])

  async function confirmar() {
    if (!selecionado) return
    try {
      await api.post(`/publico/agendar/${token}`, { dataHora: selecionado, tipo })
      setEnviado(true)
    } catch {
      setErro('Não foi possível agendar. O link pode ter expirado.')
    }
  }

  if (erro) return (
    <div className="min-h-screen flex items-center justify-center bg-slate-100 p-4">
      <div className="bg-white p-8 rounded-2xl shadow text-center max-w-sm">
        <div className="text-4xl mb-3">😿</div>
        <p className="text-slate-600">{erro}</p>
      </div>
    </div>
  )

  if (enviado) return (
    <div className="min-h-screen flex items-center justify-center bg-slate-100 p-4">
      <div className="bg-white p-8 rounded-2xl shadow text-center max-w-sm">
        <div className="text-4xl mb-3">🎉</div>
        <h1 className="text-xl font-bold mb-2">Pedido enviado!</h1>
        <p className="text-slate-600">A clínica vai confirmar seu horário em breve pelo WhatsApp.</p>
      </div>
    </div>
  )

  if (!dados) return <div className="min-h-screen flex items-center justify-center">Carregando...</div>

  // agrupa slots por dia
  const porDia = {}
  dados.slots.forEach((s) => {
    const dia = new Date(s.inicio).toLocaleDateString('pt-BR', { weekday: 'short', day: '2-digit', month: '2-digit' })
    ;(porDia[dia] = porDia[dia] || []).push(s)
  })

  return (
    <div className="min-h-screen bg-slate-100 p-4">
      <div className="max-w-md mx-auto bg-white rounded-2xl shadow p-6">
        <div className="text-center mb-6">
          <div className="text-4xl mb-2">🐾</div>
          <h1 className="text-xl font-bold">Agendar para {dados.petNome || 'seu pet'}</h1>
          <p className="text-sm text-slate-500">Escolha o melhor dia e horário</p>
        </div>

        <select className="w-full border rounded-lg px-3 py-2 mb-4"
          value={tipo} onChange={(e) => setTipo(e.target.value)}>
          <option value="banho">Banho</option>
          <option value="banho_tosa">Banho e tosa</option>
          <option value="consulta">Consulta</option>
        </select>

        <div className="max-h-96 overflow-auto">
          {Object.entries(porDia).map(([dia, slots]) => (
            <div key={dia} className="mb-4">
              <div className="text-sm font-semibold text-slate-600 mb-2 capitalize">{dia}</div>
              <div className="grid grid-cols-3 gap-2">
                {slots.map((s) => {
                  const hora = new Date(s.inicio).toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' })
                  const ativo = selecionado === s.inicio
                  return (
                    <button key={s.inicio} onClick={() => setSelecionado(s.inicio)}
                      className={`py-2 rounded-lg text-sm border ${ativo
                        ? 'bg-slate-900 text-white border-slate-900'
                        : 'bg-white hover:bg-slate-50 border-slate-200'}`}>
                      {hora}
                    </button>
                  )
                })}
              </div>
            </div>
          ))}
        </div>

        <button onClick={confirmar} disabled={!selecionado}
          className="w-full mt-4 bg-emerald-600 text-white py-3 rounded-lg disabled:bg-slate-300">
          Confirmar agendamento
        </button>
      </div>
    </div>
  )
}
