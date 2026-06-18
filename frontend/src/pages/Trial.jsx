import { useState, useEffect } from 'react'
import api from '../api/client'

function mascaraCnpj(v = '') {
  return v.replace(/\D/g, '').slice(0, 14)
    .replace(/(\d{2})(\d)/, '$1.$2')
    .replace(/(\d{3})(\d)/, '$1.$2')
    .replace(/(\d{3})(\d)/, '$1/$2')
    .replace(/(\d{4})(\d)/, '$1-$2')
}

function mascaraTel(v = '') {
  return v.replace(/\D/g, '').slice(0, 11)
    .replace(/(\d{2})(\d)/, '($1) $2')
    .replace(/(\d{5})(\d)/, '$1-$2')
}

function mascaraCep(v = '') {
  return v.replace(/\D/g, '').slice(0, 8)
    .replace(/(\d{5})(\d)/, '$1-$2')
}

const ESTADOS = ['AC','AL','AP','AM','BA','CE','DF','ES','GO','MA','MT','MS','MG','PA','PB','PR','PE','PI','RJ','RN','RS','RO','RR','SC','SP','SE','TO']

// Componente de countdown do trial
function TrialCountdown({ trialExpiraEm, diasRestantes }) {
  const expira = new Date(trialExpiraEm + 'T23:59:59')
  const agora  = new Date()
  const diffMs = expira - agora
  const dias   = Math.max(0, Math.ceil(diffMs / (1000 * 60 * 60 * 24)))

  const porcentagem = Math.round((dias / 14) * 100)
  const cor = dias > 7 ? 'bg-green-500' : dias > 3 ? 'bg-yellow-500' : 'bg-red-500'
  const corTexto = dias > 7 ? 'text-green-700' : dias > 3 ? 'text-yellow-700' : 'text-red-700'
  const bgTexto  = dias > 7 ? 'bg-green-50 border-green-200' : dias > 3 ? 'bg-yellow-50 border-yellow-200' : 'bg-red-50 border-red-200'

  return (
    <div className={`border rounded-xl p-4 ${bgTexto}`}>
      <div className="flex justify-between items-center mb-2">
        <span className={`text-sm font-semibold ${corTexto}`}>
          Periodo Trial
        </span>
        <span className={`text-sm font-bold ${corTexto}`}>
          {dias} {dias === 1 ? 'dia restante' : 'dias restantes'}
        </span>
      </div>
      <div className="w-full bg-slate-200 rounded-full h-2 mb-2">
        <div
          className={`h-2 rounded-full transition-all ${cor}`}
          style={{ width: `${porcentagem}%` }}
        />
      </div>
      <p className={`text-xs ${corTexto}`}>
        Expira em: <strong>{expira.toLocaleDateString('pt-BR')}</strong>
      </p>
    </div>
  )
}

