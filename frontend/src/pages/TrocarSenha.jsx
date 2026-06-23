import { useState } from 'react'
import api from '../api/client'

export default function TrocarSenha() {
  const [senhaAtual, setSenhaAtual] = useState('')
  const [novaSenha, setNovaSenha] = useState('')
  const [confirmaSenha, setConfirmaSenha] = useState('')
  const [msg, setMsg] = useState('')
  const [erro, setErro] = useState('')
  const [loading, setLoading] = useState(false)

  async function salvar(e) {
    e.preventDefault()
    setMsg(''); setErro('')

    if (novaSenha !== confirmaSenha) {
      setErro('A confirmacao nao bate com a nova senha.')
      return
    }
    if (novaSenha.length < 6) {
      setErro('A nova senha deve ter pelo menos 6 caracteres.')
      return
    }

    setLoading(true)
    try {
      await api.post('/conta/trocar-senha', { senhaAtual, novaSenha })
      setMsg('Senha alterada com sucesso! Enviamos uma confirmacao para o seu email.')
      setSenhaAtual(''); setNovaSenha(''); setConfirmaSenha('')
    } catch (err) {
      setErro(err.response?.data?.erro || 'Erro ao trocar a senha.')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="max-w-md mx-auto py-8 px-4">
      <div className="bg-white rounded-2xl shadow p-6">
        <h1 className="font-bold text-lg mb-1">Trocar senha</h1>
        <p className="text-sm text-slate-500 mb-5">Informe sua senha atual e a nova senha desejada.</p>

        <form onSubmit={salvar} className="space-y-4">
          <div>
            <label className="block text-xs font-medium text-slate-600 mb-1">Senha atual</label>
            <input type="password" value={senhaAtual} onChange={e => setSenhaAtual(e.target.value)}
              required className="w-full border rounded-lg px-3 py-2 text-sm" />
          </div>
          <div>
            <label className="block text-xs font-medium text-slate-600 mb-1">Nova senha</label>
            <input type="password" value={novaSenha} onChange={e => setNovaSenha(e.target.value)}
              required minLength={6} className="w-full border rounded-lg px-3 py-2 text-sm" />
          </div>
          <div>
            <label className="block text-xs font-medium text-slate-600 mb-1">Confirmar nova senha</label>
            <input type="password" value={confirmaSenha} onChange={e => setConfirmaSenha(e.target.value)}
              required minLength={6} className="w-full border rounded-lg px-3 py-2 text-sm" />
          </div>

          {erro && <div className="text-sm px-3 py-2 rounded-lg bg-red-50 text-red-700">{erro}</div>}
          {msg && <div className="text-sm px-3 py-2 rounded-lg bg-green-50 text-green-700">{msg}</div>}

          <button type="submit" disabled={loading}
            className="w-full bg-blue-600 hover:bg-blue-700 disabled:bg-blue-400 text-white font-bold py-2 rounded-lg text-sm">
            {loading ? 'Salvando...' : 'Salvar nova senha'}
          </button>
        </form>
      </div>
    </div>
  )
}
