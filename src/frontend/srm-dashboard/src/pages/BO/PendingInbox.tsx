import React, { useState, useMemo, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useQuery, keepPreviousData } from '@tanstack/react-query';
import api from '../../api/axios';
import { DocumentResponse, PagedResult, FailedFileResponse } from '../../types/api';
import { 
  FileSearch, PlusCircle, RefreshCw, ChevronLeft, ChevronRight, 
  AlertTriangle, ArrowRight, FileText, ShieldAlert
} from 'lucide-react';

const PendingInbox: React.FC = () => {
  const navigate = useNavigate();
  const [page, setPage] = useState(1);
  const pageSize = 15;

  // 🌟 ÉTATS POUR LES ONGLETS
  const [activeTab, setActiveTab] = useState<'new' | 'rejected'>('new');
  const [hasAutoSwitched, setHasAutoSwitched] = useState(false);

  // 1. Surveillance des échecs OCR (Notification)
  const { data: failedDocs = [] } = useQuery({
    queryKey: ['failed-count'],
    queryFn: async () => (await api.get<FailedFileResponse[]>('/api/v1/document/failed')).data,
    refetchInterval: 10000, 
  });

  // 2. Documents à indexer
  const { data, isLoading, refetch, isFetching } = useQuery({
    queryKey: ['pending-documents', page],
    queryFn: async () => (await api.get<PagedResult<DocumentResponse>>(`/api/v1/document/pending-indexation?pageNumber=${page}&pageSize=${pageSize}`)).data,
    placeholderData: keepPreviousData,
    refetchInterval: 30000,
  });

  const documents = data?.items || [];
  const totalPages = data?.totalPages || 1;

  // 🌟 HEAVY LIFTING FRONTEND (Séparation intelligente en mémoire)
  const { newDocs, rejectedDocs } = useMemo(() => {
    // On cherche les statuts qui contiennent "REJECT" (ex: "REJECTED", "REJECTED_BY_MANAGER")
    const rejected = documents.filter(doc => (doc.status as string)?.toUpperCase().includes('REJECT'));
    const nouveaux = documents.filter(doc => !(doc.status as string)?.toUpperCase().includes('REJECT'));
    
    return { newDocs: nouveaux, rejectedDocs: rejected };
  }, [documents]);

  // 🌟 SMART DEFAULT : Si des documents sont rejetés, on force l'onglet "À Corriger" au premier chargement
  useEffect(() => {
    if (rejectedDocs.length > 0 && !hasAutoSwitched) {
      setActiveTab('rejected');
      setHasAutoSwitched(true);
    }
  }, [rejectedDocs.length, hasAutoSwitched]);

  // Sélection des documents à afficher en fonction de l'onglet actif
  const displayedDocs = activeTab === 'new' ? newDocs : rejectedDocs;

  return (
    <div className="space-y-6 animate-in fade-in duration-500 pb-20">
      {/* BANNIÈRE DE NOTIFICATION D'ÉCHEC OCR */}
      {failedDocs.length > 0 && (
        <div className="p-4 bg-rose-500/10 border-2 border-rose-500/30 rounded-2xl flex items-center justify-between animate-pulse shadow-lg shadow-rose-500/5">
          <div className="flex items-center gap-4 text-rose-400">
            <div className="p-2 bg-rose-500 rounded-lg text-white">
              <AlertTriangle className="w-5 h-5" />
            </div>
            <div>
              <h4 className="font-black text-sm uppercase tracking-tight">Alerte Système OCR</h4>
              <p className="text-xs opacity-80 font-medium">{failedDocs.length} document(s) ont échoué et attendent une récupération.</p>
            </div>
          </div>
          <button 
            onClick={() => navigate('/bo/failed')}
            className="flex items-center gap-2 px-4 py-2 bg-rose-500 text-white rounded-xl text-xs font-black uppercase hover:bg-rose-600 transition-all shadow-lg"
          >
            Récupérer <ArrowRight className="w-4 h-4" />
          </button>
        </div>
      )}

      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-black text-white tracking-tighter">CENTRE D'INDEXATION</h1>
          <p className="text-slate-400 text-sm font-medium">Flux post-OCR en attente de certification métier.</p>
        </div>
        <div className="flex items-center gap-3">
          <button onClick={() => refetch()} className="p-2 text-slate-500 hover:text-white"><RefreshCw className={`w-5 h-5 ${isFetching ? 'animate-spin text-blue-500' : ''}`} /></button>
          <button onClick={() => navigate('/bo/manual-upload')} className="flex items-center gap-2 px-6 py-3 bg-blue-600 hover:bg-blue-500 text-white rounded-xl font-bold text-sm shadow-xl shadow-blue-600/20">
            <PlusCircle className="w-4 h-4" /> Upload / Saisie
          </button>
        </div>
      </div>

      {/* 🌟 ONGLETS DYNAMIQUES */}
      <div className="flex gap-2 p-1 bg-slate-900 border border-slate-800 rounded-xl w-fit">
        <button 
          onClick={() => setActiveTab('new')} 
          className={`px-5 py-2.5 rounded-lg text-sm font-bold flex items-center gap-2 transition-all ${activeTab === 'new' ? 'bg-blue-600 text-white shadow-lg' : 'text-slate-400 hover:text-white'}`}
        >
          <FileText className="w-4 h-4" /> Nouveaux 
          <span className={`px-2 py-0.5 rounded-full text-[10px] ${activeTab === 'new' ? 'bg-white/20' : 'bg-slate-800'}`}>{newDocs.length}</span>
        </button>
        <button 
          onClick={() => setActiveTab('rejected')} 
          className={`px-5 py-2.5 rounded-lg text-sm font-bold flex items-center gap-2 transition-all ${activeTab === 'rejected' ? 'bg-rose-600 text-white shadow-lg' : 'text-slate-400 hover:text-white'}`}
        >
          <ShieldAlert className={`w-4 h-4 ${activeTab === 'rejected' ? '' : 'text-rose-500'}`} /> À Corriger
          <span className={`px-2 py-0.5 rounded-full text-[10px] ${activeTab === 'rejected' ? 'bg-white/20' : 'bg-rose-500/10 text-rose-500'}`}>{rejectedDocs.length}</span>
        </button>
      </div>

      {isLoading ? (
        <div className="space-y-3">{[1, 2, 3].map(i => <div key={i} className="w-full h-16 bg-slate-800/40 rounded-xl animate-pulse" />)}</div>
      ) : displayedDocs.length === 0 ? (
        <div className="p-20 text-center border-2 border-slate-800 border-dashed rounded-3xl bg-slate-900/20">
          {activeTab === 'new' ? (
            <>
              <FileSearch className="w-16 h-16 mx-auto mb-4 text-slate-700" />
              <h3 className="text-white font-bold text-lg">Aucun nouveau document</h3>
              <p className="text-slate-500 text-sm">La file d'attente OCR est actuellement vide.</p>
            </>
          ) : (
            <>
              <ShieldAlert className="w-16 h-16 mx-auto mb-4 text-emerald-500/20" />
              <h3 className="text-white font-bold text-lg">Zéro rejet métier !</h3>
              <p className="text-slate-500 text-sm">Aucun document n'a été retourné par les approbateurs.</p>
            </>
          )}
        </div>
      ) : (
        <div className="space-y-4">
          <div className="overflow-hidden rounded-2xl border border-slate-800 bg-slate-900/50 shadow-2xl">
            <table className="w-full text-left">
              <thead>
                <tr className="bg-slate-950/80 text-slate-500 text-[10px] uppercase font-black tracking-widest border-b border-slate-800">
                  <th className="p-5">Référence</th>
                  <th className="p-5">Arrivée</th>
                  <th className="p-5">Statut</th>
                  <th className="p-5 text-right">Action</th>
                </tr>
              </thead>
              <tbody className="text-sm text-slate-300">
                {displayedDocs.map((doc) => (
                  <tr 
                    key={doc.id} 
                    // 🌟 Ligne stylisée si le document est rejeté
                    className={`hover:bg-slate-800/40 border-b border-slate-800/50 transition-colors ${activeTab === 'rejected' ? 'bg-rose-500/5 border-l-2 border-l-rose-500' : ''}`}
                  >
                    <td className="p-5 font-mono text-blue-400 font-bold flex items-center gap-2">
                      {activeTab === 'rejected' && <ShieldAlert className="w-4 h-4 text-rose-500" />}
                      {doc.reference}
                    </td>
                    <td className="p-5 text-slate-500">{new Date(doc.createdAt).toLocaleString()}</td>
                    <td className="p-5">
                      <span className={`px-3 py-1 rounded-full border text-[10px] font-black uppercase ${
                        activeTab === 'rejected' 
                          ? 'bg-rose-500/10 text-rose-400 border-rose-500/20' 
                          : 'bg-blue-500/10 text-blue-400 border-blue-500/20'
                      }`}>
                        {doc.status as string}
                      </span>
                    </td>
                    <td className="p-5 text-right">
                      <button 
                        onClick={() => navigate(`/bo/indexation/${doc.id}`)} 
                        className={`px-4 py-2 text-white rounded-lg transition-all text-xs font-black uppercase ${
                          activeTab === 'rejected' ? 'bg-rose-600 hover:bg-rose-500' : 'bg-slate-800 hover:bg-white hover:text-black'
                        }`}
                      >
                        {activeTab === 'rejected' ? 'Corriger' : 'Indexer'}
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <div className="flex items-center justify-between p-2">
            <span className="text-[10px] text-slate-500 font-bold font-mono">PAGE GLOBALE {page} / {totalPages}</span>
            <div className="flex items-center gap-2">
              <button onClick={() => setPage(p => Math.max(1, p - 1))} disabled={page === 1} className="p-2 bg-slate-800 text-slate-300 rounded-lg hover:bg-slate-700 disabled:opacity-50"><ChevronLeft className="w-4 h-4" /></button>
              <button onClick={() => setPage(p => Math.min(totalPages, p + 1))} disabled={page >= totalPages} className="p-2 bg-slate-800 text-slate-300 rounded-lg hover:bg-slate-700 disabled:opacity-50"><ChevronRight className="w-4 h-4" /></button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default PendingInbox;