export default function Trial() {
  const [step, setStep] = useState(1)
  const [loading, setLoading] = useState(false)
  const [erro, setErro] = useState('')
  const [resultado, setResultado] = useState(null)
  const [copiado, setCopiado] = useState(false)

  const [form, setForm] = useState({
    nomeClinica: '', nomeDono: '', email: '', telefone: '',
    razaoSocial: '', cnpj: '', inscricaoEstadual: '',
    logradouro: '', numero: '', complemento: '', bairro: '',
    cidade: '', estado: '', cep: ''
  })

  const set = (k, v) => setForm(f => ({ ...f, [k]: v }))

  async function buscarCep(cep) {
    const c = cep.replace(/\D/g, '')
    if (c.length !== 8) return
    try {
      const r = await fetch(`https://viacep.com.br/ws/${c}/json/`)
      const d = await r.json()
      if (!d.erro) {
        set('logradouro', d.logradouro || '')
        set('bairro', d.bairro || '')
        set('cidade', d.localidade || '')
        set('estado', d.uf || '')
      }
    } catch { }
  }

  async function handleSubmit() {
    setErro('')
    setLoading(true)
    try {
      const r = await api.post('/trial', {
        nomeClinica:       form.nomeClinica.trim(),
        nomeDono:          form.nomeDono.trim(),
        email:             form.email.trim().toLowerCase(),
        telefone:          form.telefone,
        razaoSocial:       form.razaoSocial || null,
        cnpj:              form.cnpj.replace(/\D/g, '') || null,
        inscricaoEstadual: form.inscricaoEstadual || null,
        logradouro:        form.logradouro || null,
        numero:            form.numero || null,
        complemento:       form.complemento || null,
        bairro:            form.bairro || null,
        cidade:            form.cidade || null,
        estado:            form.estado || null,
        cep:               form.cep.replace(/\D/g, '') || null
      })
      if (r.data?.jaExiste) {
        window.location.href = '/login?msg=ja_existe'
        return
      }
      setResultado(r.data)
      setStep(3)
    } catch (err) {
      const msg = err.response?.data?.erro || 'Erro ao criar conta. Tente novamente.'
      setErro(msg)
    } finally {
      setLoading(false)
    }
  }

  function validarStep1() {
    if (!form.nomeClinica.trim()) return 'Informe o nome da clinica.'
    if (!form.nomeDono.trim()) return 'Informe seu nome.'
    if (!form.email.trim() || !form.email.includes('@')) return 'Informe um e-mail valido.'
    if (!form.telefone.trim()) return 'Informe seu telefone.'
    return ''
  }

  function copiarSenha() {
    navigator.clipboard.writeText(resultado.senha).then(() => {
      setCopiado(true)
      setTimeout(() => setCopiado(false), 2000)
    })
  }

  // TELA DE SUCESSO
  if (step === 3 && resultado) {
    return (
      <div className="min-h-screen bg-gradient-to-br from-slate-900 to-blue-950 flex items-center justify-center p-4">
        <div className="bg-white rounded-2xl shadow-2xl max-w-md w-full overflow-hidden">
          <div className="bg-gradient-to-r from-green-500 to-emerald-600 p-8 text-center">
            <div className="text-6xl mb-3">🎉</div>
            <h1 className="text-2xl font-bold text-white">Conta criada!</h1>
            <p className="text-green-100 mt-1">Seu trial de 14 dias comecou agora</p>
          </div>

          <div className="p-6 space-y-4">

            {/* Countdown do trial */}
            {resultado.trialExpiraEm && (
              <TrialCountdown
                trialExpiraEm={resultado.trialExpiraEm}
                diasRestantes={resultado.diasRestantes || 14}
              />
            )}

            {/* Dados de acesso */}
            <div className="bg-blue-50 border-2 border-blue-200 rounded-xl p-5">
              <h2 className="font-bold text-blue-900 mb-3">Seus dados de acesso</h2>
              <div className="space-y-2 text-sm">
                <div className="flex justify-between">
                  <span className="text-slate-500">Clinica:</span>
                  <span className="font-medium text-slate-800">{form.nomeClinica}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-slate-500">E-mail:</span>
                  <span className="font-medium text-slate-800">{resultado.loginEmail}</span>
                </div>
                <div className="flex justify-between items-center">
                  <span className="text-slate-500">Senha:</span>
                  <div className="flex items-center gap-2">
                    <span className="font-mono font-bold text-lg tracking-widest bg-slate-100 px-3 py-1 rounded-lg text-slate-800">
                      {resultado.senha}
                    </span>
                    <button
                      onClick={copiarSenha}
                      className="text-xs bg-slate-200 hover:bg-slate-300 px-2 py-1 rounded transition-colors"
                    >
                      {copiado ? 'Copiado!' : 'Copiar'}
                    </button>
                  </div>
                </div>
              </div>
            </div>

            {/* Status email */}
            {resultado.emailEnviado ? (
              <p className="text-sm text-green-700 bg-green-50 px-4 py-3 rounded-lg flex items-center gap-2">
                <span>Enviamos esses dados para <strong>{resultado.loginEmail}</strong></span>
              </p>
            ) : (
              <p className="text-sm text-amber-700 bg-amber-50 px-4 py-3 rounded-lg flex items-center gap-2">
                <span>Anote seus dados acima — nao conseguimos enviar o e-mail agora.</span>
              </p>
            )}

            {/* Proximo passo: definir senha */}
            <div className="bg-slate-50 rounded-xl p-4 text-sm text-slate-600">
              <p className="font-semibold text-slate-800 mb-1">Dica: defina sua propria senha</p>
              <p>Verifique seu e-mail — enviamos um link para voce criar uma senha personalizada.</p>
            </div>

            <a
              href="/login"
              className="block w-full bg-blue-600 hover:bg-blue-700 text-white text-center font-bold py-4 rounded-xl text-lg transition-colors"
            >
              Acessar o Sistema
            </a>

            {/* Link planos */}
            <p className="text-center text-sm text-slate-500">
              Quer contratar antes do trial acabar?{' '}
              <a href="/planos" className="text-blue-600 hover:underline font-medium">
                Ver planos e precos
              </a>
            </p>
          </div>
        </div>
      </div>
    )
  }

  // FORMULARIO
  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-900 to-blue-950 flex items-center justify-center p-4">
      <div className="bg-white rounded-2xl shadow-2xl max-w-lg w-full overflow-hidden">

        {/* Header */}
        <div className="bg-gradient-to-r from-slate-900 to-blue-900 p-6 text-center">
          <div className="text-4xl mb-2">🐾</div>
          <h1 className="text-xl font-bold text-white">VetClinica</h1>
          <p className="text-slate-300 text-sm">Gestao Inteligente para Clinicas Veterinarias</p>
          <div className="flex justify-center gap-3 mt-4 flex-wrap">
            <span className="bg-blue-500/20 text-blue-200 text-xs px-3 py-1 rounded-full border border-blue-400/30">
              14 dias gratis
            </span>
            <span className="bg-blue-500/20 text-blue-200 text-xs px-3 py-1 rounded-full border border-blue-400/30">
              Treinamento gratuito
            </span>
            <span className="bg-blue-500/20 text-blue-200 text-xs px-3 py-1 rounded-full border border-blue-400/30">
              Suporte incluido
            </span>
          </div>
        </div>

        {/* Steps indicator */}
        <div className="flex border-b border-slate-200">
          <button
            onClick={() => step > 1 && setStep(1)}
            className={`flex-1 py-3 text-sm font-medium text-center transition-colors ${
              step === 1 ? 'text-blue-600 border-b-2 border-blue-600 bg-blue-50' : 'text-slate-400 hover:text-slate-600'
            }`}
          >
            1. Dados da Clinica
          </button>
          <button
            onClick={() => step > 2 && setStep(2)}
            className={`flex-1 py-3 text-sm font-medium text-center transition-colors ${
              step === 2 ? 'text-blue-600 border-b-2 border-blue-600 bg-blue-50' : 'text-slate-400'
            }`}
          >
            2. Dados Fiscais
          </button>
        </div>

        <div className="p-6">

          {/* STEP 1 */}
          {step === 1 && (
            <div className="space-y-4">
              <div>
                <label className="block text-sm font-medium text-slate-700 mb-1">
                  Nome da Clinica / Pet Shop *
                </label>
                <input
                  value={form.nomeClinica}
                  onChange={e => set('nomeClinica', e.target.value)}
                  placeholder="Ex: Clinica Veterinaria Sao Francisco"
                  className="w-full border border-slate-300 rounded-lg px-3 py-2.5 text-sm focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-slate-700 mb-1">
                  Seu Nome Completo *
                </label>
                <input
                  value={form.nomeDono}
                  onChange={e => set('nomeDono', e.target.value)}
                  placeholder="Ex: Dra. Ana Silva"
                  className="w-full border border-slate-300 rounded-lg px-3 py-2.5 text-sm focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-slate-700 mb-1">E-mail *</label>
                <input
                  type="email"
                  value={form.email}
                  onChange={e => set('email', e.target.value)}
                  placeholder="seu@email.com.br"
                  className="w-full border border-slate-300 rounded-lg px-3 py-2.5 text-sm focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none"
                />
                <p className="text-xs text-slate-400 mt-1">Este sera seu login no sistema</p>
              </div>
              <div>
                <label className="block text-sm font-medium text-slate-700 mb-1">
                  WhatsApp / Telefone *
                </label>
                <input
                  value={form.telefone}
                  onChange={e => set('telefone', mascaraTel(e.target.value))}
                  placeholder="(31) 99999-9999"
                  className="w-full border border-slate-300 rounded-lg px-3 py-2.5 text-sm focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none"
                />
                <p className="text-xs text-slate-400 mt-1">Usado para envio de codigo de recuperacao de senha</p>
              </div>

              {erro && (
                <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg text-sm">
                  {erro}
                </div>
              )}

              <button
                onClick={() => {
                  const v = validarStep1()
                  if (v) { setErro(v); return }
                  setErro('')
                  setStep(2)
                }}
                className="w-full bg-blue-600 hover:bg-blue-700 text-white font-bold py-3 rounded-xl transition-colors"
              >
                Continuar
              </button>

              <p className="text-center text-sm text-slate-500">
                Ja tem conta?{' '}
                <a href="/login" className="text-blue-600 hover:underline font-medium">
                  Fazer login
                </a>
              </p>
            </div>
          )}

          {/* STEP 2 */}
          {step === 2 && (
            <div className="space-y-4">
              <p className="text-sm text-slate-500 bg-slate-50 px-4 py-3 rounded-lg">
                Dados fiscais para emissao futura de notas fiscais. Podem ser preenchidos depois nas configuracoes.
              </p>

              <div>
                <label className="block text-sm font-medium text-slate-700 mb-1">Razao Social</label>
                <input
                  value={form.razaoSocial}
                  onChange={e => set('razaoSocial', e.target.value)}
                  placeholder="Razao Social ou Nome Completo"
                  className="w-full border border-slate-300 rounded-lg px-3 py-2.5 text-sm focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none"
                />
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block text-sm font-medium text-slate-700 mb-1">CNPJ</label>
                  <input
                    value={form.cnpj}
                    onChange={e => set('cnpj', mascaraCnpj(e.target.value))}
                    placeholder="00.000.000/0000-00"
                    className="w-full border border-slate-300 rounded-lg px-3 py-2.5 text-sm focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-slate-700 mb-1">Inscricao Estadual</label>
                  <input
                    value={form.inscricaoEstadual}
                    onChange={e => set('inscricaoEstadual', e.target.value)}
                    placeholder="Isento ou numero"
                    className="w-full border border-slate-300 rounded-lg px-3 py-2.5 text-sm focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none"
                  />
                </div>
              </div>

              <div className="grid grid-cols-3 gap-3">
                <div className="col-span-2">
                  <label className="block text-sm font-medium text-slate-700 mb-1">CEP</label>
                  <input
                    value={form.cep}
                    onChange={e => {
                      const v = mascaraCep(e.target.value)
                      set('cep', v)
                      if (v.replace(/\D/g, '').length === 8) buscarCep(v)
                    }}
                    placeholder="00000-000"
                    className="w-full border border-slate-300 rounded-lg px-3 py-2.5 text-sm focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-slate-700 mb-1">Estado</label>
                  <select
                    value={form.estado}
                    onChange={e => set('estado', e.target.value)}
                    className="w-full border border-slate-300 rounded-lg px-3 py-2.5 text-sm focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none bg-white"
                  >
                    <option value="">UF</option>
                    {ESTADOS.map(uf => <option key={uf}>{uf}</option>)}
                  </select>
                </div>
              </div>

              <div>
                <label className="block text-sm font-medium text-slate-700 mb-1">Logradouro</label>
                <input
                  value={form.logradouro}
                  onChange={e => set('logradouro', e.target.value)}
                  placeholder="Rua, Avenida..."
                  className="w-full border border-slate-300 rounded-lg px-3 py-2.5 text-sm focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none"
                />
              </div>

              <div className="grid grid-cols-3 gap-3">
                <div>
                  <label className="block text-sm font-medium text-slate-700 mb-1">Numero</label>
                  <input
                    value={form.numero}
                    onChange={e => set('numero', e.target.value)}
                    placeholder="123"
                    className="w-full border border-slate-300 rounded-lg px-3 py-2.5 text-sm focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none"
                  />
                </div>
                <div className="col-span-2">
                  <label className="block text-sm font-medium text-slate-700 mb-1">Complemento</label>
                  <input
                    value={form.complemento}
                    onChange={e => set('complemento', e.target.value)}
                    placeholder="Sala, Apto..."
                    className="w-full border border-slate-300 rounded-lg px-3 py-2.5 text-sm focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none"
                  />
                </div>
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block text-sm font-medium text-slate-700 mb-1">Bairro</label>
                  <input
                    value={form.bairro}
                    onChange={e => set('bairro', e.target.value)}
                    className="w-full border border-slate-300 rounded-lg px-3 py-2.5 text-sm focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-slate-700 mb-1">Cidade</label>
                  <input
                    value={form.cidade}
                    onChange={e => set('cidade', e.target.value)}
                    className="w-full border border-slate-300 rounded-lg px-3 py-2.5 text-sm focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none"
                  />
                </div>
              </div>

              {erro && (
                <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg text-sm">
                  {erro}
                </div>
              )}

              <div className="flex gap-3">
                <button
                  onClick={() => setStep(1)}
                  className="flex-1 border border-slate-300 text-slate-600 font-medium py-3 rounded-xl hover:bg-slate-50 transition-colors"
                >
                  Voltar
                </button>
                <button
                  onClick={handleSubmit}
                  disabled={loading}
                  className="flex-2 flex-grow bg-blue-600 hover:bg-blue-700 disabled:bg-blue-400 text-white font-bold py-3 rounded-xl transition-colors"
                >
                  {loading ? (
                    <span className="flex items-center justify-center gap-2">
                      <svg className="animate-spin h-4 w-4" viewBox="0 0 24 24" fill="none">
                        <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"/>
                        <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8z"/>
                      </svg>
                      Criando conta...
                    </span>
                  ) : 'Criar minha conta gratis'}
                </button>
              </div>

              <p className="text-center text-xs text-slate-400">
                Ao criar sua conta voce concorda com os{' '}
                <a href="#" className="underline">Termos de Uso</a> e{' '}
                <a href="#" className="underline">Politica de Privacidade</a>
              </p>
            </div>
          )}
        </div>

        <div className="bg-slate-50 px-6 py-4 text-center border-t border-slate-200">
          <p className="text-xs text-slate-400">
            Suporte: <a href="mailto:suporte@ketra.com.br" className="text-blue-500 hover:underline">suporte@ketra.com.br</a>
            {' · '}
            <strong>Ketra Solucoes Inteligentes</strong>
          </p>
        </div>
      </div>
    </div>
  )
}
