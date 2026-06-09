import { useEffect, useState } from 'react'
import api from '../../api/client'

const STATUS_CFG = {
  trabalhando: { label: '🟢 Trabalhando', cls: 'bg-green-100 text-green-700' },
  ferias:      { label: '🟡 Férias',      cls: 'bg-yellow-100 text-yellow-700' },
  demitido:    { label: '🔴 Demitido',    cls: 'bg-red-100 text-red-700' },
}

const ESTADOS = ['AC','AL','AP','AM','BA','CE','DF','ES','GO','MA','MT','MS','MG','PA',
  'PB','PR','PE','PI','RJ','RN','RS','RO','RR','SC','SP','SE','TO']

const VAZIO = {
  nome: '', codigo: '', cpf: '', rg: '', dataNascimento: '',
  logradouro: '', numero: '', complemento: '', bairro: '', cidade: '', estado: '', cep: '',
  telefone: '', email: '', cargo: '', crmv: '', registroMapa: '', dataAdmissao: '',
  salario: '', percentualComissao: '', status: 'trabalhando', assinaReceituario: false
}

function maskCpf(v)   { return v.replace(/\D/g,'').slice(0,11).replace(/(\d{3})(\d)/,'$1.$2').replace(/(\d{3})(\d)/,'$1.$2').replace(/(\d{3})(\d{1,2})$/,'$1-$2') }
function maskPhone(v) { return v.replace(/\D/g,'').slice(0,11).replace(/(\d{2})(\d)/,'($1) $2').replace(/(\d{5})(\d{4})$/,'$1-$2') }
function maskCep(v)   { return v.replace(/\D/g,'').slice(0,8).replace(/(\d{5})(\d)/,'$1-$2') }

async function buscaCep(cep, setForm) {
  const limpo = cep.replace(/\D/g,'')
  if (limpo.length !== 8) return
  try {
    const r = await fetch(`https://viacep.com.br/ws/${limpo}/json/`)
    const d = await r.json()
    if (!d.erro) setForm(f => ({ ...f, logradouro: d.logradouro||'', bairro: d.bairro||'', cidade: d.localidade||'', estado: d.uf||'' }))
  } catch {}
}

function Sec({ title, children }) {
  return (
    <div className="border rounded-xl p-4">
      <h3 className="text-xs font-semibold text-slate-500 uppercase tracking-wide mb-3">{title}</h3>
      <div className="grid grid-cols-2 gap-3">{children}</div>
    </div>
  )
}

