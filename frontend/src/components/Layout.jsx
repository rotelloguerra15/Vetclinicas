import { useEffect, useState } from 'react'
import { Outlet, NavLink, useNavigate } from 'react-router-dom'
import Logo from './Logo'
import api from '../api/client'

const links = [
  { to: '/', label: 'Dashboard', end: true },
  { to: '/agenda', label: 'Agenda' },
  { to: '/atendimentos', label: 'Atendimentos' },
  { to: '/pets', label: 'Pets' },
  { to: '/tutores', label: 'Tutores' },
  null,
  { to: '/servicos', label: 'Serviços e Valores' },
  { to: '/pdv', label: 'PDV / Vendas' },
  { to: '/estoque', label: 'Estoque' },
  { to: '/financeiro', label: 'Financeiro' },
  { to: '/bancario', label: 'Movim. Bancária' },
  null,
  { to: '/usuarios', label: 'Usuários' },
  { to: '/promocoes', label: 'Promoções' },
  { to: '/relatorios', label: 'Relatórios' },
  { to: '/mensagens', label: 'Mensagens' },
  null,
  { to: '/rh/funcionarios', label: 'RH — Funcionários' },
  { to: '/rh/fechamento', label: 'RH — Fechamento' },
  { to: '/rh/relatorios', label: 'RH — Relatórios' },
  null,
  { to: '/cadastros/vias', label: 'Cadastros — Vias' },
  null,
  { to: '/parametros', label: '⚙️ Parâmetros' },
  { to: '/parametros/bot', label: '🤖 Bot WhatsApp' },
]

export default function Layout() {
  const nav = useNavigate()
  const nome = localStorage.getItem('nome')
  const [branding, setBranding] = useState({ nome: 'Carregando...', tagline: '' })

  useEffect(() => {
    api.get('/tenant/branding')
      .then((r) => setBranding(r.data))
      .catch(() => setBranding({ nome: 'Minha Clínica', tagline: '' }))
  }, [])

  function sair() {
    localStorage.clear()
    nav('/login')
  }

  return (
    <div className="min-h-screen flex">
      <aside className="w-60 bg-slate-900 text-slate-100 p-4 flex flex-col">
        <div className="mb-6">
          <div className="flex items-center gap-2 mb-1">
            <Logo size={32} />
            <h1 className="text-base font-bold leading-tight">{branding.nome}</h1>
          </div>
          {branding.tagline && <p className="text-[10px] text-slate-400 leading-tight">{branding.tagline}</p>}
        </div>
        <nav className="flex flex-col gap-1 flex-1">
          {links.map((l, i) => l === null ? (
            <div key={`sep-${i}`} className="border-t border-slate-700 my-1"></div>
          ) : (
            <NavLink
              key={l.to}
              to={l.to}
              end={l.end}
              className={({ isActive }) =>
                `px-3 py-2 rounded-lg text-sm ${isActive ? 'bg-slate-700' : 'hover:bg-slate-800'}`
              }
            >
              {l.label}
            </NavLink>
          ))}
        </nav>
        <div className="text-xs text-slate-400 mt-4">
          <div className="mb-2">{nome}</div>
          <button onClick={sair} className="text-red-400 hover:text-red-300">Sair</button>
        </div>
      </aside>
      <main className="flex-1 p-8 overflow-auto">
        <Outlet />
      </main>
    </div>
  )
}
