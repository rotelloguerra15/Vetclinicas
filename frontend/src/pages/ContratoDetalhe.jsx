import { useEffect, useState } from 'react'
import { useParams, Link } from 'react-router-dom'
import api from '../api/client'

const STATUS_MEDICAO_COR = {
  pendente: 'bg-slate-100 text-slate-600',
  medido: 'bg-amber-100 text-amber-700',
  aprovado: 'bg-emerald-100 text-emerald-700',
  rejeitado: 'bg-red-100 text-red-700'
}

export default function ContratoDetalhe() {
  const { id } = useParams()
  const [contrato, setContrato] = useState(null)
  const [parcelaEmEdicao, setParcelaEmEdicao] = useState(null) // { id, modo: 'medir'|'rejeitar' }
  const [quantidade, setQuantidade] = useState('')
  const [obsMedicao, setObsMedicao] = useState('')
  const [motivoRejeicao, setMotivoRejeicao] = useState('')
  const [erro, setErro] = useState('')

  const papel = localStorage.getItem('papel')
  const podeAprovar = papel === 'owner' || papel === 'admin'

  function carregar() {
    api.get(`/contratos/${id}`).then(r => setContrato(r.data)).catch(() => {})
  }
  useEffect(() => { carregar() }, [id])

  async function medir(parcelaId) {
    setErro('')
    try {
      await api.post(`/contratos/${id}/parcelas/${parcelaId}/medir`, {
        quantidadeMedida: quantidade ? Number(quantidade) : null,
        obs: obsMedicao || null
      })
      setParcelaEmEdicao(null); setQuantidade(''); setObsMedicao('')
      carregar()
    } catch (err) {
      setErro(err.response?.data?.erro || 'Erro ao registrar medição.')
    }
  }

  async function aprovar(parcelaId) {
    if (!confirm('Aprovar essa medição? O título correspondente sai de "previsão" e vira pagável.')) return
    setErro('')
    try {
      await api.post(`/contratos/${id}/parcelas/${parcelaId}/aprovar`)
      carregar()
    } catch (err) {
      setErro(err.response?.data?.erro || 'Erro ao aprovar.')
    }
  }

  async function rejeitar(parcelaId) {
    setErro('')
    try {
      await api.post(`/contratos/${id}/parcelas/${parcelaId}/rejeitar`, { motivo: motivoRejeicao })
      setParcelaEmEdicao(null); setMotivoRejeicao('')
      carregar()
    } catch (err) {
      setErro(err.response?.data?.erro || 'Erro ao rejeitar.')
    }
  }

  if (!contrato) return <div className="p-6 text-slate-400">Carregando...</div>

  return (
    <div className="p-6 max-w-3xl">
      <Link to="/contratos" className="text-sm text-blue-600 hover:underline">← Voltar para contratos</Link>

      <div className="bg-white rounded-2xl shadow p-6 mt-3 mb-6">
        <h1 className="text-lg font-bold">{contrato.descricao || 'Contrato'}</h1>
        <p className="text-sm text-slate-500">{contrato.fornecedor?.nome}</p>
        <p className="text-sm mt-2">Valor total: <strong>R$ {contrato.valorTotal.toFixed(2)}</strong> em {contrato.numeroParcelas}x</p>
        {contrato.obs && <p className="text-sm text-slate-500 mt-2">{contrato.obs}</p>}
      </div>

      {erro && <div className="text-sm px-3 py-2 rounded-lg bg-red-50 text-red-700 mb-4">{erro}</div>}

      <div className="space-y-3">
        {contrato.parcelas.map(p => (
          <div key={p.id} className="bg-white rounded-xl shadow p-4">
            <div className="flex items-center justify-between">
              <div>
                <span className="font-medium">Parcela {p.numero}</span>
                <span className="text-sm text-slate-500 ml-2">R$ {p.valorPrevisto.toFixed(2)} — prevista para {p.dataPrevista}</span>
              </div>
              <span className={`text-xs px-2 py-1 rounded-full ${STATUS_MEDICAO_COR[p.statusMedicao]}`}>{p.statusMedicao}</span>
            </div>

            {p.obsMedicao && <p className="text-xs text-slate-500 mt-2">Medição: {p.obsMedicao} {p.quantidadeMedida != null && `(qtd: ${p.quantidadeMedida})`}</p>}
            {p.motivoRejeicao && <p className="text-xs text-red-600 mt-2">Rejeitado: {p.motivoRejeicao}</p>}

            {/* Acoes */}
            {(p.statusMedicao === 'pendente' || p.statusMedicao === 'rejeitado') && parcelaEmEdicao?.id !== p.id && (
              <button onClick={() => setParcelaEmEdicao({ id: p.id, modo: 'medir' })}
                className="text-xs text-blue-600 hover:underline mt-2">Registrar medição</button>
            )}

            {parcelaEmEdicao?.id === p.id && parcelaEmEdicao.modo === 'medir' && (
              <div className="mt-3 space-y-2 bg-slate-50 p-3 rounded-lg">
                <input type="number" placeholder="Quantidade medida (opcional)" value={quantidade}
                  onChange={e => setQuantidade(e.target.value)} className="w-full border rounded-lg px-3 py-2 text-sm" />
                <textarea placeholder="Observação da medição" value={obsMedicao} onChange={e => setObsMedicao(e.target.value)}
                  rows={2} className="w-full border rounded-lg px-3 py-2 text-sm" />
                <div className="flex gap-2">
                  <button onClick={() => setParcelaEmEdicao(null)} className="flex-1 border rounded-lg py-1.5 text-xs">Cancelar</button>
                  <button onClick={() => medir(p.id)} className="flex-1 bg-blue-600 text-white rounded-lg py-1.5 text-xs font-bold">Confirmar medição</button>
                </div>
              </div>
            )}

            {p.statusMedicao === 'medido' && podeAprovar && (
              <div className="flex gap-3 mt-2">
                <button onClick={() => aprovar(p.id)} className="text-xs text-emerald-600 hover:underline">Aprovar (libera o título)</button>
                <button onClick={() => setParcelaEmEdicao({ id: p.id, modo: 'rejeitar' })} className="text-xs text-red-600 hover:underline">Rejeitar</button>
              </div>
            )}
            {p.statusMedicao === 'medido' && !podeAprovar && (
              <p className="text-xs text-slate-400 mt-2">Aguardando aprovação do gestor.</p>
            )}

            {parcelaEmEdicao?.id === p.id && parcelaEmEdicao.modo === 'rejeitar' && (
              <div className="mt-3 space-y-2 bg-slate-50 p-3 rounded-lg">
                <textarea placeholder="Motivo da rejeição" value={motivoRejeicao} onChange={e => setMotivoRejeicao(e.target.value)}
                  rows={2} className="w-full border rounded-lg px-3 py-2 text-sm" />
                <div className="flex gap-2">
                  <button onClick={() => setParcelaEmEdicao(null)} className="flex-1 border rounded-lg py-1.5 text-xs">Cancelar</button>
                  <button onClick={() => rejeitar(p.id)} className="flex-1 bg-red-600 text-white rounded-lg py-1.5 text-xs font-bold">Confirmar rejeição</button>
                </div>
              </div>
            )}
          </div>
        ))}
      </div>
    </div>
  )
}
