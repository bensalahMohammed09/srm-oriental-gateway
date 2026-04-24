import React from 'react';
import { useNavigate } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import api from '../../api/axios';
import { DocumentResponse } from '../../types/api';
import { FileSearch, AlertCircle, Edit3, PlusCircle, RefreshCw } from 'lucide-react';

const PendingInbox: React.FC = () => {
  const navigate = useNavigate();

  // 🔥 SRE Standard: Utilisation de TanStack Query avec Polling toutes les 15s
  const { data: documents = [], isLoading, isError, refetch, isFetching } = useQuery({
    queryKey: ['pending-documents'],
    queryFn: async () => {
      const response = await api.get<DocumentResponse[]>('/api/v1/document/pending');
      return response.data;
    },
    refetchInterval: 15000, // Auto-Polling agressif pour le BO (15 sec)
  });

  return (
    <div className="space-y-6 animate-in fade-in slide-in-from-bottom-4 duration-500">
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-white tracking-tight flex items-center gap-3">
            Centre d'Indexation
            {isFetching && !isLoading && (
              <span className="flex h-2 w-2 relative">
                <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-blue-400 opacity-75"></span>
                <span className="relative inline-flex rounded-full h-2 w-2 bg-blue-500"></span>
              </span>
            )}
          </h1>
          <p className="text-slate-400 text-sm">Gestion des documents Post-OCR en attente de validation.</p>
        </div>
        
        <div className="flex items-center gap-3">
          <button 
            onClick={() => refetch()}
            className="p-2 text-slate-400 hover:text-white transition-colors"
            title="Forcer l'actualisation"
          >
            <RefreshCw className={`w-5 h-5 ${isFetching ? 'animate-spin text-blue-500' : ''}`} />
          </button>
          <button 
            onClick={() => navigate('/bo/manual-upload')}
            className="flex items-center gap-2 px-4 py-2 bg-blue-600 hover:bg-blue-500 text-white rounded-lg transition-all font-medium text-sm shadow-lg shadow-blue-600/20"
          >
            <PlusCircle className="w-4 h-4" />
            Upload / Saisie Manuelle
          </button>
        </div>
      </div>

      {/* 🛡️ SRE Standard: Skeleton State au lieu d'un spinner */}
      {isLoading ? (
        <div className="space-y-3">
          {[1, 2, 3, 4].map((i) => (
            <div key={i} className="w-full h-16 bg-slate-800/50 animate-pulse rounded-xl border border-slate-800/30"></div>
          ))}
        </div>
      ) : isError ? (
        <div className="p-4 bg-rose-500/10 border border-rose-500/20 rounded-lg text-rose-400 text-sm flex items-center gap-3">
          <AlertCircle className="w-5 h-5" />
          Échec de la connexion au flux d'ingestion. Le backend est peut-être hors ligne.
        </div>
      ) : documents.length === 0 ? (
        <div className="p-12 text-center border border-slate-800 border-dashed rounded-xl bg-slate-900/20">
          <FileSearch className="w-16 h-16 mx-auto mb-4 text-slate-700" />
          <h3 className="text-white font-semibold">Boîte de réception vide</h3>
          <p className="text-slate-500 text-sm mt-1">Aucun document en attente d'indexation pour le moment.</p>
        </div>
      ) : (
        <div className="overflow-hidden rounded-xl border border-slate-800 bg-slate-900/50 shadow-2xl">
          <table className="w-full text-left border-collapse">
            <thead>
              <tr className="bg-slate-900/80 text-slate-500 text-[10px] uppercase font-mono tracking-widest">
                <th className="p-4 border-b border-slate-800">Référence</th>
                <th className="p-4 border-b border-slate-800">Date d'Ingestion</th>
                <th className="p-4 border-b border-slate-800">Statut</th>
                <th className="p-4 border-b border-slate-800 text-right">Action</th>
              </tr>
            </thead>
            <tbody className="text-sm text-slate-300">
              {documents.map((doc) => (
                <tr key={doc.id} className="group hover:bg-slate-800/40 transition-colors border-b border-slate-800/50">
                  <td className="p-4 font-mono text-blue-400">
                    {doc.reference || 'REF_AUTO'}
                  </td>
                  <td className="p-4 text-slate-500">
                    {new Date(doc.createdAt).toLocaleString()}
                  </td>
                  <td className="p-4">
                    <span className="px-2 py-1 rounded-md bg-amber-500/10 text-amber-400 border border-amber-500/20 text-[10px] font-bold uppercase">
                      TECH_TO_INDEX
                    </span>
                  </td>
                  <td className="p-4 text-right">
                    <button 
                      onClick={() => navigate(`/bo/indexation/${doc.id}`)}
                      className="inline-flex items-center gap-2 px-3 py-1.5 bg-slate-800 hover:bg-blue-600 text-slate-300 hover:text-white rounded-md transition-all text-xs font-medium border border-slate-700 hover:border-blue-500"
                    >
                      <Edit3 className="w-3.5 h-3.5" />
                      Indexer
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
};

export default PendingInbox;