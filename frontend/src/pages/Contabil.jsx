import { useEffect, useRef, useState } from 'react'
import api from '../api/client'

const MESES = ['Janeiro','Fevereiro','Marco','Abril','Maio','Junho',
               'Julho','Agosto','Setembro','Outubro','Novembro','Dezembro']

const TIPOS_DOC = {
  extrato_bancario:   { label: 'Extratos Bancarios',              emoji: '🏦', cor: 'blue'   },
  extrato_aplicacoes: { label: 'Extratos de Aplicacoes',          emoji: '📈', cor: 'green'  },
  dre:                { label: 'DRE / Demonstrativo Financeiro',   emoji: '📊', cor: 'indigo' },
  despesas:           { label: 'Despesas do CNPJ',                 emoji: '💸', cor: 'red'    },
  fatura_cartao:      { label: 'Faturas de Cartao de Credito',     emoji: '💳', cor: 'purple' },
  nf_tomada:          { label: 'NF-e de Servicos Tomados',         emoji: '🧾', cor: 'amber'  },
  estoque:            { label: 'Estoque Final do Mes',             emoji: '📦', cor: 'orange' },
  repasses:           { label: 'Repasses ao Socio/Responsavel',    emoji: '👤', cor: 'teal'   },
  contratos:          { label: 'Contratos de Emprestimos',         emoji: '📝', cor: 'slate'  },
}

function fmtTamanho(bytes) {
  if (!bytes) return ''
  if (bytes < 1024) return `${bytes} B`
  if (bytes < 1024*1024) return `${(bytes/1024).toFixed(1)} KB`
  return `${(bytes/1024/1024).toFixed(1)} MB`
}