// ─── Formulário ──────────────────────────────────────────────────────────────
function Form({ initial, onSalvar, onCancelar }) {
  const [form, setForm] = useState(initial ? {
    ...initial,
    dataNascimento: initial.dataNascimento ? String(initial.dataNascimento).slice(0,10) : '',
    dataAdmissao:   initial.dataAdmissao   ? String(initial.dataAdmissao).slice(0,10)   : '',
    salario: initial.salario ?? '',
    percentualComissao: initial.percentualComissao ?? '',
    assinaReceituario: initial.assinaReceituario ?? false
  } : { ...VAZIO })
  const [saving, setSaving] = useState(false)

  const set = (field, value) => setForm(p => ({ ...p, [field]: value }))

  async function salvar() {
    if (!form.nome.trim()) return alert('Nome é obrigatório.')
    setSaving(true)
    try {
      const payload = {
        ...form,
        salario: parseFloat(form.salario) || 0,
        percentualComissao: parseFloat(form.percentualComissao) || 0,
        dataNascimento: form.dataNascimento || null,
        dataAdmissao:   form.dataAdmissao   || null,
      }
      if (initial?.id) await api.put(`/rh/funcionarios/${initial.id}`, payload)
      else             await api.post('/rh/funcionarios', payload)
      onSalvar()
    } catch (e) {
      alert('Erro ao salvar: ' + (e.response?.data?.erro || e.message))
    } finally { setSaving(false) }
  }

  // Campo simples — sem máscara no onChange
  const campo = (label, field, type='text', colSpan='') => (
    <div className={colSpan}>
      <label className="block text-xs font-medium text-slate-500 mb-1">{label}</label>
      <input
        type={type}
        value={form[field]}
        onChange={e => set(field, e.target.value)}
        className="w-full border rounded-lg px-3 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
      />
    </div>
  )

  // Campo com máscara aplicada só no onBlur
  const campoMask = (label, field, maskFn, colSpan='') => (
    <div className={colSpan}>
      <label className="block text-xs font-medium text-slate-500 mb-1">{label}</label>
      <input
        type="text"
        value={form[field]}
        onChange={e => set(field, e.target.value)}
        onBlur={e => set(field, maskFn(e.target.value))}
        className="w-full border rounded-lg px-3 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
      />
    </div>
  )

  const select = (label, field, options) => (
    <div>
      <label className="block text-xs font-medium text-slate-500 mb-1">{label}</label>
      <select
        value={form[field]}
        onChange={e => set(field, e.target.value)}
        className="w-full border rounded-lg px-3 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
      >
        <option value="">Selecione...</option>
        {options.map(o => <option key={o.v||o} value={o.v||o}>{o.l||o}</option>)}
      </select>
    </div>
  )

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h2 className="text-lg font-semibold">{initial?.id ? 'Editar Funcionário' : 'Novo Funcionário'}</h2>
        <button onClick={onCancelar} className="text-sm text-slate-500 hover:text-slate-700">← Voltar</button>
      </div>

      <Sec title="Dados Pessoais">
        <div className="flex gap-2 col-span-2">
          <div className="w-28">{campo('Codigo', 'codigo')}</div>
          <div className="flex-1">{campo('Nome completo *', 'nome', 'text')}</div>
        </div>
        {campoMask('CPF', 'cpf', maskCpf)}
        {campo('RG', 'rg')}
        {campo('Data de Nascimento', 'dataNascimento', 'date')}
      </Sec>

      <Sec title="Endereço">
        <div>
          <label className="block text-xs font-medium text-slate-500 mb-1">CEP</label>
          <input
            type="text"
            value={form.cep}
            onChange={e => set('cep', e.target.value)}
            onBlur={e => {
              const v = maskCep(e.target.value)
              set('cep', v)
              buscaCep(v, setForm)
            }}
            className="w-full border rounded-lg px-3 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
        </div>
        {campo('Logradouro', 'logradouro', 'text', 'col-span-2')}
        {campo('Número', 'numero')}
        {campo('Complemento', 'complemento')}
        {campo('Bairro', 'bairro')}
        {campo('Cidade', 'cidade')}
        {select('Estado', 'estado', ESTADOS)}
      </Sec>

      <Sec title="Contato">
        {campoMask('Telefone', 'telefone', maskPhone)}
        {campo('E-mail', 'email', 'email')}
      </Sec>

      <Sec title="Dados Profissionais">
        {campo('Cargo', 'cargo')}
        {campo('CRMV (Conselho Regional de Medicina Veterinária)', 'crmv')}
        <div className="col-span-2 flex items-center gap-3 mt-1">
          <button
            type="button"
            onClick={() => setForm(f => ({ ...f, assinaReceituario: !f.assinaReceituario }))}
            className={`relative inline-flex h-6 w-11 items-center rounded-full transition-colors flex-shrink-0 ${form.assinaReceituario ? 'bg-emerald-600' : 'bg-slate-200'}`}>
            <span className={`inline-block h-4 w-4 transform rounded-full bg-white transition-transform ${form.assinaReceituario ? 'translate-x-6' : 'translate-x-1'}`} />
          </button>
          <div>
            <span className="text-sm font-medium text-slate-700">Assina receituário</span>
            <p className="text-xs text-slate-400">Aparece na lista de veterinários no receituário</p>
          </div>
        </div>
        {campo('Registro no MAPA', 'registroMapa')}
        {campo('Data de Admissão', 'dataAdmissao', 'date')}
        {campo('Salário (R$)', 'salario', 'number')}
        {campo('% Comissão', 'percentualComissao', 'number')}
        {select('Status', 'status', [
          { v:'trabalhando', l:'🟢 Trabalhando' },
          { v:'ferias',      l:'🟡 Férias' },
          { v:'demitido',    l:'🔴 Demitido' },
        ])}
      </Sec>

      <p className="text-xs text-amber-600 bg-amber-50 rounded-lg px-3 py-2">
        ⚠️ Salário é informação sigilosa — visível apenas para administradores.
      </p>

      <div className="flex gap-3">
        <button onClick={salvar} disabled={saving}
          className="bg-blue-600 text-white px-5 py-2 rounded-lg text-sm hover:bg-blue-700 disabled:opacity-50">
          {saving ? 'Salvando...' : 'Salvar Funcionário'}
        </button>
        <button onClick={onCancelar} className="text-sm text-slate-500 px-5 py-2 rounded-lg border hover:bg-slate-50">
          Cancelar
        </button>
      </div>
    </div>
  )
}

