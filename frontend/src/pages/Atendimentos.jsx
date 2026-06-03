import { useEffect, useState } from 'react'
import api from '../api/client'

const COLUNAS = [
  { status: 'aguardando',   label: 'Aguardando',       cor: 'bg-slate-100',  txt: 'text-slate-700',  btn: 'Iniciar Atendimento', proximoStatus: 'em_andamento' },
  { status: 'em_andamento', label: 'Em Atendimento',   cor: 'bg-blue-50',    txt: 'text-blue-700',   btn: 'Finalizar',            proximoStatus: 'entregue'     },
]

export default function Atendimentos() {
  const [oss, setOss]           = useState([])
  const [financial, setFinancial] = useState([])
  const [modal, setModal]       = useState(false)
  const [loading, setLoading]   = useState(false)

  async function carregar() {
    try {
      const { data } = await api.get('/ordens-servico/abertas')
      setOss(data.filter(o => o.status === 'aguardando' || o.status === 'em_andamento'))
      setFinancial(data.filter(o => o.status === 'entregue'))
    } catch {}
  }

  useEffect(() => { carregar() }, [])

  async function avancar(os, proximoStatus) {
    if (loading) return
    setLoading(true)
    try {
      await api.put(`/ordens-servico/${os.id}/status`, { status: proximoStatus })
      await carregar()
    } catch (e) {
      alert(e.response?.data?.erro || 'Erro ao atualizar status')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div>
      <div className="flex justify-between items-center mb-6">
        <div>
          <h2 className="text-2xl font-bold">Atendimentos</h2>
          {financial.length > 0 && (
            <p className="text-sm text-emerald-600 font-medium mt-1">
              💰 {financial.length} atendimento{financial.length > 1 ? 's' : ''} aguardando cobrança no PDV
            </p>
          )}
        </div>
        <button onClick={() => setModal(true)}
          className="bg-slate-900 text-white px-4 py-2 rounded-lg text-sm font-medium">
          + Nova OS
        </button>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        {COLUNAS.map(col => (
          <div key={col.status} className={`${col.cor} rounded-2xl p-3`}>
            <div className="flex items-center justify-between mb-3 px-1">
              <h3 className={`font-semibold text-sm ${col.txt}`}>{col.label}</h3>
              {oss.filter(o => o.status === col.status).length > 0 && (
                <span className={`text-xs font-bold px-2 py-0.5 rounded-full bg-white ${col.txt}`}>
                  {oss.filter(o => o.status === col.status).length}
                </span>
              )}
            </div>
            <div className="flex flex-col gap-2">
              {oss.filter(o => o.status === col.status).map(os => (
                <div key={os.id}
                  className="bg-white rounded-xl shadow-sm p-3 border border-white hover:border-slate-200 transition">
                  <div className="font-medium text-slate-800">{os.tutorNome ?? '—'}</div>
                  <div className="text-xs text-slate-400 mb-1">🐾 {os.petNome}</div>
                  {os.funcionarioNome && (
                    <div className="text-xs text-blue-600 mb-1">👤 {os.funcionarioNome}</div>
                  )}
                  <div className="text-sm font-semibold text-emerald-600 mb-2">
                    R$ {os.valorTotal?.toFixed(2) ?? '0,00'}
                  </div>
                  <button
                    onClick={() => avancar(os, col.proximoStatus)}
                    disabled={loading}
                    className="text-xs w-full bg-slate-900 text-white py-1.5 rounded-lg hover:bg-slate-700 transition disabled:opacity-50">
                    {col.btn} →
                  </button>
                </div>
              ))}
              {oss.filter(o => o.status === col.status).length === 0 && (
                <p className="text-xs text-slate-400 text-center py-6">Vazio</p>
              )}
            </div>
          </div>
        ))}

        <div className="bg-emerald-50 rounded-2xl p-3">
          <div className="flex items-center justify-between mb-3 px-1">
            <h3 className="font-semibold text-sm text-emerald-700">💰 Financeiro</h3>
            {financial.length > 0 && (
              <span className="text-xs font-bold px-2 py-0.5 rounded-full bg-white text-emerald-700">
                {financial.length}
              </span>
            )}
          </div>
          <div className="flex flex-col gap-2">
            {financial.map(os => (
              <div key={os.id} className="bg-white rounded-xl shadow-sm p-3 border border-emerald-100">
                <div className="font-medium text-slate-800">{os.tutorNome ?? '—'}</div>
                <div className="text-xs text-slate-400 mb-1">🐾 {os.petNome}</div>
                {os.funcionarioNome && (
                  <div className="text-xs text-blue-600 mb-1">👤 {os.funcionarioNome}</div>
                )}
                <div className="text-sm font-semibold text-emerald-600 mb-2">
                  R$ {os.valorTotal?.toFixed(2) ?? '0,00'}
                </div>
                <div className="text-xs text-emerald-600 bg-emerald-50 rounded px-2 py-1 text-center">
                  Aguardando cobrança no PDV
                </div>
              </div>
            ))}
            {financial.length === 0 && (
              <p className="text-xs text-slate-400 text-center py-6">Nenhum pendente</p>
            )}
          </div>
        </div>
      </div>

      {modal && (
        <ModalNovaOS
          onClose={() => setModal(false)}
          onSaved={() => { setModal(false); carregar() }}
        />
      )}
    </div>
  )
}

function ModalNovaOS({ onClose, onSaved }) {
  const [pets, setPets]               = useState([])
  const [servicos, setServicos]       = useState([])
  const [funcionarios, setFuncionarios] = useState([])
  const [petId, setPetId]             = useState('')
  const [funcionarioId, setFuncionarioId] = useState('')
  const [itens, setItens]             = useState([])
  const [saving, setSaving]           = useState(false)

  useEffect(() => {
    api.get('/pets', { params: { pageSize: 200 } }).then(r => setPets(r.data.items)).catch(() => {})
    api.get('/servicos').then(r => setServicos(r.data)).catch(() => {})
    api.get('/rh/funcionarios').then(r => setFuncionarios(r.data.filter(f => f.status === 'trabalhando'))).catch(() => {})
  }, [])

  function addServico(s) {
    if (itens.find(i => i.servicoId === s.id)) return
    setItens([...itens, { servicoId: s.id, nome: s.nome, precoCobrado: s.precoBase }])
  }
  function remover(sId) { setItens(itens.filter(i => i.servicoId !== sId)) }
  function mudarPreco(sId, preco) {
    setItens(itens.map(i => i.servicoId === sId ? { ...i, precoCobrado: +preco } : i))
  }

  const total = itens.reduce((s, i) => s + i.precoCobrado, 0)

  async function salvar(e) {
    e.preventDefault()
    if (!petId || itens.length === 0) return
    setSaving(true)
    try {
      await api.post('/ordens-servico', {
        petId,
        agendamentoId: null,
        funcionarioId: funcionarioId || null,
        servicos: itens.map(i => ({ servicoId: i.servicoId, precoCobrado: i.precoCobrado, obs: null }))
      })
      onSaved()
    } catch (e) {
      alert(e.response?.data?.erro || 'Erro ao abrir OS')
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50 p-4"
      onClick={onClose}>
      <div className="bg-white rounded-2xl shadow-xl p-6 w-full max-w-lg"
        onClick={e => e.stopPropagation()}>
        <h3 className="font-bold text-lg mb-4">Nova Ordem de Serviço</h3>
        <form onSubmit={salvar} className="grid gap-3">

          <select className="border rounded-lg px-3 py-2" required
            value={petId} onChange={e => setPetId(e.target.value)}>
            <option value="">Selecione o pet...</option>
            {pets.map(p => (
              <option key={p.id} value={p.id}>{p.nome} — {p.tutorNome}</option>
            ))}
          </select>

          <select className="border rounded-lg px-3 py-2"
            value={funcionarioId} onChange={e => setFuncionarioId(e.target.value)}>
            <option value="">Funcionário responsável (opcional)</option>
            {funcionarios.map(f => (
              <option key={f.id} value={f.id}>{f.nome} {f.cargo ? `— ${f.cargo}` : ''} ({f.percentualComissao}% comissão)</option>
            ))}
          </select>

          <div>
            <label className="text-xs text-slate-500 block mb-1">Serviços</label>
            <div className="flex flex-wrap gap-1">
              {servicos.map(s => (
                <button type="button" key={s.id} onClick={() => addServico(s)}
                  className="text-xs bg-slate-100 hover:bg-slate-200 px-2 py-1 rounded transition">
                  + {s.nome} (R$ {s.precoBase?.toFixed(2)})
                </button>
              ))}
            </div>
          </div>

          {itens.length > 0 && (
            <div className="bg-slate-50 rounded-lg p-3 space-y-2">
              {itens.map(i => (
                <div key={i.servicoId} className="flex items-center gap-2 text-sm">
                  <span className="flex-1">{i.nome}</span>
                  <span className="text-slate-400">R$</span>
                  <input type="number" step="0.01" className="border rounded px-2 py-1 w-24 text-right"
                    value={i.precoCobrado}
                    onChange={e => mudarPreco(i.servicoId, e.target.value)} />
                  <button type="button" onClick={() => remover(i.servicoId)}
                    className="text-red-400 hover:text-red-600">×</button>
                </div>
              ))}
              <div className="text-right font-bold pt-2 border-t text-sm">
                Total: R$ {total.toFixed(2)}
              </div>
            </div>
          )}

          <button
            disabled={!petId || itens.length === 0 || saving}
            className="bg-emerald-600 text-white py-2 rounded-lg disabled:bg-slate-300 font-medium">
            {saving ? 'Salvando...' : 'Abrir OS'}
          </button>
        </form>
      </div>
    </div>
  )
}
