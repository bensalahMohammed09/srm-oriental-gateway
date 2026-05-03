import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import api from '../../api/axios';
import { DocumentDetailsResponse } from '../../types/api';
import { 
  Loader2, CheckCircle2, XCircle, AlertCircle, 
  Database, FileWarning, Fingerprint, Lock, History, MessageSquare
} from 'lucide-react';

// 🌟 FIX : On importe TON composant DocumentViewer
import DocumentViewer from '../../components/documents/DocumentViewer';

interface WorkflowHistory { stepName: string; action: string; userFullName: string; roleName: string; date: string; comment: string; }

const DocumentApproval: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  
  const [submitting, setSubmitting] = useState(false);
  const [errorMsg, setErrorMsg] = useState<string | null>(null);
  
  const [showSuccess, setShowSuccess] = useState(false);
  const [isReject, setIsReject] = useState(false);
  const [comment, setComment] = useState(''); 
  const [blobUrl, setBlobUrl] = useState<string | null>(null);
  const [blobLoading, setBlobLoading] = useState(false);

  const { data: doc, isLoading: isDocLoading } = useQuery({
    queryKey: ['doc-details', id],
    queryFn: async () => (await api.get<DocumentDetailsResponse>(`/api/v1/document/${id}/details`)).data,
    enabled: !!id,
  });

  const { data: history } = useQuery({
    queryKey: ['doc-history', id],
    queryFn: async () => (await api.get<WorkflowHistory[]>(`/api/v1/workflow/${id}/history`)).data,
    enabled: !!id,
  });

  useEffect(() => {
    if (!id || !doc || !doc.sourceFile) return;
    setBlobLoading(true);
    // On télécharge le flux binaire de manière sécurisée et on génère une URL locale
    api.get(`/api/v1/document/${id}/view`, { responseType: 'blob' })
      .then(res => setBlobUrl(URL.createObjectURL(res.data)))
      .catch(() => setBlobUrl(null))
      .finally(() => setBlobLoading(false));
    return () => { if (blobUrl) URL.revokeObjectURL(blobUrl); };
  }, [id, doc]);

  const handleApprove = async () => {
    setSubmitting(true);
    setErrorMsg(null);
    try {
      await api.post(`/api/v1/workflow/${id}/approve`, { comment: comment.trim() || null });
      setIsReject(false);
      setShowSuccess(true);
    } catch (err: any) {
      setErrorMsg(err.response?.data?.detail || "Erreur lors de l'approbation.");
      setSubmitting(false);
    }
  };

  const handleReject = async () => {
    if (!comment.trim()) {
      setErrorMsg("Un motif de rejet est obligatoire dans le champ commentaire.");
      return;
    }
    setSubmitting(true);
    setErrorMsg(null);
    try {
      await api.post(`/api/v1/workflow/${id}/reject`, { reason: comment.trim() });
      setIsReject(true);
      setShowSuccess(true);
    } catch (err: any) {
      setErrorMsg(err.response?.data?.detail || "Erreur lors du rejet.");
      setSubmitting(false);
    }
  };

  if (isDocLoading) return <div className="h-screen flex items-center justify-center text-emerald-500 font-black animate-pulse uppercase tracking-widest"><Loader2 className="w-10 h-10 animate-spin mb-4" /> Chargement...</div>;

  const isDataOnly = !doc?.sourceFile;
  const displaySupplier = doc?.supplierName || doc?.metadata?.SupplierName?.value || doc?.metadata?.supplierName?.value || 'N/A';
  
  const categoryName = (doc?.category || "").toUpperCase();
  let expectedRoles = ["ROLE_FINANCE"];
  if (categoryName.includes("INFORMATIQUE") || categoryName.includes("TELECOM") || categoryName.includes("TÉLÉCOM")) {
      expectedRoles = ["ROLE_TECH", "ROLE_FINANCE"];
  } else if (categoryName.includes("MAINTENANCE") || categoryName.includes("TRAVAUX")) {
      expectedRoles = ["ROLE_MAINTENANCE", "ROLE_DIRECTOR", "ROLE_FINANCE"];
  }

  const votes = history?.filter(h => h.action === 'APPROVED' || h.action === 'REJECTED') || [];
  const departmentStatuses = expectedRoles.map(role => {
    const vote = votes.find(v => v.roleName === role);
    return { role, status: vote ? vote.action : 'WAITING', comment: vote?.comment, date: vote?.date };
  });

  return (
    <>
      <div className="flex flex-col lg:flex-row gap-6 h-[calc(100vh-40px)] animate-in fade-in duration-500 pt-4">
        
        {/* --- PANNEAU DE GAUCHE : FORMULAIRE DE DÉCISION --- */}
        <div className="lg:w-[42%] overflow-y-auto pr-4 custom-scrollbar">
          
          {errorMsg && (
            <div className="p-4 bg-rose-500/10 border border-rose-500/20 rounded-xl text-rose-400 text-xs flex items-center gap-3 mb-6 animate-shake shadow-lg">
              <AlertCircle className="w-5 h-5 shrink-0" /> <p className="font-bold">{errorMsg}</p>
            </div>
          )}

          <div className="space-y-6 flex-1 pb-6">
            <div className="p-6 rounded-[2rem] border border-slate-800 bg-slate-900/50 space-y-5 shadow-xl">
              <div className="flex justify-between items-center border-b border-slate-800 pb-2">
                <h3 className="text-[10px] font-black text-slate-500 uppercase tracking-widest">Dossier Certifié</h3>
                <span className="flex items-center gap-1.5 text-[9px] font-black font-mono text-emerald-500 bg-emerald-500/10 px-2 py-1 rounded border border-emerald-500/20 uppercase"><Lock className="w-3 h-3" /> Lecture Seule</span>
              </div>
              <div className="space-y-1">
                <label className="text-[10px] font-black text-slate-500 uppercase">Référence</label>
                <div className="w-full bg-slate-950 border border-slate-800 rounded-xl p-3.5 text-blue-400 font-mono text-sm font-bold">{doc?.reference || 'N/A'}</div>
              </div>
              <div className="space-y-1">
                <label className="text-[10px] font-black text-slate-500 uppercase">Fournisseur / Tiers</label>
                <div className="w-full bg-slate-950 border border-slate-800 rounded-xl p-3.5 text-white text-sm font-bold">{displaySupplier}</div>
              </div>
              <div className="space-y-1">
                <label className="text-[10px] font-black text-slate-500 uppercase">Montant Identifié (DH)</label>
                <div className="w-full bg-slate-950 border border-slate-800 rounded-xl p-3.5 text-emerald-400 font-mono text-sm font-black">{doc?.totalAmount?.toFixed(2) || '0.00'}</div>
              </div>
            </div>

            <div className="p-6 rounded-[2rem] border border-slate-800 bg-slate-900/50 space-y-4 shadow-xl">
              <h3 className="text-[10px] font-black text-slate-500 uppercase tracking-widest border-b border-slate-800 pb-2 flex items-center justify-between">
                <span>Décisions du Workflow</span>
                <History className="w-3 h-3 text-slate-600" />
              </h3>
              <div className="space-y-3">
                {departmentStatuses.map((dept, idx) => (
                  <div key={idx} className={`p-4 rounded-xl border transition-colors ${
                    dept.status === 'APPROVED' ? 'bg-emerald-500/5 border-emerald-500/10' :
                    dept.status === 'REJECTED' ? 'bg-rose-500/5 border-rose-500/20' :
                    'bg-slate-800/20 border-slate-800/50'
                  }`}>
                    <div className="flex items-center justify-between mb-1.5">
                      <span className="text-[10px] font-black uppercase text-slate-400">Département {dept.role.replace('ROLE_', '')}</span>
                      <span className={`text-[10px] font-black uppercase ${
                        dept.status === 'APPROVED' ? 'text-emerald-500' :
                        dept.status === 'REJECTED' ? 'text-rose-500' :
                        'text-slate-500'
                      }`}>
                        {dept.status === 'APPROVED' ? '✅ Approuvé' :
                         dept.status === 'REJECTED' ? '❌ Rejeté' :
                         '⏳ En attente...'}
                      </span>
                    </div>
                    {dept.comment && <p className="text-xs text-slate-300 font-medium bg-slate-950/50 p-2.5 rounded-lg border border-slate-800 mt-2 italic">"{dept.comment}"</p>}
                  </div>
                ))}
              </div>
            </div>

            <div className="space-y-2 pt-2">
              <label className="text-[10px] font-black text-slate-500 uppercase flex items-center gap-2">
                <MessageSquare className="w-3 h-3" /> Votre Commentaire
              </label>
              <textarea 
                value={comment}
                onChange={(e) => setComment(e.target.value)}
                placeholder="Optionnel pour approuver, obligatoire pour rejeter..."
                className="w-full h-24 bg-slate-950 border border-slate-800 rounded-xl p-4 text-white text-sm focus:border-blue-500 outline-none resize-none custom-scrollbar transition-all"
              />
            </div>

            <div className="grid grid-cols-2 gap-4 pt-2">
              <button 
                onClick={handleReject}
                disabled={submitting || !doc}
                className="w-full bg-rose-500/10 hover:bg-rose-600 text-rose-500 hover:text-white border border-rose-500/20 py-4 rounded-2xl font-black uppercase text-xs flex items-center justify-center gap-2 transition-all active:scale-95 disabled:opacity-50"
              >
                <XCircle className="w-4 h-4" /> Rejeter
              </button>
              <button 
                onClick={handleApprove}
                disabled={submitting || !doc}
                className="w-full bg-emerald-600 hover:bg-emerald-500 text-white py-4 rounded-2xl font-black uppercase text-xs flex items-center justify-center gap-2 transition-all shadow-lg shadow-emerald-600/20 active:scale-95 disabled:opacity-50"
              >
                {submitting ? <Loader2 className="animate-spin w-4 h-4" /> : <CheckCircle2 className="w-4 h-4" />} Approuver
              </button>
            </div>
          </div>
        </div>

        {/* --- PANNEAU DE DROITE : VISUALISATION DU DOCUMENT --- */}
        <div className="lg:w-[58%] flex flex-col h-full overflow-hidden shadow-2xl relative rounded-[2.5rem]">
          {isDataOnly ? (
            <div className="absolute inset-0 flex flex-col items-center justify-center text-center p-8 bg-[radial-gradient(ellipse_at_center,_var(--tw-gradient-stops))] from-slate-900 via-[#0a0a0c] to-[#0a0a0c] border border-slate-800 rounded-[2.5rem]">
              <div className="w-24 h-24 bg-emerald-500/5 rounded-full flex items-center justify-center mb-6 border border-emerald-500/10 shadow-[0_0_50px_rgba(16,185,129,0.1)]">
                <Database className="w-10 h-10 text-emerald-500/50" />
              </div>
              <h2 className="text-2xl font-black text-white tracking-tighter uppercase mb-2">Fiche 100% Numérique</h2>
              <p className="text-slate-500 text-sm max-w-md mx-auto leading-relaxed mb-8">Cette fiche a été générée via une saisie manuelle directe. Aucun document physique n'y est rattaché.</p>
              <div className="flex items-center gap-2 text-[10px] font-mono text-slate-600 bg-slate-900 px-4 py-2 rounded-lg border border-slate-800">
                <Fingerprint className="w-3 h-3" /> SECURE_DATA_ENTRY // {doc?.createdAt ? new Date(doc.createdAt).toISOString() : 'N/A'}
              </div>
            </div>
          ) : blobLoading ? (
            <div className="absolute inset-0 flex flex-col items-center justify-center text-slate-700 bg-[#0a0a0c] border border-slate-800 rounded-[2.5rem]">
              <Loader2 className="w-10 h-10 animate-spin mb-4 opacity-20" />
              <p className="text-[10px] font-black uppercase tracking-widest opacity-50">Récupération sécurisée...</p>
            </div>
          ) : !blobUrl ? (
            <div className="absolute inset-0 flex flex-col items-center justify-center text-slate-600 bg-[#0a0a0c] border border-slate-800 rounded-[2.5rem]">
              <FileWarning className="w-12 h-12 mb-2 opacity-20" />
              <p className="text-[10px] font-black uppercase opacity-50">Flux binaire inaccessible</p>
            </div>
          ) : (
            /* 🌟 L'INTÉGRATION MAGIQUE DE TON COMPOSANT EST LÀ */
            <DocumentViewer 
              streamUrl={blobUrl} 
              title={doc?.sourceFile || 'Aperçu du document'} 
              className="w-full h-full border-slate-800 !rounded-[2.5rem]" 
            />
          )}
        </div>
      </div>

      {showSuccess && (
        <div className="fixed inset-0 bg-slate-950/80 backdrop-blur-md flex items-center justify-center z-50 animate-in fade-in duration-200">
          <div className="bg-slate-900 border border-slate-800 p-8 rounded-[2.5rem] max-w-sm w-full text-center shadow-2xl shadow-emerald-900/20 animate-in zoom-in-95 duration-300">
            <div className={`w-20 h-20 rounded-full flex items-center justify-center mx-auto mb-6 shadow-inner border ${isReject ? 'bg-rose-500/10 text-rose-500 border-rose-500/20' : 'bg-emerald-500/10 text-emerald-500 border-emerald-500/20'}`}>
              {isReject ? <XCircle className="w-10 h-10" /> : <CheckCircle2 className="w-10 h-10" />}
            </div>
            <h2 className="text-2xl font-black text-white mb-2">{isReject ? 'Dossier Rejeté' : 'Vote Enregistré'}</h2>
            <p className="text-slate-400 text-sm mb-8 leading-relaxed">
              {isReject 
                ? "L'information a été transmise aux autres services et au Back-Office pour correction." 
                : "Votre décision a été ajoutée au consensus du workflow avec succès."}
            </p>
            <button 
              onClick={() => navigate('/approvals/inbox')} 
              className={`w-full text-white font-black uppercase tracking-wider text-xs py-4 rounded-xl transition-all shadow-lg active:scale-95 ${isReject ? 'bg-rose-600 hover:bg-rose-500 shadow-rose-600/20' : 'bg-emerald-600 hover:bg-emerald-500 shadow-emerald-600/20'}`}
            >
              Retour à la bannette
            </button>
          </div>
        </div>
      )}
    </>
  );
};

export default DocumentApproval;