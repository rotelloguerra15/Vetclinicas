import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import api from '../api/client'

const PLANOS = ['trial', 'basico', 'pro', 'enterprise']
const PLANO_COR = { trial: 'bg-slate-100 text-slate-600', basico: 'bg-blue-100 text-blue-700', pro: 'bg-emerald-100 text-emerald-700', enterprise: 'bg-purple-100 text-purple-700' }

export default function AdminPanel() {
  const nav = useNavigate()
  const [clinicas, setClinicas] = useState([])
  const [metricas, setMetricas] = useState(null)
  const [modal, setModal] = useState(false)
  const [criada, setCriada] = useState(null)

  function carregar() {
    api.get('/admin/clinicas').then((r) => setClinicas(r.data)).catch(() => nav('/admin/login'))
    api.get('/admin/metricas').then((r) => setMetricas(r.data)).catch(() => {})
  }
  useEffect(() => { carregar() }, [])

  async function suspender(c) {
    if (c.suspenso) await api.put(`/admin/clinicas/${c.id}/reativar`)
    else await api.put(`/admin/clinicas/${c.id}/suspender`)
    carregar()
  }
  async function mudarPlano(c, plano) {
    await api.put(`/admin/clinicas/${c.id}/plano`, { plano })
    carregar()
  }
  function sair() { localStorage.clear(); nav('/admin/login') }

  return (
    <div className="min-h-screen bg-slate-100 p-8">
      <div className="max-w-5xl mx-auto">
        <div className="flex justify-between items-center mb-6">
          <div>
            <h1 className="text-2xl font-bold">⚙️ Painel da Plataforma</h1>
            <p className="text-sm text-slate-500">Gestão de clínicas — Ketra</p>
          </div>
          <div className="flex gap-2">
            <button onClick={() => setModal(true)} className="bg-emerald-600 text-white px-4 py-2 rounded-lg">+ Nova clínica</button>
            <button onClick={sair} className="bg-slate-200 px-4 py-2 rounded-lg">Sair</button>
          </div>
        </div>

        {metricas && (
          <div className="grid grid-cols-2 md:grid-cols-4 gap-3 mb-6">
            <Metric titulo="Clínicas" valor={metricas.totalClinicas} />
            <Metric titulo="Ativas" valor={metricas.clinicasAtivas} cor="text-emerald-600" />
            <Metric titulo="Suspensas" valor={metricas.clinicasSuspensas} cor="text-red-600" />
            <Metric titulo="Pets na base" valor={metricas.totalPets} cor="text-blue-600" />
          </div>
        )}

        <div className="bg-white rounded-2xl shadow overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-slate-50 text-left">
              <tr>
                <th className="p-3">Clínica</th><th className="p-3">Plano</th>
                <th className="p-3 text-center">Usuários</th><th className="p-3 text-center">Pets</th>
                <th className="p-3">Status</th><th className="p-3"></th>
              </tr>
            </thead>
            <tbody>
              {clinicas.map((c) => (
                <tr key={c.id} className="border-t hover:bg-slate-50">
                  <td className="p-3 font-medium">{c.nome}</td>
                  <td className="p-3">
                    <select value={c.plano} onChange={(e) => mudarPlano(c, e.target.value)}
                      className={`text-xs px-2 py-1 rounded-full border-0 ${PLANO_COR[c.plano] || ''}`}>
                      {PLANOS.map((p) => <option key={p} value={p}>{p}</option>)}
                    </select>
                  </td>
                  <td className="p-3 text-center">{c.totalUsuarios}</td>
                  <td className="p-3 text-center">{c.totalPets}</td>
                  <td className="p-3">
                    {c.suspenso
                      ? <span className="text-xs px-2 py-1 rounded-full bg-red-100 text-red-700">Suspensa</span>
                      : <span className="text-xs px-2 py-1 rounded-full bg-emerald-100 text-emerald-700">Ativa</span>}
                  </td>
                  <td className="p-3 text-right">
                    <button onClick={() => suspender(c)} className="text-xs text-blue-600 hover:underline">
                      {c.suspenso ? 'Reativar' : 'Suspender'}
                    </button>
                  </td>
                </tr>
              ))}
              {clinicas.length === 0 && <tr><td colSpan="6" className="p-6 text-center text-slate-400">Nenhuma clínica ainda</td></tr>}
            </tbody>
          </table>
        </div>
      </div>

      {modal && <ModalNovaClinica onClose={() => setModal(false)} onCriada={(r) => { setModal(false); setCriada(r); carregar() }} />}
      {criada && <ModalCredenciais dados={criada} onClose={() => setCriada(null)} />}
    </div>
  )
}

