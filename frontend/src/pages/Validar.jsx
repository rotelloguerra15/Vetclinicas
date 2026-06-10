import { useEffect, useState } from 'react'
import { useParams } from 'react-router-dom'

const API = import.meta.env.VITE_API_URL || 'https://vetclinicas-production.up.railway.app'

export default function Validar() {
  const { codigo } = useParams()
  const [resultado, setResultado] = useState(null)
  const [loading, setLoading]     = useState(true)

  useEffect(() => {
    if (!codigo) { setLoading(false); return }
    fetch(`${API}/api/validar/${codigo}`)
      .then(r => r.json())
      .then(d => setResultado(d))
      .catch(() => setResultado({ valido: false, mensagem: 'Erro ao consultar. Tente novamente.' }))
      .finally(() => setLoading(false))
  }, [codigo])

  return (
    <div style={{ minHeight: '100vh', background: '#f8fafc', display: 'flex', alignItems: 'center', justifyContent: 'center', padding: 20 }}>
      <div style={{ width: '100%', maxWidth: 520 }}>

        {/* Header */}
        <div style={{ textAlign: 'center', marginBottom: 24 }}>
          <div style={{ fontSize: 40, marginBottom: 8 }}>🐾</div>
          <h1 style={{ fontSize: 22, fontWeight: 700, color: '#1e293b', margin: 0 }}>Validação de Receituário</h1>
          <p style={{ fontSize: 13, color: '#64748b', marginTop: 4 }}>VetClinica — Sistema de Gestão Veterinária</p>
        </div>

        {/* Card principal */}
        <div style={{ background: 'white', borderRadius: 20, boxShadow: '0 4px 24px rgba(0,0,0,0.08)', overflow: 'hidden' }}>

          {loading ? (
            <div style={{ padding: 48, textAlign: 'center', color: '#94a3b8' }}>
              <div style={{ fontSize: 32, marginBottom: 12, animation: 'spin 1s linear infinite' }}>⏳</div>
              <p>Consultando documento...</p>
            </div>
          ) : !resultado ? (
            <div style={{ padding: 48, textAlign: 'center', color: '#94a3b8' }}>
              <p>Código não informado.</p>
            </div>
          ) : resultado.valido ? (
            <>
              {/* Badge válido */}
              <div style={{ background: '#f0fdf4', borderBottom: '1px solid #bbf7d0', padding: '16px 24px', display: 'flex', alignItems: 'center', gap: 12 }}>
                <div style={{ width: 44, height: 44, background: '#16a34a', borderRadius: '50%', display: 'flex', alignItems: 'center', justifyContent: 'center', flexShrink: 0 }}>
                  <span style={{ color: 'white', fontSize: 22 }}>✓</span>
                </div>
                <div>
                  <div style={{ fontWeight: 700, color: '#15803d', fontSize: 15 }}>Documento Autêntico</div>
                  <div style={{ fontSize: 12, color: '#16a34a' }}>{resultado.mensagem}</div>
                </div>
              </div>

              {/* Dados */}
              <div style={{ padding: 24, display: 'flex', flexDirection: 'column', gap: 16 }}>

                {/* Código */}
                <div style={{ background: '#f8fafc', borderRadius: 12, padding: 12, border: '1px solid #e2e8f0' }}>
                  <div style={{ fontSize: 11, color: '#94a3b8', fontWeight: 600, textTransform: 'uppercase', letterSpacing: 1, marginBottom: 4 }}>Código do documento</div>
                  <div style={{ fontSize: 16, fontWeight: 700, color: '#1e293b', fontFamily: 'monospace' }}>{resultado.codigo}</div>
                  <div style={{ fontSize: 12, color: '#64748b', marginTop: 2 }}>Emitido em {resultado.emitidoEm}</div>
                </div>

                {/* Grid de dados */}
                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12 }}>
                  <Bloco titulo="Clínica" valor={resultado.clinica?.nome} sub={resultado.clinica?.telefone} />
                  <Bloco titulo="Veterinário" valor={resultado.veterinario?.nome} sub={resultado.veterinario?.crmv ? `CRMV ${resultado.veterinario.crmv}` : null} />
                  <Bloco titulo="Paciente" valor={resultado.paciente?.nome}
                    sub={[resultado.paciente?.especie, resultado.paciente?.raca].filter(Boolean).join(' · ')} />
                  <Bloco titulo="Responsável (Tutor)" valor={resultado.tutor?.nome} />
                </div>

                {/* Medicamentos */}
                {resultado.medicamentos?.length > 0 && (
                  <div>
                    <div style={{ fontSize: 11, color: '#94a3b8', fontWeight: 600, textTransform: 'uppercase', letterSpacing: 1, marginBottom: 8 }}>Medicamentos prescritos</div>
                    <div style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
                      {resultado.medicamentos.map((m, i) => (
                        <div key={i} style={{ fontSize: 13, color: '#374151', padding: '6px 12px', background: '#f8fafc', borderRadius: 8, borderLeft: '3px solid #6366f1' }}>
                          {m}
                        </div>
                      ))}
                    </div>
                  </div>
                )}

                {resultado.motivo && (
                  <div>
                    <div style={{ fontSize: 11, color: '#94a3b8', fontWeight: 600, textTransform: 'uppercase', letterSpacing: 1, marginBottom: 4 }}>Motivo / Diagnóstico</div>
                    <div style={{ fontSize: 13, color: '#374151' }}>{resultado.motivo}</div>
                  </div>
                )}
              </div>
            </>
          ) : (
            <>
              {/* Badge inválido */}
              <div style={{ background: '#fef2f2', borderBottom: '1px solid #fecaca', padding: '16px 24px', display: 'flex', alignItems: 'center', gap: 12 }}>
                <div style={{ width: 44, height: 44, background: '#dc2626', borderRadius: '50%', display: 'flex', alignItems: 'center', justifyContent: 'center', flexShrink: 0 }}>
                  <span style={{ color: 'white', fontSize: 22 }}>✗</span>
                </div>
                <div>
                  <div style={{ fontWeight: 700, color: '#dc2626', fontSize: 15 }}>Documento Não Encontrado</div>
                  <div style={{ fontSize: 12, color: '#b91c1c' }}>{resultado.mensagem}</div>
                </div>
              </div>
              <div style={{ padding: 24, color: '#64748b', fontSize: 13 }}>
                <p>O código <strong style={{ fontFamily: 'monospace' }}>{codigo}</strong> não foi encontrado no sistema.</p>
                <p style={{ marginTop: 8 }}>Possíveis motivos:</p>
                <ul style={{ paddingLeft: 20, marginTop: 4 }}>
                  <li>O código foi digitado incorretamente</li>
                  <li>O documento foi emitido antes da implantação do sistema de validação</li>
                  <li>O documento não é autêntico</li>
                </ul>
              </div>
            </>
          )}
        </div>

        <p style={{ textAlign: 'center', fontSize: 11, color: '#cbd5e1', marginTop: 16 }}>
          VetClinica — Gestão Inteligente para Clínicas Veterinárias<br />
          Este sistema garante a autenticidade dos documentos emitidos.
        </p>
      </div>
    </div>
  )
}

function Bloco({ titulo, valor, sub }) {
  return (
    <div style={{ background: '#f8fafc', borderRadius: 12, padding: 12, border: '1px solid #e2e8f0' }}>
      <div style={{ fontSize: 11, color: '#94a3b8', fontWeight: 600, textTransform: 'uppercase', letterSpacing: 0.8, marginBottom: 4 }}>{titulo}</div>
      <div style={{ fontSize: 14, fontWeight: 600, color: '#1e293b' }}>{valor || '—'}</div>
      {sub && <div style={{ fontSize: 11, color: '#64748b', marginTop: 2 }}>{sub}</div>}
    </div>
  )
}