export default function Contabil() {
  const now = new Date()
  const [ano, setAno] = useState(now.getFullYear())
  const [mes, setMes] = useState(now.getMonth() + 1)
  const [aba, setAba] = useState('fechamento') // fechamento | config | historico

  const [fechamento, setFechamento]   = useState(null)
  const [checklist, setChecklist]     = useState([])
  const [carregando, setCarregando]   = useState(false)
  const [config, setConfig]           = useState({})
  const [salvandoConfig, setSalvandoConfig] = useState(false)
  const [testando, setTestando]       = useState(false)
  const [enviando, setEnviando]       = useState(false)
  const [gerandoTipo, setGerandoTipo] = useState(null)
  const [obs, setObs]                 = useState('')
  const [msg, setMsg]                 = useState(null)
  const [historico, setHistorico]     = useState([])
  const [smtpSenha, setSmtpSenha]     = useState('')
  const [mostrarSenha, setMostrarSenha] = useState(false)
  const uploadRefs = useRef({})

  function flash(tipo, texto) {
    setMsg({ tipo, texto })
    setTimeout(() => setMsg(null), 4000)
  }

  function carregar() {
    setCarregando(true)
    api.get(`/contabil/fechamentos/${ano}/${mes}`)
      .then(r => { setFechamento(r.data.fechamento); setChecklist(r.data.checklist?.itens || []) })
      .catch(() => {})
      .finally(() => setCarregando(false))
  }

  function carregarConfig() {
    api.get('/contabil/config').then(r => setConfig(r.data)).catch(() => {})
  }

  function carregarHistorico() {
    api.get('/contabil/fechamentos').then(r => setHistorico(r.data)).catch(() => {})
  }

  useEffect(() => { carregar() }, [ano, mes])
  useEffect(() => { carregarConfig(); carregarHistorico() }, [])

  async function gerar(tipo) {
    setGerandoTipo(tipo)
    try {
      await api.post(`/contabil/fechamentos/${ano}/${mes}/gerar/${tipo}`)
      flash('ok', 'Documento gerado com sucesso!')
      carregar()
    } catch (e) {
      flash('erro', e.response?.data?.erro || 'Erro ao gerar.')
    } finally { setGerandoTipo(null) }
  }

  async function processarUpload(file, tipo) {
    if (file.size > 7 * 1024 * 1024) { flash('erro', 'Arquivo muito grande. Maximo 7MB.'); return }
    try {
      const base64 = await new Promise((res, rej) => {
        const r = new FileReader()
        r.onload = e => res(e.target.result.split(',')[1])
        r.onerror = rej
        r.readAsDataURL(file)
      })
      await api.post(`/contabil/fechamentos/${ano}/${mes}/documentos`, {
        tipo, nome: file.name,
        descricao: TIPOS_DOC[tipo]?.label,
        tipoArquivo: file.type || 'application/pdf',
        dadosBase64: base64
      })
      flash('ok', `${file.name} adicionado!`)
      carregar()
    } catch (e) {
      flash('erro', e.response?.data?.erro || 'Erro ao enviar.')
    }
  }

  async function removerDoc(docId) {
    if (!confirm('Remover este documento?')) return
    await api.delete(`/contabil/documentos/${docId}`).catch(() => {})
    carregar()
  }

  function download(docId, nome) {
    api.get(`/contabil/documentos/${docId}/download`, { responseType: 'blob' })
      .then(r => {
        const url = URL.createObjectURL(r.data)
        const a   = document.createElement('a')
        a.href = url; a.download = nome
        document.body.appendChild(a); a.click()
        document.body.removeChild(a)
        setTimeout(() => URL.revokeObjectURL(url), 1000)
      })
  }

  async function salvarConfig(e) {
    e.preventDefault()
    setSalvandoConfig(true)
    try {
      await api.put('/contabil/config', { ...config, smtpSenha: smtpSenha || undefined })
      setSmtpSenha('')
      flash('ok', 'Configuracao salva!')
      carregarConfig()
    } catch (e) {
      flash('erro', e.response?.data?.erro || 'Erro ao salvar.')
    } finally { setSalvandoConfig(false) }
  }

  async function testarSmtp() {
    setTestando(true)
    try {
      const r = await api.post('/contabil/config/testar')
      flash('ok', r.data.mensagem)
    } catch (e) {
      flash('erro', e.response?.data?.erro || 'Falha no teste.')
    } finally { setTestando(false) }
  }

  async function enviar() {
    if (!confirm(`Enviar todos os documentos para ${config.emailContabil}?`)) return
    setEnviando(true)
    try {
      const r = await api.post(`/contabil/fechamentos/${ano}/${mes}/enviar`, { obs })
      flash('ok', r.data.mensagem)
      carregar(); carregarHistorico()
    } catch (e) {
      flash('erro', e.response?.data?.erro || 'Erro ao enviar.')
    } finally { setEnviando(false) }
  }

  // Agrupa documentos por tipo
  const docsPorTipo = (fechamento?.documentos || []).reduce((acc, d) => {
    if (!acc[d.tipo]) acc[d.tipo] = []
    acc[d.tipo].push(d)
    return acc
  }, {})

  const totalDocs   = fechamento?.documentos?.length || 0
  const prontos     = checklist.filter(i => i.temDocumento).length
  const obrigatorios= checklist.filter(i => i.obrigatorio)
  const prontoEnviar= obrigatorios.every(i => i.temDocumento)

  return (
    <div className="max-w-4xl mx-auto space-y-5">

      {/* Header */}
      <div className="flex items-center justify-between flex-wrap gap-3">
        <div className="flex items-center gap-3">
          <div className="text-4xl">🏛️</div>
          <div>
            <h2 className="text-2xl font-bold">Modulo Contabil</h2>
            <p className="text-sm text-slate-400">Fechamento mensal e envio para contabilidade</p>
          </div>
        </div>
        <div className="flex gap-1 bg-slate-100 rounded-xl p-1">
          {[['fechamento','📋 Fechamento'],['config','⚙️ Configuracao'],['historico','📅 Historico']].map(([v,l]) => (
            <button key={v} onClick={() => setAba(v)}
              className={`px-4 py-2 rounded-lg text-sm font-medium transition ${aba === v ? 'bg-white shadow text-slate-800' : 'text-slate-500 hover:text-slate-700'}`}>
              {l}
            </button>
          ))}
        </div>
      </div>

      {/* Flash */}
      {msg && (
        <div className={`px-4 py-3 rounded-xl text-sm font-medium ${msg.tipo === 'ok' ? 'bg-emerald-50 text-emerald-700 border border-emerald-200' : 'bg-red-50 text-red-700 border border-red-200'}`}>
          {msg.tipo === 'ok' ? '✅' : '❌'} {msg.texto}
        </div>
      )}

      {/* ABA: FECHAMENTO */}
      {aba === 'fechamento' && (
        <div className="space-y-4">

          {/* Seletor mês/ano */}
          <div className="bg-white rounded-2xl p-4 shadow-sm border border-slate-100 flex items-center justify-between flex-wrap gap-3">
            <div className="flex items-center gap-3">
              <select className="border rounded-xl px-3 py-2 text-sm" value={mes} onChange={e => setMes(+e.target.value)}>
                {MESES.map((m,i) => <option key={i} value={i+1}>{m}</option>)}
              </select>
              <select className="border rounded-xl px-3 py-2 text-sm" value={ano} onChange={e => setAno(+e.target.value)}>
                {[2024,2025,2026,2027].map(a => <option key={a}>{a}</option>)}
              </select>
              <div className={`text-xs px-3 py-1.5 rounded-full font-semibold ${fechamento?.status === 'enviado' ? 'bg-emerald-100 text-emerald-700' : 'bg-amber-100 text-amber-700'}`}>
                {fechamento?.status === 'enviado' ? `✅ Enviado em ${new Date(fechamento.enviadoEm).toLocaleDateString('pt-BR')}` : '⏳ Em aberto'}
              </div>
            </div>
            <div className="text-sm text-slate-500">
              {prontos}/{checklist.length} documentos · {totalDocs} arquivo{totalDocs !== 1 ? 's' : ''}
            </div>
          </div>

          {/* Checklist */}
          <div className="bg-white rounded-2xl shadow-sm border border-slate-100 overflow-hidden">
            <div className="px-5 py-3 bg-slate-50 border-b">
              <span className="text-sm font-semibold text-slate-700">Checklist de Documentos</span>
            </div>

            {carregando ? (
              <div className="p-8 text-center text-slate-400">Carregando...</div>
            ) : (
              <div className="divide-y divide-slate-50">
                {checklist.map(item => {
                  const cfg     = TIPOS_DOC[item.id] || { label: item.label, emoji: '📎', cor: 'slate' }
                  const docs    = docsPorTipo[item.id] || []
                  const pronto  = item.temDocumento || docs.length > 0
                  const podeGerar = item.tipo === 'gerar' && item.temNoSistema

                  return (
                    <div key={item.id} className={`p-4 ${pronto ? 'bg-emerald-50/30' : ''}`}>
                      <div className="flex items-start gap-3">
                        {/* Status */}
                        <div className={`w-7 h-7 rounded-full flex items-center justify-center text-sm flex-shrink-0 mt-0.5 ${pronto ? 'bg-emerald-500 text-white' : item.obrigatorio ? 'bg-red-100 text-red-400' : 'bg-slate-100 text-slate-400'}`}>
                          {pronto ? '✓' : item.obrigatorio ? '!' : '○'}
                        </div>

                        <div className="flex-1 min-w-0">
                          <div className="flex items-center gap-2 flex-wrap">
                            <span className="text-sm font-semibold text-slate-800">
                              {cfg.emoji} {item.label}
                            </span>
                            {item.obrigatorio && !pronto && (
                              <span className="text-xs bg-red-100 text-red-500 px-2 py-0.5 rounded-full">Obrigatorio</span>
                            )}
                            {podeGerar && !pronto && (
                              <span className="text-xs bg-blue-100 text-blue-600 px-2 py-0.5 rounded-full">Pode gerar automaticamente</span>
                            )}
                          </div>

                          {/* Documentos existentes */}
                          {docs.length > 0 && (
                            <div className="mt-2 space-y-1">
                              {docs.map(d => (
                                <div key={d.id} className="flex items-center gap-2 text-xs bg-white rounded-lg px-3 py-1.5 border border-slate-100">
                                  <span className="flex-1 truncate text-slate-700">{d.nome}</span>
                                  <span className="text-slate-400">{fmtTamanho(d.tamanhoBytes)}</span>
                                  {d.origem === 'sistema' && <span className="text-blue-500 text-xs">sistema</span>}
                                  <button onClick={() => download(d.id, d.nome)} className="text-blue-500 hover:text-blue-700">⬇</button>
                                  <button onClick={() => removerDoc(d.id)} className="text-red-400 hover:text-red-600">✕</button>
                                </div>
                              ))}
                            </div>
                          )}

                          {/* Ações */}
                          <div className="flex gap-2 mt-2 flex-wrap">
                            {podeGerar && (
                              <button onClick={() => gerar(item.id)}
                                disabled={gerandoTipo === item.id}
                                className="text-xs bg-blue-600 text-white px-3 py-1.5 rounded-lg hover:bg-blue-700 disabled:opacity-50 font-medium">
                                {gerandoTipo === item.id ? '⏳ Gerando...' : '⚡ Gerar do sistema'}
                              </button>
                            )}
                            <label className="text-xs border border-slate-200 text-slate-600 px-3 py-1.5 rounded-lg hover:bg-slate-50 cursor-pointer">
                              📎 {docs.length > 0 ? 'Adicionar outro' : 'Upload'}
                              <input type="file" className="hidden"
                                accept=".pdf,.jpg,.jpeg,.png,.xml,.xlsx,.xls"
                                onChange={e => { const f = e.target.files?.[0]; if (f) processarUpload(f, item.id); e.target.value = '' }} />
                            </label>
                          </div>
                        </div>
                      </div>
                    </div>
                  )
                })}
              </div>
            )}
          </div>

          {/* Botão enviar */}
          <div className="bg-white rounded-2xl p-5 shadow-sm border border-slate-100 space-y-3">
            <div className="flex items-start justify-between flex-wrap gap-3">
              <div>
                <p className="font-semibold text-slate-800">Enviar para Contabilidade</p>
                <p className="text-sm text-slate-400 mt-0.5">
                  {config.emailContabil ? `Destino: ${config.emailContabil}` : '⚠️ Email da contabilidade nao configurado'}
                  {config.contabilNome ? ` (${config.contabilNome})` : ''}
                </p>
              </div>
              <div className={`text-sm font-semibold px-3 py-1.5 rounded-full ${prontoEnviar ? 'bg-emerald-100 text-emerald-700' : 'bg-amber-100 text-amber-600'}`}>
                {prontoEnviar ? '✅ Pronto para enviar' : `⚠️ ${obrigatorios.filter(i => !i.temDocumento).length} obrigatorio(s) faltando`}
              </div>
            </div>

            <textarea className="border rounded-xl px-3 py-2 w-full text-sm" rows={2}
              placeholder="Observacoes para a contabilidade (opcional)..."
              value={obs} onChange={e => setObs(e.target.value)} />

            <button onClick={enviar}
              disabled={enviando || !config.temSmtp || !config.emailContabil || totalDocs === 0}
              className="w-full bg-slate-900 text-white py-3 rounded-xl text-sm font-bold disabled:opacity-40 hover:bg-slate-800 flex items-center justify-center gap-2">
              {enviando ? '⏳ Enviando...' : `📧 Enviar ZIP com ${totalDocs} documento${totalDocs !== 1 ? 's' : ''} para contabilidade`}
            </button>
            {!config.temSmtp && (
              <p className="text-xs text-amber-600 text-center">Configure o SMTP na aba Configuracao para enviar emails</p>
            )}
          </div>
        </div>
      )}

      {/* ABA: CONFIGURACAO */}
      {aba === 'config' && (
        <form onSubmit={salvarConfig} className="space-y-4">

          {/* Email da contabilidade */}
          <div className="bg-white rounded-2xl p-5 shadow-sm border border-slate-100 space-y-3">
            <h3 className="font-semibold text-slate-700">🏛️ Escritorio de Contabilidade</h3>
            <div className="grid grid-cols-2 gap-3">
              <div className="col-span-2">
                <label className="text-xs text-slate-500 block mb-1">Nome do escritorio</label>
                <input className="border rounded-xl px-3 py-2.5 w-full text-sm"
                  placeholder="Ex: Contabilidade Fulano & Associados"
                  value={config.contabilNome || ''}
                  onChange={e => setConfig({...config, contabilNome: e.target.value})} />
              </div>
              <div className="col-span-2">
                <label className="text-xs text-slate-500 block mb-1">Email para envio dos documentos</label>
                <input type="email" className="border rounded-xl px-3 py-2.5 w-full text-sm"
                  placeholder="contabilidade@escritorio.com.br"
                  value={config.emailContabil || ''}
                  onChange={e => setConfig({...config, emailContabil: e.target.value})} />
              </div>
            </div>
          </div>

          {/* SMTP */}
          <div className="bg-white rounded-2xl p-5 shadow-sm border border-slate-100 space-y-3">
            <h3 className="font-semibold text-slate-700">📧 Configuracao de Email (SMTP)</h3>
            <p className="text-xs text-slate-400">Funciona com Gmail, Outlook, Hotmail, Zoho, ou qualquer servidor SMTP</p>

            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="text-xs text-slate-500 block mb-1">Servidor SMTP</label>
                <input className="border rounded-xl px-3 py-2.5 w-full text-sm"
                  placeholder="smtp.gmail.com"
                  value={config.smtpHost || ''}
                  onChange={e => setConfig({...config, smtpHost: e.target.value})} />
              </div>
              <div>
                <label className="text-xs text-slate-500 block mb-1">Porta</label>
                <input type="number" className="border rounded-xl px-3 py-2.5 w-full text-sm"
                  placeholder="587"
                  value={config.smtpPorta || 587}
                  onChange={e => setConfig({...config, smtpPorta: +e.target.value})} />
              </div>
              <div className="col-span-2">
                <label className="text-xs text-slate-500 block mb-1">Usuario / Email remetente</label>
                <input type="email" className="border rounded-xl px-3 py-2.5 w-full text-sm"
                  placeholder="seuemail@gmail.com"
                  value={config.smtpUsuario || ''}
                  onChange={e => setConfig({...config, smtpUsuario: e.target.value, emailRemetente: e.target.value})} />
              </div>
              <div className="col-span-2">
                <label className="text-xs text-slate-500 block mb-1">Senha / App Password</label>
                <div className="flex gap-2">
                  <input type={mostrarSenha ? 'text' : 'password'} className="border rounded-xl px-3 py-2.5 flex-1 text-sm"
                    placeholder={config.temSmtp ? '••••••••••• (mantida)' : 'Senha do email ou App Password'}
                    value={smtpSenha}
                    onChange={e => setSmtpSenha(e.target.value)} />
                  <button type="button" onClick={() => setMostrarSenha(v => !v)}
                    className="border px-3 rounded-xl text-slate-500 hover:bg-slate-50">
                    {mostrarSenha ? '🙈' : '👁️'}
                  </button>
                </div>
              </div>
              <div className="col-span-2 flex items-center gap-3">
                <button type="button"
                  onClick={() => setConfig({...config, smtpSsl: !config.smtpSsl})}
                  className={`relative inline-flex h-6 w-11 flex-shrink-0 items-center rounded-full transition-colors ${config.smtpSsl ? 'bg-emerald-500' : 'bg-slate-300'}`}>
                  <span className={`inline-block h-4 w-4 transform rounded-full bg-white shadow transition-transform ${config.smtpSsl ? 'translate-x-6' : 'translate-x-1'}`} />
                </button>
                <span className="text-sm text-slate-600">Usar SSL/TLS</span>
              </div>
            </div>

            <div className="bg-blue-50 border border-blue-200 rounded-xl p-3 text-xs text-blue-700 space-y-1">
              <p>📌 <strong>Gmail:</strong> smtp.gmail.com · porta 587 · use App Password (nao a senha normal)</p>
              <p>📌 <strong>Outlook/Hotmail:</strong> smtp.office365.com · porta 587</p>
              <p>📌 <strong>Zoho:</strong> smtp.zoho.com · porta 587</p>
            </div>
          </div>

          <div className="flex gap-3">
            <button type="submit" disabled={salvandoConfig}
              className="flex-1 bg-emerald-600 text-white py-3 rounded-xl text-sm font-bold disabled:opacity-50 hover:bg-emerald-700">
              {salvandoConfig ? 'Salvando...' : 'Salvar configuracao'}
            </button>
            <button type="button" onClick={testarSmtp} disabled={testando || !config.temSmtp}
              className="border border-slate-200 text-slate-700 px-5 py-3 rounded-xl text-sm font-medium disabled:opacity-40 hover:bg-slate-50">
              {testando ? '⏳ Testando...' : '🧪 Testar conexao'}
            </button>
          </div>
        </form>
      )}

      {/* ABA: HISTORICO */}
      {aba === 'historico' && (
        <div className="bg-white rounded-2xl shadow-sm border border-slate-100 overflow-hidden">
          <div className="px-5 py-3 bg-slate-50 border-b">
            <span className="text-sm font-semibold text-slate-700">Historico de Fechamentos</span>
          </div>
          {historico.length === 0 ? (
            <div className="p-8 text-center text-slate-400">Nenhum fechamento registrado ainda.</div>
          ) : (
            <table className="w-full text-sm">
              <thead className="bg-slate-50 text-left">
                <tr>
                  <th className="px-4 py-3 text-xs font-semibold text-slate-500">Periodo</th>
                  <th className="px-4 py-3 text-xs font-semibold text-slate-500">Status</th>
                  <th className="px-4 py-3 text-xs font-semibold text-slate-500">Documentos</th>
                  <th className="px-4 py-3 text-xs font-semibold text-slate-500">Enviado em</th>
                  <th className="px-4 py-3 text-xs font-semibold text-slate-500"></th>
                </tr>
              </thead>
              <tbody>
                {historico.map(h => (
                  <tr key={h.id} className="border-t hover:bg-slate-50">
                    <td className="px-4 py-3 font-semibold">{MESES[h.mes-1]} {h.ano}</td>
                    <td className="px-4 py-3">
                      <span className={`text-xs px-2 py-1 rounded-full font-medium ${h.status === 'enviado' ? 'bg-emerald-100 text-emerald-700' : 'bg-amber-100 text-amber-700'}`}>
                        {h.status === 'enviado' ? '✅ Enviado' : '⏳ Em aberto'}
                      </span>
                    </td>
                    <td className="px-4 py-3 text-slate-500">{h.qtdDocumentos} arquivo(s)</td>
                    <td className="px-4 py-3 text-slate-500">
                      {h.enviadoEm ? new Date(h.enviadoEm).toLocaleDateString('pt-BR') : '—'}
                    </td>
                    <td className="px-4 py-3">
                      <button onClick={() => { setAno(h.ano); setMes(h.mes); setAba('fechamento') }}
                        className="text-xs text-blue-600 hover:underline">
                        Abrir
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      )}
    </div>
  )
}
