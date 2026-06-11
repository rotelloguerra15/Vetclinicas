import { useEffect, useRef, useState } from 'react'
import api from '../api/client'

export default function Parametros() {
  const [branding, setBranding] = useState({ nome: '', tagline: '', logoUrl: '', corPrimaria: '' })
  const [comissao, setComissao] = useState({ comissaoOsAtivo: true, comissaoPdvAtivo: false })
  const [salvandoComissao, setSalvandoComissao] = useState(false)
  const [logoPreview, setLogoPreview] = useState(null)
  const [salvando, setSalvando] = useState(false)
  const [msg, setMsg] = useState(null) // { tipo: 'ok'|'erro', texto }
  const fileRef = useRef()

  // IA
  const [iaStatus, setIaStatus]     = useState({ ativo: false })

  // SMTP / Contabilidade
  const [smtp, setSmtp]               = useState({ smtpHost: '', smtpPorta: 587, smtpUsuario: '', smtpSsl: true, emailContabil: '', contabilNome: '' })
  const [smtpSenha, setSmtpSenha]     = useState('')
  const [mostrarSmtpSenha, setMostrarSmtpSenha] = useState(false)
  const [salvandoSmtp, setSalvandoSmtp] = useState(false)
  const [testando, setTestando]         = useState(false)

  // Certificado Digital
  const [certStatus, setCertStatus]   = useState({ configurado: false })
  const [certArquivo, setCertArquivo] = useState(null)
  const [certSenha, setCertSenha]     = useState('')
  const [certMostrarSenha, setCertMostrarSenha] = useState(false)
  const [uploadandoCert, setUploadandoCert] = useState(false)
  const certRef = useRef(null)
  const [iaChave, setIaChave]       = useState('')
  const [iaAtivo, setIaAtivo]       = useState(false)
  const [salvandoIa, setSalvandoIa] = useState(false)
  const [mostrarChave, setMostrarChave] = useState(false)

  function carregar() {
    api.get('/certificado/status').then(r => setCertStatus(r.data)).catch(() => {})
    api.get('/contabil/config').then(r => setSmtp(r.data || {})).catch(() => {})
    api.get('/ia/status').then(r => {
      setIaStatus(r.data)
      setIaAtivo(r.data.ativo)
    }).catch(() => {})
    api.get('/tenant/branding').then(r => {
      setBranding({
        nome: r.data.nome || '',
        tagline: r.data.tagline || '',
        logoUrl: r.data.logoUrl || '',
        corPrimaria: r.data.corPrimaria || ''
      })
      setLogoPreview(r.data.logoUrl || null)
    }).catch(() => {})
  }

  useEffect(() => { carregar() }, [])

  function onArquivoSelecionado(e) {
    const file = e.target.files?.[0]
    if (!file) return
    if (file.size > 500 * 1024) {
      setMsg({ tipo: 'erro', texto: 'A logo deve ter no máximo 500 KB.' })
      return
    }
    const reader = new FileReader()
    reader.onload = ev => setLogoPreview(ev.target.result)
    reader.readAsDataURL(file)
  }

  async function uploadLogo() {
    const file = fileRef.current?.files?.[0]
    if (!file) return
    setSalvando(true)
    try {
      const form = new FormData()
      form.append('arquivo', file)
      const r = await api.post('/tenant/logo', form, {
        headers: { 'Content-Type': 'multipart/form-data' }
      })
      setLogoPreview(r.data.logoUrl)
      setBranding(b => ({ ...b, logoUrl: r.data.logoUrl }))
      setMsg({ tipo: 'ok', texto: 'Logo atualizada com sucesso!' })
    } catch {
      setMsg({ tipo: 'erro', texto: 'Erro ao enviar logo. Verifique o formato e tamanho.' })
    } finally {
      setSalvando(false)
    }
  }

  async function removerLogo() {
    if (!confirm('Remover a logo da clínica?')) return
    await api.delete('/tenant/logo').catch(() => {})
    setLogoPreview(null)
    setBranding(b => ({ ...b, logoUrl: '' }))
    if (fileRef.current) fileRef.current.value = ''
    setMsg({ tipo: 'ok', texto: 'Logo removida.' })
  }

  async function salvarBranding(e) {
    e.preventDefault()
    setSalvando(true)
    try {
      await api.put('/tenant/branding', {
        tagline: branding.tagline || null,
        logoUrl: branding.logoUrl || null,
        corPrimaria: branding.corPrimaria || null
      })
      setMsg({ tipo: 'ok', texto: 'Configurações salvas!' })
    } catch {
      setMsg({ tipo: 'erro', texto: 'Erro ao salvar.' })
    } finally {
      setSalvando(false)
    }
  }

  async function salvarComissao() {
    setSalvandoComissao(true)
    try {
      await api.put('/parametros', comissao)
      setMsg({ tipo: 'ok', texto: 'Configurações de comissão salvas!' })
    } catch {
      setMsg({ tipo: 'erro', texto: 'Erro ao salvar.' })
    } finally {
      setSalvandoComissao(false)
    }
  }

  async function uploadCertificado() {
    if (!certArquivo) return alert('Selecione o arquivo .pfx.')
    if (!certSenha.trim()) return alert('Informe a senha do certificado.')
    setUploadandoCert(true)
    try {
      const fd = new FormData()
      fd.append('arquivo', certArquivo)
      fd.append('senha', certSenha)
      const r = await api.post('/certificado/upload-a1', fd, {
        headers: { 'Content-Type': 'multipart/form-data' }
      })
      setCertStatus({ configurado: true, tipo: 'a1', titular: r.data.titular, validade: r.data.validade, ativo: true })
      setCertArquivo(null)
      setCertSenha('')
      setMsg({ tipo: 'ok', texto: `Certificado de ${r.data.titular} configurado com sucesso!` })
    } catch (e) {
      setMsg({ tipo: 'erro', texto: e.response?.data?.erro || 'Erro ao processar certificado.' })
    } finally {
      setUploadandoCert(false)
    }
  }

  async function ativarCertificado(ativo) {
    try {
      await api.put('/certificado/ativar', { ativo })
      setCertStatus(s => ({ ...s, ativo }))
      setMsg({ tipo: 'ok', texto: ativo ? 'Assinatura digital ativada.' : 'Assinatura digital desativada.' })
    } catch (e) {
      setMsg({ tipo: 'erro', texto: e.response?.data?.erro || 'Erro.' })
    }
  }

  async function removerCertificado() {
    if (!confirm('Remover o certificado digital? A assinatura automática será desativada.')) return
    try {
      await api.delete('/certificado')
      setCertStatus({ configurado: false })
      setMsg({ tipo: 'ok', texto: 'Certificado removido.' })
    } catch {
      setMsg({ tipo: 'erro', texto: 'Erro ao remover.' })
    }
  }

  async function salvarSmtp(e) {
    e.preventDefault()
    setSalvandoSmtp(true)
    try {
      await api.put('/contabil/config', { ...smtp, smtpSenha: smtpSenha || undefined })
      setSmtpSenha('')
      setMsg({ tipo: 'ok', texto: 'Configuração de email salva!' })
      api.get('/contabil/config').then(r => setSmtp(r.data || {})).catch(() => {})
    } catch (e) {
      setMsg({ tipo: 'erro', texto: e.response?.data?.erro || 'Erro ao salvar.' })
    } finally { setSalvandoSmtp(false) }
  }

  async function testarSmtp() {
    setTestando(true)
    try {
      const r = await api.post('/contabil/config/testar')
      setMsg({ tipo: 'ok', texto: r.data.mensagem })
    } catch (e) {
      setMsg({ tipo: 'erro', texto: e.response?.data?.erro || 'Falha no teste.' })
    } finally { setTestando(false) }
  }

  async function salvarIa() {
    setSalvandoIa(true)
    try {
      const r = await api.put('/ia/configurar', {
        anthropicApiKey: iaChave || null,
        iaAtivo
      })
      setIaStatus({ ativo: r.data.ativo })
      setIaChave('')
      setMsg({ tipo: 'ok', texto: 'Configuração de IA salva!' })
    } catch (e) {
      setMsg({ tipo: 'erro', texto: e.response?.data?.erro || 'Erro ao salvar IA.' })
    } finally {
      setSalvandoIa(false)
    }
  }

  async function removerChaveIa() {
    if (!confirm('Remover a chave API? A IA será desativada.')) return
    try {
      await api.delete('/ia/chave')
      setIaStatus({ ativo: false })
      setIaAtivo(false)
      setMsg({ tipo: 'ok', texto: 'Chave removida.' })
    } catch {
      setMsg({ tipo: 'erro', texto: 'Erro ao remover chave.' })
    }
  }

  return (
    <div className="max-w-2xl mx-auto space-y-6">
      <h2 className="text-2xl font-bold">⚙️ Parâmetros da Clínica</h2>

      {msg && (
        <div className={`px-4 py-3 rounded-lg text-sm font-medium ${
          msg.tipo === 'ok'
            ? 'bg-emerald-50 text-emerald-700 border border-emerald-200'
            : 'bg-red-50 text-red-700 border border-red-200'
        }`}>
          {msg.tipo === 'ok' ? '✅' : '❌'} {msg.texto}
        </div>
      )}

      {/* ── Logo ───────────────────────────────────────────────────────── */}
      <div className="bg-white rounded-2xl shadow p-6">
        <h3 className="font-semibold text-slate-700 mb-4">🖼️ Logo da Clínica</h3>
        <p className="text-xs text-slate-400 mb-4">
          A logo aparece no cabeçalho dos receituários em PDF.
          Formatos aceitos: PNG, JPG, WEBP, SVG — máximo 500 KB.
        </p>

        <div className="flex items-start gap-6">
          {/* Preview */}
          <div className="w-28 h-28 border-2 border-dashed border-slate-200 rounded-xl flex items-center justify-center bg-slate-50 shrink-0 overflow-hidden">
            {logoPreview
              ? <img src={logoPreview} alt="Logo" className="w-full h-full object-contain p-2" />
              : <span className="text-slate-300 text-4xl">🏥</span>
            }
          </div>

          {/* Controles */}
          <div className="flex-1 space-y-3">
            <input
              ref={fileRef}
              type="file"
              accept=".png,.jpg,.jpeg,.webp,.svg"
              onChange={onArquivoSelecionado}
              className="block w-full text-sm text-slate-500 file:mr-3 file:py-1.5 file:px-4
                         file:rounded-lg file:border-0 file:text-sm file:font-medium
                         file:bg-slate-900 file:text-white hover:file:bg-slate-700 cursor-pointer"
            />
            <div className="flex gap-2">
              <button
                onClick={uploadLogo}
                disabled={salvando || !fileRef.current?.files?.[0]}
                className="bg-emerald-600 text-white px-4 py-2 rounded-lg text-sm font-medium
                           disabled:opacity-40 hover:bg-emerald-700">
                {salvando ? '⏳ Enviando...' : '⬆️ Enviar logo'}
              </button>
              {logoPreview && (
                <button
                  onClick={removerLogo}
                  className="bg-red-50 text-red-600 px-4 py-2 rounded-lg text-sm hover:bg-red-100">
                  🗑️ Remover
                </button>
              )}
            </div>
          </div>
        </div>
      </div>

      {/* ── Identidade visual ──────────────────────────────────────────── */}
      <form onSubmit={salvarBranding} className="bg-white rounded-2xl shadow p-6 space-y-4">
        <h3 className="font-semibold text-slate-700">🎨 Identidade Visual</h3>

        <div>
          <label className="text-xs text-slate-500 block mb-1">Nome da clínica</label>
          <input
            className="border rounded-lg px-3 py-2 w-full bg-slate-50 text-slate-400 cursor-not-allowed"
            value={branding.nome}
            disabled
            title="O nome da clínica é gerenciado pelo administrador da plataforma"
          />
          <p className="text-xs text-slate-400 mt-1">Gerenciado pelo admin da plataforma.</p>
        </div>

        <div>
          <label className="text-xs text-slate-500 block mb-1">Slogan / Tagline</label>
          <input
            className="border rounded-lg px-3 py-2 w-full"
            placeholder="ex: Cuidando com amor desde 2010"
            value={branding.tagline}
            onChange={e => setBranding(b => ({ ...b, tagline: e.target.value }))}
          />
        </div>

        <div>
          <label className="text-xs text-slate-500 block mb-1">Cor principal (hex)</label>
          <div className="flex gap-3 items-center">
            <input
              type="color"
              className="w-10 h-10 rounded border cursor-pointer"
              value={branding.corPrimaria || '#0f172a'}
              onChange={e => setBranding(b => ({ ...b, corPrimaria: e.target.value }))}
            />
            <input
              className="border rounded-lg px-3 py-2 w-32 font-mono text-sm"
              placeholder="#0f172a"
              value={branding.corPrimaria}
              onChange={e => setBranding(b => ({ ...b, corPrimaria: e.target.value }))}
            />
          </div>
        </div>

        <button
          type="submit"
          disabled={salvando}
          className="bg-slate-900 text-white px-6 py-2 rounded-lg font-medium disabled:opacity-40 w-full">
          {salvando ? '⏳ Salvando...' : '💾 Salvar configurações'}
        </button>
      </form>

      {/* ── Certificado Digital ───────────────────────────────────────── */}
      <div className="bg-white rounded-2xl shadow p-6 space-y-4">
        <div className="flex items-center justify-between">
          <div>
            <h3 className="font-semibold text-slate-700">🔏 Assinatura Digital</h3>
            <p className="text-xs text-slate-400 mt-0.5">
              Certificado A1 ICP-Brasil (Safeweb, Certisign, Serasa, Soluti...)
            </p>
          </div>
          <span className={`text-xs font-semibold px-3 py-1 rounded-full ${certStatus.ativo ? 'bg-emerald-100 text-emerald-700' : certStatus.configurado ? 'bg-amber-100 text-amber-700' : 'bg-slate-100 text-slate-500'}`}>
            {certStatus.ativo ? '✓ Assinando' : certStatus.configurado ? '⏸ Pausado' : 'Não configurado'}
          </span>
        </div>

        {certStatus.configurado ? (
          <div className="space-y-3">
            {/* Info do certificado */}
            <div className="bg-emerald-50 border border-emerald-200 rounded-xl p-4">
              <div className="flex items-center gap-3">
                <div className="text-3xl">🔐</div>
                <div className="flex-1">
                  <p className="font-semibold text-emerald-800">{certStatus.titular || 'Certificado A1'}</p>
                  <p className="text-xs text-emerald-600 mt-0.5">
                    Tipo: {certStatus.tipo?.toUpperCase()} · Validade: {certStatus.validade}
                  </p>
                </div>
              </div>
            </div>

            {/* Toggle ativo */}
            <div className="flex items-center gap-3 p-3 bg-slate-50 rounded-xl border border-slate-100">
              <button type="button" onClick={() => ativarCertificado(!certStatus.ativo)}
                className={`relative inline-flex h-6 w-11 flex-shrink-0 items-center rounded-full transition-colors ${certStatus.ativo ? 'bg-emerald-500' : 'bg-slate-300'}`}>
                <span className={`inline-block h-4 w-4 transform rounded-full bg-white shadow transition-transform ${certStatus.ativo ? 'translate-x-6' : 'translate-x-1'}`} />
              </button>
              <div>
                <p className="text-sm font-semibold text-slate-700">
                  {certStatus.ativo ? 'Assinatura automática ativada' : 'Assinatura automática desativada'}
                </p>
                <p className="text-xs text-slate-400">Todos os receituários gerados serão assinados digitalmente</p>
              </div>
            </div>

            <div className="flex gap-2">
              <button onClick={() => { setCertArquivo(null); certRef.current?.click() }}
                className="border border-slate-200 text-slate-600 px-4 py-2 rounded-lg text-sm hover:bg-slate-50">
                🔄 Substituir certificado
              </button>
              <button onClick={removerCertificado}
                className="border border-red-200 text-red-500 px-4 py-2 rounded-lg text-sm hover:bg-red-50">
                🗑️ Remover
              </button>
            </div>
          </div>
        ) : (
          <div className="space-y-3">
            <div className="bg-blue-50 border border-blue-200 rounded-xl p-3 text-xs text-blue-700 space-y-1">
              <p>📌 Compatível com qualquer certificado A1 ICP-Brasil (.pfx)</p>
              <p>🔒 O arquivo é criptografado com AES-256 antes de ser armazenado</p>
              <p>✍️ Todos os receituários gerados terão assinatura digital válida</p>
            </div>

            {/* Upload .pfx */}
            <div>
              <label className="text-xs text-slate-500 font-medium block mb-1">Arquivo do certificado (.pfx)</label>
              <input ref={certRef} type="file" accept=".pfx,.p12" className="hidden"
                onChange={e => setCertArquivo(e.target.files?.[0] || null)} />
              <div className="flex gap-2">
                <div className={`flex-1 border rounded-xl px-3 py-2.5 text-sm cursor-pointer flex items-center gap-2 ${certArquivo ? 'border-emerald-400 bg-emerald-50 text-emerald-700' : 'border-slate-200 text-slate-400 hover:border-slate-300'}`}
                  onClick={() => certRef.current?.click()}>
                  <span>{certArquivo ? '📄 ' + certArquivo.name : '📂 Clique para selecionar o arquivo .pfx'}</span>
                </div>
                {certArquivo && (
                  <button onClick={() => { setCertArquivo(null); if(certRef.current) certRef.current.value = '' }}
                    className="border border-slate-200 px-3 py-2 rounded-xl text-slate-400 hover:bg-slate-50">✕</button>
                )}
              </div>
            </div>

            {/* Senha */}
            <div>
              <label className="text-xs text-slate-500 font-medium block mb-1">Senha do certificado</label>
              <div className="flex gap-2">
                <input type={certMostrarSenha ? 'text' : 'password'}
                  className="border rounded-xl px-3 py-2.5 flex-1 text-sm"
                  placeholder="Senha definida na emissão do certificado"
                  value={certSenha} onChange={e => setCertSenha(e.target.value)} />
                <button type="button" onClick={() => setCertMostrarSenha(v => !v)}
                  className="border px-3 py-2 rounded-xl text-slate-500 hover:bg-slate-50 text-sm">
                  {certMostrarSenha ? '🙈' : '👁️'}
                </button>
              </div>
            </div>

            <button onClick={uploadCertificado}
              disabled={uploadandoCert || !certArquivo || !certSenha.trim()}
              className="w-full bg-slate-900 text-white py-2.5 rounded-xl text-sm font-semibold disabled:opacity-40 hover:bg-slate-800">
              {uploadandoCert ? '⏳ Verificando certificado...' : '🔏 Configurar certificado digital'}
            </button>
          </div>
        )}
      </div>

      {/* ── Email / Contabilidade ──────────────────────────────────────── */}
      <div className="bg-white rounded-2xl shadow p-6 space-y-4">
        <h3 className="font-semibold text-slate-700">📧 Email e Contabilidade (SMTP)</h3>
        <form onSubmit={salvarSmtp} className="space-y-3">
          <div className="grid grid-cols-2 gap-3">
            <div className="col-span-2">
              <label className="text-xs text-slate-500 block mb-1">Nome do escritório de contabilidade</label>
              <input className="border rounded-xl px-3 py-2.5 w-full text-sm"
                placeholder="Ex: Contabilidade Fulano & Associados"
                value={smtp.contabilNome || ''}
                onChange={e => setSmtp({...smtp, contabilNome: e.target.value})} />
            </div>
            <div className="col-span-2">
              <label className="text-xs text-slate-500 block mb-1">Email da contabilidade (destino dos fechamentos)</label>
              <input type="email" className="border rounded-xl px-3 py-2.5 w-full text-sm"
                placeholder="contabilidade@escritorio.com.br"
                value={smtp.emailContabil || ''}
                onChange={e => setSmtp({...smtp, emailContabil: e.target.value})} />
            </div>
            <div>
              <label className="text-xs text-slate-500 block mb-1">Servidor SMTP</label>
              <input className="border rounded-xl px-3 py-2.5 w-full text-sm"
                placeholder="smtp.gmail.com"
                value={smtp.smtpHost || ''}
                onChange={e => setSmtp({...smtp, smtpHost: e.target.value})} />
            </div>
            <div>
              <label className="text-xs text-slate-500 block mb-1">Porta</label>
              <input type="number" className="border rounded-xl px-3 py-2.5 w-full text-sm"
                placeholder="587"
                value={smtp.smtpPorta || 587}
                onChange={e => setSmtp({...smtp, smtpPorta: +e.target.value})} />
            </div>
            <div className="col-span-2">
              <label className="text-xs text-slate-500 block mb-1">Usuário / Email remetente</label>
              <input type="email" className="border rounded-xl px-3 py-2.5 w-full text-sm"
                placeholder="seuemail@gmail.com"
                value={smtp.smtpUsuario || ''}
                onChange={e => setSmtp({...smtp, smtpUsuario: e.target.value})} />
            </div>
            <div className="col-span-2">
              <label className="text-xs text-slate-500 block mb-1">Senha / App Password</label>
              <div className="flex gap-2">
                <input type={mostrarSmtpSenha ? 'text' : 'password'} className="border rounded-xl px-3 py-2.5 flex-1 text-sm"
                  placeholder={smtp.temSmtp ? '••••••• (mantida)' : 'Senha ou App Password'}
                  value={smtpSenha} onChange={e => setSmtpSenha(e.target.value)} />
                <button type="button" onClick={() => setMostrarSmtpSenha(v => !v)}
                  className="border px-3 rounded-xl text-slate-500 hover:bg-slate-50">
                  {mostrarSmtpSenha ? '🙈' : '👁️'}
                </button>
              </div>
            </div>
            <div className="col-span-2 flex items-center gap-3">
              <button type="button" onClick={() => setSmtp({...smtp, smtpSsl: !smtp.smtpSsl})}
                className={`relative inline-flex h-6 w-11 flex-shrink-0 items-center rounded-full transition-colors ${smtp.smtpSsl ? 'bg-emerald-500' : 'bg-slate-300'}`}>
                <span className={`inline-block h-4 w-4 transform rounded-full bg-white shadow transition-transform ${smtp.smtpSsl ? 'translate-x-6' : 'translate-x-1'}`} />
              </button>
              <span className="text-sm text-slate-600">Usar SSL/TLS</span>
            </div>
          </div>
          <div className="bg-blue-50 border border-blue-200 rounded-xl p-3 text-xs text-blue-700 space-y-0.5">
            <p>📌 <strong>Gmail:</strong> smtp.gmail.com · porta 587 · use App Password</p>
            <p>📌 <strong>Outlook:</strong> smtp.office365.com · porta 587</p>
          </div>
          <div className="flex gap-2">
            <button type="submit" disabled={salvandoSmtp}
              className="flex-1 bg-emerald-600 text-white py-2.5 rounded-xl text-sm font-semibold disabled:opacity-50 hover:bg-emerald-700">
              {salvandoSmtp ? 'Salvando...' : 'Salvar configuração de email'}
            </button>
            <button type="button" onClick={testarSmtp} disabled={testando || !smtp.temSmtp}
              className="border border-slate-200 text-slate-700 px-4 py-2.5 rounded-xl text-sm disabled:opacity-40 hover:bg-slate-50">
              {testando ? '⏳' : '🧪 Testar'}
            </button>
          </div>
        </form>
      </div>

      {/* ── Assistente IA ──────────────────────────────────────────────── */}
      <div className="bg-white rounded-2xl shadow p-6 space-y-4">
        <div className="flex items-center justify-between">
          <div>
            <h3 className="font-semibold text-slate-700">🤖 Assistente IA — Diagnóstico</h3>
            <p className="text-xs text-slate-400 mt-0.5">
              Powered by Claude (Anthropic). Cada clínica usa sua própria chave — o custo vai direto para sua conta Anthropic.
            </p>
          </div>
          <span className={`text-xs font-semibold px-3 py-1 rounded-full ${iaStatus.ativo ? 'bg-emerald-100 text-emerald-700' : 'bg-slate-100 text-slate-500'}`}>
            {iaStatus.ativo ? '✓ Ativo' : 'Inativo'}
          </span>
        </div>

        <div className="bg-blue-50 border border-blue-200 rounded-xl p-3 text-xs text-blue-700 space-y-1">
          <p>📌 Para obter sua chave API acesse <strong>console.anthropic.com/settings/keys</strong></p>
          <p>💡 Recomendamos criar uma chave exclusiva para esta clínica.</p>
          <p>💰 O custo por consulta é de aproximadamente <strong>US$ 0,003</strong> (claude-sonnet).</p>
        </div>

        <div>
          <label className="text-xs text-slate-500 block mb-1">Chave API Anthropic</label>
          <div className="flex gap-2">
            <input
              type={mostrarChave ? 'text' : 'password'}
              className="border rounded-lg px-3 py-2 flex-1 font-mono text-sm"
              placeholder={iaStatus.ativo ? '••••••••••••• (chave já salva)' : 'sk-ant-api03-...'}
              value={iaChave}
              onChange={e => setIaChave(e.target.value)}
            />
            <button
              type="button"
              onClick={() => setMostrarChave(v => !v)}
              className="border px-3 py-2 rounded-lg text-sm text-slate-500 hover:bg-slate-50">
              {mostrarChave ? '🙈' : '👁️'}
            </button>
          </div>
          <p className="text-xs text-slate-400 mt-1">
            Deixe em branco para manter a chave atual.
          </p>
        </div>

        <div className="flex items-center gap-3">
          <button
            type="button"
            onClick={() => setIaAtivo(v => !v)}
            className={`relative inline-flex h-6 w-11 items-center rounded-full transition-colors ${iaAtivo ? 'bg-emerald-600' : 'bg-slate-200'}`}>
            <span className={`inline-block h-4 w-4 transform rounded-full bg-white transition-transform ${iaAtivo ? 'translate-x-6' : 'translate-x-1'}`} />
          </button>
          <span className="text-sm text-slate-700">
            {iaAtivo ? 'IA ativada no receituário' : 'IA desativada'}
          </span>
        </div>

        <div className="flex gap-2 pt-1">
          <button
            onClick={salvarIa}
            disabled={salvandoIa}
            className="bg-slate-900 text-white px-5 py-2 rounded-lg text-sm font-medium disabled:opacity-40 hover:bg-slate-800">
            {salvandoIa ? '⏳ Salvando...' : '💾 Salvar configuração IA'}
          </button>
          {iaStatus.ativo && (
            <button
              onClick={removerChaveIa}
              className="border border-red-200 text-red-500 px-5 py-2 rounded-lg text-sm hover:bg-red-50">
              🗑️ Remover chave
            </button>
          )}
        </div>
      </div>

    </div>
  )
}
