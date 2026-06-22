// Pagina publica de planos e precos — /planos
// Ainda sem integracao de pagamento (Asaas pendente)
// CTA aponta para /trial (cadastro) ou /login (ja tem conta)
import { useSearchParams } from 'react-router-dom'

export default function Planos() {
  const [params] = useSearchParams()
  const suspenso = params.get('motivo') === 'suspenso'

  const planos = [
    {
      id: 'starter',
      nome: 'Starter',
      preco: 149,
      periodo: '/mes',
      descricao: 'Ideal para clinicas pequenas e consultórios individuais',
      destaque: false,
      cor: 'border-slate-200',
      botaoCor: 'bg-slate-800 hover:bg-slate-700',
      recursos: [
        '1 usuario',
        'Tutores e Pets ilimitados',
        'Agenda e Atendimentos',
        'Receituario eletronico',
        'PDV e Caixa',
        'Financeiro basico',
        'Suporte por e-mail',
      ],
      naoIncluido: [
        'WhatsApp Bot',
        'Diagnostico por IA',
        'Modulo Compras',
        'Gestao de Contratos',
        'Relatorios avancados',
      ]
    },
    {
      id: 'profissional',
      nome: 'Profissional',
      preco: 289,
      periodo: '/mes',
      descricao: 'O mais escolhido. Para clinicas em crescimento',
      destaque: true,
      cor: 'border-blue-500',
      botaoCor: 'bg-blue-600 hover:bg-blue-700',
      recursos: [
        'Ate 5 usuarios',
        'Tutores e Pets ilimitados',
        'Agenda e Atendimentos',
        'Receituario eletronico + QR Code',
        'PDV e Caixa completo',
        'Financeiro + Bancario',
        'WhatsApp Bot',
        'Diagnostico por IA',
        'Modulo Compras e Estoque',
        'Relatorios e Dashboard',
        'Suporte prioritario',
      ],
      naoIncluido: [
        'Gestao de Contratos',
        'API personalizada',
      ]
    },
    {
      id: 'enterprise',
      nome: 'Enterprise',
      preco: null,
      periodo: '',
      descricao: 'Para redes de clinicas e hospitais veterinarios',
      destaque: false,
      cor: 'border-slate-300',
      botaoCor: 'bg-slate-700 hover:bg-slate-600',
      recursos: [
        'Usuarios ilimitados',
        'Multi-unidades',
        'Todos os modulos',
        'Gestao de Contratos',
        'API personalizada',
        'Integracao ERP (TOTVS)',
        'SLA garantido',
        'Gerente de conta dedicado',
        'Treinamento presencial',
      ],
      naoIncluido: []
    }
  ]

  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-900 to-blue-950">

      {/* Header */}
      <div className="text-center pt-16 pb-10 px-4">
        {suspenso && (
          <div className="max-w-xl mx-auto mb-8 bg-orange-500/20 border border-orange-400/40 text-orange-200 px-5 py-3 rounded-xl text-sm">
            Seu acesso foi suspenso (trial vencido ou pagamento pendente). Escolha um plano abaixo para continuar usando o sistema.
          </div>
        )}
        <div className="text-4xl mb-3">🐾</div>
        <h1 className="text-3xl font-bold text-white mb-2">VetClinica</h1>
        <p className="text-slate-300 text-lg mb-2">Planos e Precos</p>
        <p className="text-slate-400 text-sm max-w-xl mx-auto">
          Comece com 14 dias gratis. Sem cartao de credito. Cancele quando quiser.
        </p>

        {/* Trial badge */}
        <div className="inline-flex items-center gap-2 bg-green-500/20 border border-green-400/30 text-green-300 px-5 py-2 rounded-full mt-4 text-sm font-medium">
          14 dias de trial gratuito em qualquer plano
        </div>
      </div>

      {/* Planos */}
      <div className="max-w-5xl mx-auto px-4 pb-16">
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
          {planos.map(plano => (
            <div
              key={plano.id}
              className={`bg-white rounded-2xl border-2 ${plano.cor} overflow-hidden relative ${
                plano.destaque ? 'shadow-2xl shadow-blue-500/20 scale-105' : 'shadow-lg'
              }`}
            >
              {plano.destaque && (
                <div className="bg-blue-600 text-white text-center py-1.5 text-xs font-bold tracking-wide">
                  MAIS POPULAR
                </div>
              )}

              <div className="p-6">
                <h2 className="text-xl font-bold text-slate-800">{plano.nome}</h2>
                <p className="text-sm text-slate-500 mt-1 mb-4 min-h-10">{plano.descricao}</p>

                {/* Preco */}
                <div className="mb-6">
                  {plano.preco ? (
                    <div className="flex items-end gap-1">
                      <span className="text-sm text-slate-400 self-start mt-1">R$</span>
                      <span className="text-4xl font-bold text-slate-800">{plano.preco}</span>
                      <span className="text-slate-400 text-sm mb-1">{plano.periodo}</span>
                    </div>
                  ) : (
                    <div className="text-2xl font-bold text-slate-800">Sob consulta</div>
                  )}
                  <p className="text-xs text-slate-400 mt-1">
                    {plano.preco ? `ou R$${Math.round(plano.preco * 10 * 0.83)}/ano (2 meses gratis)` : 'Fale com nossa equipe'}
                  </p>
                </div>

                {/* CTA */}
                {plano.id === 'enterprise' ? (
                  <a
                    href="mailto:comercial@ketra.com.br?subject=VetClinica Enterprise"
                    className={`block w-full ${plano.botaoCor} text-white text-center font-bold py-3 rounded-xl transition-colors mb-6`}
                  >
                    Falar com Vendas
                  </a>
                ) : (
                  <a
                    href="/trial"
                    className={`block w-full ${plano.botaoCor} text-white text-center font-bold py-3 rounded-xl transition-colors mb-6`}
                  >
                    Comecar trial gratis
                  </a>
                )}

                {/* Recursos incluidos */}
                <div className="space-y-2">
                  {plano.recursos.map(r => (
                    <div key={r} className="flex items-start gap-2 text-sm">
                      <span className="text-green-500 mt-0.5 flex-shrink-0">✓</span>
                      <span className="text-slate-700">{r}</span>
                    </div>
                  ))}
                  {plano.naoIncluido.map(r => (
                    <div key={r} className="flex items-start gap-2 text-sm">
                      <span className="text-slate-300 mt-0.5 flex-shrink-0">✕</span>
                      <span className="text-slate-400">{r}</span>
                    </div>
                  ))}
                </div>
              </div>
            </div>
          ))}
        </div>

        {/* FAQ rapido */}
        <div className="mt-12 bg-white/10 backdrop-blur rounded-2xl p-8 text-white">
          <h3 className="text-xl font-bold mb-6 text-center">Perguntas frequentes</h3>
          <div className="grid md:grid-cols-2 gap-6 text-sm">
            <div>
              <p className="font-semibold mb-1">O trial e realmente gratuito?</p>
              <p className="text-slate-300">Sim. 14 dias com acesso completo, sem precisar de cartao de credito.</p>
            </div>
            <div>
              <p className="font-semibold mb-1">Posso cancelar a qualquer momento?</p>
              <p className="text-slate-300">Sim. Sem fidelidade, sem multa. Cancele quando quiser.</p>
            </div>
            <div>
              <p className="font-semibold mb-1">Meus dados ficam seguros?</p>
              <p className="text-slate-300">Cada clinica tem seu proprio banco de dados isolado. Backup diario automatico.</p>
            </div>
            <div>
              <p className="font-semibold mb-1">Tem suporte em portugues?</p>
              <p className="text-slate-300">Sim. Suporte por e-mail, WhatsApp e treinamento gratuito para todos os planos.</p>
            </div>
          </div>
        </div>

        {/* Footer links */}
        <div className="text-center mt-8 space-x-4">
          <a href="/login" className="text-slate-400 hover:text-white text-sm transition-colors">
            Ja tenho conta → Login
          </a>
          <span className="text-slate-600">·</span>
          <a href="mailto:suporte@ketra.com.br" className="text-slate-400 hover:text-white text-sm transition-colors">
            suporte@ketra.com.br
          </a>
        </div>
      </div>
    </div>
  )
}
