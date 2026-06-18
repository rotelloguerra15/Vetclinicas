import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import api from '../api/client'

const PLANOS = ['trial', 'basico', 'pro', 'enterprise']
const PLANO_COR = { trial: 'bg-slate-100 text-slate-600', basico: 'bg-blue-100 text-blue-700', pro: 'bg-emerald-100 text-emerald-700', enterprise: 'bg-purple-100 text-purple-700' }

export default function AdminPanel() {
  const nav = useNavigate()
  const [aba, setAba] = useState('clinicas')
  const [clinicas, setClinicas] = useState([])
  const [metricas, setMetricas] = useState(null)
  const [modal, setModal] = useState(false)
  const [criada, setCriada] = useState(null)

  // Config SMTP
  const [smtp, setSmtp] = useState({ host: '', porta: '587', usuario: '', senha: '', ssl: 'true', remetente: '' })
  const [smtpMsg, setSmtpMsg] = useState('')
  const [smtpLoading, setSmtpLoading] = useState(false)

  function carregar() {
    api.get('/admin/clinicas').then((r) => setClinicas(r.data)).catch(() => nav('/admin/login'))
    api.get('/admin/metricas').then((r) => setMetricas(r.data)).catch(() => {})
    api.get('/admin/smtp-config').then((r) => setSmtp(r.data)).catch(() => {})
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
  async function salvarSmtp(e) {
    e.preventDefault()
    setSmtpLoading(true)
    setSmtpMsg('')
    try {
      await api.post('/admin/smtp-config', smtp)
      setSmtpMsg('Salvo com sucesso!')
    } catch {
      setSmtpMsg('Erro ao salvar.')
    } finally {
      setSmtpLoading(false)
    }
  }
  async function testarSmtp() {
    setSmtpLoading(true)
    setSmtpMsg('')
    try {
      await api.post('/admin/smtp-teste')
      setSmtpMsg('Email de teste enviado para ' + smtp.usuario)
    } catch (err) {
      setSmtpMsg('Erro: ' + (err.response?.data?.erro || err.message))
    } finally {
      setSmtpLoading(false)
    }
  }

  function sair() { localStorage.clear(); nav('/admin/login') }

  return (
    <div className="min-h-screen bg-slate-100">
      {/* Header */}
      <div className="bg-slate-900 text-white px-8 py-4 flex justify-between items-center">
        <div>
          <h1 className="text-xl font-bold">Painel da Plataforma</h1>
          <p className="text-xs text-slate-400">Ketra Solucoes Inteligentes</p>
        </div>
        <div className="flex gap-3 items-center">
          <button onClick={() => setModal(true)} className="bg-emerald-600 hover:bg-emerald-700 text-white px-4 py-2 rounded-lg text-sm">+ Nova clinica</button>
          <button onClick={sair} className="bg-slate-700 hover:bg-slate-600 text-white px-4 py-2 rounded-lg text-sm">Sair</button>
        </div>
      </div>

      {/* Abas */}
      <div className="bg-white border-b border-slate-200 px-8">
        <div className="flex gap-6">
          {[['clinicas', 'Clinicas'], ['config', 'Configuracoes']].map(([id, label]) => (
            <button key={id} onClick={() => setAba(id)}
              className={`py-3 text-sm font-medium border-b-2 transition-colors ${aba === id ? 'border-blue-600 text-blue-600' : 'border-transparent text-slate-500 hover:text-slate-700'}`}>
              {label}
            </button>
          ))}
        </div>
      </div>

      <div className="max-w-6xl mx-auto p-8">

        {/* ABA CLINICAS */}
        {aba === 'clinicas' && (
          <>
            {metricas && (
              <div className="grid grid-cols-2 md:grid-cols-4 gap-3 mb-6">
                <Metric titulo="Clinicas" valor={metricas.totalClinicas} />
                <Metric titulo="Ativas" valor={metricas.clinicasAtivas} cor="text-emerald-600" />
                <Metric titulo="Suspensas" valor={metricas.clinicasSuspensas} cor="text-red-600" />
                <Metric titulo="Pets na base" valor={metricas.totalPets} cor="text-blue-600" />
              </div>
            )}

            <div className="bg-white rounded-2xl shadow overflow-hidden">
              <table className="w-full text-sm">
                <thead className="bg-slate-50 text-left">
                  <tr>
                    <th className="p-3">Clinica</th>
                    <th className="p-3">Email</th>
                    <th className="p-3">Plano</th>
                    <th className="p-3">Trial expira</th>
                    <th className="p-3">Status</th>
                    <th className="p-3"></th>
                  </tr>
                </thead>
                <tbody>
                  {clinicas.map((c) => {
                    const expira = c.trialExpiraEm ? new Date(c.trialExpiraEm) : null
                    const diasRestantes = expira ? Math.ceil((expira - new Date()) / (1000 * 60 * 60 * 24)) : null
                    return (
                      <tr key={c.id} className="border-t hover:bg-slate-50">
                        <td className="p-3 font-medium">{c.nome}</td>
                        <td className="p-3 text-slate-500 text-xs">{c.email}</td>
                        <td className="p-3">
                          <select value={c.plano} onChange={(e) => mudarPlano(c, e.target.value)}
                            className={`text-xs px-2 py-1 rounded-full border-0 ${PLANO_COR[c.plano] || ''}`}>
                            {PLANOS.map((p) => <option key={p} value={p}>{p}</option>)}
                          </select>
                        </td>
                        <td className="p-3 text-xs">
                          {expira ? (
                            <span className={diasRestantes <= 3 ? 'text-red-600 font-bold' : diasRestantes <= 7 ? 'text-yellow-600' : 'text-slate-500'}>
                              {expira.toLocaleDateString('pt-BR')} ({diasRestantes}d)
                            </span>
                          ) : <span className="text-slate-300">—</span>}
                        </td>
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
                    )
                  })}
                  {clinicas.length === 0 && <tr><td colSpan="6" className="p-6 text-center text-slate-400">Nenhuma clinica ainda</td></tr>}
                </tbody>
              </table>
            </div>
          </>
        )}

        {/* ABA CONFIGURACOES */}
        {aba === 'config' && (
          <div className="max-w-lg">
            <div className="bg-white rounded-2xl shadow p-6">
              <h2 className="font-bold text-lg mb-1">Configuracoes de E-mail (SMTP)</h2>
              <p className="text-sm text-slate-500 mb-5">
                Usado para envio de boas-vindas, recuperacao de senha e notificacoes do sistema.
              </p>

              <form onSubmit={salvarSmtp} className="space-y-4">
                <div className="grid grid-cols-2 gap-3">
                  <div className="col-span-2">
                    <label className="block text-xs font-medium text-slate-600 mb-1">Servidor SMTP</label>
                    <input value={smtp.host} onChange={e => setSmtp({...smtp, host: e.target.value})}
                      placeholder="smtp.office365.com"
                      className="w-full border rounded-lg px-3 py-2 text-sm" />
                  </div>
                  <div>
                    <label className="block text-xs font-medium text-slate-600 mb-1">Porta</label>
                    <input value={smtp.porta} onChange={e => setSmtp({...smtp, porta: e.target.value})}
                      placeholder="587"
                      className="w-full border rounded-lg px-3 py-2 text-sm" />
                  </div>
                  <div>
                    <label className="block text-xs font-medium text-slate-600 mb-1">SSL</label>
                    <select value={smtp.ssl} onChange={e => setSmtp({...smtp, ssl: e.target.value})}
                      className="w-full border rounded-lg px-3 py-2 text-sm bg-white">
                      <option value="true">Habilitado</option>
                      <option value="false">Desabilitado</option>
                    </select>
                  </div>
                  <div className="col-span-2">
                    <label className="block text-xs font-medium text-slate-600 mb-1">Usuario (email)</label>
                    <input value={smtp.usuario} onChange={e => setSmtp({...smtp, usuario: e.target.value})}
                      placeholder="workflow@ketra.com.br" type="email"
                      className="w-full border rounded-lg px-3 py-2 text-sm" />
                  </div>
                  <div className="col-span-2">
                    <label className="block text-xs font-medium text-slate-600 mb-1">Senha</label>
                    <input value={smtp.senha} onChange={e => setSmtp({...smtp, senha: e.target.value})}
                      placeholder="Senha do email"  type="password"
                      className="w-full border rounded-lg px-3 py-2 text-sm" />
                  </div>
                  <div className="col-span-2">
                    <label className="block text-xs font-medium text-slate-600 mb-1">Remetente (nome exibido)</label>
                    <input value={smtp.remetente} onChange={e => setSmtp({...smtp, remetente: e.target.value})}
                      placeholder="workflow@ketra.com.br"
                      className="w-full border rounded-lg px-3 py-2 text-sm" />
                  </div>
                </div>

                {smtpMsg && (
                  <div className={`text-sm px-3 py-2 rounded-lg ${smtpMsg.includes('Erro') ? 'bg-red-50 text-red-700' : 'bg-green-50 text-green-700'}`}>
                    {smtpMsg}
                  </div>
                )}

                <div className="flex gap-3">
                  <button type="submit" disabled={smtpLoading}
                    className="flex-1 bg-blue-600 hover:bg-blue-700 disabled:bg-blue-400 text-white font-bold py-2 rounded-lg text-sm">
                    {smtpLoading ? 'Salvando...' : 'Salvar'}
                  </button>
                  <button type="button" onClick={testarSmtp} disabled={smtpLoading}
                    className="flex-1 bg-slate-700 hover:bg-slate-600 disabled:bg-slate-400 text-white font-bold py-2 rounded-lg text-sm">
                    Testar envio
                  </button>
                </div>
              </form>
            </div>
          </div>
        )}
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
      setErro(err.response?.data?.erro || 'Erro ao criar clinica')
    }
  }

  return (
    <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50 p-4" onClick={onClose}>
      <div className="bg-white rounded-2xl shadow-xl p-6 w-full max-w-md" onClick={(e) => e.stopPropagation()}>
        <h3 className="font-bold text-lg mb-4">Nova clinica</h3>
        {erro && <div className="bg-red-50 text-red-600 text-sm p-2 rounded mb-3">{erro}</div>}
        <form onSubmit={salvar} className="grid gap-3">
          <input className="border rounded-lg px-3 py-2" placeholder="Nome da clinica" required
            value={f.nomeClinica} onChange={(e) => setF({ ...f, nomeClinica: e.target.value })} />
          <input className="border rounded-lg px-3 py-2" placeholder="Nome do responsavel" required
            value={f.nomeDono} onChange={(e) => setF({ ...f, nomeDono: e.target.value })} />
          <input className="border rounded-lg px-3 py-2" placeholder="Email de login do responsavel" type="email" required
            value={f.emailDono} onChange={(e) => setF({ ...f, emailDono: e.target.value })} />
          <input className="border rounded-lg px-3 py-2" placeholder="Telefone (opcional)"
            value={f.telefone} onChange={(e) => setF({ ...f, telefone: e.target.value })} />
          <select className="border rounded-lg px-3 py-2" value={f.plano} onChange={(e) => setF({ ...f, plano: e.target.value })}>
            {['trial', 'basico', 'pro', 'enterprise'].map((p) => <option key={p} value={p}>{p}</option>)}
          </select>
          <button className="bg-emerald-600 text-white py-2 rounded-lg">Criar clinica</button>
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
        <h3 className="font-bold text-lg mb-3">Clinica criada!</h3>
        <p className="text-sm text-slate-500 mb-4">Envie estas credenciais ao responsavel. A senha e temporaria.</p>
        <div className="bg-slate-50 rounded-lg p-4 text-left text-sm space-y-2">
          <div><span className="text-slate-400">Login:</span> <b>{dados.loginEmail}</b></div>
          <div><span className="text-slate-400">Senha temporaria:</span> <b className="font-mono">{dados.senhaTemporaria}</b></div>
        </div>
        <button onClick={onClose} className="mt-4 bg-slate-900 text-white px-6 py-2 rounded-lg">Entendi</button>
      </div>
    </div>
  )
}
