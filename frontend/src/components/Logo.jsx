import { useState } from 'react'
import { LOGO_URL, EMOJI_FALLBACK } from '../lib/branding'

// Mostra a logo (public/logo.png). Se o arquivo não existir, exibe o emoji.
export default function Logo({ size = 40 }) {
  const [erro, setErro] = useState(false)

  if (erro) {
    return <span style={{ fontSize: size }} role="img" aria-label="logo">{EMOJI_FALLBACK}</span>
  }
  return (
    <img
      src={LOGO_URL}
      alt="Logo"
      onError={() => setErro(true)}
      style={{ height: size, width: 'auto', objectFit: 'contain' }}
    />
  )
}
