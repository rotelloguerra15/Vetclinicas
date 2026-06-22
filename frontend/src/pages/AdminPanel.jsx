import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import api from '../api/client'

const PLANOS = ['trial', 'starter', 'profissional', 'enterprise']
const PLANO_COR = { trial: 'bg-slate-100 text-slate-600', starter: 'bg-blue-100 text-blue-700', profissional: 'bg-emerald-100 text-emerald-700', enterprise: 'bg-purple-100 text-purple-700' }

export default function AdminPanel() {
  const nav = useNavigate()
  const [aba, setAba] = useState('clinicas')
  const [clinicas, setClinicas] = useState([])
  const [metricas, setMetricas] = useState(null)
  const [modal, setModal] = useState(false)
  const [criada, setCriada] = useState(null)

  // Config de E-mail (provider + Resend + Graph + SMTP)
  const [smtp, setSmtp] = useState({
    provider: 'resend', resendApiKey: '', resendRemetente: '', resendConfigurado: false,
    graphTenantId: '', graphClientId: '', graphClientSecret: '', graphRemetente: '', graphConfigurado: false,
    host: '', porta: '587', usuario: '', senha: '', ssl: 'true', remetente: ''
  })
  const [smtpMsg, setSmtpMsg] = useState('')
  const [smtpLoading, setSmtpLoading] = useState(false)
  const [emailTeste, setEmailTeste] = useState('')

  // Config da assinatura SaaS (Asaas — conta da Ketra)
  const [asaasSaas, setAsaasSaas] = useState({
    apiKey: '', apiKeyConfigurada: false, ambiente: 'sandbox', webhookToken: '', webhookConfigurado: false
  })
  const [asaasSaasMsg, setAsaasSaasMsg] = useState('')
  const [asaasSaasLoading, setAsaasSaasLoading] = useState(false)

  function carregar() {
    api.get('/admin/clinicas').then((r) => setClinicas(r.data)).catch(() => nav('/admin/login'))
    api.get('/admin/metricas').then((r) => setMetricas(r.data)).catch(() => {})
    api.get('/admin/smtp-config').then((r) => setSmtp(r.data)).catch(() => {})
    api.get('/admin/asaas-saas-config').then((r) => setAsaasSaas(r.data)).catch(() => {})
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
  async function resetarSenha(c) {
    if (!confirm(`Gerar uma nova senha temporaria para o usuario principal de "${c.nome}"? A senha atual deixa de funcionar.`)) return
    try {
      const { data } = await api.post(`/admin/clinicas/${c.id}/resetar-senha`)
      setCriada({ ...data, _contexto: 'reset' })
    } catch (err) {
      alert(err.response?.data?.erro || 'Erro ao resetar a senha.')
    }
  }
  async function atualizarPagamento(c, campo, valor) {
    const corpo = {
      statusPagamento: campo === 'statusPagamento' ? valor : (c.statusPagamento || null),
      proximoFaturamento: campo === 'proximoFaturamento' ? (valor || null) : (c.proximoFaturamento || null)
    }
    await api.put(`/admin/clinicas/${c.id}/pagamento`, corpo)
    carregar()
  }
  async function salvarAsaasSaas(e) {
    e.preventDefault()
    setAsaasSaasLoading(true)
    setAsaasSaasMsg('')
    try {
      await api.post('/admin/asaas-saas-config', asaasSaas)
      setAsaasSaasMsg('Salvo!')
      carregar()
    } catch {
      setAsaasSaasMsg('Erro ao salvar.')
    } finally {
      setAsaasSaasLoading(false)
    }
  }
  async function gerarAssinatura(c) {
    if (c.plano !== 'starter' && c.plano !== 'profissional') {
      alert('Selecione o plano "starter" ou "profissional" no dropdown de Plano antes de gerar a assinatura. Enterprise é negociado direto.')
      return
    }
    if (!confirm(`Gerar cobranca recorrente (plano ${c.plano}) para "${c.nome}" na Asaas?`)) return
    try {
      const { data } = await api.post(`/admin/clinicas/${c.id}/gerar-assinatura`, { planoId: c.plano })
      if (data.invoiceUrl) {
        prompt('Link de pagamento gerado — copie e envie para a clinica:', data.invoiceUrl)
      } else {
        alert('Assinatura criada na Asaas, mas o link de pagamento ainda nao ficou disponivel (o Asaas gera de forma assincrona). Confira em alguns minutos direto no painel do Asaas.')
      }
      carregar()
    } catch (err) {
      alert(err.response?.data?.erro || 'Erro ao gerar assinatura.')
    }
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
      const dest = emailTeste || smtp.usuario
      await api.post('/admin/smtp-teste?destino=' + encodeURIComponent(dest))
      setSmtpMsg('Email de teste enviado para ' + dest)
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
                    <th className="p-3">Pagamento</th>
                    <th className="p-3">Proximo faturamento</th>
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
                          <select
                            value={c.statusPagamento || ''}
                            onChange={(e) => atualizarPagamento(c, 'statusPagamento', e.target.value || null)}
                            className={`text-xs px-2 py-1 rounded-full border-0 ${
                              c.statusPagamento === 'pago' ? 'bg-emerald-100 text-emerald-700'
                              : c.statusPagamento === 'atraso' ? 'bg-red-100 text-red-700'
                              : 'bg-slate-100 text-slate-500'
                            }`}>
                            <option value="">—</option>
                            <option value="pago">Pago</option>
                            <option value="atraso">Em atraso</option>
                          </select>
                        </td>
                        <td className="p-3">
                          <input
                            type="date"
                            value={c.proximoFaturamento ? c.proximoFaturamento.slice(0, 10) : ''}
                            onChange={(e) => atualizarPagamento(c, 'proximoFaturamento', e.target.value)}
                            className="text-xs border rounded-md px-2 py-1 text-slate-600"
                          />
                        </td>
                        <td className="p-3">
                          {c.suspenso
                            ? <span className="text-xs px-2 py-1 rounded-full bg-red-100 text-red-700">Suspensa</span>
                            : <span className="text-xs px-2 py-1 rounded-full bg-emerald-100 text-emerald-700">Ativa</span>}
                        </td>
                        <td className="p-3 text-right space-x-3 whitespace-nowrap">
                          <button onClick={() => gerarAssinatura(c)} className="text-xs text-emerald-600 hover:underline">
                            Gerar assinatura
                          </button>
                          <button onClick={() => resetarSenha(c)} className="text-xs text-slate-500 hover:underline">
                            Resetar senha
                          </button>
                          <button onClick={() => suspender(c)} className="text-xs text-blue-600 hover:underline">
                            {c.suspenso ? 'Reativar' : 'Suspender'}
                          </button>
                        </td>
                      </tr>
                    )
                  })}
                  {clinicas.length === 0 && <tr><td colSpan="8" className="p-6 text-center text-slate-400">Nenhuma clinica ainda</td></tr>}
                </tbody>
              </table>
            </div>
            <p className="text-xs text-slate-400 mt-2">
              "Pagamento" e "Proximo faturamento" sao manuais por enquanto (cobranca recorrente via Asaas ainda nao integrada — backlog item 7).
            </p>
          </>
        )}

        {/* ABA CONFIGURACOES */}
        {aba === 'config' && (
          <div className="max-w-lg">
            <div className="bg-white rounded-2xl shadow p-6">
              <h2 className="font-bold text-lg mb-1">Configuracoes de E-mail</h2>
              <p className="text-sm text-slate-500 mb-5">
                Usado para envio de boas-vindas, recuperacao de senha e notificacoes do sistema.
              </p>

              <form onSubmit={salvarSmtp} className="space-y-4">
                <div>
                  <label className="block text-xs font-medium text-slate-600 mb-1">Provedor de envio</label>
                  <select value={smtp.provider} onChange={e => setSmtp({...smtp, provider: e.target.value})}
                    className="w-full border rounded-lg px-3 py-2 text-sm bg-white">
                    <option value="resend">Resend (HTTP/API - recomendado p/ Railway)</option>
                    <option value="graph">Microsoft 365 / Office 365 (Graph API - HTTP)</option>
                    <option value="smtp">SMTP (apenas Railway Pro ou ambiente local)</option>
                  </select>
                  {smtp.provider === 'smtp' && (
                    <p className="text-xs text-amber-600 mt-1">
                      Atencao: o Railway bloqueia portas SMTP (25/465/587) nos planos Free/Trial/Hobby. Use Resend ou Microsoft 365.
                    </p>
                  )}
                  {smtp.provider === 'graph' && (
                    <p className="text-xs text-blue-600 mt-1">
                      Usa sua caixa do Office 365 (ex: workflow@ketra.com.br) via HTTPS. Funciona no Railway Hobby, sem custo extra.
                    </p>
                  )}
                </div>

                {smtp.provider === 'resend' && (
                  <div className="space-y-3 border border-blue-100 bg-blue-50/40 rounded-xl p-3">
                    <div>
                      <label className="block text-xs font-medium text-slate-600 mb-1">
                        Resend API Key {smtp.resendConfigurado && <span className="text-green-600">(ja salva)</span>}
                      </label>
                      <input value={smtp.resendApiKey} onChange={e => setSmtp({...smtp, resendApiKey: e.target.value})}
                        placeholder={smtp.resendConfigurado ? 'Deixe vazio para manter a atual' : 're_xxxxxxxxxxxx'}
                        type="password"
                        className="w-full border rounded-lg px-3 py-2 text-sm" />
                    </div>
                    <div>
                      <label className="block text-xs font-medium text-slate-600 mb-1">Remetente</label>
                      <input value={smtp.resendRemetente} onChange={e => setSmtp({...smtp, resendRemetente: e.target.value})}
                        placeholder="VetClinica by Ketra <nao-responda@ketra.com.br>"
                        className="w-full border rounded-lg px-3 py-2 text-sm" />
                      <p className="text-xs text-slate-400 mt-1">
                        Formato: Nome &lt;email@dominio&gt;. Precisa de dominio verificado no Resend.
                        Para teste rapido use: VetClinica &lt;onboarding@resend.dev&gt; (so envia para o email da sua conta Resend).
                      </p>
                    </div>
                  </div>
                )}

                {smtp.provider === 'graph' && (
                  <div className="space-y-3 border border-blue-100 bg-blue-50/40 rounded-xl p-3">
                    <div>
                      <label className="block text-xs font-medium text-slate-600 mb-1">Directory (tenant) ID</label>
                      <input value={smtp.graphTenantId} onChange={e => setSmtp({...smtp, graphTenantId: e.target.value})}
                        placeholder="ex: 00000000-0000-0000-0000-000000000000"
                        className="w-full border rounded-lg px-3 py-2 text-sm" />
                    </div>
                    <div>
                      <label className="block text-xs font-medium text-slate-600 mb-1">Application (client) ID</label>
                      <input value={smtp.graphClientId} onChange={e => setSmtp({...smtp, graphClientId: e.target.value})}
                        placeholder="ex: 00000000-0000-0000-0000-000000000000"
                        className="w-full border rounded-lg px-3 py-2 text-sm" />
                    </div>
                    <div>
                      <label className="block text-xs font-medium text-slate-600 mb-1">
                        Client Secret {smtp.graphConfigurado && <span className="text-green-600">(ja salvo)</span>}
                      </label>
                      <input value={smtp.graphClientSecret} onChange={e => setSmtp({...smtp, graphClientSecret: e.target.value})}
                        placeholder={smtp.graphConfigurado ? 'Deixe vazio para manter o atual' : 'valor do secret do Azure'}
                        type="password"
                        className="w-full border rounded-lg px-3 py-2 text-sm" />
                    </div>
                    <div>
                      <label className="block text-xs font-medium text-slate-600 mb-1">Remetente (caixa do Office 365)</label>
                      <input value={smtp.graphRemetente} onChange={e => setSmtp({...smtp, graphRemetente: e.target.value})}
                        placeholder="workflow@ketra.com.br" type="email"
                        className="w-full border rounded-lg px-3 py-2 text-sm" />
                      <p className="text-xs text-slate-400 mt-1">
                        Caixa de email que vai enviar. O app no Azure precisa da permissao Mail.Send (consentida pelo admin).
                      </p>
                    </div>
                  </div>
                )}

                {smtp.provider === 'smtp' && (
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
                )}

                {smtpMsg && (
                  <div className={`text-sm px-3 py-2 rounded-lg ${smtpMsg.includes('Erro') ? 'bg-red-50 text-red-700' : 'bg-green-50 text-green-700'}`}>
                    {smtpMsg}
                  </div>
                )}

                <div>
                  <label className="block text-xs font-medium text-slate-600 mb-1">Enviar teste para</label>
                  <input value={emailTeste} onChange={e => setEmailTeste(e.target.value)}
                    placeholder={smtp.usuario || 'seu@email.com.br'} type="email"
                    className="w-full border rounded-lg px-3 py-2 text-sm" />
                  <p className="text-xs text-slate-400 mt-1">
                    {smtp.provider === 'resend'
                      ? 'Com onboarding@resend.dev, use o email da sua conta Resend. Salve a config antes de testar.'
                      : smtp.provider === 'graph'
                      ? 'Salve a config antes de testar. Pode enviar para qualquer email (digite o seu para conferir).'
                      : 'Deixe vazio para usar o usuario SMTP.'}
                  </p>
                </div>
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

            <div className="bg-white rounded-2xl shadow p-6 mt-6">
              <h2 className="font-bold text-lg mb-1">Assinatura SaaS (Asaas)</h2>
              <p className="text-sm text-slate-500 mb-5">
                Conta Asaas <strong>da Ketra</strong> (cobra cada clinica pelo uso da plataforma) —
                diferente da chave Asaas de cada clinica, que e configurada por ela mesma para receber Pix no PDV.
              </p>
              <form onSubmit={salvarAsaasSaas} className="space-y-4">
                <div>
                  <label className="block text-xs font-medium text-slate-600 mb-1">Ambiente</label>
                  <select value={asaasSaas.ambiente} onChange={e => setAsaasSaas({...asaasSaas, ambiente: e.target.value})}
                    className="w-full border rounded-lg px-3 py-2 text-sm bg-white">
                    <option value="sandbox">Sandbox (testes)</option>
                    <option value="producao">Producao</option>
                  </select>
                </div>
                <div>
                  <label className="block text-xs font-medium text-slate-600 mb-1">
                    API Key {asaasSaas.apiKeyConfigurada && <span className="text-emerald-600">(configurada — deixe vazio para manter)</span>}
                  </label>
                  <input value={asaasSaas.apiKey} onChange={e => setAsaasSaas({...asaasSaas, apiKey: e.target.value})}
                    placeholder={asaasSaas.apiKeyConfigurada ? '••••••••' : '$aact_...'}
                    className="w-full border rounded-lg px-3 py-2 text-sm" />
                </div>
                <div>
                  <label className="block text-xs font-medium text-slate-600 mb-1">
                    Token do webhook (opcional) {asaasSaas.webhookConfigurado && <span className="text-emerald-600">(configurado — deixe vazio para manter)</span>}
                  </label>
                  <input value={asaasSaas.webhookToken} onChange={e => setAsaasSaas({...asaasSaas, webhookToken: e.target.value})}
                    placeholder="defina um valor e cole o mesmo no Asaas"
                    className="w-full border rounded-lg px-3 py-2 text-sm" />
                  <p className="text-xs text-slate-400 mt-1">
                    Se preenchido, o webhook so aceita chamadas com esse token no header <code>asaas-access-token</code>.
                    Configure o mesmo valor em: Asaas → Integracoes → Webhooks → Token de autenticacao.
                  </p>
                </div>
                {asaasSaasMsg && (
                  <div className={`text-sm px-3 py-2 rounded-lg ${asaasSaasMsg.includes('Erro') ? 'bg-red-50 text-red-700' : 'bg-green-50 text-green-700'}`}>
                    {asaasSaasMsg}
                  </div>
                )}
                <button type="submit" disabled={asaasSaasLoading}
                  className="w-full bg-emerald-600 hover:bg-emerald-700 disabled:bg-emerald-400 text-white font-bold py-2 rounded-lg text-sm">
                  {asaasSaasLoading ? 'Salvando...' : 'Salvar'}
                </button>
                <p className="text-xs text-slate-400">
                  URL do webhook pra cadastrar no Asaas: <code>https://vetclinicas-production.up.railway.app/api/webhooks/asaas-assinatura</code>
                </p>
              </form>
            </div>
          </div>
        )}
      </div>

      {modal && <ModalNovaClinica onClose={() => setModal(false)} onCriada={(r) => { setModal(false); setCriada(r); carregar() }} />}
      {criada && (
        <ModalCredenciais
          dados={criada}
          onClose={() => setCriada(null)}
          {...(criada._contexto === 'reset'
            ? { titulo: 'Senha redefinida!', subtitulo: criada.emailEnviado
                ? 'Email com a senha nova ja foi enviado para a clinica.'
                : 'Nao foi possivel enviar o email automaticamente -- copie e envie a senha manualmente.' }
            : {})}
        />
      )}
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
            {PLANOS.map((p) => <option key={p} value={p}>{p}</option>)}
          </select>
          <button className="bg-emerald-600 text-white py-2 rounded-lg">Criar clinica</button>
        </form>
      </div>
    </div>
  )
}

function ModalCredenciais({ dados, onClose, titulo = 'Clinica criada!', subtitulo = 'Envie estas credenciais ao responsavel. A senha e temporaria.' }) {
  return (
    <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50 p-4" onClick={onClose}>
      <div className="bg-white rounded-2xl shadow-xl p-6 w-full max-w-md text-center" onClick={(e) => e.stopPropagation()}>
        <div className="text-4xl mb-2">✅</div>
        <h3 className="font-bold text-lg mb-3">{titulo}</h3>
        <p className="text-sm text-slate-500 mb-4">{subtitulo}</p>
        <div className="bg-slate-50 rounded-lg p-4 text-left text-sm space-y-2">
          <div><span className="text-slate-400">Login:</span> <b>{dados.loginEmail}</b></div>
          <div><span className="text-slate-400">Senha temporaria:</span> <b className="font-mono">{dados.senhaTemporaria}</b></div>
        </div>
        <button onClick={onClose} className="mt-4 bg-slate-900 text-white px-6 py-2 rounded-lg">Entendi</button>
      </div>
    </div>
  )
}
