import { useEffect, useState } from 'react'
import api from '../api/client'

const PUBLICOS = {
  todos: 'Todos os clientes',
  inativos_30d: 'Inativos há +30 dias',
  aniversariantes_mes: 'Aniversariantes do mês'
}

const STATUS = {
  aguardando_aprovacao: { label: 'Aguardando aprovação', cor: 'bg-amber-100 text-amber-700' },
  enviada: { label: 'Enviada', cor: 'bg-emerald-100 text-emerald-700' },
  reprovada: { label: 'Reprovada', cor: 'bg-red-100 text-red-700' },
  rascunho: { label: 'Rascunho', cor: 'bg-slate-100 text-slate-600' }
}

export default function Promocoes() {
  const [campanhas, setCampanhas] = useState([])
  const [modal, setModal] = useState(false)
  const podeAprovar = ['owner', 'admin'].includes(localStorage.getItem('papel'))

  function carregar() {
    api.get('/campanhas').then((r) => setCampanhas(r.data)).catch(() => {})
  }
  useEffect(() => { carregar() }, [])

  async function aprovar(c) {
    if (!confirm(`Aprovar e enviar para ${c.totalDestinatarios} cliente(s)? Esta ação dispara as mensagens.`)) return
    await api.put(`/campanhas/${c.id}/aprovar`)
    carregar()
  }
  async function reprovar(c) {
    const motivo = prompt('Motivo da reprovação (opcional):') || ''
    await api.put(`/campanhas/${c.id}/reprovar`, { motivo })
    carregar()
  }

  return (
    <div>
      <div className="flex justify-between items-center mb-2">
        <h2 className="text-2xl font-bold">Promoções</h2>
        <button onClick={() => setModal(true)} className="bg-slate-900 text-white px-4 py-2 rounded-lg">
          + Nova promoção
        </button>
      </div>
      <p className="text-slate-500 text-sm mb-6">
        Convide para eventos ou ofereça descontos. Toda campanha precisa da aprovação do dono/admin antes de sair.
      </p>

      <div className="grid gap-3">
        {campanhas.map((c) => {
          const st = STATUS[c.status] || STATUS.rascunho
          return (
            <div key={c.id} className="bg-white rounded-2xl shadow p-5">
              <div className="flex items-start justify-between">
                <div className="flex-1">
                  <div className="flex items-center gap-2">
                    <h3 className="font-semibold">{c.titulo}</h3>
                    <span className={`text-xs px-2 py-0.5 rounded-full ${st.cor}`}>{st.label}</span>
                  </div>
                  <p className="text-sm text-slate-600 mt-2 whitespace-pre-wrap">{c.mensagem}</p>
                  {c.imagemUrl && <div className="text-xs text-blue-600 mt-1">📷 com imagem</div>}
                  <div className="text-xs text-slate-400 mt-2">
                    {PUBLICOS[c.publico] || c.publico} · {c.totalDestinatarios} destinatário(s)
                    {c.status === 'enviada' && ` · ${c.totalEnviados} enviado(s)`}
                  </div>
                  {c.status === 'reprovada' && c.motivoReprovacao && (
                    <div className="text-xs text-red-500 mt-1">Motivo: {c.motivoReprovacao}</div>
                  )}
                </div>
              </div>

              {c.status === 'aguardando_aprovacao' && (
                <div className="flex gap-2 mt-3 pt-3 border-t">
                  {podeAprovar ? (
                    <>
                      <button onClick={() => aprovar(c)} className="bg-emerald-600 text-white text-sm px-4 py-1.5 rounded-lg">
                        Aprovar e enviar
                      </button>
                      <button onClick={() => reprovar(c)} className="bg-slate-100 text-slate-600 text-sm px-4 py-1.5 rounded-lg">
                        Reprovar
                      </button>
                    </>
                  ) : (
                    <span className="text-xs text-amber-600">⏳ Aguardando aprovação do dono/administrador</span>
                  )}
                </div>
              )}
            </div>
          )
        })}
        {campanhas.length === 0 && <p className="text-slate-400">Nenhuma campanha ainda.</p>}
      </div>

      {modal && <ModalNova onClose={() => setModal(false)} onSaved={() => { setModal(false); carregar() }} />}
    </div>
  )
}

function ModalNova({ onClose, onSaved }) {
  const [f, setF] = useState({ titulo: '', mensagem: '', imagemUrl: '', publico: 'todos' })
  const [previa, setPrevia] = useState(null)

  useEffect(() => {
    api.get('/campanhas/previa', { params: { publico: f.publico } })
      .then((r) => setPrevia(r.data.total)).catch(() => setPrevia(null))
  }, [f.publico])

  async function salvar(e) {
    e.preventDefault()
    await api.post('/campanhas', { ...f, imagemUrl: f.imagemUrl || null })
    onSaved()
  }

  return (
    <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50 p-4" onClick={onClose}>
      <div className="bg-white rounded-2xl shadow-xl p-6 w-full max-w-md" onClick={(e) => e.stopPropagation()}>
        <h3 className="font-bold text-lg mb-1">Nova promoção</h3>
        <p className="text-xs text-slate-400 mb-4">Vai para aprovação do dono/admin antes de enviar.</p>
        <form onSubmit={salvar} className="grid gap-3">
          <input className="border rounded-lg px-3 py-2" placeholder="Título (uso interno)" required
            value={f.titulo} onChange={(e) => setF({ ...f, titulo: e.target.value })} />
          <textarea className="border rounded-lg px-3 py-2" rows="4" required
            placeholder="Mensagem que o cliente recebe no WhatsApp..."
            value={f.mensagem} onChange={(e) => setF({ ...f, mensagem: e.target.value })} />
          <input className="border rounded-lg px-3 py-2" placeholder="URL da imagem (opcional)"
            value={f.imagemUrl} onChange={(e) => setF({ ...f, imagemUrl: e.target.value })} />
          <select className="border rounded-lg px-3 py-2"
            value={f.publico} onChange={(e) => setF({ ...f, publico: e.target.value })}>
            {Object.entries(PUBLICOS).map(([k, v]) => <option key={k} value={k}>{v}</option>)}
          </select>
          <div className="bg-slate-50 rounded-lg p-3 text-sm text-slate-600">
            {previa === null ? 'Calculando público...' : <>📣 Vai para <b>{previa}</b> cliente(s) que aceitam promoções.</>}
          </div>
          <button className="bg-emerald-600 text-white py-2 rounded-lg">Enviar para aprovação</button>
        </form>
      </div>
    </div>
  )
}
