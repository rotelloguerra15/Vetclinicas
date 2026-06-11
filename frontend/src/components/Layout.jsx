import { useEffect, useState } from 'react'
import { Outlet, NavLink, useNavigate, useLocation } from 'react-router-dom'
import Logo from './Logo'
import api from '../api/client'

const menu = [
  { group: 'Dashboards' },
  { to: '/',              label: 'Clinica',      end: true },
  { to: '/relatorios',    label: 'Financeiro BI' },
  { to: '/rh/relatorios',      label: 'RH' },
  { to: '/dashboard/vendas',   label: 'Vendas' },

  { group: 'Clinica' },

  { to: '/agenda',       label: 'Agenda' },
  { to: '/atendimentos', label: 'Atendimentos' },
  { to: '/pets',         label: 'Pets' },
  { to: '/tutores',      label: 'Tutores' },
  { to: '/planos-saude', label: 'Planos de Saude' },

  { group: 'Vendas' },
  { to: '/pdv',          label: 'PDV / Caixa',              highlight: true },
  { to: '/servicos',     label: 'Servicos' },

  { group: 'Estoque' },
  { to: '/estoque',      label: 'Controle de Estoque' },
  { to: '/recebimentos', label: 'Recebimento de Mercadoria' },

  { group: 'Compras' },
  { to: '/compras',           label: 'Pedidos de Compra' },
  { to: '/compras/condicoes', label: 'Cond. de Pagamento' },
  { to: '/fornecedores',      label: 'Fornecedores' },

  { group: 'Financeiro' },
  { to: '/financeiro',   label: 'Contas' },
  { to: '/bancario',                label: 'Movim. Bancaria' },
  { to: '/financeiro/categorias',   label: 'Categorias' },

  { group: 'RH' },
  { to: '/rh/funcionarios', label: 'Funcionarios' },
  { to: '/rh/cargos',        label: 'Cargos' },
  { to: '/rh/fechamento',   label: 'Fechamento' },


  { group: 'Marketing' },
  { to: '/promocoes',    label: 'Promocoes' },
  { to: '/mensagens',    label: 'Mensagens' },

  { group: 'Cadastros' },
  { to: '/usuarios',         label: 'Usuarios' },
  { to: '/cadastros/vias',     label: 'Vias de Administracao' },
  { to: '/cadastros/pelagens', label: 'Pelagens' },



  { group: 'Contabil' },
  { to: '/contabil', label: 'Fechamento Contabil' },

  { group: 'Configuracoes' },
  { to: '/parametros',      label: 'Parametros' },
  { to: '/parametros/bot',  label: 'Bot WhatsApp' },

  { group: 'Gestao a Vista' },
  { to: '/gestao-vista', label: 'Gestao a Vista', tv: true },
]

