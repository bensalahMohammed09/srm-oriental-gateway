import React from 'react';
import { useNavigate } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import api from '../../api/axios';
import { DocumentResponse } from '../../types/api';
import { CheckSquare, FileSearch, RefreshCw, PenTool, CheckCircle2, XCircle, Clock } from 'lucide-react';

const ApprovalInbox: React.FC = () => {
  const navigate = useNavigate();

  const { data: documents = [], isLoading, refetch, isFetching } = useQuery({
    queryKey: ['my-pending-tasks'],
    queryFn: async () => (await api.get<DocumentResponse[]>('/api/v1/workflow/my-tasks')).data,
    refetchInterval: 30000,
  });

  return (
    <div className="space-y-6 animate-in fade-in duration-500 pb-20">
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div className="flex items-center gap-4">
          <div className="p-3 bg-emerald-500/10 rounded-xl border border-emerald-500/20">
            <CheckSquare className="w-6 h-6 text-emerald-500" />
          </div>
          <div>
            <h1 className="text-2xl font-black text-white tracking-tighter uppercase">Mes Validations</h1>
            <p className="text-slate-400 text-sm font-medium">Dossiers assignés à votre département ({documents.length} en attente).</p>
          </div>
        </div>
        <button onClick={() => refetch()} className="p-2 text-slate-500 hover:text-white transition-colors">
          <RefreshCw className={`w-5 h-5 ${isFetching ? 'animate-spin text-emerald-500' : ''}`} />
        </button>
      </div>

      {isLoading ? (
        <div className="space-y-3">{[1, 2, 3].map(i => <div key={i} className="w-full h-16 bg-slate-800/40 rounded-xl animate-pulse" />)}</div>
      ) : documents.length === 0 ? (
        <div className="p-20 text-center border-2 border-slate-800 border-dashed rounded-3xl bg-slate-900/20">
          <FileSearch className="w-16 h-16 mx-auto mb-4 text-emerald-500/20" />
          <h3 className="text-white font-bold text-lg">Aucune tâche en attente</h3>
          <p className="text-slate-500 text-sm">Votre bannette de validation est vide pour le moment.</p>
        </div>
      ) : (
        <div className="overflow-hidden rounded-2xl border border-slate-800 bg-slate-900/50 shadow-2xl">
          <table className="w-full text-left">
            <thead>
              <tr className="bg-slate-950/80 text-slate-500 text-[10px] uppercase font-black tracking-widest border-b border-slate-800">
                <th className="p-5">Référence</th>
                <th className="p-5">Catégorie</th>
                <th className="p-5">Avis en cours</th>
                <th className="p-5">Soumis le</th>
                <th className="p-5 text-right">Action</th>
              </tr>
            </thead>
            <tbody className="text-sm text-slate-300">
              {documents.map((doc) => (
                <tr key={doc.id} className="hover:bg-slate-800/40 border-b border-slate-800/50 transition-colors">
                  <td className="p-5 font-mono text-emerald-400 font-bold">{doc.reference}</td>
                  <td className="p-5 font-semibold text-white">{doc.category || 'N/A'}</td>
                  
                  {/* 🌟 NOUVEAU : Affichage des pastilles d'état d'approbation */}
                  <td className="p-5">
                    {doc.currentApprovals ? (
                       <div className="flex gap-1.5 flex-wrap">
                        {Object.entries(doc.currentApprovals).map(([role, status]) => {
                          const shortRoleName = role.replace('ROLE_', '').substring(0, 3);
                          return (
                            <span 
                              key={role} 
                              title={`Département ${role.replace('ROLE_', '')}`}
                              className={`flex items-center gap-1 px-2 py-0.5 rounded border text-[9px] font-black cursor-default ${
                                status === 'APPROVED' ? 'bg-emerald-500/10 text-emerald-500 border-emerald-500/20' :
                                status === 'REJECTED' ? 'bg-rose-500/10 text-rose-500 border-rose-500/20' :
                                'bg-slate-800/50 text-slate-400 border-slate-700'
                              }`}
                            >
                              {status === 'APPROVED' && <CheckCircle2 className="w-3 h-3" />}
                              {status === 'REJECTED' && <XCircle className="w-3 h-3" />}
                              {status === 'WAITING' && <Clock className="w-3 h-3" />}
                              {shortRoleName}
                            </span>
                          );
                        })}
                      </div>
                    ) : (
                      <span className="text-slate-500 text-xs italic">N/A</span>
                    )}
                  </td>

                  <td className="p-5 text-slate-500">{new Date(doc.createdAt).toLocaleString()}</td>
                  <td className="p-5 text-right">
                    <button 
                      onClick={() => navigate(`/approvals/${doc.id}`)} 
                      className="px-5 py-2.5 bg-emerald-600/10 text-emerald-500 hover:bg-emerald-600 hover:text-white border border-emerald-500/20 rounded-lg transition-all text-xs font-black uppercase flex items-center justify-center gap-2 ml-auto"
                    >
                      <PenTool className="w-4 h-4" /> Décision
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

export default ApprovalInbox;