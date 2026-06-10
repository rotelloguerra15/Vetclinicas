import { useEffect, useRef, useState, useCallback } from 'react'

const FONT_URL = 'https://fonts.googleapis.com/css2?family=Nunito:wght@400;600;700;800;900&family=Baloo+2:wght@700;800&display=swap'
const INTERVALO_MS = 60_000

function injectCSS() {
  if (document.getElementById('gv-styles')) return
  const s = document.createElement('style')
  s.id = 'gv-styles'
  s.textContent = `
@import url('${FONT_URL}');
.gv-root{font-family:'Nunito',sans-serif;background:#0a1628;color:#e2e8f0;width:100vw;height:100vh;overflow:hidden;position:fixed;top:0;left:0;display:flex;flex-direction:column;}
.gv-root *{box-sizing:border-box;margin:0;padding:0;}
.gv-root::after{content:'';position:fixed;inset:0;background-image:radial-gradient(rgba(99,179,237,0.07) 1px,transparent 1px);background-size:36px 36px;pointer-events:none;z-index:0;}
.gv-header{position:relative;z-index:2;display:flex;align-items:center;justify-content:space-between;padding:clamp(8px,1.5vh,16px) clamp(10px,2vw,24px);background:rgba(10,22,40,0.85);border-bottom:1px solid rgba(99,179,237,0.12);flex-shrink:0;}
.gv-logo-area{display:flex;align-items:center;gap:12px;}
.gv-logo-icon{width:clamp(36px,4vw,50px);height:clamp(36px,4vw,50px);background:linear-gradient(135deg,#0ea5e9,#7c3aed);border-radius:14px;display:flex;align-items:center;justify-content:center;font-size:clamp(18px,2vw,24px);animation:gv-pulse 3s ease-in-out infinite;}
@keyframes gv-pulse{0%,100%{box-shadow:0 0 20px rgba(56,189,248,0.3);}50%{box-shadow:0 0 36px rgba(56,189,248,0.55);}}
.gv-clinic-name{font-family:'Baloo 2',cursive;font-weight:800;font-size:clamp(14px,2vw,24px);background:linear-gradient(135deg,#fff 30%,#38bdf8);-webkit-background-clip:text;-webkit-text-fill-color:transparent;line-height:1.1;}
.gv-clinic-sub{font-size:clamp(9px,0.9vw,11px);color:#64748b;letter-spacing:1.5px;text-transform:uppercase;margin-top:2px;}
.gv-center{text-align:center;}
.gv-clock{font-family:'Baloo 2',cursive;font-weight:700;font-size:clamp(22px,3.5vw,42px);color:#fff;line-height:1;}
.gv-date{font-size:clamp(9px,0.9vw,12px);color:#64748b;margin-top:2px;}
.gv-header-right{display:flex;flex-direction:column;align-items:flex-end;gap:5px;}
.gv-live{display:flex;align-items:center;gap:6px;background:rgba(74,222,128,0.12);border:1px solid rgba(74,222,128,0.3);border-radius:20px;padding:3px 10px;font-size:clamp(9px,0.8vw,11px);font-weight:700;color:#4ade80;letter-spacing:1.5px;text-transform:uppercase;}
.gv-live-dot{width:7px;height:7px;border-radius:50%;background:#4ade80;animation:gv-blink 1.2s ease-in-out infinite;}
@keyframes gv-blink{0%,100%{opacity:1;}50%{opacity:0.2;}}
.gv-stat-mini{font-size:clamp(10px,0.9vw,13px);color:#64748b;}
.gv-stat-mini span{color:#38bdf8;font-weight:700;}
.gv-progress-wrap{position:relative;z-index:2;height:3px;background:rgba(255,255,255,0.05);flex-shrink:0;}
.gv-progress-bar{height:100%;background:linear-gradient(90deg,#0ea5e9,#7c3aed);transition:width 1s linear;}
.gv-slide-label{position:relative;z-index:2;flex-shrink:0;display:flex;align-items:center;gap:8px;padding:clamp(3px,0.5vh,6px) clamp(10px,2vw,24px);background:rgba(255,255,255,0.03);border-bottom:1px solid rgba(255,255,255,0.05);font-size:clamp(9px,0.85vw,12px);font-weight:700;letter-spacing:1px;text-transform:uppercase;color:#64748b;}
.gv-slide-dot{width:6px;height:6px;border-radius:50%;}
.gv-slide-name{color:#e2e8f0;}
.gv-main{position:relative;z-index:1;flex:1;overflow:hidden;}
.gv-panel{width:100%;height:100%;display:grid;grid-template-columns:1fr 1fr 1fr clamp(200px,22vw,300px);gap:clamp(8px,1.2vw,14px);padding:clamp(8px,1.2vh,14px) clamp(10px,1.5vw,20px);}
.gv-col{display:flex;flex-direction:column;gap:clamp(6px,0.8vh,10px);overflow:hidden;}
.gv-col-header{display:flex;align-items:center;gap:8px;padding:clamp(6px,0.8vh,10px) clamp(10px,1.2vw,14px);border-radius:12px;font-weight:800;font-size:clamp(9px,0.85vw,13px);letter-spacing:1px;text-transform:uppercase;}
.gv-col-header.agendado{background:rgba(99,179,237,.1);border:1px solid rgba(99,179,237,.25);color:#63b3ed;}
.aguardando .gv-col-count{background:rgba(251,191,36,.25);color:#fbbf24;}
.gv-col-header.aguardando{background:rgba(251,191,36,.12);border:1px solid rgba(251,191,36,.3);color:#fbbf24;}
.gv-col-header.em-andamento{background:rgba(56,189,248,.12);border:1px solid rgba(56,189,248,.3);color:#38bdf8;}
.gv-col-header.pronto{background:rgba(74,222,128,.12);border:1px solid rgba(74,222,128,.3);color:#4ade80;}
.gv-col-count{margin-left:auto;width:clamp(18px,1.6vw,22px);height:clamp(18px,1.6vw,22px);border-radius:50%;display:flex;align-items:center;justify-content:center;font-size:clamp(10px,0.8vw,12px);font-weight:900;}
.aguardando .gv-col-count{background:rgba(251,191,36,.25);color:#fbbf24;}
.em-andamento .gv-col-count{background:rgba(56,189,248,.25);color:#38bdf8;}
.pronto .gv-col-count{background:rgba(74,222,128,.25);color:#4ade80;}
.gv-cards{display:flex;flex-direction:column;gap:clamp(5px,0.7vh,9px);overflow-y:auto;flex:1;scrollbar-width:none;}
.gv-card{background:#112240;border-radius:12px;padding:clamp(8px,1vh,12px) clamp(10px,1.2vw,14px);border:1px solid rgba(99,179,237,0.13);position:relative;overflow:hidden;animation:gv-card-in .45s ease both;}
@keyframes gv-card-in{from{opacity:0;transform:translateY(10px);}to{opacity:1;transform:translateY(0);}}
.gv-card::before{content:'';position:absolute;left:0;top:0;bottom:0;width:3px;border-radius:2px 0 0 2px;}
.gv-card.agendado::before{background:#63b3ed;}
.gv-card.agendado{border-color:rgba(99,179,237,.22);}
.gv-card.agendado .gv-avatar{opacity:0.85;}
.gv-card.aguardando::before{background:#fbbf24;}
.gv-card.em-andamento::before{background:#38bdf8;}
.gv-card.pronto::before{background:#4ade80;}
.gv-card.pronto{border-color:rgba(74,222,128,.25);animation:gv-card-in .45s ease both,gv-ring 2.5s ease-in-out infinite;}
@keyframes gv-ring{0%,100%{box-shadow:0 0 0 0 rgba(74,222,128,.3);}50%{box-shadow:0 0 0 5px rgba(74,222,128,0);}}
.gv-card.em-andamento{border-color:rgba(56,189,248,.22);}
.gv-card-top{display:flex;align-items:center;gap:clamp(6px,0.8vw,10px);margin-bottom:clamp(5px,0.7vh,8px);}
.gv-avatar{width:clamp(32px,3.8vw,46px);height:clamp(32px,3.8vw,46px);border-radius:50%;display:flex;align-items:center;justify-content:center;font-size:clamp(16px,1.8vw,22px);flex-shrink:0;position:relative;background:rgba(56,189,248,0.1);}
.gv-avatar.spin::after{content:'';position:absolute;inset:-3px;border-radius:50%;border:2px solid #38bdf8;animation:gv-spin 3.5s linear infinite;border-top-color:transparent;border-right-color:transparent;}
@keyframes gv-spin{to{transform:rotate(360deg);}}
.gv-pet-name{font-size:clamp(12px,1.2vw,16px);font-weight:800;color:#fff;line-height:1.1;white-space:nowrap;overflow:hidden;text-overflow:ellipsis;}
.gv-pet-sub{font-size:clamp(9px,0.75vw,11px);color:#64748b;margin-top:1px;}
.gv-owner{font-size:clamp(9px,0.75vw,11px);color:#64748b;}
.gv-tags{display:flex;flex-wrap:wrap;gap:4px;margin-bottom:clamp(5px,0.6vh,7px);}
.gv-tag{font-size:clamp(9px,0.7vw,10px);font-weight:700;padding:2px 8px;border-radius:20px;background:rgba(56,189,248,.1);color:#38bdf8;border:1px solid rgba(56,189,248,.2);}
.gv-tag.green{background:rgba(74,222,128,.1);color:#4ade80;border-color:rgba(74,222,128,.25);}
.gv-tag.orange{background:rgba(251,146,60,.1);color:#fb923c;border-color:rgba(251,146,60,.25);}
.gv-card-footer{display:flex;align-items:center;justify-content:space-between;}
.gv-time-info{font-size:clamp(9px,0.72vw,11px);color:#64748b;}
.gv-badge{font-size:clamp(9px,0.72vw,11px);font-weight:700;padding:2px 8px;border-radius:8px;}
.gv-badge.wait{background:rgba(251,191,36,.15);color:#fbbf24;}
.gv-badge.go{background:rgba(56,189,248,.15);color:#38bdf8;animation:gv-glow 2s ease-in-out infinite;}
@keyframes gv-glow{0%,100%{box-shadow:none;}50%{box-shadow:0 0 8px rgba(56,189,248,.45);}}
.gv-badge.done{background:rgba(74,222,128,.15);color:#4ade80;}
.gv-side{display:flex;flex-direction:column;gap:clamp(8px,1vh,12px);}
.gv-stats{background:#112240;border-radius:12px;padding:clamp(8px,1vh,14px);border:1px solid rgba(99,179,237,0.13);display:grid;grid-template-columns:1fr 1fr;gap:clamp(6px,0.8vw,10px);}
.gv-stat-item{text-align:center;padding:clamp(5px,0.7vh,8px);border-radius:10px;background:rgba(255,255,255,0.03);}
.gv-stat-num{font-family:'Baloo 2',cursive;font-weight:800;line-height:1;font-size:clamp(20px,2.8vw,32px);}
.gv-stat-num.teal{color:#38bdf8;}.gv-stat-num.green{color:#4ade80;}.gv-stat-num.yellow{color:#fbbf24;}.gv-stat-num.purple{color:#a78bfa;}
.gv-stat-label{font-size:clamp(8px,0.65vw,10px);color:#64748b;margin-top:2px;letter-spacing:.5px;}
.gv-agenda-card{background:#112240;border-radius:12px;border:1px solid rgba(99,179,237,0.13);flex:1;overflow:hidden;display:flex;flex-direction:column;}
.gv-agenda-hdr{background:linear-gradient(135deg,rgba(56,189,248,.12),rgba(167,139,250,.08));padding:clamp(7px,0.9vh,12px) clamp(10px,1.2vw,14px);border-bottom:1px solid rgba(99,179,237,0.12);font-weight:800;font-size:clamp(10px,0.9vw,14px);color:#fff;}
.gv-agenda-list{padding:clamp(5px,0.6vh,8px);display:flex;flex-direction:column;gap:5px;overflow-y:auto;flex:1;scrollbar-width:none;}
.gv-ag-item{display:flex;align-items:center;gap:clamp(6px,0.8vw,10px);padding:clamp(5px,0.7vh,9px) clamp(8px,1vw,10px);border-radius:9px;background:rgba(255,255,255,0.025);border:1px solid rgba(255,255,255,0.04);}
.gv-ag-item.current{background:rgba(56,189,248,.09);border-color:rgba(56,189,248,.22);}
.gv-ag-time{font-size:clamp(10px,0.8vw,12px);font-weight:800;min-width:36px;}
.gv-ag-time.past{color:#64748b;}.gv-ag-time.current{color:#38bdf8;}.gv-ag-time.future{color:#e2e8f0;}
.gv-ag-dot{width:7px;height:7px;border-radius:50%;flex-shrink:0;}
.gv-ag-dot.done{background:#4ade80;}.gv-ag-dot.current{background:#38bdf8;animation:gv-blink 1.2s infinite;}.gv-ag-dot.upcoming{background:#64748b;}
.gv-ag-name{font-size:clamp(10px,0.85vw,13px);font-weight:700;color:#fff;line-height:1.1;}
.gv-ag-svc{font-size:clamp(8px,0.65vw,10px);color:#64748b;}
.gv-empty{display:flex;flex-direction:column;align-items:center;justify-content:center;height:100%;gap:8px;opacity:.45;}
.gv-empty-icon{font-size:clamp(28px,3.5vw,42px);}
.gv-empty-text{font-size:clamp(10px,0.9vw,13px);color:#64748b;}
.gv-ticker{position:relative;z-index:2;flex-shrink:0;display:flex;align-items:center;height:clamp(38px,5vh,54px);background:#0d1b35;border-top:1px solid rgba(99,179,237,0.1);overflow:hidden;}
.gv-ticker-label{flex-shrink:0;padding:0 clamp(10px,1.5vw,16px);font-size:clamp(9px,0.75vw,11px);font-weight:800;letter-spacing:2px;color:#38bdf8;text-transform:uppercase;border-right:1px solid rgba(99,179,237,0.15);height:100%;display:flex;align-items:center;background:rgba(56,189,248,0.05);white-space:nowrap;}
.gv-ticker-track{display:flex;align-items:center;white-space:nowrap;animation:gv-ticker 35s linear infinite;padding-left:16px;}
@keyframes gv-ticker{from{transform:translateX(0);}to{transform:translateX(-50%);}}
.gv-tick-item{display:inline-flex;align-items:center;gap:5px;margin-right:36px;font-size:clamp(11px,1vw,14px);}
.gv-tick-n{font-weight:700;color:#fff;}
.gv-tick-s{color:#64748b;font-size:clamp(10px,0.8vw,12px);}
.gv-pill{font-size:clamp(9px,0.7vw,10px);font-weight:700;padding:2px 8px;border-radius:10px;text-transform:uppercase;letter-spacing:.5px;}
.gv-pill-ok{background:rgba(74,222,128,.2);color:#4ade80;}
.gv-pill-go{background:rgba(56,189,248,.2);color:#38bdf8;}
.gv-pill-wait{background:rgba(251,191,36,.2);color:#fbbf24;}
.gv-sep{color:#334155;margin-right:8px;}
.gv-news{width:100%;height:100%;padding:clamp(10px,1.5vh,18px) clamp(12px,2vw,28px);display:flex;flex-direction:column;gap:clamp(8px,1.2vh,14px);overflow:hidden;}
.gv-news-header{display:flex;align-items:center;gap:12px;flex-shrink:0;}
.gv-news-badge{display:flex;align-items:center;gap:8px;padding:clamp(6px,0.8vh,10px) clamp(14px,1.5vw,20px);border-radius:40px;font-size:clamp(13px,1.2vw,18px);font-weight:800;color:#fff;}
.gv-news-title{font-family:'Baloo 2',cursive;font-weight:800;font-size:clamp(16px,1.8vw,26px);color:#fff;}
.gv-news-sub{font-size:clamp(10px,0.85vw,12px);color:#64748b;letter-spacing:1px;text-transform:uppercase;}
.gv-news-grid{flex:1;overflow:hidden;display:grid;gap:clamp(7px,1vh,12px);grid-template-columns:1fr 1fr;grid-template-rows:1fr 1fr;}
.gv-news-card{background:#112240;border-radius:14px;padding:clamp(10px,1.3vh,16px) clamp(12px,1.3vw,18px);border:1px solid rgba(99,179,237,0.12);display:flex;flex-direction:column;gap:clamp(5px,0.7vh,8px);overflow:hidden;animation:gv-card-in .5s ease both;}
.gv-news-card:nth-child(2){animation-delay:.07s;}.gv-news-card:nth-child(3){animation-delay:.14s;}.gv-news-card:nth-child(4){animation-delay:.21s;}
.gv-news-cat{font-size:clamp(8px,0.65vw,10px);font-weight:700;letter-spacing:1.5px;text-transform:uppercase;opacity:.7;}
.gv-news-headline{font-size:clamp(12px,1.1vw,16px);font-weight:800;color:#fff;line-height:1.35;display:-webkit-box;-webkit-line-clamp:3;-webkit-box-orient:vertical;overflow:hidden;}
.gv-news-desc{font-size:clamp(10px,0.8vw,12px);color:#94a3b8;line-height:1.5;display:-webkit-box;-webkit-line-clamp:3;-webkit-box-orient:vertical;overflow:hidden;flex:1;}
.gv-news-date{font-size:clamp(9px,0.68vw,10px);color:#475569;margin-top:auto;}
.gv-loading{display:flex;flex-direction:column;align-items:center;justify-content:center;height:100%;gap:12px;opacity:.5;}
.gv-spinner{width:36px;height:36px;border-radius:50%;border:3px solid rgba(56,189,248,0.2);border-top-color:#38bdf8;animation:gv-spin 1s linear infinite;}
.gv-slide-enter{animation:gv-slide-in .6s ease both;}
@keyframes gv-slide-in{from{opacity:0;transform:scale(.98);}to{opacity:1;transform:scale(1);}}
  `
  document.head.appendChild(s)
}

