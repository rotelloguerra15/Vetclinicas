import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import api from '../api/client'
import Logo from '../components/Logo'
import { PRODUTO_NOME, PRODUTO_TAGLINE, PRODUTO_POR } from '../lib/branding'

export default function Login() {
  const [email, setEmail] = useState('')
  const [senha, setSenha] = useState('')
  const [erro, setErro] = useState('')
  const [loading, setLoading] = useState(false)
  const nav = useNavigate()

  async function entrar(e) {
    e.preventDefault()
    setErro('')
    setLoading(true)
    try {
      const { data } = await api.post('/auth/login', { email, senha })
      localStorage.setItem('token', data.token)
      localStorage.setItem('nome', data.nome)
      localStorage.setItem('papel', data.papel)
      localStorage.setItem('tenantId', data.tenantId)
      if (data.plano) localStorage.setItem('plano', data.plano)
      else localStorage.removeItem('plano')
      if (data.trialExpiraEm) localStorage.setItem('trialExpiraEm', data.trialExpiraEm)
      else localStorage.removeItem('trialExpiraEm')
      nav('/')
    } catch {
      setErro('Email ou senha invalidos')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-slate-100">
      <form onSubmit={entrar} className="bg-white p-8 rounded-2xl shadow w-80">
        <div className="flex flex-col items-center mb-6">
          <Logo size={56} />
          <h1 className="text-xl font-bold mt-3 text-center">{PRODUTO_NOME}</h1>
          <p className="text-xs text-slate-400 text-center">{PRODUTO_TAGLINE}</p>
          <p className="text-[10px] text-slate-300 text-center mt-1">{PRODUTO_POR}</p>
        </div>

        {erro && (
          <div className="bg-red-50 text-red-600 text-sm p-2 rounded mb-3">{erro}</div>
        )}

        <input
          className="w-full border rounded-lg px-3 py-2 mb-3"
          placeholder="Email"
          type="email"
          value={email}
          onChange={e => setEmail(e.target.value)}
          required
        />
        <input
          className="w-full border rounded-lg px-3 py-2 mb-1"
          placeholder="Senha"
          type="password"
          value={senha}
          onChange={e => setSenha(e.target.value)}
          required
        />

        {/* Link esqueci senha */}
        <div className="text-right mb-4">
          <a
            href="/esqueci-senha"
            className="text-xs text-blue-600 hover:underline"
          >
            Esqueceu a senha?
          </a>
        </div>

        <button
          type="submit"
          disabled={loading}
          className="w-full bg-slate-900 text-white py-2 rounded-lg hover:bg-slate-800 disabled:bg-slate-400 transition-colors"
        >
          {loading ? 'Entrando...' : 'Entrar'}
        </button>

        <p className="text-center text-xs text-slate-400 mt-4">
          Nao tem conta?{' '}
          <a href="/trial" className="text-blue-600 hover:underline font-medium">
            Teste gratis 14 dias
          </a>
        </p>
      </form>
    </div>
  )
}
