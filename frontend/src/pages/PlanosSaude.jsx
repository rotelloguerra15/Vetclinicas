import { useState, useEffect, useCallback } from 'react';
import api from '../api/client';

// ─── helpers ────────────────────────────────────────────────────────────────
const fmtPct = (v) => `${Number(v).toFixed(0)}%`;
const fmtDate = (d) => (d ? new Date(d + 'T00:00:00').toLocaleDateString('pt-BR') : '—');
const isVencido = (validade) => {
  if (!validade) return false;
  return new Date(validade + 'T00:00:00') < new Date();
};

// ─── Badge ──────────────────────────────────────────────────────────────────
function Badge({ ativo, validade }) {
  if (!ativo) return <span className="px-2 py-0.5 rounded-full text-xs bg-slate-100 text-slate-500">Inativo</span>;
  if (isVencido(validade)) return <span className="px-2 py-0.5 rounded-full text-xs bg-red-100 text-red-700">Vencido</span>;
  return <span className="px-2 py-0.5 rounded-full text-xs bg-emerald-100 text-emerald-700">Ativo</span>;
}

// ─── Modal genérico ──────────────────────────────────────────────────────────
function Modal({ title, onClose, children }) {
  return (
    <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50 p-4">
      <div className="bg-white rounded-2xl shadow-xl w-full max-w-md">
        <div className="flex items-center justify-between px-6 py-4 border-b border-slate-100">
          <h3 className="font-semibold text-slate-800">{title}</h3>
          <button onClick={onClose} className="text-slate-400 hover:text-slate-600 text-xl leading-none">&times;</button>
        </div>
        <div className="p-6">{children}</div>
      </div>
    </div>
  );
}

