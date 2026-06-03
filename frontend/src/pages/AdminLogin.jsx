import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import api from '../api/client'

export default function AdminLogin() {
  const [email, setEmail] = useState('')
  const [senha, setSenha] = useState('')
  const [erro, setErro] = useState('')
  const nav = useNavigate()

  async function entrar(e) {
    e.preventDefault()
    setErro('')
    try {
      const { data } = await api.post('/auth/login-plataforma', { email, senha })
      localStorage.setItem('token', data.token)
      localStorage.setItem('nome', data.nome)
      localStorage.setItem('papel', 'superadmin')
      nav('/admin')
    } catch {
      setErro('Credenciais inválidas')
    }
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-slate-900">
      <form onSubmit={entrar} className="bg-white p-8 rounded-2xl shadow w-80">
        <div className="text-center mb-6">
          <div className="text-3xl mb-1">⚙️</div>
          <h1 className="text-xl font-bold">Painel da Plataforma</h1>
          <p className="text-xs text-slate-400">Administração Ketra</p>
        </div>
        {erro && <div className="bg-red-50 text-red-600 text-sm p-2 rounded mb-3">{erro}</div>}
        <input className="w-full border rounded-lg px-3 py-2 mb-3" placeholder="Email" type="email"
          value={email} onChange={(e) => setEmail(e.target.value)} required />
        <input className="w-full border rounded-lg px-3 py-2 mb-4" placeholder="Senha" type="password"
          value={senha} onChange={(e) => setSenha(e.target.value)} required />
        <button className="w-full bg-slate-900 text-white py-2 rounded-lg hover:bg-slate-800">Entrar</button>
      </form>
    </div>
  )
}
