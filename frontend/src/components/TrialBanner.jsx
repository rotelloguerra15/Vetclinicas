import { useEffect, useState } from 'react'

const DISMISS_KEY = 'trialBannerDismissedAt'

export default function TrialBanner() {
  const [agora, setAgora] = useState(() => new Date())
  const [escondido, setEscondido] = useState(
    () => sessionStorage.getItem(DISMISS_KEY) === 'true'
  )

  // Recalcula a cada hora — nao precisa de mais frequencia que isso
  useEffect(() => {
    const id = setInterval(() => setAgora(new Date()), 60 * 60 * 1000)
    return () => clearInterval(id)
  }, [])

  const plano = localStorage.getItem('plano')
  const trialExpiraEmRaw = localStorage.getItem('trialExpiraEm')

  if (plano !== 'trial' || !trialExpiraEmRaw || escondido) return null

  const expiraEm = new Date(trialExpiraEmRaw)
  const diasRestantes = Math.ceil((expiraEm - agora) / (1000 * 60 * 60 * 24))

  // So aparece quando faltam 7 dias ou menos (ou ja expirou)
  if (diasRestantes > 7) return null

  const expirado = diasRestantes <= 0

  function fechar() {
    sessionStorage.setItem(DISMISS_KEY, 'true')
    setEscondido(true)
  }

  const estilos = expirado
    ? 'bg-red-600 text-white'
    : diasRestantes <= 3
      ? 'bg-orange-500 text-white'
      : 'bg-amber-400 text-slate-900'

  const mensagem = expirado
    ? 'Seu periodo de teste expirou.'
    : diasRestantes === 1
      ? 'Seu periodo de teste termina amanha.'
      : `Seu periodo de teste termina em ${diasRestantes} dias.`

  return (
    <div className={`${estilos} px-4 py-2.5 flex items-center justify-between gap-3 text-sm font-medium`}>
      <span className="truncate">
        {mensagem} Escolha um plano para continuar usando o sistema sem interrupcoes.
      </span>
      <div className="flex items-center gap-3 flex-shrink-0">
        <a
          href="/planos"
          className="bg-white/90 hover:bg-white text-slate-900 px-3 py-1 rounded-md font-semibold whitespace-nowrap transition-colors"
        >
          Ver planos
        </a>
        {!expirado && (
          <button
            onClick={fechar}
            aria-label="Fechar aviso"
            className="opacity-70 hover:opacity-100 transition-opacity"
          >
            ✕
          </button>
        )}
      </div>
    </div>
  )
}
