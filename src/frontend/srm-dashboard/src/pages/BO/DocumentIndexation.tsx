import React, { useState, useEffect, useRef } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import api from '../../api/axios'; 
import { 
  Loader2, Eye, Plus, Trash2, CheckCircle2, FileWarning, 
  RefreshCcw, ShieldAlert, AlertCircle, ZoomIn, ZoomOut, Maximize, Move, Database
} from 'lucide-react';

interface MetaRow { id: string; key: string; value: string; confidence: number; }
interface WorkflowHistory { stepName: string; action: string; userFullName: string; roleName: string; date: string; comment: string; }

const DocumentIndexation: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  
  const [submitting, setSubmitting] = useState(false);
  const [errorMsg, setErrorMsg] = useState<string | null>(null);
  const [conflictError, setConflictError] = useState(false);
  const [showSuccess, setShowSuccess] = useState(false);

  const [coreData, setCoreData] = useState({ reference: '', supplierName: '', totalAmount: 0, categoryId: '' });
  const [metaList, setMetaList] = useState<MetaRow[]>([]);
  const [rowVersion, setRowVersion] = useState<string>('');
  
  const [blobUrl, setBlobUrl] = useState<string | null>(null);
  const [blobLoading, setBlobLoading] = useState(true);
  const [zoom, setZoom] = useState(1);
  const [offset, setOffset] = useState({ x: 0, y: 0 });
  const [isDragging, setIsDragging] = useState(false);
  const dragStart = useRef({ x: 0, y: 0 });

  const { data: categories = [] } = useQuery({
    queryKey: ['categories'],
    queryFn: async () => (await api.get<any[]>('/api/v1/category')).data,
    staleTime: 5 * 60 * 1000
  });

  const { data: doc, isLoading, refetch } = useQuery({
    queryKey: ['doc-details', id],
    queryFn: async () => (await api.get<any>(`/api/v1/document/${id}/details`)).data,
    enabled: !!id,
  });

  // 🌟 NOUVEAU : On récupère l'historique pour lire les rejets
  const { data: history } = useQuery({
    queryKey: ['doc-history', id],
    queryFn: async () => (await api.get<WorkflowHistory[]>(`/api/v1/workflow/${id}/history`)).data,
    enabled: !!id,
  });

  useEffect(() => {
    if (doc) {
      setCoreData({ 
        reference: doc.reference || '', 
        supplierName: doc.supplierName || '', 
        totalAmount: doc.totalAmount || 0, 
        categoryId: doc.categoryId || '' 
      });
      
      const token = doc.rowVersion || doc.RowVersion;
      setRowVersion(token && token.length > 0 ? token : "REVGQVVMVA==");
      
      setMetaList(Object.entries(doc.metadata || {}).map(([k, v]: any) => ({ 
        id: Math.random().toString(36).substr(2, 9), 
        key: k, 
        value: v.value?.toString() || '', 
        confidence: v.confidence 
      })));
    }
  }, [doc]);

  useEffect(() => {
    if (!id || !doc || !doc.sourceFile) {
      setBlobLoading(false);
      return;
    }
    setBlobLoading(true);
    api.get(`/api/v1/document/${id}/file`, { responseType: 'blob' })
      .then(res => setBlobUrl(URL.createObjectURL(res.data)))
      .catch(() => setBlobUrl(null))
      .finally(() => setBlobLoading(false));
      
    return () => { if (blobUrl) URL.revokeObjectURL(blobUrl); };
  }, [id, doc]);

  const handleMouseDown = (e: React.MouseEvent) => {
    setIsDragging(true);
    dragStart.current = { x: e.clientX - offset.x, y: e.clientY - offset.y };
  };
  const handleMouseMove = (e: React.MouseEvent) => {
    if (!isDragging) return;
    setOffset({ x: e.clientX - dragStart.current.x, y: e.clientY - dragStart.current.y });
  };
  const handleMouseUp = () => setIsDragging(false);

  const updateRow = (rowId: string, field: 'key'|'value', val: string) => {
    setMetaList(prev => prev.map(r => r.id === rowId ? { ...r, [field]: val, confidence: 1.0 } : r));
  };

  const onCertify = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!coreData.categoryId) return setErrorMsg("Veuillez sélectionner une catégorie.");
    if (!rowVersion) return setErrorMsg("Le jeton de sécurité est manquant. Rafraîchissez la page.");
    
    setSubmitting(true);
    setErrorMsg(null);

    const metadataPayload: Record<string, any> = {};
    metaList.forEach(m => { 
      if(m.key.trim()) metadataPayload[m.key.trim()] = { value: m.value, confidence: m.confidence }; 
    });

    try {
      await api.put(`/api/v1/document/${id}/confirm-indexation`, { 
        categoryId: coreData.categoryId,
        supplierName: coreData.supplierName,
        reference: coreData.reference,
        totalAmount: coreData.totalAmount,
        newMetadata: metadataPayload,
        rowVersion: rowVersion 
      });
      setShowSuccess(true);
    } catch (err: any) {
      if (err.response?.status === 409) {
        setConflictError(true);
      } else {
        const errorDetail = err.response?.data?.errors 
          ? Object.values(err.response.data.errors).flat().join(', ')
          : (err.message || "Erreur de validation (400)");
        setErrorMsg(errorDetail);
      }
    } finally { 
      setSubmitting(false); 
    }
  };

  if (isLoading) return <div className="h-screen flex items-center justify-center text-blue-500 font-black animate-pulse uppercase tracking-widest"><Loader2 className="w-10 h-10 animate-spin mb-4" /> Chargement...</div>;

  const isDataOnly = !doc?.sourceFile;
  const isPdf = doc?.sourceFile?.toLowerCase().endsWith('.pdf');
  const rejections = history?.filter(h => h.action === 'REJECTED') || [];

  return (
    <>
      {/* 🌟 FIX : Hauteur 100vh sans la bannette */}
      <div className="flex flex-col lg:flex-row gap-6 h-[calc(100vh-40px)] animate-in fade-in duration-500 select-none pt-4">
        
        <div className="lg:w-[42%] overflow-y-auto pr-4 custom-scrollbar select-text">
          <form onSubmit={onCertify} className="space-y-6 pb-20">
            
            {/* ALERTES (Erreurs & Conflits) EN HAUT */}
            {errorMsg && (
              <div className="p-4 bg-rose-500/10 border border-rose-500/20 rounded-xl text-rose-400 text-xs flex items-center gap-3 animate-shake shadow-lg">
                <AlertCircle className="w-5 h-5 shrink-0" />
                <p className="font-bold">{errorMsg}</p>
              </div>
            )}

            {conflictError && (
              <div className="p-4 bg-rose-500/10 border-2 border-rose-500 rounded-2xl flex items-center justify-between shadow-lg">
                <ShieldAlert className="text-rose-500 w-6 h-6" />
                <p className="text-[10px] font-black uppercase text-rose-500">Document modifié par ailleurs</p>
                <button type="button" onClick={() => { setConflictError(false); refetch(); }} className="p-2 bg-rose-500 text-white rounded-lg hover:bg-rose-600 transition-colors"><RefreshCcw className="w-4 h-4"/></button>
              </div>
            )}

            {/* 🌟 NOUVEAU : ALERTE DE REJET MÉTIER */}
            {rejections.length > 0 && (
              <div className="p-5 bg-rose-500/10 border-2 border-rose-500/30 rounded-2xl space-y-3 shadow-xl">
                <div className="flex items-center gap-3 border-b border-rose-500/20 pb-3">
                  <div className="p-2 bg-rose-500/20 rounded-lg"><ShieldAlert className="w-5 h-5 text-rose-500" /></div>
                  <div>
                    <h3 className="font-black text-rose-400 uppercase text-sm tracking-widest">Dossier Rejeté</h3>
                    <p className="text-[10px] text-rose-500/70 font-bold uppercase">Corrections requises suite au workflow</p>
                  </div>
                </div>
                {rejections.map((rej, idx) => (
                  <div key={idx} className="bg-slate-950/50 p-4 rounded-xl border border-rose-500/20">
                    <div className="flex justify-between items-center mb-2">
                      <span className="text-[10px] font-black text-slate-300 uppercase bg-slate-800 px-2 py-1 rounded-md">Dépt: {rej.roleName?.replace('ROLE_', '')}</span>
                      <span className="text-[10px] text-slate-500 font-mono">{new Date(rej.date).toLocaleString()}</span>
                    </div>
                    <p className="text-xs text-rose-300 font-medium italic">"{rej.comment || "Aucun motif spécifique fourni."}"</p>
                  </div>
                ))}
              </div>
            )}

            <div className="p-6 bg-slate-900/50 rounded-[2rem] border border-slate-800 space-y-4 shadow-xl">
              <h3 className="text-[10px] font-black text-slate-500 uppercase tracking-widest border-b border-slate-800 pb-2">Routage Métier</h3>
              <div className="space-y-1">
                <label className="text-[10px] font-black text-slate-500 uppercase">Destination</label>
                <select required value={coreData.categoryId} onChange={e => setCoreData({...coreData, categoryId: e.target.value})} className="w-full bg-slate-950 border border-slate-800 rounded-xl p-3 text-white text-sm font-bold focus:border-blue-500 outline-none transition-colors">
                  <option value="">Sélectionner...</option>
                  {categories.map((c:any) => <option key={c.id} value={c.id}>{c.name}</option>)}
                </select>
              </div>
              <div className="space-y-1">
                <label className="text-[10px] font-black text-slate-500 uppercase">Tiers / Fournisseur</label>
                <input required value={coreData.supplierName} onChange={e => setCoreData({...coreData, supplierName: e.target.value})} className="w-full bg-slate-950 border border-slate-800 rounded-xl p-3 text-white text-sm focus:border-blue-500 outline-none transition-colors" />
              </div>
              <div className="grid grid-cols-2 gap-4">
                <div className="space-y-1">
                  <label className="text-[10px] font-black text-slate-500 uppercase font-mono">Référence</label>
                  <input required value={coreData.reference} onChange={e => setCoreData({...coreData, reference: e.target.value})} className="w-full bg-slate-950 border border-slate-800 rounded-xl p-3 text-white text-sm font-mono focus:border-blue-500 outline-none transition-colors" />
                </div>
                <div className="space-y-1">
                  <label className="text-[10px] font-black text-slate-500 uppercase font-mono">Montant (DH)</label>
                  <input required type="number" step="0.01" value={coreData.totalAmount} onChange={e => setCoreData({...coreData, totalAmount: parseFloat(e.target.value)})} className="w-full bg-slate-950 border border-slate-800 rounded-xl p-3 text-white text-sm font-mono focus:border-blue-500 outline-none transition-colors" />
                </div>
              </div>
            </div>

            <div className="p-6 bg-slate-900/50 rounded-[2rem] border border-slate-800 space-y-4 shadow-xl">
              <div className="flex justify-between items-center border-b border-slate-800 pb-2">
                <h3 className="text-[10px] font-black text-slate-500 uppercase tracking-widest">Données Associées</h3>
                <button type="button" onClick={() => setMetaList([...metaList, { id: Math.random().toString(), key: '', value: '', confidence: 1.0 }])} className="p-1.5 bg-blue-600/20 text-blue-500 rounded-lg hover:bg-blue-600 hover:text-white transition-colors"><Plus className="w-4 h-4" /></button>
              </div>
              <div className="space-y-3">
                {metaList.map(row => (
                  <div key={row.id} className="flex gap-2 group animate-in zoom-in-95 duration-200">
                    <input value={row.key} onChange={e => updateRow(row.id, 'key', e.target.value)} className="w-1/3 bg-slate-950 border border-slate-800 rounded-lg p-2.5 text-white text-[10px] font-black uppercase outline-none focus:border-blue-500 transition-colors" placeholder="CLÉ" />
                    <input value={row.value} onChange={e => updateRow(row.id, 'value', e.target.value)} placeholder="Valeur" className={`flex-1 border-2 rounded-lg p-2.5 text-xs font-bold outline-none transition-all ${row.confidence >= 0.85 ? 'border-emerald-500/30 text-emerald-400 bg-emerald-500/5' : row.confidence >= 0.5 ? 'border-amber-500/30 text-amber-400 bg-amber-500/5' : 'border-rose-500/30 text-rose-400 bg-rose-500/5 focus:border-blue-500'}`} />
                    <button type="button" onClick={() => setMetaList(metaList.filter(r => r.id !== row.id))} className="p-2 text-slate-600 hover:text-rose-500 transition-colors bg-slate-950 rounded-lg border border-slate-800"><Trash2 className="w-4 h-4"/></button>
                  </div>
                ))}
              </div>
            </div>

            <button type="submit" disabled={submitting || conflictError} className="w-full bg-emerald-600 hover:bg-emerald-500 disabled:opacity-50 disabled:hover:bg-emerald-600 text-white py-5 rounded-3xl font-black uppercase text-sm flex items-center justify-center gap-3 shadow-2xl shadow-emerald-600/20 active:scale-95 transition-all">
              {submitting ? <Loader2 className="animate-spin" /> : <CheckCircle2 className="w-5 h-5" />} {rejections.length > 0 ? "Valider la correction" : "Certifier et Transmettre"}
            </button>
          </form>
        </div>

        <div className="flex-1 bg-[#0a0a0c] rounded-[2.5rem] border border-slate-800 overflow-hidden relative shadow-2xl flex flex-col">
          <div className="h-14 bg-slate-900 border-b border-slate-800 flex items-center justify-between px-6 shrink-0 z-20">
            <div className="flex items-center gap-3 text-slate-400">
              {isDataOnly ? <Database className="w-4 h-4 text-emerald-500" /> : <Move className={`w-4 h-4 ${isDragging ? 'text-blue-500 animate-pulse' : 'text-slate-600'}`} />}
              <span className="text-[10px] font-black uppercase tracking-widest font-mono truncate max-w-[200px]">{isDataOnly ? 'FICHE NUMÉRIQUE' : (doc?.sourceFile || 'Streaming...')}</span>
            </div>
            {!isPdf && !isDataOnly && (
              <div className="flex items-center gap-1 bg-slate-950 p-1 rounded-xl border border-slate-800">
                <button onClick={() => setZoom(z => Math.max(0.1, z - 0.2))} className="p-2 hover:bg-slate-800 rounded-lg text-slate-400 transition-colors"><ZoomOut className="w-4 h-4"/></button>
                <span className="text-[10px] font-mono w-12 text-center text-blue-500 font-bold">{(zoom * 100).toFixed(0)}%</span>
                <button onClick={() => setZoom(z => Math.min(5, z + 0.2))} className="p-2 hover:bg-slate-800 rounded-lg text-slate-400 transition-colors"><ZoomIn className="w-4 h-4"/></button>
                <button onClick={() => { setZoom(1); setOffset({x:0, y:0}); }} className="p-2 hover:bg-slate-800 rounded-lg text-slate-400 transition-colors ml-1 border-l border-slate-800"><Maximize className="w-4 h-4"/></button>
              </div>
            )}
          </div>

          <div 
            className={`flex-1 relative overflow-hidden ${isDragging && !isDataOnly ? 'cursor-grabbing' : isDataOnly ? '' : 'cursor-grab'}`}
            onMouseDown={handleMouseDown}
            onMouseMove={handleMouseMove}
            onMouseUp={handleMouseUp}
            onMouseLeave={handleMouseUp}
          >
            {/* 🌟 FIX : Affichage de l'écran "Saisie Manuelle" si aucun fichier physique */}
            {isDataOnly ? (
              <div className="absolute inset-0 flex flex-col items-center justify-center text-center p-8 bg-[radial-gradient(ellipse_at_center,_var(--tw-gradient-stops))] from-slate-900 via-[#0a0a0c] to-[#0a0a0c]">
                <div className="w-24 h-24 bg-emerald-500/5 rounded-full flex items-center justify-center mb-6 border border-emerald-500/10 shadow-[0_0_50px_rgba(16,185,129,0.1)]">
                  <Database className="w-10 h-10 text-emerald-500/50" />
                </div>
                <h2 className="text-2xl font-black text-white tracking-tighter uppercase mb-2">Fiche 100% Numérique</h2>
                <p className="text-slate-500 text-sm max-w-md mx-auto leading-relaxed mb-8">Cette fiche a été générée via une saisie manuelle directe. Aucun document physique n'y est rattaché.</p>
              </div>
            ) : blobLoading ? (
               <div className="absolute inset-0 flex items-center justify-center"><Loader2 className="w-10 h-10 animate-spin text-blue-600 opacity-20" /></div>
            ) : blobUrl ? (
              isPdf ? (
                <iframe src={blobUrl} className="w-full h-full border-0 rounded-2xl" title="Source PDF" />
              ) : (
                <div 
                  className="absolute inset-0 flex items-center justify-center transition-transform duration-75 ease-out pointer-events-none"
                  style={{ transform: `translate(${offset.x}px, ${offset.y}px) scale(${zoom})` }}
                >
                  <img src={blobUrl} alt="Source" className="max-w-none shadow-2xl rounded-sm" onDragStart={(e) => e.preventDefault()} />
                </div>
              )
            ) : (
              <div className="h-full flex flex-col items-center justify-center text-slate-800"><FileWarning className="w-16 h-16 mb-2 opacity-10" /><p className="text-[10px] font-black uppercase opacity-20">Flux inaccessible</p></div>
            )}
          </div>
        </div>
      </div>

      {showSuccess && (
        <div className="fixed inset-0 bg-slate-950/80 backdrop-blur-md flex items-center justify-center z-50 animate-in fade-in duration-200">
          <div className="bg-slate-900 border border-slate-800 p-8 rounded-[2.5rem] max-w-sm w-full text-center shadow-2xl shadow-emerald-900/20 animate-in zoom-in-95 duration-300">
            <div className="w-20 h-20 bg-emerald-500/10 text-emerald-500 rounded-full flex items-center justify-center mx-auto mb-6 shadow-inner border border-emerald-500/20">
              <CheckCircle2 className="w-10 h-10" />
            </div>
            <h2 className="text-2xl font-black text-white mb-2">Transmis !</h2>
            <p className="text-slate-400 text-sm mb-8 leading-relaxed">
              Les données ont été corrigées et le dossier a été renvoyé au circuit de validation.
            </p>
            <button 
              onClick={() => navigate('/bo/pending')} 
              className="w-full bg-blue-600 hover:bg-blue-500 text-white font-black uppercase tracking-wider text-xs py-4 rounded-xl transition-all shadow-lg shadow-blue-600/20 active:scale-95"
            >
              Retour à la bannette
            </button>
          </div>
        </div>
      )}
    </>
  );
};

export default DocumentIndexation;