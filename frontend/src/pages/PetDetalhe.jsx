import { useEffect, useRef, useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import api from '../api/client'

const ESPECIE_EMOJI = { cao: '🐕', gato: '🐈', ave: '🦜', roedor: '🐹', reptil: '🦎', outro: '🐾' }

const TIPO_PRONTUARIO = {
  consulta: { label: 'Consulta', cor: 'bg-blue-100 text-blue-700', icone: '🩺' },
  exame: { label: 'Exame', cor: 'bg-purple-100 text-purple-700', icone: '🔬' },
  cirurgia: { label: 'Cirurgia', cor: 'bg-red-100 text-red-700', icone: '🔪' },
  internacao: { label: 'Internação', cor: 'bg-amber-100 text-amber-700', icone: '🏥' },
  observacao: { label: 'Observação', cor: 'bg-slate-100 text-slate-700', icone: '📝' },
  receita: { label: 'Receita', cor: 'bg-emerald-100 text-emerald-700', icone: '💊' }
}

function idade(dataNasc) {
  if (!dataNasc) return null
  const nasc = new Date(dataNasc)
  const meses = (new Date().getFullYear() - nasc.getFullYear()) * 12 + (new Date().getMonth() - nasc.getMonth())
  if (meses < 12) return `${meses} ${meses === 1 ? 'mês' : 'meses'}`
  const anos = Math.floor(meses / 12)
  const resto = meses % 12
  return resto ? `${anos}a ${resto}m` : `${anos} ${anos === 1 ? 'ano' : 'anos'}`
}

function statusVacina(proximaDose) {
  if (!proximaDose) return null
  const hoje = new Date()
  const prox = new Date(proximaDose)
  const dias = Math.ceil((prox - hoje) / 86400000)
  if (dias < 0) return { txt: 'Vencida', cor: 'bg-red-100 text-red-700' }
  if (dias <= 30) return { txt: `Vence em ${dias}d`, cor: 'bg-amber-100 text-amber-700' }
  return { txt: 'Em dia', cor: 'bg-emerald-100 text-emerald-700' }
}

export default function PetDetalhe() {
  const { id } = useParams()
  const nav = useNavigate()
  const [pet, setPet] = useState(null)
  const [aba, setAba] = useState('prontuario')
  const [prontuario, setProntuario] = useState([])
  const [vacinas, setVacinas] = useState([])
  const [agendamentos, setAgendamentos] = useState([])
  const [modal, setModal] = useState(null) // 'prontuario' | 'vacina' | null

  function carregarTudo() {
    api.get(`/pets/${id}`).then((r) => setPet(r.data)).catch(() => {})
    api.get(`/pets/${id}/prontuario`).then((r) => setProntuario(r.data)).catch(() => {})
    api.get(`/pets/${id}/vacinas`).then((r) => setVacinas(r.data)).catch(() => {})
    api.get(`/pets/${id}/agendamentos`).then((r) => setAgendamentos(r.data)).catch(() => {})
  }

  useEffect(() => { carregarTudo() }, [id])

  async function reimprimirReceita(prontuarioItemId, enviarWhatsApp) {
    try {
      const resp = await api.post('/receituario/reimprimir', {
        prontuarioItemId,
        enviarWhatsApp
      }, { responseType: 'blob' })
      const blob = new Blob([resp.data], { type: 'application/pdf' })
      const url = URL.createObjectURL(blob)
      const a = document.createElement('a')
      a.href = url
      a.target = '_blank'
      a.rel = 'noopener'
      document.body.appendChild(a)
      a.click()
      document.body.removeChild(a)
      setTimeout(() => URL.revokeObjectURL(url), 1000)
      if (enviarWhatsApp) {
        const ok = resp.headers['x-whatsapp-enviado'] === 'true'
        alert(ok ? '✅ WhatsApp enviado!' : '⚠️ PDF gerado, mas WhatsApp falhou. Verifique a configuração.')
      }
    } catch {
      alert('Erro ao gerar o PDF. Tente novamente.')
    }
  }

  if (!pet) return <p>Carregando...</p>

  return (
    <div>
      <button onClick={() => nav('/pets')} className="text-sm text-slate-500 mb-4 hover:text-slate-700">
        ← Voltar para pets
      </button>

      {/* Cabeçalho do pet */}
      <div className="bg-white rounded-2xl shadow p-6 mb-6 flex items-center gap-5">
        <div className="text-6xl">{ESPECIE_EMOJI[pet.especie] || '🐾'}</div>
        <div className="flex-1">
          <div className="flex items-center justify-between">
            <h2 className="text-2xl font-bold">{pet.nome}</h2>
            <button onClick={() => setModal('editar')}
              className="text-sm text-blue-600 hover:underline">✏️ Editar</button>
          </div>
          <div className="text-slate-500 text-sm mt-1">
            {pet.raca || 'SRD'} · {pet.sexo} {pet.castrado ? '· castrado' : ''}
          </div>
          <div className="flex gap-4 mt-3 text-sm">
            {pet.dataNascimento && (
              <span className="bg-slate-100 px-3 py-1 rounded-full">🎂 {idade(pet.dataNascimento)}</span>
            )}
            {pet.pesoKg && (
              <span className="bg-slate-100 px-3 py-1 rounded-full">⚖️ {pet.pesoKg} kg</span>
            )}
            {pet.cor && (
              <span className="bg-slate-100 px-3 py-1 rounded-full">🎨 {pet.cor}</span>
            )}
          </div>
        </div>
      </div>

      {/* Abas */}
      <div className="flex gap-2 mb-4 border-b">
        {[
          { k: 'prontuario', l: `Prontuário (${prontuario.length})` },
          { k: 'vacinas', l: `Vacinas (${vacinas.length})` },
          { k: 'agenda', l: `Histórico (${agendamentos.length})` }
        ].map((t) => (
          <button key={t.k} onClick={() => setAba(t.k)}
            className={`px-4 py-2 text-sm font-medium border-b-2 -mb-px ${
              aba === t.k ? 'border-slate-900 text-slate-900' : 'border-transparent text-slate-400'
            }`}>
            {t.l}
          </button>
        ))}
      </div>

      {/* Conteúdo: PRONTUÁRIO */}
      {aba === 'prontuario' && (
        <div>
          <div className="flex justify-end gap-2 mb-4">
            <button onClick={() => setModal('receita')}
              className="bg-emerald-600 text-white px-4 py-2 rounded-lg text-sm">
              + Receituário
            </button>
            <button onClick={() => setModal('prontuario')}
              className="bg-slate-900 text-white px-4 py-2 rounded-lg text-sm">
              + Novo registro
            </button>
          </div>
          {prontuario.length === 0 ? (
            <p className="text-slate-400 text-center py-8">Nenhum registro no prontuário ainda.</p>
          ) : (
            <div className="relative pl-6">
              <div className="absolute left-2 top-2 bottom-2 w-px bg-slate-200"></div>
              {prontuario.map((item) => {
                const t = TIPO_PRONTUARIO[item.tipo] || TIPO_PRONTUARIO.observacao
                return (
                  <div key={item.id} className="relative mb-5">
                    <div className="absolute -left-[18px] top-1 w-4 h-4 rounded-full bg-white border-2 border-slate-300 flex items-center justify-center text-[10px]">
                      {t.icone}
                    </div>
                    <div className="bg-white rounded-xl shadow p-4">
                      <div className="flex items-center justify-between mb-1">
                        <span className={`text-xs px-2 py-0.5 rounded-full ${t.cor}`}>{t.label}</span>
                        <span className="text-xs text-slate-400">
                          {new Date(item.data).toLocaleDateString('pt-BR')}
                        </span>
                      </div>
                      {item.titulo && <div className="font-medium mt-1">{item.titulo}</div>}
                      {item.motivo && (
                        <div className="text-sm text-slate-500 mt-1"><span className="font-medium">Motivo:</span> {item.motivo}</div>
                      )}
                      <p className="text-sm text-slate-600 mt-1 whitespace-pre-wrap">{item.descricao}</p>
                      {item.receituario && (
                        <div className="mt-2 bg-amber-50 border border-amber-200 rounded-lg p-3">
                          <div className="flex items-center justify-between mb-1">
                            <div className="text-xs font-semibold text-amber-700">💊 Receituário</div>
                            <div className="flex gap-2">
                              <button
                                onClick={() => reimprimirReceita(item.id, false)}
                                className="text-xs bg-white border border-amber-300 text-amber-700 px-2 py-1 rounded hover:bg-amber-50">
                                🖨️ Re-imprimir
                              </button>
                              <button
                                onClick={() => reimprimirReceita(item.id, true)}
                                className="text-xs bg-emerald-600 text-white px-2 py-1 rounded hover:bg-emerald-700">
                                📲 WhatsApp
                              </button>
                            </div>
                          </div>
                          <p className="text-sm text-amber-900 whitespace-pre-wrap">{item.receituario}</p>
                        </div>
                      )}
                      {item.anexoUrl && (
                        <a href={item.anexoUrl} target="_blank" rel="noreferrer"
                          className="text-xs text-blue-600 mt-2 inline-block">📎 Ver anexo</a>
                      )}
                    </div>
                  </div>
                )
              })}
            </div>
          )}
        </div>
      )}

      {/* Conteúdo: VACINAS */}
      {aba === 'vacinas' && (
        <div>
          <div className="flex justify-end mb-4">
            <button onClick={() => setModal('vacina')}
              className="bg-slate-900 text-white px-4 py-2 rounded-lg text-sm">
              + Registrar vacina
            </button>
          </div>
          {vacinas.length === 0 ? (
            <p className="text-slate-400 text-center py-8">Nenhuma vacina registrada.</p>
          ) : (
            <div className="grid gap-3">
              {vacinas.map((v) => {
                const st = statusVacina(v.proximaDose)
                return (
                  <div key={v.id} className="bg-white rounded-xl shadow p-4 flex items-center justify-between">
                    <div>
                      <div className="font-medium">💉 {v.vacina}</div>
                      <div className="text-xs text-slate-400 mt-1">
                        Aplicada em {new Date(v.dataAplicacao).toLocaleDateString('pt-BR')}
                        {v.fabricante && ` · ${v.fabricante}`}
                      </div>
                      {v.proximaDose && (
                        <div className="text-xs text-slate-500 mt-1">
                          Próxima dose: {new Date(v.proximaDose).toLocaleDateString('pt-BR')}
                        </div>
                      )}
                    </div>
                    {st && <span className={`text-xs px-2 py-1 rounded-full ${st.cor}`}>{st.txt}</span>}
                  </div>
                )
              })}
            </div>
          )}
        </div>
      )}

      {/* Conteúdo: HISTÓRICO DE AGENDA */}
      {aba === 'agenda' && (
        <div>
          {agendamentos.length === 0 ? (
            <p className="text-slate-400 text-center py-8">Nenhum atendimento no histórico.</p>
          ) : (
            <div className="grid gap-2">
              {agendamentos.map((a) => (
                <div key={a.id} className="bg-white rounded-xl shadow p-4 flex items-center justify-between">
                  <div>
                    <div className="font-medium">{a.tipo}</div>
                    <div className="text-xs text-slate-400">
                      {new Date(a.dataHora).toLocaleString('pt-BR')}
                    </div>
                  </div>
                  <span className="text-xs px-2 py-1 rounded-full bg-slate-100 text-slate-600">{a.status}</span>
                </div>
              ))}
            </div>
          )}
        </div>
      )}

      {modal === 'prontuario' && (
        <ModalProntuario petId={id} onClose={() => setModal(null)} onSaved={() => { setModal(null); carregarTudo() }} />
      )}
      {modal === 'vacina' && (
        <ModalVacina petId={id} onClose={() => setModal(null)} onSaved={() => { setModal(null); carregarTudo() }} />
      )}
      {modal === 'editar' && (
        <ModalEditarPet pet={pet} onClose={() => setModal(null)} onSaved={() => { setModal(null); carregarTudo() }} />
      )}
      {modal === 'receita' && (
        <ModalReceita petId={id} petNome={pet.nome} tutorNome={pet.tutorNome}
          onClose={() => setModal(null)} onSaved={() => { setModal(null); carregarTudo() }} />
      )}
    </div>
  )
}

function ModalProntuario({ petId, onClose, onSaved }) {
  const hoje = new Date().toISOString().split('T')[0]
  const [form, setForm] = useState({ tipo: 'consulta', data: hoje, titulo: '', motivo: '', descricao: '', receituario: '', anexoUrl: '' })
  async function salvar(e) {
    e.preventDefault()
    await api.post('/prontuario', {
      petId, tipo: form.tipo,
      data: new Date(form.data + 'T12:00:00Z').toISOString(),
      titulo: form.titulo || null,
      motivo: form.motivo || null,
      descricao: form.descricao,
      receituario: form.receituario || null,
      anexoUrl: form.anexoUrl || null
    })
    onSaved()
  }
  return (
    <Overlay onClose={onClose} titulo="Novo registro no prontuário">
      <form onSubmit={salvar} className="grid gap-3 max-h-[70vh] overflow-auto">
        <div className="grid grid-cols-2 gap-2">
          <select className="border rounded-lg px-3 py-2"
            value={form.tipo} onChange={(e) => setForm({ ...form, tipo: e.target.value })}>
            {Object.entries(TIPO_PRONTUARIO).map(([k, v]) => <option key={k} value={k}>{v.label}</option>)}
          </select>
          <input type="date" className="border rounded-lg px-3 py-2" required
            value={form.data} onChange={(e) => setForm({ ...form, data: e.target.value })} />
        </div>
        <input className="border rounded-lg px-3 py-2" placeholder="Título (ex: Consulta de rotina)"
          value={form.titulo} onChange={(e) => setForm({ ...form, titulo: e.target.value })} />
        <input className="border rounded-lg px-3 py-2" placeholder="Motivo (ex: Tutor relata coceira nas patas)"
          value={form.motivo} onChange={(e) => setForm({ ...form, motivo: e.target.value })} />
        <textarea className="border rounded-lg px-3 py-2" placeholder="Descrição / observações clínicas" rows="3" required
          value={form.descricao} onChange={(e) => setForm({ ...form, descricao: e.target.value })} />
        <div>
          <label className="text-xs text-slate-500 block mb-1">💊 Receituário (opcional)</label>
          <textarea className="border rounded-lg px-3 py-2 w-full" rows="3"
            placeholder="Ex: Cefalexina 500mg — 1 comprimido a cada 12h por 7 dias..."
            value={form.receituario} onChange={(e) => setForm({ ...form, receituario: e.target.value })} />
        </div>
        <input className="border rounded-lg px-3 py-2" placeholder="URL do anexo (exame, raio-X...)"
          value={form.anexoUrl} onChange={(e) => setForm({ ...form, anexoUrl: e.target.value })} />
        <button className="bg-emerald-600 text-white py-2 rounded-lg">Salvar</button>
      </form>
    </Overlay>
  )
}

function ModalVacina({ petId, onClose, onSaved }) {
  const [form, setForm] = useState({ vacina: '', fabricante: '', lote: '', dataAplicacao: '', proximaDose: '', obs: '' })
  async function salvar(e) {
    e.preventDefault()
    await api.post('/vacinas', {
      ...form, petId,
      proximaDose: form.proximaDose || null
    })
    onSaved()
  }
  return (
    <Overlay onClose={onClose} titulo="Registrar vacina">
      <form onSubmit={salvar} className="grid gap-3">
        <input className="border rounded-lg px-3 py-2" placeholder="Nome da vacina (ex: V10)" required
          value={form.vacina} onChange={(e) => setForm({ ...form, vacina: e.target.value })} />
        <input className="border rounded-lg px-3 py-2" placeholder="Fabricante (opcional)"
          value={form.fabricante} onChange={(e) => setForm({ ...form, fabricante: e.target.value })} />
        <input className="border rounded-lg px-3 py-2" placeholder="Lote (opcional)"
          value={form.lote} onChange={(e) => setForm({ ...form, lote: e.target.value })} />
        <label className="text-xs text-slate-500 -mb-2">Data de aplicação</label>
        <input type="date" className="border rounded-lg px-3 py-2" required
          value={form.dataAplicacao} onChange={(e) => setForm({ ...form, dataAplicacao: e.target.value })} />
        <label className="text-xs text-slate-500 -mb-2">Próxima dose (opcional)</label>
        <input type="date" className="border rounded-lg px-3 py-2"
          value={form.proximaDose} onChange={(e) => setForm({ ...form, proximaDose: e.target.value })} />
        <button className="bg-emerald-600 text-white py-2 rounded-lg">Salvar</button>
      </form>
    </Overlay>
  )
}

function ModalReceita({ petId, petNome, tutorNome, onClose, onSaved }) {
  const [motivo, setMotivo] = useState('')
  const motivoRef = useRef(null)
  const [obs, setObs] = useState('')
  const [funcionarioId, setFuncionarioId] = useState('')
  const [veterinarios, setVeterinarios] = useState([])
  const [vetLogado, setVetLogado]       = useState(null) // {ehVet, id, nome, crmv}
  const [viasDisponiveis, setViasDisponiveis] = useState([])
  const [tipoReceita, setTipoReceita] = useState('Receita Veterinária')
  const [viaUso, setViaUso] = useState('USO ORAL')
  const [tipoFarmacia, setTipoFarmacia] = useState('Farmácia Veterinária')
  const [meds, setMeds] = useState([{ nome: '', apresentacao: '', dosagem: '', frequencia: '', duracao: '', via: 'oral', quantidade: '' }])
  const [loading, setLoading] = useState(false)
  const [resultado, setResultado] = useState(null) // { whatsapp: bool }

  // ── IA Diagnóstico ──────────────────────────────────────────────
  const [iaDisponivel, setIaDisponivel] = useState(false)
  const [iaAberto, setIaAberto]         = useState(false)
  const [iaSintomas, setIaSintomas]     = useState('')
  const [iaResposta, setIaResposta]     = useState('')
  const [iaMedicamentos, setIaMedicamentos] = useState([])
  const [receituarioAberto, setReceituarioAberto] = useState(false)
  const [iaCarregando, setIaCarregando] = useState(false)
  const [ouvindo, setOuvindo]           = useState(false)
  const recognitionRef                  = useRef(null)

  useEffect(() => {
    api.get('/ia/status').then(r => setIaDisponivel(r.data.ativo)).catch(() => {})
  }, [])

  function iniciarMicrofone() {
    const SR = window.SpeechRecognition || window.webkitSpeechRecognition
    if (!SR) { alert('Seu navegador não suporta reconhecimento de voz. Use Chrome.'); return }
    const rec = new SR()
    rec.lang = 'pt-BR'
    rec.continuous = true
    rec.interimResults = true
    rec.onresult = (e) => {
      const texto = Array.from(e.results).map(r => r[0].transcript).join(' ')
      setIaSintomas(texto)
    }
    rec.onerror = () => { setOuvindo(false) }
    rec.onend   = () => { setOuvindo(false) }
    recognitionRef.current = rec
    rec.start()
    setOuvindo(true)
  }

  function pararMicrofone() {
    recognitionRef.current?.stop()
    setOuvindo(false)
  }

  async function consultarIa() {
    if (!iaSintomas.trim()) return
    setIaCarregando(true)
    setIaResposta('')
    try {
      const r = await api.post('/ia/diagnostico-receituario', {
        sintomas: iaSintomas,
        petNome: petNome,
        especie: null,
        raca: null,
        idade: null,
        pesoKg: null
      })
      console.log('IA response:', r.data)
      // Armazena o objeto completo (diagnostico + medicamentos)
      setIaResposta(r.data?.diagnostico || 'Sem resposta da IA.')
      setIaMedicamentos(r.data?.medicamentos || [])
    } catch (err) {
      const msg = err.response?.data?.erro || err.response?.data?.detalhe || 'Erro ao consultar a IA.'
      setIaResposta(`Erro: ${msg}`)
    } finally {
      setIaCarregando(false)
    }
  }

  useEffect(() => {
    if (motivoRef.current) {
      motivoRef.current.style.height = 'auto'
      motivoRef.current.style.height = motivoRef.current.scrollHeight + 'px'
    }
  }, [motivo])

  function usarDiagnostico() {
    if (iaResposta) {
      setMotivo(iaResposta)
      // Popula os medicamentos sugeridos pela IA
      if (iaMedicamentos && iaMedicamentos.length > 0) {
        setMeds(iaMedicamentos.map(m => ({
          nome:         m.nome         || '',
          apresentacao: m.apresentacao || '',
          dosagem:      m.dosagem      || '',
          frequencia:   m.frequencia   || '',
          duracao:      m.duracao      || '',
          via:          m.via          || '',
          quantidade:   m.quantidade   || ''
        })))
      }
      setIaAberto(false)
      // Abre o modal do receituário automaticamente
      setReceituarioAberto(true)
    }
  }

  useEffect(() => {
    api.get('/receituario/vet-logado').then(r => {
      if (r.data.ehVet) {
        setVetLogado(r.data)
        setFuncionarioId(r.data.id)
      }
    }).catch(() => {})
    api.get('/receituario/veterinarios').then(r => setVeterinarios(r.data)).catch(() => {})
    api.get('/cadastros/vias').then(r => setViasDisponiveis(r.data)).catch(() => {})
  }, [])

  function addMed() {
    setMeds([...meds, { nome: '', apresentacao: '', dosagem: '', frequencia: '', duracao: '', via: 'oral', quantidade: '' }])
  }
  function updateMed(i, field, val) {
    setMeds(meds.map((m, idx) => idx === i ? { ...m, [field]: val } : m))
  }
  function removeMed(i) { setMeds(meds.filter((_, idx) => idx !== i)) }

  const vetSelecionado = veterinarios.find(v => v.id === funcionarioId)

  async function emitir(enviarWhatsApp) {
    const medsValidos = meds.filter(m => m.nome.trim())
    if (medsValidos.length === 0) {
      alert('Informe pelo menos um medicamento.')
      return
    }
    setLoading(true)
    try {
      const resp = await api.post('/receituario', {
        petId,
        funcionarioId: funcionarioId || null,
        tipoReceita,
        viaUso: viaUso || null,
        tipoFarmacia: tipoFarmacia || null,
        motivo,
        medicamentos: medsValidos,
        observacoes: obs || null,
        enviarWhatsApp
      }, { responseType: 'blob' })

      // Abre o PDF para impressão / download
      const blob = new Blob([resp.data], { type: 'application/pdf' })
      const url = URL.createObjectURL(blob)
      const a = document.createElement('a')
      a.href = url
      a.target = '_blank'
      a.rel = 'noopener'
      document.body.appendChild(a)
      a.click()
      document.body.removeChild(a)
      setTimeout(() => URL.revokeObjectURL(url), 1000)

      const whatsappEnviado = resp.headers['x-whatsapp-enviado'] === 'true'
      setResultado({ whatsapp: whatsappEnviado })
      onSaved()
    } catch (err) {
      // Se o backend retornou JSON de erro (não PDF), extrai a mensagem
      if (err.response?.data instanceof Blob) {
        const txt = await err.response.data.text()
        try { const j = JSON.parse(txt); alert('Erro: ' + (j.erro || j.message || txt)) }
        catch { alert('Erro ao gerar receituário: ' + txt) }
      } else {
        alert('Erro ao gerar receituário: ' + (err.response?.data?.erro || err.message || 'Tente novamente.'))
      }
    } finally {
      setLoading(false)
    }
  }

  if (resultado) {
    return (
      <Overlay onClose={onClose} titulo="Receituário emitido ✅">
        <div className="text-center space-y-4 py-4">
          <div className="text-5xl">📄</div>
          <p className="font-medium">PDF aberto para impressão!</p>
          {resultado.whatsapp
            ? <p className="text-emerald-600 text-sm">✅ WhatsApp enviado para o tutor.</p>
            : <p className="text-slate-400 text-sm">WhatsApp não enviado.</p>
          }
          <button onClick={onClose}
            className="bg-slate-900 text-white px-6 py-2 rounded-lg w-full">
            Fechar
          </button>
        </div>
      </Overlay>
    )
  }

  return (
    <div className="fixed inset-0 bg-black/50 z-50 flex items-center justify-center p-3" onClick={onClose}>
      <div className="bg-white rounded-2xl shadow-2xl w-full flex flex-col" style={{ maxWidth: 900, height: '92vh' }} onClick={e => e.stopPropagation()}>

        {/* Header */}
        <div className="flex items-center justify-between px-6 py-4 border-b flex-shrink-0">
          <h3 className="font-bold text-lg">📋 Receituário Veterinário</h3>
          <button onClick={onClose} className="text-slate-400 hover:text-slate-600 text-2xl leading-none">×</button>
        </div>

        {/* Body scrollável */}
        <div className="flex-1 overflow-y-auto px-6 py-4 space-y-4">

          {/* Linha 1: tipo receita + via + farmácia */}
          <div className="grid grid-cols-3 gap-3">
            <div>
              <label className="text-xs text-slate-500 block mb-1">Tipo de receita</label>
              <select className="border rounded-lg px-3 py-2 w-full text-sm"
                value={tipoReceita} onChange={e => setTipoReceita(e.target.value)}>
                <option>Receita Veterinária</option>
                <option>Receita de Controle Especial</option>
              </select>
            </div>
            <div>
              <label className="text-xs text-slate-500 block mb-1">Via de uso</label>
              <select className="border rounded-lg px-3 py-2 w-full text-sm"
                value={viaUso} onChange={e => setViaUso(e.target.value)}>
                <option value="">— Selecionar —</option>
                {viasDisponiveis.map(v => (
                  <option key={v.id} value={`USO ${v.nome.toUpperCase()}`}>USO {v.nome.toUpperCase()}</option>
                ))}
              </select>
            </div>
            <div>
              <label className="text-xs text-slate-500 block mb-1">Tipo de farmácia</label>
              <select className="border rounded-lg px-3 py-2 w-full text-sm"
                value={tipoFarmacia} onChange={e => setTipoFarmacia(e.target.value)}>
                <option>Farmácia Veterinária</option>
                <option>Farmácia Humana</option>
              </select>
            </div>
          </div>

          {/* IA */}
          {iaDisponivel && (
            <div className="border border-violet-200 rounded-xl overflow-hidden">
              <button type="button" onClick={() => setIaAberto(v => !v)}
                className="w-full flex items-center justify-between px-4 py-3 bg-violet-50 hover:bg-violet-100 transition">
                <div className="flex items-center gap-2">
                  <span className="text-lg">🤖</span>
                  <span className="text-sm font-semibold text-violet-800">Assistente IA — Apoio ao Diagnóstico</span>
                  <span className="text-xs bg-violet-200 text-violet-700 px-2 py-0.5 rounded-full">Beta</span>
                </div>
                <span className="text-violet-500 text-sm">{iaAberto ? '▲' : '▼'}</span>
              </button>

              {iaAberto && (
                <div className="p-4 space-y-3 bg-white">
                  <p className="text-xs text-slate-500">
                    Descreva os sintomas. A IA sugere diagnósticos diferenciais, exames e conduta. O veterinário valida antes de usar.
                  </p>
                  <div className="relative">
                    <textarea
                      className="border rounded-xl px-3 py-2 w-full text-sm focus:outline-none focus:ring-2 focus:ring-violet-300"
                      rows={3}
                      placeholder="Ex: Golden Retriever 3 anos, vômito há 2 dias, prostrado, febre 39.8°C..."
                      value={iaSintomas}
                      onChange={e => setIaSintomas(e.target.value)}
                    />
                    <button type="button" onClick={ouvindo ? pararMicrofone : iniciarMicrofone}
                      className={`absolute bottom-2 right-2 px-3 py-1.5 rounded-lg text-xs font-medium transition ${ouvindo ? 'bg-red-500 text-white animate-pulse' : 'bg-slate-100 text-slate-600 hover:bg-slate-200'}`}>
                      {ouvindo ? '⏹ Parar' : '🎙️ Falar'}
                    </button>
                  </div>
                  <button type="button" onClick={consultarIa}
                    disabled={iaCarregando || !iaSintomas.trim()}
                    className="w-full bg-violet-700 text-white py-2 rounded-xl text-sm font-medium disabled:opacity-40 hover:bg-violet-800 transition">
                    {iaCarregando ? '⏳ Analisando...' : '✨ Analisar com IA'}
                  </button>
                  {iaResposta && !iaCarregando && (
                    <div className="bg-slate-50 border rounded-xl p-3 space-y-2">
                      <span className="text-xs font-semibold text-slate-500 uppercase tracking-wide">Parecer da IA</span>
                      <div className="text-sm text-slate-700 whitespace-pre-wrap leading-relaxed">{iaResposta}</div>
                      <div className="pt-2 border-t flex gap-2">
                        <button type="button" onClick={usarDiagnostico}
                          className="flex-1 bg-emerald-600 text-white py-2 rounded-lg text-sm font-medium hover:bg-emerald-700">
                          ✅ Usar como Diagnóstico
                        </button>
                        <button type="button" onClick={() => setIaResposta('')}
                          className="border px-4 py-2 rounded-lg text-sm text-slate-500 hover:bg-slate-50">
                          Descartar
                        </button>
                      </div>
                    </div>
                  )}
                  {iaCarregando && (
                    <div className="text-center text-sm text-violet-600 animate-pulse py-2">⏳ Consultando IA...</div>
                  )}
                </div>
              )}
            </div>
          )}

          {/* Motivo / Diagnóstico — GRANDE */}
          <div>
            <label className="text-xs text-slate-500 block mb-1">Motivo / Diagnóstico</label>
            <textarea
              ref={motivoRef}
              className="border rounded-xl px-3 py-2 w-full text-sm focus:outline-none focus:ring-2 focus:ring-slate-300"
              style={{ minHeight: 100, resize: 'vertical' }}
              placeholder="Preenchido automaticamente pela IA ou manualmente..."
              value={motivo}
              onChange={e => setMotivo(e.target.value)}
            />
          </div>

          {/* Medicamentos */}
          <div className="border-t pt-3">
            <div className="flex justify-between items-center mb-3">
              <label className="text-sm font-semibold">Medicamentos</label>
              <button type="button" onClick={addMed} className="text-xs text-blue-600 hover:underline font-medium">+ Adicionar</button>
            </div>
            <div className="space-y-3">
              {meds.map((m, i) => (
                <div key={i} className="bg-slate-50 rounded-xl p-3 border border-slate-100">
                  <div className="flex justify-between items-center mb-2">
                    <span className="text-xs font-semibold text-slate-500">Medicamento {i + 1}</span>
                    {meds.length > 1 && (
                      <button type="button" onClick={() => removeMed(i)} className="text-xs text-red-400 hover:underline">Remover</button>
                    )}
                  </div>
                  <div className="grid grid-cols-2 gap-2">
                    <input className="border rounded-lg px-2 py-1.5 text-sm col-span-2"
                      placeholder="Nome do medicamento * (ex: Trazodona 50 mg)"
                      value={m.nome} onChange={e => updateMed(i, 'nome', e.target.value)} />
                    <input className="border rounded-lg px-2 py-1.5 text-sm col-span-2"
                      placeholder="Apresentação (ex: comprimido 30 un)"
                      value={m.apresentacao} onChange={e => updateMed(i, 'apresentacao', e.target.value)} />
                    <input className="border rounded-lg px-2 py-1.5 text-sm" placeholder="Dosagem (ex: 500mg)"
                      value={m.dosagem} onChange={e => updateMed(i, 'dosagem', e.target.value)} />
                    <input className="border rounded-lg px-2 py-1.5 text-sm" placeholder="Frequência (ex: 12/12h)"
                      value={m.frequencia} onChange={e => updateMed(i, 'frequencia', e.target.value)} />
                    <input className="border rounded-lg px-2 py-1.5 text-sm" placeholder="Duração (ex: 7 dias)"
                      value={m.duracao} onChange={e => updateMed(i, 'duracao', e.target.value)} />
                    <input className="border rounded-lg px-2 py-1.5 text-sm" placeholder="Quantidade (ex: 1 UNIDADE)"
                      value={m.quantidade} onChange={e => updateMed(i, 'quantidade', e.target.value)} />
                  </div>
                </div>
              ))}
            </div>
          </div>

          {/* Observações */}
          <textarea className="border rounded-xl px-3 py-2 text-sm w-full" placeholder="Observações gerais" rows={2}
            value={obs} onChange={e => setObs(e.target.value)} />

          {/* Veterinário */}
          <div>
            <label className="text-xs text-slate-500 block mb-1">Veterinário responsável</label>
            {vetLogado ? (
              <div className="flex items-center gap-3 p-3 bg-emerald-50 border border-emerald-200 rounded-xl">
                <div className="w-8 h-8 rounded-full bg-emerald-600 flex items-center justify-center text-white text-sm font-bold flex-shrink-0">
                  {vetLogado.nome?.charAt(0)}
                </div>
                <div className="flex-1 min-w-0">
                  <p className="text-sm font-semibold text-emerald-800">{vetLogado.nome}</p>
                  {vetLogado.crmv && <p className="text-xs text-emerald-600">CRMV {vetLogado.crmv}</p>}
                </div>
                <span className="text-xs bg-emerald-200 text-emerald-700 px-2 py-0.5 rounded-full font-medium">Você</span>
              </div>
            ) : (
              <>
                <select className="border rounded-lg px-3 py-2 w-full text-sm"
                  value={funcionarioId} onChange={e => setFuncionarioId(e.target.value)}>
                  <option value="">— Selecionar veterinário —</option>
                  {veterinarios.map(v => (
                    <option key={v.id} value={v.id}>
                      {v.nome}{v.crmv ? ` (CRMV ${v.crmv})` : ''}
                    </option>
                  ))}
                </select>
                {!veterinarios.length && (
                  <p className="text-xs text-amber-600 mt-1">
                    ⚠️ Nenhum veterinário cadastrado. Configure em RH → Funcionários → vínculo de login.
                  </p>
                )}
              </>
            )}
          </div>

        </div>

        {/* Footer fixo */}
        <div className="flex gap-3 px-6 py-4 border-t bg-slate-50 rounded-b-2xl flex-shrink-0">
          <button disabled={loading} onClick={() => emitir(false)}
            className="flex-1 bg-slate-900 text-white py-3 rounded-xl text-sm font-semibold disabled:opacity-50 flex items-center justify-center gap-2 hover:bg-slate-800">
            {loading ? '⏳' : '🖨️'} Imprimir PDF
          </button>
          <button disabled={loading} onClick={() => emitir(true)}
            className="flex-1 bg-emerald-600 text-white py-3 rounded-xl text-sm font-semibold disabled:opacity-50 flex items-center justify-center gap-2 hover:bg-emerald-700">
            {loading ? '⏳' : '📲'} PDF + WhatsApp
          </button>
        </div>

      </div>
    </div>
  )
}

function Overlay({ titulo, children, onClose, largo }) {
  return (
    <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50 p-4"
      onClick={onClose}>
      <div className={`bg-white rounded-2xl shadow-xl p-6 w-full ${largo ? 'max-w-2xl' : 'max-w-md'}`} onClick={(e) => e.stopPropagation()}>
        <div className="flex justify-between items-center mb-4">
          <h3 className="font-bold text-lg">{titulo}</h3>
          <button onClick={onClose} className="text-slate-400 hover:text-slate-600 text-xl">×</button>
        </div>
        {children}
      </div>
    </div>
  )
}

const ESPECIES_OPC = { cao: 'Cão', gato: 'Gato', ave: 'Ave', roedor: 'Roedor', reptil: 'Réptil', outro: 'Outro' }

function ModalEditarPet({ pet, onClose, onSaved }) {
  const [f, setF] = useState({
    nome: pet.nome || '',
    especie: pet.especie || 'cao',
    raca: pet.raca || '',
    sexo: pet.sexo || 'indefinido',
    dataNascimento: pet.dataNascimento ? pet.dataNascimento.substring(0, 10) : '',
    pelagem: pet.pelagem || '',
    pesoKg: pet.pesoKg ?? '',
    castrado: pet.castrado ?? false,
    cor: pet.cor || '',
    obs: pet.obs || ''
  })

  async function salvar(e) {
    e.preventDefault()
    await api.put(`/pets/${pet.id}`, {
      tutorId: pet.tutorId,
      nome: f.nome,
      especie: f.especie,
      raca: f.raca || null,
      sexo: f.sexo,
      dataNascimento: f.dataNascimento || null,
      pelagem: f.pelagem || null,
      pesoKg: f.pesoKg !== '' ? +f.pesoKg : null,
      castrado: f.castrado,
      cor: f.cor || null,
      obs: f.obs || null
    })
    onSaved()
  }

  return (
    <Overlay titulo={`Editar ${pet.nome}`} onClose={onClose}>
      <form onSubmit={salvar} className="grid grid-cols-2 gap-3">
        <input className="border rounded-lg px-3 py-2 col-span-2" placeholder="Nome" required
          value={f.nome} onChange={(e) => setF({ ...f, nome: e.target.value })} />
        <select className="border rounded-lg px-3 py-2"
          value={f.especie} onChange={(e) => setF({ ...f, especie: e.target.value })}>
          {Object.entries(ESPECIES_OPC).map(([k, v]) => <option key={k} value={k}>{v}</option>)}
        </select>
        <input className="border rounded-lg px-3 py-2" placeholder="Raça"
          value={f.raca} onChange={(e) => setF({ ...f, raca: e.target.value })} />
        <select className="border rounded-lg px-3 py-2"
          value={f.sexo} onChange={(e) => setF({ ...f, sexo: e.target.value })}>
          <option value="indefinido">Sexo</option>
          <option value="macho">Macho</option>
          <option value="femea">Fêmea</option>
        </select>
        <input className="border rounded-lg px-3 py-2" placeholder="Peso (kg)" type="number" step="0.1"
          value={f.pesoKg} onChange={(e) => setF({ ...f, pesoKg: e.target.value })} />
        <label className="text-xs text-slate-500 col-span-2 -mb-2">Data de nascimento</label>
        <input type="date" className="border rounded-lg px-3 py-2"
          value={f.dataNascimento} onChange={(e) => setF({ ...f, dataNascimento: e.target.value })} />
        <input className="border rounded-lg px-3 py-2" placeholder="Cor"
          value={f.cor} onChange={(e) => setF({ ...f, cor: e.target.value })} />
        <input className="border rounded-lg px-3 py-2" placeholder="Pelagem"
          value={f.pelagem} onChange={(e) => setF({ ...f, pelagem: e.target.value })} />
        <label className="flex items-center gap-2 text-sm px-1">
          <input type="checkbox" checked={f.castrado}
            onChange={(e) => setF({ ...f, castrado: e.target.checked })} />
          Castrado
        </label>
        <textarea className="border rounded-lg px-3 py-2 col-span-2" placeholder="Observações" rows="2"
          value={f.obs} onChange={(e) => setF({ ...f, obs: e.target.value })} />
        <button className="bg-emerald-600 text-white py-2 rounded-lg col-span-2">Salvar alterações</button>
      </form>
    </Overlay>
  )
}
