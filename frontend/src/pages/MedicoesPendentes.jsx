import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import api from '../api/client'

const STATUS_COR = {
  pendente: 'bg-slate-100 text-slate-600',
  medido: 'bg-amber-100 text-amber-700',
  rejeitado: 'bg-red-100 text-red-700'
}

export default function MedicoesPendentes() {
  const [lista, setLista] = useState([])
  const [emEdicao, setEmEdicao] = useState(null) // { id, contratoId, modo: 'medir'|'rejeitar' }
  const [quantidade, setQuantidade] = useState('')
  const [obs, setObs] = useState('')
  const [motivo, setMotivo] = useState('')
  const [erro, setErro] = useState('')

  const papel = localStorage.getItem('papel')
  const podeAprovar = papel === 'owner' || papel === 'admin'

  function carregar() {
    // pendente, medido e rejeitado -- so nao traz o que ja foi aprovado (esse fica so no detalhe do contrato)
    api.get('/contratos/parcelas/pendentes').then(r => setLista(r.data)).catch(() => {})
  }
  useEffect(() => { carregar() }, [])

  function limparEdicao() {
    setEmEdicao(null); setQuantidade(''); setObs(''); setMotivo('')
  }

  async function medir(p) {
    setErro('')
    try {
      await api.post(`/contratos/${p.contratoId}/parcelas/${p.id}/medir`, {
        quantidadeMedida: quantidade ? Number(quantidade) : null,
        obs: obs || null
      })
      limparEdicao(); carregar()
    } catch (err) {
      setErro(err.response?.data?.erro || 'Erro ao registrar medição.')
    }
  }

  async function aprovar(p) {
    if (!confirm('Aprovar essa medição? O título correspondente sai de "previsão" e vira pagável.')) return
    setErro('')
    try {
      await api.post(`/contratos/${p.contratoId}/parcelas/${p.id}/aprovar`)
      carregar()
    } catch (err) {
      setErro(err.response?.data?.erro || 'Erro ao aprovar.')
    }
  }

  async function rejeitar(p) {
    setErro('')
    try {
      await api.post(`/contratos/${p.contratoId}/parcelas/${p.id}/rejeitar`, { motivo })
      limparEdicao(); carregar()
    } catch (err) {
      setErro(err.response?.data?.erro || 'Erro ao rejeitar.')
    }
  }

  return (
    <div className="p-6 max-w-4xl">
      <h1 className="text-xl font-bold mb-1">Medições pendentes</h1>
      <p className="text-sm text-slate-500 mb-6">Rotina de medição — todas as parcelas de contratos ativos que ainda precisam de medição ou aprovação, de todos os fornecedores juntos.</p>

      {erro && <div className="text-sm px-3 py-2 rounded-lg bg-red-50 text-red-700 mb-4">{erro}</div>}

      <div className="space-y-3">
        {lista.map(p => (
          <div key={p.id} className="bg-white rounded-xl shadow p-4">
            <div className="flex items-center justify-between">
              <div>
                <Link to={`/contratos/${p.contratoId}`} className="font-medium text-blue-600 hover:underline">
                  {p.produtoNome} — {p.fornecedorNome}
                </Link>
                <div className="text-sm text-slate-500">
                  Parcela {p.numero} · R$ {p.valorPrevisto.toFixed(2)} · prevista para {p.dataPrevista}
                </div>
              </div>
              <span className={`text-xs px-2 py-1 rounded-full ${STATUS_COR[p.statusMedicao]}`}>{p.statusMedicao}</span>
            </div>

            {p.obsMedicao && <p className="text-xs text-slate-500 mt-2">Medição: {p.obsMedicao} {p.quantidadeMedida != null && `(qtd: ${p.quantidadeMedida})`}</p>}
            {p.motivoRejeicao && <p className="text-xs text-red-600 mt-2">Rejeitado: {p.motivoRejeicao}</p>}

            {(p.statusMedicao === 'pendente' || p.statusMedicao === 'rejeitado') && emEdicao?.id !== p.id && (
              <button onClick={() => setEmEdicao({ id: p.id, contratoId: p.contratoId, modo: 'medir' })}
                className="text-xs text-blue-600 hover:underline mt-2">Registrar medição</button>
            )}

            {emEdicao?.id === p.id && emEdicao.modo === 'medir' && (
              <div className="mt-3 space-y-2 bg-slate-50 p-3 rounded-lg">
                <input type="number" placeholder="Quantidade medida (opcional)" value={quantidade}
                  onChange={e => setQuantidade(e.target.value)} className="w-full border rounded-lg px-3 py-2 text-sm" />
                <textarea placeholder="Observação da medição" value={obs} onChange={e => setObs(e.target.value)}
                  rows={2} className="w-full border rounded-lg px-3 py-2 text-sm" />
                <div className="flex gap-2">
                  <button onClick={limparEdicao} className="flex-1 border rounded-lg py-1.5 text-xs">Cancelar</button>
                  <button onClick={() => medir(p)} className="flex-1 bg-blue-600 text-white rounded-lg py-1.5 text-xs font-bold">Confirmar medição</button>
                </div>
              </div>
            )}

            {p.statusMedicao === 'medido' && podeAprovar && (
              <div className="flex gap-3 mt-2">
                <button onClick={() => aprovar(p)} className="text-xs text-emerald-600 hover:underline">Aprovar (libera o título)</button>
                <button onClick={() => setEmEdicao({ id: p.id, contratoId: p.contratoId, modo: 'rejeitar' })} className="text-xs text-red-600 hover:underline">Rejeitar</button>
              </div>
            )}
            {p.statusMedicao === 'medido' && !podeAprovar && (
              <p className="text-xs text-slate-400 mt-2">Aguardando aprovação do gestor.</p>
            )}

            {emEdicao?.id === p.id && emEdicao.modo === 'rejeitar' && (
              <div className="mt-3 space-y-2 bg-slate-50 p-3 rounded-lg">
                <textarea placeholder="Motivo da rejeição" value={motivo} onChange={e => setMotivo(e.target.value)}
                  rows={2} className="w-full border rounded-lg px-3 py-2 text-sm" />
                <div className="flex gap-2">
                  <button onClick={limparEdicao} className="flex-1 border rounded-lg py-1.5 text-xs">Cancelar</button>
                  <button onClick={() => rejeitar(p)} className="flex-1 bg-red-600 text-white rounded-lg py-1.5 text-xs font-bold">Confirmar rejeição</button>
                </div>
              </div>
            )}
          </div>
        ))}
        {lista.length === 0 && (
          <div className="bg-white rounded-xl shadow p-6 text-center text-slate-400 text-sm">
            Nenhuma medição pendente. Tudo certo por aqui.
          </div>
        )}
      </div>
    </div>
  )
}