// ─── Aba 1: Cadastro de Planos ───────────────────────────────────────────────
function AbaPlanos() {
  const [planos, setPlanos] = useState([]);
  const [loading, setLoading] = useState(true);
  const [modal, setModal] = useState(false);
  const [editando, setEditando] = useState(null);
  const [form, setForm] = useState({ nome: '', operadora: '', descontoPercent: 0, obs: '' });
  const [salvando, setSalvando] = useState(false);

  const carregar = useCallback(async () => {
    setLoading(true);
    try {
      const r = await api.get('/planos-saude/todos');
      setPlanos(r.data);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { carregar(); }, [carregar]);

  const abrirNovo = () => {
    setEditando(null);
    setForm({ nome: '', operadora: '', descontoPercent: 0, obs: '' });
    setModal(true);
  };

  const abrirEditar = (p) => {
    setEditando(p);
    setForm({ nome: p.nome, operadora: p.operadora || '', descontoPercent: p.descontoPercent, obs: p.obs || '' });
    setModal(true);
  };

  const salvar = async () => {
    if (!form.nome.trim()) return;
    setSalvando(true);
    try {
      const payload = { nome: form.nome.trim(), operadora: form.operadora || null, descontoPercent: Number(form.descontoPercent), obs: form.obs || null };
      if (editando) await api.put(`/planos-saude/${editando.id}`, payload);
      else await api.post('/planos-saude', payload);
      setModal(false);
      carregar();
    } finally {
      setSalvando(false);
    }
  };

  const desativar = async (id) => {
    if (!confirm('Desativar este plano?')) return;
    await api.delete(`/planos-saude/${id}`);
    carregar();
  };

  return (
    <div>
      <div className="flex justify-between items-center mb-4">
        <p className="text-sm text-slate-500">{planos.length} plano(s) cadastrado(s)</p>
        <button onClick={abrirNovo} className="px-4 py-2 bg-blue-600 text-white text-sm rounded-lg hover:bg-blue-700">
          + Novo Plano
        </button>
      </div>

      {loading ? (
        <p className="text-slate-500 text-sm">Carregando...</p>
      ) : planos.length === 0 ? (
        <div className="text-center py-16 text-slate-400">
          <div className="text-5xl mb-3">🏥</div>
          <p className="font-medium">Nenhum plano cadastrado</p>
          <p className="text-sm mt-1">Cadastre as operadoras que a clínica aceita</p>
        </div>
      ) : (
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="text-left text-slate-500 border-b border-slate-100">
                <th className="pb-2 font-medium">Plano / Operadora</th>
                <th className="pb-2 font-medium">Desconto padrão</th>
                <th className="pb-2 font-medium">Status</th>
                <th className="pb-2"></th>
              </tr>
            </thead>
            <tbody>
              {planos.map(p => (
                <tr key={p.id} className="border-b border-slate-50 hover:bg-slate-50">
                  <td className="py-3">
                    <div className="font-medium text-slate-800">{p.nome}</div>
                    {p.operadora && <div className="text-xs text-slate-400">{p.operadora}</div>}
                  </td>
                  <td className="py-3">
                    <span className={`font-semibold ${p.descontoPercent > 0 ? 'text-emerald-600' : 'text-slate-400'}`}>
                      {fmtPct(p.descontoPercent)}
                    </span>
                  </td>
                  <td className="py-3"><Badge ativo={p.ativo} /></td>
                  <td className="py-3 text-right space-x-2">
                    <button onClick={() => abrirEditar(p)} className="text-blue-600 hover:underline text-xs">Editar</button>
                    {p.ativo && (
                      <button onClick={() => desativar(p.id)} className="text-red-500 hover:underline text-xs">Desativar</button>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {modal && (
        <Modal title={editando ? 'Editar Plano' : 'Novo Plano'} onClose={() => setModal(false)}>
          <div className="space-y-4">
            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1">Nome do Plano *</label>
              <input className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                value={form.nome} onChange={e => setForm(f => ({ ...f, nome: e.target.value }))} placeholder="Ex: PetMed Básico" />
            </div>
            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1">Operadora</label>
              <input className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                value={form.operadora} onChange={e => setForm(f => ({ ...f, operadora: e.target.value }))} placeholder="Ex: Petlove Saúde" />
            </div>
            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1">Desconto padrão (%)</label>
              <input type="number" min="0" max="100" step="0.5"
                className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                value={form.descontoPercent} onChange={e => setForm(f => ({ ...f, descontoPercent: e.target.value }))} />
              <p className="text-xs text-slate-400 mt-1">Pode ser sobrescrito no vínculo individual com cada pet/tutor</p>
            </div>
            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1">Observações</label>
              <textarea rows={2} className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                value={form.obs} onChange={e => setForm(f => ({ ...f, obs: e.target.value }))} />
            </div>
            <div className="flex gap-3 pt-2">
              <button onClick={() => setModal(false)} className="flex-1 px-4 py-2 border border-slate-200 rounded-lg text-sm text-slate-700 hover:bg-slate-50">
                Cancelar
              </button>
              <button onClick={salvar} disabled={salvando || !form.nome.trim()}
                className="flex-1 px-4 py-2 bg-blue-600 text-white rounded-lg text-sm hover:bg-blue-700 disabled:opacity-50">
                {salvando ? 'Salvando...' : 'Salvar'}
              </button>
            </div>
          </div>
        </Modal>
      )}
    </div>
  );
}

// ─── Modal de vínculo (shared) ───────────────────────────────────────────────
function ModalVincular({ planos, onClose, onSalvar }) {
  const [form, setForm] = useState({ planoId: '', numCarteirinha: '', validade: '', descontoPercent: '' });
  const planoSel = planos.find(p => p.id === form.planoId);

  const salvar = () => {
    if (!form.planoId) return;
    onSalvar({
      planoId: form.planoId,
      numCarteirinha: form.numCarteirinha || null,
      validade: form.validade || null,
      descontoPercent: form.descontoPercent !== '' ? Number(form.descontoPercent) : null
    });
  };

  return (
    <Modal title="Vincular Plano" onClose={onClose}>
      <div className="space-y-4">
        <div>
          <label className="block text-sm font-medium text-slate-700 mb-1">Plano *</label>
          <select className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
            value={form.planoId} onChange={e => setForm(f => ({ ...f, planoId: e.target.value }))}>
            <option value="">Selecione...</option>
            {planos.map(p => (
              <option key={p.id} value={p.id}>{p.nome}{p.operadora ? ` — ${p.operadora}` : ''}</option>
            ))}
          </select>
        </div>
        {planoSel && (
          <div className="bg-emerald-50 rounded-lg px-3 py-2 text-sm text-emerald-700">
            Desconto padrao do plano: <strong>{fmtPct(planoSel.descontoPercent)}</strong>
          </div>
        )}
        <div>
          <label className="block text-sm font-medium text-slate-700 mb-1">Numero da Carteirinha</label>
          <input className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
            value={form.numCarteirinha} onChange={e => setForm(f => ({ ...f, numCarteirinha: e.target.value }))} />
        </div>
        <div>
          <label className="block text-sm font-medium text-slate-700 mb-1">Validade</label>
          <input type="date" className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
            value={form.validade} onChange={e => setForm(f => ({ ...f, validade: e.target.value }))} />
        </div>
        <div>
          <label className="block text-sm font-medium text-slate-700 mb-1">Desconto especifico (%) — opcional</label>
          <input type="number" min="0" max="100" step="0.5" placeholder="Deixe em branco para usar o do plano"
            className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
            value={form.descontoPercent} onChange={e => setForm(f => ({ ...f, descontoPercent: e.target.value }))} />
        </div>
        <div className="flex gap-3 pt-2">
          <button onClick={onClose} className="flex-1 px-4 py-2 border border-slate-200 rounded-lg text-sm text-slate-700 hover:bg-slate-50">
            Cancelar
          </button>
          <button onClick={salvar} disabled={!form.planoId}
            className="flex-1 px-4 py-2 bg-blue-600 text-white rounded-lg text-sm hover:bg-blue-700 disabled:opacity-50">
            Vincular
          </button>
        </div>
      </div>
    </Modal>
  );
}

// ─── Aba 2: Vínculos por Pet ─────────────────────────────────────────────────
function AbaVinculosPet({ planos }) {
  const [busca, setBusca] = useState('');
  const [pets, setPets] = useState([]);
  const [selPet, setSelPet] = useState(null);
  const [vinculos, setVinculos] = useState([]);
  const [modalVincular, setModalVincular] = useState(false);
  const [loadingPets, setLoadingPets] = useState(false);
  const [loadingV, setLoadingV] = useState(false);

  const buscarPets = useCallback(async () => {
    if (busca.length < 2) return;
    setLoadingPets(true);
    try {
      const r = await api.get('/pets', { params: { busca, pageSize: 10 } });
      setPets(r.data.items);
    } finally {
      setLoadingPets(false);
    }
  }, [busca]);

  useEffect(() => {
    const t = setTimeout(buscarPets, 350);
    return () => clearTimeout(t);
  }, [buscarPets]);

  const selecionarPet = async (pet) => {
    setSelPet(pet);
    setPets([]);
    setBusca('');
    setLoadingV(true);
    try {
      const r = await api.get(`/planos-saude/pet/${pet.id}`);
      setVinculos(r.data);
    } finally {
      setLoadingV(false);
    }
  };

  const desvincular = async (vinculoId) => {
    if (!confirm('Desvincular este plano?')) return;
    await api.delete(`/planos-saude/pet/${selPet.id}/desvincular/${vinculoId}`);
    const r = await api.get(`/planos-saude/pet/${selPet.id}`);
    setVinculos(r.data);
  };

  const vincular = async (payload) => {
    await api.post(`/planos-saude/pet/${selPet.id}/vincular`, payload);
    setModalVincular(false);
    const r = await api.get(`/planos-saude/pet/${selPet.id}`);
    setVinculos(r.data);
  };

  return (
    <div>
      <div className="mb-4">
        <label className="block text-sm font-medium text-slate-700 mb-1">Buscar Pet</label>
        <input className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
          placeholder="Digite o nome do pet..."
          value={busca}
          onChange={e => { setBusca(e.target.value); setSelPet(null); }} />
        {loadingPets && <p className="text-xs text-slate-400 mt-1">Buscando...</p>}
        {pets.length > 0 && (
          <div className="border border-slate-200 rounded-lg mt-1 shadow-sm bg-white max-h-48 overflow-y-auto">
            {pets.map(p => (
              <button key={p.id} onClick={() => selecionarPet(p)}
                className="w-full text-left px-4 py-2 hover:bg-blue-50 text-sm border-b border-slate-50 last:border-0">
                <span className="font-medium">{p.nome}</span>
                <span className="text-slate-400 ml-2 text-xs">{p.especie} — {p.tutorNome}</span>
              </button>
            ))}
          </div>
        )}
      </div>

      {selPet && (
        <div>
          <div className="flex items-center justify-between mb-3">
            <div>
              <h3 className="font-semibold text-slate-800">{selPet.nome}</h3>
              <p className="text-xs text-slate-400">{selPet.tutorNome} · {selPet.especie}</p>
            </div>
            <button onClick={() => setModalVincular(true)}
              className="px-3 py-1.5 bg-blue-600 text-white text-xs rounded-lg hover:bg-blue-700">
              + Vincular Plano
            </button>
          </div>

          {loadingV ? <p className="text-slate-400 text-sm">Carregando...</p> : vinculos.length === 0 ? (
            <p className="text-slate-400 text-sm text-center py-6">Nenhum plano vinculado a este pet.</p>
          ) : (
            <table className="w-full text-sm">
              <thead>
                <tr className="text-left text-slate-500 border-b border-slate-100">
                  <th className="pb-2 font-medium">Plano</th>
                  <th className="pb-2 font-medium">Carteirinha</th>
                  <th className="pb-2 font-medium">Validade</th>
                  <th className="pb-2 font-medium">Desconto</th>
                  <th className="pb-2 font-medium">Status</th>
                  <th className="pb-2"></th>
                </tr>
              </thead>
              <tbody>
                {vinculos.map(v => (
                  <tr key={v.id} className="border-b border-slate-50 hover:bg-slate-50">
                    <td className="py-2">
                      <div className="font-medium">{v.planoNome}</div>
                      {v.operadora && <div className="text-xs text-slate-400">{v.operadora}</div>}
                    </td>
                    <td className="py-2 text-slate-600">{v.numCarteirinha || '—'}</td>
                    <td className="py-2 text-slate-600">{fmtDate(v.validade)}</td>
                    <td className="py-2 font-semibold text-emerald-600">{fmtPct(v.descontoEfetivo)}</td>
                    <td className="py-2"><Badge ativo={v.ativo} validade={v.validade} /></td>
                    <td className="py-2 text-right">
                      {v.ativo && (
                        <button onClick={() => desvincular(v.id)} className="text-red-500 hover:underline text-xs">
                          Desvincular
                        </button>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      )}

      {modalVincular && (
        <ModalVincular planos={planos} onClose={() => setModalVincular(false)} onSalvar={vincular} />
      )}
    </div>
  );
}

// ─── Aba 3: Vínculos por Tutor ───────────────────────────────────────────────
function AbaVinculosTutor({ planos }) {
  const [busca, setBusca] = useState('');
  const [tutores, setTutores] = useState([]);
  const [selTutor, setSelTutor] = useState(null);
  const [vinculos, setVinculos] = useState([]);
  const [modalVincular, setModalVincular] = useState(false);
  const [loadingT, setLoadingT] = useState(false);
  const [loadingV, setLoadingV] = useState(false);

  const buscarTutores = useCallback(async () => {
    if (busca.length < 2) return;
    setLoadingT(true);
    try {
      const r = await api.get('/tutores', { params: { busca, pageSize: 10 } });
      setTutores(r.data.items || r.data);
    } finally {
      setLoadingT(false);
    }
  }, [busca]);

  useEffect(() => {
    const t = setTimeout(buscarTutores, 350);
    return () => clearTimeout(t);
  }, [buscarTutores]);

  const selecionarTutor = async (tutor) => {
    setSelTutor(tutor);
    setTutores([]);
    setBusca('');
    setLoadingV(true);
    try {
      const r = await api.get(`/planos-saude/tutor/${tutor.id}`);
      setVinculos(r.data);
    } finally {
      setLoadingV(false);
    }
  };

  const desvincular = async (vinculoId) => {
    if (!confirm('Desvincular este plano?')) return;
    await api.delete(`/planos-saude/tutor/${selTutor.id}/desvincular/${vinculoId}`);
    const r = await api.get(`/planos-saude/tutor/${selTutor.id}`);
    setVinculos(r.data);
  };

  const vincular = async (payload) => {
    await api.post(`/planos-saude/tutor/${selTutor.id}/vincular`, payload);
    setModalVincular(false);
    const r = await api.get(`/planos-saude/tutor/${selTutor.id}`);
    setVinculos(r.data);
  };

  return (
    <div>
      <div className="mb-4">
        <label className="block text-sm font-medium text-slate-700 mb-1">Buscar Tutor</label>
        <input className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
          placeholder="Digite o nome do tutor..."
          value={busca}
          onChange={e => { setBusca(e.target.value); setSelTutor(null); }} />
        {loadingT && <p className="text-xs text-slate-400 mt-1">Buscando...</p>}
        {tutores.length > 0 && (
          <div className="border border-slate-200 rounded-lg mt-1 shadow-sm bg-white max-h-48 overflow-y-auto">
            {tutores.map(t => (
              <button key={t.id} onClick={() => selecionarTutor(t)}
                className="w-full text-left px-4 py-2 hover:bg-blue-50 text-sm border-b border-slate-50 last:border-0">
                <span className="font-medium">{t.nome}</span>
                {t.telefone && <span className="text-slate-400 ml-2 text-xs">{t.telefone}</span>}
              </button>
            ))}
          </div>
        )}
      </div>

      {selTutor && (
        <div>
          <div className="flex items-center justify-between mb-3">
            <div>
              <h3 className="font-semibold text-slate-800">{selTutor.nome}</h3>
              <p className="text-xs text-slate-400">Plano cobre todos os pets do tutor</p>
            </div>
            <button onClick={() => setModalVincular(true)}
              className="px-3 py-1.5 bg-blue-600 text-white text-xs rounded-lg hover:bg-blue-700">
              + Vincular Plano
            </button>
          </div>

          {loadingV ? <p className="text-slate-400 text-sm">Carregando...</p> : vinculos.length === 0 ? (
            <p className="text-slate-400 text-sm text-center py-6">Nenhum plano vinculado a este tutor.</p>
          ) : (
            <table className="w-full text-sm">
              <thead>
                <tr className="text-left text-slate-500 border-b border-slate-100">
                  <th className="pb-2 font-medium">Plano</th>
                  <th className="pb-2 font-medium">Carteirinha</th>
                  <th className="pb-2 font-medium">Validade</th>
                  <th className="pb-2 font-medium">Desconto</th>
                  <th className="pb-2 font-medium">Status</th>
                  <th className="pb-2"></th>
                </tr>
              </thead>
              <tbody>
                {vinculos.map(v => (
                  <tr key={v.id} className="border-b border-slate-50 hover:bg-slate-50">
                    <td className="py-2">
                      <div className="font-medium">{v.planoNome}</div>
                      {v.operadora && <div className="text-xs text-slate-400">{v.operadora}</div>}
                    </td>
                    <td className="py-2 text-slate-600">{v.numCarteirinha || '—'}</td>
                    <td className="py-2 text-slate-600">{fmtDate(v.validade)}</td>
                    <td className="py-2 font-semibold text-emerald-600">{fmtPct(v.descontoEfetivo)}</td>
                    <td className="py-2"><Badge ativo={v.ativo} validade={v.validade} /></td>
                    <td className="py-2 text-right">
                      {v.ativo && (
                        <button onClick={() => desvincular(v.id)} className="text-red-500 hover:underline text-xs">
                          Desvincular
                        </button>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      )}

      {modalVincular && (
        <ModalVincular planos={planos} onClose={() => setModalVincular(false)} onSalvar={vincular} />
      )}
    </div>
  );
}

// ─── Página principal ────────────────────────────────────────────────────────
export default function PlanosSaude() {
  const [aba, setAba] = useState('planos');
  const [planos, setPlanos] = useState([]);

  useEffect(() => {
    api.get('/planos-saude').then(r => setPlanos(r.data)).catch(() => {});
  }, []);

  const abas = [
    { key: 'planos', label: 'Planos / Operadoras' },
    { key: 'pets', label: 'Vinculos por Pet' },
    { key: 'tutores', label: 'Vinculos por Tutor' },
  ];

  return (
    <div className="p-4 md:p-6 max-w-4xl mx-auto">
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-slate-800">Planos de Saude</h1>
        <p className="text-slate-500 text-sm mt-1">Gerencie operadoras, carteirinhas e descontos por plano</p>
      </div>

      {/* Abas */}
      <div className="flex gap-1 border-b border-slate-200 mb-6">
        {abas.map(a => (
          <button key={a.key} onClick={() => setAba(a.key)}
            className={`px-4 py-2 text-sm font-medium rounded-t-lg transition-colors ${
              aba === a.key
                ? 'bg-white border border-b-white border-slate-200 text-blue-600 -mb-px'
                : 'text-slate-500 hover:text-slate-700'
            }`}>
            {a.label}
          </button>
        ))}
      </div>

      {/* Conteúdo */}
      <div className="bg-white rounded-2xl border border-slate-100 shadow-sm p-6">
        {aba === 'planos' && <AbaPlanos />}
        {aba === 'pets' && <AbaVinculosPet planos={planos} />}
        {aba === 'tutores' && <AbaVinculosTutor planos={planos} />}
      </div>
    </div>
  );
}