function Metric({ titulo, valor, cor }) {
  return (
    <div className="bg-white rounded-2xl shadow p-4">
      <div className="text-xs text-slate-500">{titulo}</div>
      <div className={`text-2xl font-bold ${cor || ''}`}>{valor}</div>
    </div>
  )
}

function ModalNovaClinica({ onClose, onCriada }) {
  const [f, setF] = useState({ nomeClinica: '', plano: 'trial', nomeDono: '', emailDono: '', telefone: '', tagline: '' })
  const [erro, setErro] = useState('')

  async function salvar(e) {
    e.preventDefault()
    setErro('')
    try {
      const { data } = await api.post('/admin/clinicas', f)
      onCriada(data)
    } catch (err) {
      setErro(err.response?.data?.erro || 'Erro ao criar clínica')
    }
  }

  return (
    <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50 p-4" onClick={onClose}>
      <div className="bg-white rounded-2xl shadow-xl p-6 w-full max-w-md" onClick={(e) => e.stopPropagation()}>
        <h3 className="font-bold text-lg mb-4">Nova clínica</h3>
        {erro && <div className="bg-red-50 text-red-600 text-sm p-2 rounded mb-3">{erro}</div>}
        <form onSubmit={salvar} className="grid gap-3">
          <input className="border rounded-lg px-3 py-2" placeholder="Nome da clínica" required
            value={f.nomeClinica} onChange={(e) => setF({ ...f, nomeClinica: e.target.value })} />
          <input className="border rounded-lg px-3 py-2" placeholder="Nome do responsável" required
            value={f.nomeDono} onChange={(e) => setF({ ...f, nomeDono: e.target.value })} />
          <input className="border rounded-lg px-3 py-2" placeholder="Email de login do responsável" type="email" required
            value={f.emailDono} onChange={(e) => setF({ ...f, emailDono: e.target.value })} />
          <input className="border rounded-lg px-3 py-2" placeholder="Telefone (opcional)"
            value={f.telefone} onChange={(e) => setF({ ...f, telefone: e.target.value })} />
          <select className="border rounded-lg px-3 py-2" value={f.plano} onChange={(e) => setF({ ...f, plano: e.target.value })}>
            {['trial', 'basico', 'pro', 'enterprise'].map((p) => <option key={p} value={p}>{p}</option>)}
          </select>
          <button className="bg-emerald-600 text-white py-2 rounded-lg">Criar clínica</button>
        </form>
      </div>
    </div>
  )
}

function ModalCredenciais({ dados, onClose }) {
  return (
    <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50 p-4" onClick={onClose}>
      <div className="bg-white rounded-2xl shadow-xl p-6 w-full max-w-md text-center" onClick={(e) => e.stopPropagation()}>
        <div className="text-4xl mb-2">✅</div>
        <h3 className="font-bold text-lg mb-3">Clínica criada!</h3>
        <p className="text-sm text-slate-500 mb-4">Envie estas credenciais ao responsável. A senha é temporária.</p>
        <div className="bg-slate-50 rounded-lg p-4 text-left text-sm space-y-2">
          <div><span className="text-slate-400">Login:</span> <b>{dados.loginEmail}</b></div>
          <div><span className="text-slate-400">Senha temporária:</span> <b className="font-mono">{dados.senhaTemporaria}</b></div>
        </div>
        <button onClick={onClose} className="mt-4 bg-slate-900 text-white px-6 py-2 rounded-lg">Entendi</button>
      </div>
    </div>
  )
}