function NavContent({ onClose }) {
  const nav = useNavigate()

  function sair() {
    localStorage.clear()
    nav('/login')
    onClose?.()
  }

  const nome     = localStorage.getItem('nome')
  const [branding, setBranding] = useState({ nome: '', tagline: '' })

  useEffect(() => {
    api.get('/tenant/branding')
      .then(r => setBranding(r.data))
      .catch(() => setBranding({ nome: 'Minha Clinica', tagline: '' }))
  }, [])

  return (
    <div className="flex flex-col h-full">
      {/* Logo */}
      <div className="p-4 mb-2 border-b border-slate-800">
        <div className="flex items-center gap-2">
          <Logo size={30} />
          <div className="min-w-0">
            <h1 className="text-sm font-bold leading-tight truncate">{branding.nome || 'VetClinica'}</h1>
            {branding.tagline && <p className="text-[10px] text-slate-400 leading-tight truncate">{branding.tagline}</p>}
          </div>
        </div>
      </div>

      {/* Menu */}
      <nav className="flex-1 overflow-y-auto px-2 py-1 space-y-0.5">
        {menu.map((item, i) => {
          if ('group' in item) {
            return (
              <div key={`g-${i}`} className={`${i > 0 ? 'pt-3' : 'pt-1'} pb-1`}>
                <span className="text-[10px] font-semibold uppercase tracking-widest text-slate-500 px-2">
                  {item.group}
                </span>
              </div>
            )
          }

          if (item.tv) {
            return (
              <a key={item.to} href={item.to} target="_blank" rel="noopener noreferrer"
                onClick={onClose}
                className="flex items-center gap-2 px-3 py-2 rounded-lg text-sm text-sky-300 hover:bg-slate-800">
                <span className="text-base">TV</span>
                <span>{item.label}</span>
              </a>
            )
          }

          if (item.highlight) {
            return (
              <NavLink key={item.to} to={item.to} onClick={onClose}
                className={({ isActive }) =>
                  `flex items-center px-3 py-2 rounded-lg text-sm font-semibold border ${
                    isActive
                      ? 'bg-emerald-600 border-emerald-500 text-white'
                      : 'bg-emerald-700/30 border-emerald-700/50 text-emerald-300 hover:bg-emerald-700/50'
                  }`
                }>
                {item.label}
              </NavLink>
            )
          }

          return (
            <NavLink key={item.to} to={item.to} end={item.end} onClick={onClose}
              className={({ isActive }) =>
                `flex items-center px-3 py-2 rounded-lg text-sm ${
                  isActive ? 'bg-slate-700 text-white' : 'text-slate-300 hover:bg-slate-800'
                }`
              }>
              {item.label}
            </NavLink>
          )
        })}
      </nav>

      {/* Footer */}
      <div className="p-4 border-t border-slate-800 text-xs text-slate-400">
        <div className="truncate mb-2">{nome}</div>
        <button onClick={sair} className="text-red-400 hover:text-red-300">Sair</button>
      </div>
    </div>
  )
}

export default function Layout() {
  const [menuAberto, setMenuAberto] = useState(false)
  const location = useLocation()

  // Fecha menu ao navegar
  useEffect(() => { setMenuAberto(false) }, [location.pathname])

  // Bloqueia scroll do body quando menu aberto no mobile
  useEffect(() => {
    if (menuAberto) document.body.style.overflow = 'hidden'
    else document.body.style.overflow = ''
    return () => { document.body.style.overflow = '' }
  }, [menuAberto])

  return (
    <div className="min-h-screen flex bg-slate-50">

      {/* Sidebar desktop — sempre visivel em lg+ */}
      <aside className="hidden lg:flex w-60 flex-shrink-0 bg-slate-900 text-slate-100 flex-col h-screen sticky top-0 overflow-hidden">
        <NavContent />
      </aside>

      {/* Overlay mobile */}
      {menuAberto && (
        <div className="fixed inset-0 z-40 lg:hidden">
          {/* Fundo escuro */}
          <div className="absolute inset-0 bg-black/50" onClick={() => setMenuAberto(false)} />
          {/* Drawer */}
          <aside className="absolute left-0 top-0 bottom-0 w-72 bg-slate-900 text-slate-100 flex flex-col z-50 shadow-2xl">
            <NavContent onClose={() => setMenuAberto(false)} />
          </aside>
        </div>
      )}

      {/* Conteudo principal */}
      <div className="flex-1 flex flex-col min-w-0">

        {/* Topbar mobile/tablet */}
        <header className="lg:hidden sticky top-0 z-30 bg-white border-b border-slate-200 px-4 py-3 flex items-center gap-3 shadow-sm">
          <button onClick={() => setMenuAberto(true)}
            className="p-2 rounded-lg text-slate-600 hover:bg-slate-100 transition">
            <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 6h16M4 12h16M4 18h16" />
            </svg>
          </button>
          <span className="font-semibold text-slate-700 text-sm truncate">
            {localStorage.getItem('clinica') || 'VetClinica'}
          </span>
        </header>

        {/* Pagina */}
        <main className="flex-1 p-4 md:p-6 lg:p-8 overflow-auto">
          <Outlet />
        </main>
      </div>
    </div>
  )
}