// ─── Página principal ─────────────────────────────────────────────────────────
export default function Funcionarios() {
  const [lista, setLista]       = useState([])
  const [busca, setBusca]       = useState('')
  const [filtro, setFiltro]     = useState('todos')
  const [editando, setEditando] = useState(null)
  const [loading, setLoading]   = useState(true)

  useEffect(() => { carregar() }, [])

  async function carregar() {
    setLoading(true)
    try { const { data } = await api.get('/rh/funcionarios'); setLista(data) }
    finally { setLoading(false) }
  }

  if (editando !== null) {
    return (
      <Form
        initial={editando?.id ? editando : null}
        onSalvar={() => { setEditando(null); carregar() }}
        onCancelar={() => setEditando(null)}
      />
    )
  }

  const exibidos = lista.filter(f => {
    const ok = f.nome.toLowerCase().includes(busca.toLowerCase()) || (f.cargo||'').toLowerCase().includes(busca.toLowerCase())
    const st = filtro === 'todos' || f.status === filtro
    return ok && st
  })

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold">Funcionários</h1>
          <p className="text-sm text-slate-500">{lista.length} cadastrado(s)</p>
        </div>
        <button onClick={() => setEditando({})}
          className="bg-blue-600 text-white px-4 py-2 rounded-lg text-sm hover:bg-blue-700">
          + Novo Funcionário
        </button>
      </div>

      <div className="flex gap-3 mb-4">
        <input value={busca} onChange={e => setBusca(e.target.value)}
          placeholder="Buscar por nome ou cargo..."
          className="flex-1 border rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
        {['todos','trabalhando','ferias','demitido'].map(s => (
          <button key={s} onClick={() => setFiltro(s)}
            className={`px-3 py-2 rounded-lg text-sm capitalize ${filtro===s ? 'bg-blue-600 text-white' : 'bg-slate-100 text-slate-600 hover:bg-slate-200'}`}>
            {s === 'todos' ? 'Todos' : STATUS_CFG[s].label}
          </button>
        ))}
      </div>

      {loading ? (
        <p className="text-slate-400 text-center py-10">Carregando...</p>
      ) : (
        <div className="bg-white rounded-xl border divide-y">
          {exibidos.length === 0 && (
            <p className="text-slate-400 text-center py-10">Nenhum funcionário encontrado.</p>
          )}
          {exibidos.map(f => {
            const cfg = STATUS_CFG[f.status] || STATUS_CFG.trabalhando
            return (
              <div key={f.id} onClick={() => setEditando(f)}
                className="flex items-center justify-between px-4 py-3 hover:bg-slate-50 cursor-pointer">
                <div className="flex items-center gap-3">
                  <div className="w-9 h-9 rounded-full bg-blue-100 flex items-center justify-center text-blue-700 font-semibold text-sm">
                    {f.nome.charAt(0).toUpperCase()}
                  </div>
                  <div>
                    <p className="font-medium text-slate-800 text-sm">{f.nome}</p>
                    <p className="text-xs text-slate-500">{f.cargo || '—'}</p>
                  </div>
                </div>
                <div className="flex items-center gap-4">
                  <span className="text-xs text-slate-500">{f.percentualComissao}% comissão</span>
                  <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${cfg.cls}`}>{cfg.label}</span>
                  <span className="text-slate-300">›</span>
                </div>
              </div>
            )
          })}
        </div>
      )}
    </div>
  )
}
