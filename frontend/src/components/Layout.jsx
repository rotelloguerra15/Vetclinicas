import { useEffect, useState } from 'react'
import { Outlet, NavLink, useNavigate } from 'react-router-dom'
import Logo from './Logo'
import api from '../api/client'

const menu = [
  // Clinica
  { group: 'Clinica' },
  { to: '/',             label: 'Dashboard',          end: true },
  { to: '/agenda',       label: 'Agenda' },
  { to: '/atendimentos', label: 'Atendimentos' },
  { to: '/pets',         label: 'Pets' },
  { to: '/tutores',      label: 'Tutores' },

  // Vendas
  { group: 'Vendas' },
  { to: '/pdv',          label: 'PDV / Caixa',        highlight: true },
  { to: '/servicos',     label: 'Servicos' },
  { to: '/estoque',      label: 'Estoque' },

  // Financeiro
  { group: 'Financeiro' },
  { to: '/financeiro',   label: 'Contas' },
  { to: '/bancario',     label: 'Movim. Bancaria' },

  // RH
  { group: 'RH' },
  { to: '/rh/funcionarios', label: 'Funcionarios' },
  { to: '/rh/fechamento',   label: 'Fechamento' },
  { to: '/rh/relatorios',   label: 'Relatorios RH' },

  // Marketing
  { group: 'Marketing' },
  { to: '/promocoes',    label: 'Promocoes' },
  { to: '/mensagens',    label: 'Mensagens' },

  // Cadastros
  { group: 'Cadastros' },
  { to: '/usuarios',         label: 'Usuarios' },
  { to: '/fornecedores',     label: 'Fornecedores' },
  { to: '/cadastros/vias',   label: 'Vias de Administracao' },

  // Relatorios
  { group: 'Relatorios' },
  { to: '/relatorios',   label: 'Relatorios' },

  // Configuracoes
  { group: 'Configuracoes' },
  { to: '/parametros',      label: 'Parametros' },
  { to: '/parametros/bot',  label: 'Bot WhatsApp' },
]

export default function Layout() {
  const nav  = useNavigate()
  const nome = localStorage.getItem('nome')
  const [branding, setBranding] = useState({ nome: 'Carregando...', tagline: '' })

  useEffect(() => {
    api.get('/tenant/branding')
      .then((r) => setBranding(r.data))
      .catch(() => setBranding({ nome: 'Minha Clinica', tagline: '' }))
  }, [])

  function sair() {
    localStorage.clear()
    nav('/login')
  }

  return (
    <div className="min-h-screen flex">
      <aside className="w-60 bg-slate-900 text-slate-100 p-4 flex flex-col overflow-y-auto">
        <div className="mb-5">
          <div className="flex items-center gap-2 mb-1">
            <Logo size={32} />
            <h1 className="text-sm font-bold leading-tight">{branding.nome}</h1>
          </div>
          {branding.tagline && (
            <p className="text-[10px] text-slate-400 leading-tight">{branding.tagline}</p>
          )}
        </div>

        <nav className="flex flex-col gap-0.5 flex-1">
          {menu.map((item, i) => {
            if ('group' in item) {
              return (
                <div key={`g-${i}`} className={`${i > 0 ? 'mt-3' : ''} mb-1`}>
                  <span className="text-[10px] font-semibold uppercase tracking-widest text-slate-500 px-3">
                    {item.group}
                  </span>
                </div>
              )
            }

            if (item.highlight) {
              return (
                <NavLink key={item.to} to={item.to}
                  className={({ isActive }) =>
                    `px-3 py-2 rounded-lg text-sm font-semibold border ${
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
              <NavLink key={item.to} to={item.to} end={item.end}
                className={({ isActive }) =>
                  `px-3 py-1.5 rounded-lg text-sm ${
                    isActive ? 'bg-slate-700 text-white' : 'text-slate-300 hover:bg-slate-800'
                  }`
                }>
                {item.label}
              </NavLink>
            )
          })}
        </nav>

        <div className="text-xs text-slate-400 mt-4 pt-3 border-t border-slate-700">
          <div className="mb-2 truncate">{nome}</div>
          <button onClick={sair} className="text-red-400 hover:text-red-300">Sair</button>
        </div>
      </aside>

      <main className="flex-1 p-8 overflow-auto bg-slate-50">
        <Outlet />
      </main>
    </div>
  )
}