function emojiEspecie(e) {
  if (!e) return '🐾'
  const s = e.toLowerCase()
  if (s.includes('can') || s.includes('cao') || s.includes('cachorro')) return '🐶'
  if (s.includes('fel') || s.includes('gat')) return '🐱'
  if (s.includes('ave') || s.includes('pass')) return '🐦'
  if (s.includes('coelh')) return '🐇'
  if (s.includes('hamst') || s.includes('roedor')) return '🐹'
  return '🐾'
}
function formatHora(dt) {
  if (!dt) return ''
  return new Date(dt).toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' })
}
function tempoDecorrido(dt) {
  if (!dt) return ''
  const diff = Math.floor((Date.now() - new Date(dt).getTime()) / 60000)
  if (diff < 1) return 'agora'
  if (diff < 60) return `${diff} min`
  const h = Math.floor(diff / 60), m = diff % 60
  return m > 0 ? `${h}h ${m}min` : `${h}h`
}

function buildRotacao(feeds) {
  const seq = []
  for (let i = 0; i < feeds.length; i++) {
    seq.push({ type: 'painel' })
    seq.push({ type: 'news', feedIdx: i })
  }
  return seq
}

export default function GestaoVista() {
  const [dados, setDados] = useState(null)
  const [feeds, setFeeds] = useState([])
  const [rotacao, setRotacao] = useState([{ type: 'painel' }])
  const [slideIdx, setSlideIdx] = useState(0)
  const [progresso, setProgresso] = useState(0)
  const [transitioning, setTransitioning] = useState(false)
  const [newsCache, setNewsCache] = useState({})
  const newsCacheRef = useRef({})
  const [clock, setClock] = useState({ hora: '', data: '' })

  const progressRef = useRef(0)
  const progressInterval = useRef(null)
  const slideTimeout = useRef(null)

  const tenantId = localStorage.getItem('tenantId')
  const token = localStorage.getItem('token')
  const apiBase = (import.meta.env.VITE_API_URL || 'http://localhost:5010/api').replace(/\/api$/, '') + '/api'

  injectCSS()

  // Relógio
  useEffect(() => {
    function tick() {
      const n = new Date()
      const dias = ['Domingo', 'Segunda-feira', 'Terca-feira', 'Quarta-feira', 'Quinta-feira', 'Sexta-feira', 'Sabado']
      const meses = ['janeiro', 'fevereiro', 'marco', 'abril', 'maio', 'junho', 'julho', 'agosto', 'setembro', 'outubro', 'novembro', 'dezembro']
      setClock({
        hora: String(n.getHours()).padStart(2, '0') + ':' + String(n.getMinutes()).padStart(2, '0'),
        data: `${dias[n.getDay()]}, ${n.getDate()} de ${meses[n.getMonth()]} de ${n.getFullYear()}`
      })
    }
    tick()
    const t = setInterval(tick, 1000)
    return () => clearInterval(t)
  }, [])

  // Busca lista de feeds e monta rotacao
  useEffect(() => {
    fetch(`${apiBase}/gestao-vista/feeds`, { headers: { Authorization: `Bearer ${token}` } })
      .then(r => r.json())
      .then(data => {
        setFeeds(data)
        setRotacao(buildRotacao(data))
      })
      .catch(() => setRotacao([{ type: 'painel' }]))
  }, [apiBase, token])

  // Busca dados do painel
  const fetchDados = useCallback(async () => {
    if (!tenantId) return
    try {
      const r = await fetch(`${apiBase}/gestao-vista/${tenantId}`, { headers: { Authorization: `Bearer ${token}` } })
      if (r.ok) setDados(await r.json())
    } catch { /* silencioso */ }
  }, [tenantId, apiBase, token])

  useEffect(() => {
    fetchDados()
    const t = setInterval(fetchDados, 15_000)
    return () => clearInterval(t)
  }, [fetchDados])

  // Busca notícias do backend
  // FIX: usa ref para verificar cache sem criar dependência circular no useCallback
  const fetchNews = useCallback(async (feedId) => {
    if (newsCacheRef.current[feedId] !== undefined) return
    newsCacheRef.current[feedId] = 'loading' // marca como em andamento para evitar duplo fetch
    try {
      const r = await fetch(`${apiBase}/gestao-vista/noticias/${feedId}`, { headers: { Authorization: `Bearer ${token}` } })
      const data = await r.json()
      newsCacheRef.current[feedId] = data
      setNewsCache(prev => ({ ...prev, [feedId]: data }))
    } catch {
      newsCacheRef.current[feedId] = null
      setNewsCache(prev => ({ ...prev, [feedId]: null }))
    }
  }, [apiBase, token])

  // Avanca slide
  const avancarSlide = useCallback(() => {
    setTransitioning(true)
    setTimeout(() => {
      setSlideIdx(prev => (prev + 1) % rotacao.length)
      setProgresso(0)
      progressRef.current = 0
      setTransitioning(false)
    }, 400)
  }, [rotacao.length])

  // Progress bar
  useEffect(() => {
    clearInterval(progressInterval.current)
    clearTimeout(slideTimeout.current)
    const steps = INTERVALO_MS / 200
    progressRef.current = 0
    progressInterval.current = setInterval(() => {
      progressRef.current += 1
      setProgresso(Math.min((progressRef.current / steps) * 100, 100))
      if (progressRef.current >= steps) clearInterval(progressInterval.current)
    }, 200)
    slideTimeout.current = setTimeout(avancarSlide, INTERVALO_MS)
    return () => { clearInterval(progressInterval.current); clearTimeout(slideTimeout.current) }
  }, [slideIdx, avancarSlide])

  // Pre-carrega proximo feed
  const currentSlide = rotacao[slideIdx] || { type: 'painel' }
  const nextSlide = rotacao[(slideIdx + 1) % rotacao.length] || { type: 'painel' }

  useEffect(() => {
    if (currentSlide.type === 'news') fetchNews(feeds[currentSlide.feedIdx]?.id)
    if (nextSlide.type === 'news') fetchNews(feeds[nextSlide.feedIdx]?.id)
  }, [currentSlide, nextSlide, feeds, fetchNews])

  // ─── Render card OS ───────────────────────────────────────────
  function renderCard(os) {
    const statusClass = os.status === 'em_andamento' ? 'em-andamento' : os.status
    const badgeMap = { aguardando: 'wait', em_andamento: 'go', pronto: 'done' }
    const badgeTxt = {
      aguardando: tempoDecorrido(os.criadoEm),
      em_andamento: tempoDecorrido(os.inicio),
      pronto: 'Aguard. retirada'
    }
    return (
      <div key={os.id} className={`gv-card ${statusClass}`}>
        <div className="gv-card-top">
          <div className={`gv-avatar ${os.status === 'em_andamento' ? 'spin' : ''}`}>{emojiEspecie(os.petEspecie)}</div>
          <div style={{ flex: 1, minWidth: 0 }}>
            <div className="gv-pet-name">{os.petNome}</div>
            {os.petRaca && <div className="gv-pet-sub">{os.petRaca}</div>}
            {os.tutorNome && <div className="gv-owner">👤 {os.tutorNome}</div>}
          </div>
        </div>
        {os.servicos?.length > 0 && (
          <div className="gv-tags">
            {os.servicos.map((s, i) => (
              <span key={i} className={`gv-tag ${i % 2 === 1 ? 'orange' : os.status === 'pronto' ? 'green' : ''}`}>{s}</span>
            ))}
          </div>
        )}
        <div className="gv-card-footer">
          <div className="gv-time-info">
            {os.status === 'aguardando' && `🕐 ${formatHora(os.criadoEm)}`}
            {os.status === 'em_andamento' && `⚡ ${formatHora(os.inicio)}`}
            {os.status === 'pronto' && `✅ ${formatHora(os.fim)}`}
          </div>
          <div className={`gv-badge ${badgeMap[os.status] || 'wait'}`}>{badgeTxt[os.status]}</div>
        </div>
      </div>
    )
  }

  // ─── Render card de agendamento ──────────────────────────────
  function renderCardAgendamento(ag) {
    const agora = new Date()
    const dt = new Date(ag.dataHora)
    const diff = Math.round((dt - agora) / 60000)
    const statusLabel = {
      pendente:       '⏳ Pendente',
      confirmado:     '✓ Confirmado',
      em_atendimento: '⚡ Em atendimento',
      concluido:      '✅ Concluído',
      cancelado:      '✗ Cancelado',
      faltou:         '✗ Faltou',
    }
    const tipoEmoji = {
      consulta:'🩺', banho:'🛁', tosa:'✂️', banho_tosa:'🛁✂️',
      vacina:'💉', retorno:'🔄', cirurgia:'⚕️', exame:'🔬'
    }
    return (
      <div key={ag.id} className={`gv-card agendado`} style={{ opacity: ag.status === 'concluido' ? 0.5 : 1 }}>
        <div className="gv-card-top">
          <div className="gv-avatar">{tipoEmoji[ag.tipo] || '📋'}</div>
          <div style={{ flex: 1, minWidth: 0 }}>
            <div className="gv-pet-name">{ag.petNome}</div>
            <div className="gv-pet-sub" style={{ color: '#63b3ed' }}>{ag.tipo?.replace('_',' ')}</div>
          </div>
        </div>
        <div className="gv-card-footer">
          <div className="gv-time-info">🕐 {formatHora(ag.dataHora)}</div>
          <div className="gv-badge" style={{
            background: ag.status === 'em_atendimento' ? 'rgba(167,139,250,.2)' :
                        ag.status === 'confirmado'     ? 'rgba(99,179,237,.2)'  :
                        ag.status === 'concluido'      ? 'rgba(74,222,128,.2)'  : 'rgba(251,191,36,.15)',
            color: ag.status === 'em_atendimento' ? '#a78bfa' :
                   ag.status === 'confirmado'     ? '#63b3ed' :
                   ag.status === 'concluido'      ? '#4ade80' : '#fbbf24',
            fontSize: 'clamp(8px,0.7vw,10px)', padding: '2px 8px', borderRadius: 20
          }}>
            {statusLabel[ag.status] || ag.status}
          </div>
        </div>
      </div>
    )
  }

  // ─── Render painel ────────────────────────────────────────────
  function renderPainel() {
    const ordens = dados?.ordens || []
    const agendamentos = dados?.agendamentos || []
    const cont = dados?.contadores || {}
    const agora = new Date()

    const aguardando = ordens.filter(o => o.status === 'aguardando')
    const emAndamento = ordens.filter(o => o.status === 'em_andamento')
    const prontos = ordens.filter(o => o.status === 'pronto')

    return (
      <div style={{ width: '100%', height: '100%', position: 'relative' }}>
        <div className="gv-panel gv-slide-enter">
          <div className="gv-col">
            <div className="gv-col-header aguardando">⏳ Aguardando<div className="gv-col-count">{aguardando.length}</div></div>
            <div className="gv-cards">
              {aguardando.length === 0
                ? <div className="gv-empty"><div className="gv-empty-icon">🎉</div><div className="gv-empty-text">Nenhum aguardando</div></div>
                : aguardando.map(renderCard)}
            </div>
          </div>
          <div className="gv-col">
            <div className="gv-col-header em-andamento">⚡ Em Atendimento<div className="gv-col-count">{emAndamento.length}</div></div>
            <div className="gv-cards">
              {emAndamento.length === 0
                ? <div className="gv-empty"><div className="gv-empty-icon">💤</div><div className="gv-empty-text">Nenhum em andamento</div></div>
                : emAndamento.map(renderCard)}
            </div>
          </div>
          <div className="gv-col">
            <div className="gv-col-header pronto">✅ Pronto p/ Retirar<div className="gv-col-count">{prontos.length}</div></div>
            <div className="gv-cards">
              {prontos.length === 0
                ? <div className="gv-empty"><div className="gv-empty-icon">🐾</div><div className="gv-empty-text">Nenhum pronto ainda</div></div>
                : prontos.map(renderCard)}
            </div>
          </div>
          <div className="gv-col" style={{ minWidth: 0, maxWidth: 'clamp(160px, 16vw, 220px)' }}>
            <div className="gv-col-header agendado">📅 Agendados<div className="gv-col-count">{agendamentos.filter(a => a.status !== 'cancelado').length}</div></div>
            <div className="gv-cards">
              {agendamentos.filter(a => a.status !== 'cancelado').length === 0
                ? <div className="gv-empty"><div className="gv-empty-icon">📅</div><div className="gv-empty-text">Sem agendamentos</div></div>
                : agendamentos
                    .filter(a => a.status !== 'cancelado')
                    .sort((a,b) => new Date(a.dataHora) - new Date(b.dataHora))
                    .map(renderCardAgendamento)}
            </div>
          </div>
          <div className="gv-side">
            <div className="gv-stats">
              <div className="gv-stat-item"><div className="gv-stat-num teal">{cont.totalHoje ?? '–'}</div><div className="gv-stat-label">No dia</div></div>
              <div className="gv-stat-item"><div className="gv-stat-num green">{cont.finalizadosHoje ?? '–'}</div><div className="gv-stat-label">Finalizados</div></div>
              <div className="gv-stat-item"><div className="gv-stat-num yellow">{cont.aguardando ?? '–'}</div><div className="gv-stat-label">Aguardando</div></div>
              <div className="gv-stat-item"><div className="gv-stat-num purple">{cont.emAndamento ?? '–'}</div><div className="gv-stat-label">Em curso</div></div>
            </div>
            <div className="gv-agenda-card">
              <div className="gv-agenda-hdr">📅 Agenda de hoje</div>
              <div className="gv-agenda-list">
                {agendamentos.length === 0
                  ? <div className="gv-empty" style={{ padding: '16px 0' }}><div className="gv-empty-icon">📅</div><div className="gv-empty-text">Sem agendamentos</div></div>
                  : agendamentos.map(ag => {
                    const dt = new Date(ag.dataHora)
                    const isCurrent = Math.abs(dt - agora) < 30 * 60 * 1000
                    const isPast = dt < agora
                    return (
                      <div key={ag.id} className={`gv-ag-item ${isCurrent ? 'current' : ''}`}>
                        <div className={`gv-ag-time ${isCurrent ? 'current' : isPast ? 'past' : 'future'}`}>{formatHora(ag.dataHora)}</div>
                        <div className={`gv-ag-dot ${ag.status === 'realizado' ? 'done' : isCurrent ? 'current' : 'upcoming'}`}></div>
                        <div><div className="gv-ag-name">{ag.petNome}</div><div className="gv-ag-svc">{ag.tipo}</div></div>
                      </div>
                    )
                  })}
              </div>
            </div>
          </div>
        </div>

        {ordens.length > 0 && (
          <div className="gv-ticker" style={{ position: 'absolute', bottom: 0, left: 0, right: 0 }}>
            <div className="gv-ticker-label">🐾 Status</div>
            <div className="gv-ticker-track">
              {[...ordens, ...ordens].map((o, i) => {
                const pill = o.status === 'pronto' ? 'ok' : o.status === 'em_andamento' ? 'go' : 'wait'
                const pillTxt = o.status === 'pronto' ? 'Pronto!' : o.status === 'em_andamento' ? 'Em atendimento' : 'Aguardando'
                return (
                  <span key={i} className="gv-tick-item">
                    <span>{emojiEspecie(o.petEspecie)}</span>
                    <span className="gv-tick-n">{o.petNome}</span>
                    {o.servicos?.length > 0 && <span className="gv-tick-s">— {o.servicos.join(', ')} —</span>}
                    <span className={`gv-pill gv-pill-${pill}`}>{pillTxt}</span>
                    <span className="gv-sep">•</span>
                  </span>
                )
              })}
            </div>
          </div>
        )}
      </div>
    )
  }

  // ─── Render noticias ──────────────────────────────────────────
  function renderNews(feedIdx) {
    const feedMeta = feeds[feedIdx]
    if (!feedMeta) return null
    const news = newsCache[feedMeta.id]

    return (
      <div className="gv-news gv-slide-enter">
        <div className="gv-news-header">
          <div className="gv-news-badge" style={{ background: `${feedMeta.cor}22`, border: `1px solid ${feedMeta.cor}44` }}>
            <span>{feedMeta.icone}</span>
            <span style={{ color: feedMeta.cor }}>{feedMeta.nome}</span>
          </div>
          <div>
            <div className="gv-news-title">Manchetes do momento</div>
            <div className="gv-news-sub">Atualizado em tempo real</div>
          </div>
        </div>

        {!news && (
          <div className="gv-loading"><div className="gv-spinner"></div><div style={{ fontSize: 13, color: '#64748b' }}>Carregando...</div></div>
        )}

        {news && news.items?.length > 0 && (
          <div className="gv-news-grid">
            {news.items.slice(0, 4).map((item, i) => (
              <div key={i} className="gv-news-card" style={{ padding: 0, overflow: 'hidden' }}>
                {/* Imagem com aspect-ratio 16/7 para boa proporção em TV */}
                <div style={{
                  width: '100%',
                  aspectRatio: '16/7',
                  flexShrink: 0,
                  position: 'relative',
                  background: item.imageUrl ? 'transparent' : 'rgba(255,255,255,0.04)',
                  overflow: 'hidden'
                }}>
                  {item.imageUrl && (
                    <img
                      src={item.imageUrl}
                      alt=""
                      style={{
                        width: '100%', height: '100%',
                        objectFit: 'cover', objectPosition: 'center top',
                        display: 'block'
                      }}
                      onError={e => { e.target.parentNode.style.display = 'none' }}
                    />
                  )}
                  {/* Gradiente sobre a imagem para o texto não sufocar */}
                  {item.imageUrl && (
                    <div style={{
                      position: 'absolute', bottom: 0, left: 0, right: 0, height: '40%',
                      background: 'linear-gradient(to top, rgba(17,34,64,0.85), transparent)'
                    }} />
                  )}
                </div>
                <div style={{ padding: 'clamp(8px,1vh,12px) clamp(10px,1.2vw,14px)', display: 'flex', flexDirection: 'column', gap: 4, flex: 1 }}>
                  <div className="gv-news-cat" style={{ color: feedMeta.cor }}>{feedMeta.nome}</div>
                  <div className="gv-news-headline">{item.title}</div>
                  {item.description && !item.imageUrl && (
                    <div className="gv-news-desc">{item.description.slice(0, 180)}</div>
                  )}
                  <div className="gv-news-date">
                    {item.pubDate ? new Date(item.pubDate).toLocaleString('pt-BR', { day: '2-digit', month: 'short', hour: '2-digit', minute: '2-digit' }) : ''}
                  </div>
                </div>
              </div>
            ))}
          </div>
        )}

        {news && (!news.items || news.items.length === 0) && (
          <div className="gv-loading" style={{ opacity: .4 }}>
            <div style={{ fontSize: 32 }}>📡</div>
            <div style={{ fontSize: 13, color: '#64748b' }}>Noticias indisponiveis no momento</div>
          </div>
        )}
      </div>
    )
  }

  const nextLabel = nextSlide.type === 'painel' ? '🐾 Gestao' : `${feeds[nextSlide.feedIdx]?.icone ?? ''} ${feeds[nextSlide.feedIdx]?.nome ?? ''}`

  return (
    <div className="gv-root">
      <header className="gv-header">
        <div className="gv-logo-area">
          {dados?.clinica?.logoUrl
            ? <img src={dados.clinica.logoUrl} alt="logo" style={{ width: 'clamp(32px,4vw,48px)', height: 'clamp(32px,4vw,48px)', borderRadius: 12, objectFit: 'cover' }} />
            : <div className="gv-logo-icon">🐾</div>
          }
          <div>
            <div className="gv-clinic-name">{dados?.clinica?.nome || 'VetClinica'}</div>
            {dados?.clinica?.tagline && <div className="gv-clinic-sub">{dados.clinica.tagline}</div>}
          </div>
        </div>
        <div className="gv-center">
          <div className="gv-clock">{clock.hora}</div>
          <div className="gv-date">{clock.data}</div>
        </div>
        <div className="gv-header-right">
          <div className="gv-live"><div className="gv-live-dot"></div>Ao vivo</div>
          <div className="gv-stat-mini">Hoje: <span>{dados?.contadores?.totalHoje ?? '–'}</span> atendimentos</div>
        </div>
      </header>

      <div className="gv-progress-wrap">
        <div className="gv-progress-bar" style={{ width: `${progresso}%` }} />
      </div>

      <div className="gv-slide-label">
        {currentSlide.type === 'painel'
          ? <><div className="gv-slide-dot" style={{ background: '#38bdf8' }}></div><span className="gv-slide-name">Gestao a Vista</span><span>— painel de atendimentos</span></>
          : <><div className="gv-slide-dot" style={{ background: feeds[currentSlide.feedIdx]?.cor }}></div><span className="gv-slide-name">{feeds[currentSlide.feedIdx]?.nome}</span><span>— manchetes</span></>
        }
        <span style={{ marginLeft: 'auto', fontSize: 'clamp(9px,0.7vw,11px)' }}>proximo: {nextLabel}</span>
      </div>

      <div className="gv-main">
        {!dados && (
          <div className="gv-loading" style={{ height: '100%' }}>
            <div className="gv-spinner"></div>
            <div style={{ fontSize: 13, color: '#64748b' }}>Conectando...</div>
          </div>
        )}
        {dados && !transitioning && (
          currentSlide.type === 'painel' ? renderPainel() : renderNews(currentSlide.feedIdx)
        )}
      </div>
    </div>
  )
}
