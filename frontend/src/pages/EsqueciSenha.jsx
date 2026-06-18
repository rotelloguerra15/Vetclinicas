import { useState } from 'react'
import api from '../api/client'

export default function EsqueciSenha() {
  const [step, setStep] = useState('form') // form | enviado | redefinir | ok | erro
  const [email, setEmail] = useState('')
  const [novaSenha, setNovaSenha] = useState('')
  const [confirma, setConfirma] = useState('')
  const [loading, setLoading] = useState(false)
  const [msg, setMsg] = useState('')

  // Pega token da URL se vier via link do email
  const params  = new URLSearchParams(window.location.search)
  const tokenUrl = params.get('token')

  // Se vier com token, vai direto para redefinir
  const stepInicial = tokenUrl ? 'redefinir' : 'form'
  const [currentStep, setCurrentStep] = useState(stepInicial)

  async function handleEsqueciSenha(e) {
    e.preventDefault()
    if (!email.trim()) { setMsg('Informe o e-mail.'); return }
    setLoading(true)
    setMsg('')
    try {
      await api.post('/auth/esqueci-senha', { email: email.trim().toLowerCase() })
      setCurrentStep('enviado')
    } catch {
      setMsg('Erro ao processar. Tente novamente.')
    } finally {
      setLoading(false)
    }
  }

  async function handleRedefinir(e) {
    e.preventDefault()
    if (novaSenha.length < 6) { setMsg('A senha deve ter pelo menos 6 caracteres.'); return }
    if (novaSenha !== confirma) { setMsg('As senhas nao conferem.'); return }
    setLoading(true)
    setMsg('')
    try {
      await api.post('/auth/redefinir-senha', { token: tokenUrl, novaSenha })
      setCurrentStep('ok')
    } catch (err) {
      const erro = err.response?.data?.erro || 'Link invalido ou expirado.'
      setMsg(erro)
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="min-h-screen bg-slate-100 flex items-center justify-center p-4">
      <div className="bg-white rounded-2xl shadow-lg max-w-sm w-full p-8">

        {/* Logo */}
        <div className="text-center mb-6">
          <div className="text-4xl mb-2">🐾</div>
          <h1 className="text-xl font-bold text-slate-800">VetClinica</h1>
        </div>

        {/* STEP: form - solicitar email */}
        {currentStep === 'form' && (
          <>
            <h2 className="text-lg font-semibold text-slate-700 mb-1">Esqueceu sua senha?</h2>
            <p className="text-sm text-slate-500 mb-5">
              Informe o e-mail cadastrado. Enviaremos um link para redefinir a senha por e-mail e WhatsApp.
            </p>
            <form onSubmit={handleEsqueciSenha} className="space-y-4">
              <input
                type="email"
                value={email}
                onChange={e => setEmail(e.target.value)}
                placeholder="seu@email.com.br"
                className="w-full border border-slate-300 rounded-lg px-3 py-2.5 text-sm focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none"
                required
              />
              {msg && <p className="text-sm text-red-600">{msg}</p>}
              <button
                type="submit"
                disabled={loading}
                className="w-full bg-blue-600 hover:bg-blue-700 disabled:bg-blue-400 text-white font-bold py-2.5 rounded-xl transition-colors"
              >
                {loading ? 'Enviando...' : 'Enviar link de redefinicao'}
              </button>
            </form>
          </>
        )}

        {/* STEP: enviado */}
        {currentStep === 'enviado' && (
          <div className="text-center">
            <div className="text-5xl mb-4">📬</div>
            <h2 className="text-lg font-bold text-slate-800 mb-2">Link enviado!</h2>
            <p className="text-sm text-slate-500 mb-4">
              Se o e-mail <strong>{email}</strong> estiver cadastrado, voce recebera
              as instrucoes por <strong>e-mail</strong> e <strong>WhatsApp</strong>.
            </p>
            <p className="text-xs text-slate-400 mb-6">O link expira em 30 minutos.</p>
            <p className="text-sm text-slate-500">
              Nao recebeu?{' '}
              <button
                onClick={() => { setCurrentStep('form'); setMsg('') }}
                className="text-blue-600 hover:underline font-medium"
              >
                Tentar novamente
              </button>
            </p>
          </div>
        )}

        {/* STEP: redefinir - formulario de nova senha */}
        {currentStep === 'redefinir' && (
          <>
            <h2 className="text-lg font-semibold text-slate-700 mb-1">Criar nova senha</h2>
            <p className="text-sm text-slate-500 mb-5">Minimo 6 caracteres.</p>
            <form onSubmit={handleRedefinir} className="space-y-4">
              <input
                type="password"
                value={novaSenha}
                onChange={e => setNovaSenha(e.target.value)}
                placeholder="Nova senha"
                className="w-full border border-slate-300 rounded-lg px-3 py-2.5 text-sm focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none"
                required
              />
              <input
                type="password"
                value={confirma}
                onChange={e => setConfirma(e.target.value)}
                placeholder="Confirmar nova senha"
                className="w-full border border-slate-300 rounded-lg px-3 py-2.5 text-sm focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none"
                required
              />
              {msg && (
                <div className="bg-red-50 border border-red-200 text-red-700 px-3 py-2 rounded-lg text-sm">
                  {msg}
                </div>
              )}
              {/* Forca visual de senha */}
              {novaSenha.length > 0 && (
                <div className="space-y-1">
                  <div className="flex gap-1">
                    {[1, 2, 3, 4].map(n => (
                      <div
                        key={n}
                        className={`h-1 flex-1 rounded-full transition-colors ${
                          novaSenha.length >= n * 3
                            ? n <= 1 ? 'bg-red-400' : n <= 2 ? 'bg-yellow-400' : n <= 3 ? 'bg-blue-400' : 'bg-green-500'
                            : 'bg-slate-200'
                        }`}
                      />
                    ))}
                  </div>
                  <p className="text-xs text-slate-400">
                    {novaSenha.length < 6 ? 'Muito curta' : novaSenha.length < 8 ? 'Fraca' : novaSenha.length < 10 ? 'Boa' : 'Forte'}
                  </p>
                </div>
              )}
              <button
                type="submit"
                disabled={loading}
                className="w-full bg-green-600 hover:bg-green-700 disabled:bg-green-400 text-white font-bold py-2.5 rounded-xl transition-colors"
              >
                {loading ? 'Salvando...' : 'Salvar nova senha'}
              </button>
            </form>
          </>
        )}

        {/* STEP: ok - sucesso */}
        {currentStep === 'ok' && (
          <div className="text-center">
            <div className="text-5xl mb-4">✅</div>
            <h2 className="text-lg font-bold text-slate-800 mb-2">Senha redefinida!</h2>
            <p className="text-sm text-slate-500 mb-6">Agora voce ja pode fazer o login com a nova senha.</p>
            <a
              href="/login"
              className="block w-full bg-blue-600 hover:bg-blue-700 text-white text-center font-bold py-2.5 rounded-xl transition-colors"
            >
              Ir para o Login
            </a>
          </div>
        )}

        {/* Link voltar */}
        {currentStep !== 'redefinir' && currentStep !== 'ok' && (
          <p className="text-center text-sm text-slate-400 mt-5">
            <a href="/login" className="hover:underline text-slate-500">← Voltar ao login</a>
          </p>
        )}
      </div>
    </div>
  )
}
