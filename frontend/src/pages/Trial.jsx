import { useState } from 'react'
import api from '../services/api'

// Mascara CNPJ: 00.000.000/0000-00
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

export default function Trial() {
  const [step, setStep] = useState(1) // 1=dados clínica, 2=dados fiscais, 3=sucesso
  const [loading, setLoading] = useState(false)
  const [erro, setErro] = useState('')
  const [resultado, setResultado] = useState(null)

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
    if (!form.nomeClinica.trim()) return 'Informe o nome da clínica.'
    if (!form.nomeDono.trim()) return 'Informe seu nome.'
    if (!form.email.trim() || !form.email.includes('@')) return 'Informe um e-mail válido.'
    if (!form.telefone.trim()) return 'Informe seu telefone.'
    return ''
  }

  if (step === 3 && resultado) {
    return (
      <div className="min-h-screen bg-gradient-to-br from-slate-900 to-blue-950 flex items-center justify-center p-4">
        <div className="bg-white rounded-2xl shadow-2xl max-w-md w-full overflow-hidden">
          {/* Header */}
          <div className="bg-gradient-to-r from-green-500 to-emerald-600 p-8 text-center">
            <div className="text-6xl mb-3">🎉</div>
            <h1 className="text-2xl font-bold text-white">Conta criada com sucesso!</h1>
            <p className="text-green-100 mt-1">Seu trial de 14 dias começou agora</p>
          </div>

          {/* Dados de acesso */}
          <div className="p-6">
            <div className="bg-blue-50 border-2 border-blue-200 rounded-xl p-5 mb-5">
              <h2 className="font-bold text-blue-900 mb-3 flex items-center gap-2">
                🔑 Seus dados de acesso
              </h2>
              <div className="space-y-2 text-sm">
                <div className="flex justify-between">
                  <span className="text-slate-500">Clínica:</span>
                  <span className="font-medium text-slate-800">{form.nomeClinica}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-slate-500">E-mail:</span>
                  <span className="font-medium text-slate-800">{resultado.loginEmail}</span>
                </div>
                <div className="flex justify-between items-center">
                  <span className="text-slate-500">Senha:</span>
                  <span className="font-mono font-bold text-lg tracking-widest bg-slate-100 px-3 py-1 rounded-lg text-slate-800">
                    {resultado.senha}
                  </span>
                </div>
              </div>
            </div>

            {resultado.emailEnviado ? (
              <p className="text-sm text-green-700 bg-green-50 px-4 py-3 rounded-lg mb-4 flex items-center gap-2">
                ✉️ <span>Enviamos esses dados para <strong>{resultado.loginEmail}</strong></span>
              </p>
            ) : (
              <p className="text-sm text-amber-700 bg-amber-50 px-4 py-3 rounded-lg mb-4 flex items-center gap-2">
                ⚠️ <span>Anote seus dados acima — não conseguimos enviar o e-mail agora.</span>
              </p>
            )}

            <div className="bg-slate-50 rounded-xl p-4 mb-5 text-sm text-slate-600">
              <p className="font-semibold text-slate-800 mb-2">🎓 Treinamento gratuito</p>
              <p>Precisa de ajuda para começar? Nossa equipe oferece treinamento gratuito.</p>
              <a href="mailto:suporte@ketra.com.br" className="text-blue-600 font-medium hover:underline">
                suporte@ketra.com.br
              </a>
            </div>

            <a
              href="/login"
              className="block w-full bg-blue-600 hover:bg-blue-700 text-white text-center font-bold py-4 rounded-xl text-lg transition-colors"
            >
              Acessar o Sistema →
            </a>
          </div>
        </div>
      </div>
    )
  }

  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-900 to-blue-950 flex items-center justify-center p-4">
      <div className="bg-white rounded-2xl shadow-2xl max-w-lg w-full overflow-hidden">

        {/* Header */}
        <div className="bg-gradient-to-r from-slate-900 to-blue-900 p-6 text-center">
          <div className="text-4xl mb-2">🐾</div>
          <h1 className="text-xl font-bold text-white">VetClinica</h1>
          <p className="text-slate-300 text-sm">Gestão Inteligente para Clínicas Veterinárias</p>

          {/* Badges */}
          <div className="flex justify-center gap-3 mt-4 flex-wrap">
            <span className="bg-blue-500/20 text-blue-200 text-xs px-3 py-1 rounded-full border border-blue-400/30">
              ✅ 14 dias grátis
            </span>
            <span className="bg-blue-500/20 text-blue-200 text-xs px-3 py-1 rounded-full border border-blue-400/30">
              🎓 Treinamento gratuito
            </span>
            <span className="bg-blue-500/20 text-blue-200 text-xs px-3 py-1 rounded-full border border-blue-400/30">
              💬 Suporte incluído
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
            1. Dados da Clínica
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

          {/* STEP 1 - Dados da Clínica */}
          {step === 1 && (
            <div className="space-y-4">
              <div>
                <label className="block text-sm font-medium text-slate-700 mb-1">
                  Nome da Clínica / Pet Shop *
                </label>
                <input
                  value={form.nomeClinica}
                  onChange={e => set('nomeClinica', e.target.value)}
                  placeholder="Ex: Clínica Veterinária São Francisco"
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
                <label className="block text-sm font-medium text-slate-700 mb-1">
                  E-mail *
                </label>
                <input
                  type="email"
                  value={form.email}
                  onChange={e => set('email', e.target.value)}
                  placeholder="seu@email.com.br"
                  className="w-full border border-slate-300 rounded-lg px-3 py-2.5 text-sm focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none"
                />
                <p className="text-xs text-slate-400 mt-1">Este será seu login no sistema</p>
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
                Continuar →
              </button>

              <p className="text-center text-sm text-slate-500">
                Já tem conta?{' '}
                <a href="/login" className="text-blue-600 hover:underline font-medium">
                  Fazer login
                </a>
              </p>
            </div>
          )}

          {/* STEP 2 - Dados Fiscais */}
          {step === 2 && (
            <div className="space-y-4">
              <p className="text-sm text-slate-500 bg-slate-50 px-4 py-3 rounded-lg">
                💡 Dados fiscais são necessários para emissão futura de notas fiscais. Podem ser preenchidos depois nas configurações.
              </p>

              <div>
                <label className="block text-sm font-medium text-slate-700 mb-1">
                  Razão Social
                </label>
                <input
                  value={form.razaoSocial}
                  onChange={e => set('razaoSocial', e.target.value)}
                  placeholder="Razão Social ou Nome Completo"
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
                  <label className="block text-sm font-medium text-slate-700 mb-1">Inscrição Estadual</label>
                  <input
                    value={form.inscricaoEstadual}
                    onChange={e => set('inscricaoEstadual', e.target.value)}
                    placeholder="Isento ou número"
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
                      if (v.replace(/\D/g,'').length === 8) buscarCep(v)
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
                  <label className="block text-sm font-medium text-slate-700 mb-1">Número</label>
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
                  ← Voltar
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
                  ) : 'Criar minha conta grátis 🚀'}
                </button>
              </div>

              <p className="text-center text-xs text-slate-400">
                Ao criar sua conta você concorda com os{' '}
                <a href="#" className="underline">Termos de Uso</a> e{' '}
                <a href="#" className="underline">Política de Privacidade</a>
              </p>
            </div>
          )}
        </div>

        {/* Footer */}
        <div className="bg-slate-50 px-6 py-4 text-center border-t border-slate-200">
          <p className="text-xs text-slate-400">
            Suporte: <a href="mailto:suporte@ketra.com.br" className="text-blue-500 hover:underline">suporte@ketra.com.br</a>
            {' · '}
            <strong>Ketra Soluções Inteligentes</strong>
          </p>
        </div>
      </div>
    </div>
  )
}
