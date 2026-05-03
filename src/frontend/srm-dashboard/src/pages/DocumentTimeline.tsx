import React from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import api from '../api/axios'; // 🌟 FIX : Le bon chemin relatif
import { DocumentDetailsResponse } from '../types/api';
import { 
  Loader2, ArrowLeft, CheckCircle2, XCircle, 
  Clock, Activity, ShieldCheck, User, MessageSquare, Route
} from 'lucide-react';

interface WorkflowHistory {
  stepName: string;
  action: string;
  userFullName: string;
  roleName: string;
  date: string;
  comment: string;
}

const DocumentTimeline: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();

  const { data: doc, isLoading: isDocLoading } = useQuery({
    queryKey: ['doc-details', id],
    queryFn: async () => (await api.get<DocumentDetailsResponse>(`/api/v1/document/${id}/details`)).data,
    enabled: !!id,
  });

  const { data: history = [], isLoading: isHistoryLoading } = useQuery({
    queryKey: ['doc-history', id],
    queryFn: async () => (await api.get<WorkflowHistory[]>(`/api/v1/workflow/${id}/history`)).data,
    enabled: !!id,
  });

  if (isDocLoading || isHistoryLoading) {
    return (
      <div className="h-[calc(100vh-100px)] flex flex-col items-center justify-center text-blue-500 font-black animate-pulse uppercase tracking-widest">
        <Loader2 className="w-10 h-10 animate-spin mb-4" /> Analyse de l'Audit Trail...
      </div>
    );
  }

  const sortedHistory = [...history].sort((a, b) => new Date(a.date).getTime() - new Date(b.date).getTime());

  const getNodeStyle = (action: string) => {
    switch (action?.toUpperCase()) {
      case 'APPROVED':
        return { color: 'text-emerald-500', bg: 'bg-emerald-500/10', border: 'border-emerald-500/20', icon: <CheckCircle2 className="w-5 h-5" /> };
      case 'REJECTED':
        return { color: 'text-rose-500', bg: 'bg-rose-500/10', border: 'border-rose-500/20', icon: <XCircle className="w-5 h-5" /> };
      case 'BUS_PENDING_VAL':
      case 'PENDING':
        return { color: 'text-blue-500', bg: 'bg-blue-500/10', border: 'border-blue-500/20', icon: <Route className="w-5 h-5" /> };
      default:
        return { color: 'text-slate-400', bg: 'bg-slate-800', border: 'border-slate-700', icon: <Activity className="w-5 h-5" /> };
    }
  };

  return (
    <div className="max-w-4xl mx-auto py-8 animate-in fade-in duration-500 pb-20">
      
      <div className="flex items-center justify-between mb-8">
        <button 
          onClick={() => navigate(-1)}
          className="flex items-center gap-2 text-slate-500 hover:text-white transition-all text-xs font-black uppercase"
        >
          <ArrowLeft className="w-4 h-4" /> Retour
        </button>
        <div className="flex items-center gap-2 px-4 py-2 bg-blue-500/10 border border-blue-500/20 rounded-full">
          <ShieldCheck className="w-4 h-4 text-blue-500" />
          <span className="text-[10px] font-black font-mono text-blue-500 uppercase tracking-widest">
            Audit Trail Cryptographique
          </span>
        </div>
      </div>

      <div className="bg-slate-900 border border-slate-800 rounded-3xl p-8 mb-12 shadow-2xl relative overflow-hidden">
        <div className="absolute top-0 left-0 w-1 h-full bg-blue-500"></div>
        <div className="flex justify-between items-start">
          <div>
            <h1 className="text-3xl font-black text-white tracking-tighter mb-2">{doc?.reference || 'N/A'}</h1>
            <p className="text-slate-400 font-medium">Fournisseur : <span className="text-white font-bold">{doc?.supplierName || 'N/A'}</span></p>
          </div>
          <div className="text-right">
            <p className="text-[10px] font-black text-slate-500 uppercase tracking-widest mb-1">Montant Certifié</p>
            <p className="text-2xl font-mono font-black text-emerald-400">{doc?.totalAmount?.toFixed(2)} DH</p>
          </div>
        </div>
      </div>

      <div className="relative pl-8 md:pl-0">
        <div className="absolute left-12 md:left-1/2 top-4 bottom-4 w-1 bg-slate-800 -translate-x-1/2 rounded-full"></div>

        <div className="space-y-12">
          {sortedHistory.map((step, index) => {
            const style = getNodeStyle(step.action);
            const isEven = index % 2 === 0;

            return (
              <div key={index} className={`relative flex items-center md:justify-between w-full ${isEven ? 'md:flex-row-reverse' : ''}`}>
                
                <div className="absolute left-0 md:left-1/2 -translate-x-1/2 flex items-center justify-center w-10 h-10 rounded-full border-4 border-[#0a0a0c] bg-slate-900 z-10 shadow-[0_0_15px_rgba(0,0,0,0.5)]">
                  <div className={`w-full h-full rounded-full flex items-center justify-center ${style.bg} ${style.color}`}>
                    {style.icon}
                  </div>
                </div>

                <div className="hidden md:block w-[45%]"></div>

                <div className="w-full md:w-[45%] pl-12 md:pl-0">
                  <div className={`p-6 rounded-3xl border ${style.border} ${style.bg} backdrop-blur-sm transition-transform hover:-translate-y-1 duration-300`}>
                    
                    <div className="flex justify-between items-start mb-4">
                      <span className={`text-[10px] font-black uppercase tracking-widest px-2 py-1 rounded-md bg-slate-950/50 ${style.color}`}>
                        {step.stepName}
                      </span>
                      <span className="text-[10px] font-mono text-slate-500 flex items-center gap-1">
                        <Clock className="w-3 h-3" />
                        {new Date(step.date).toLocaleString()}
                      </span>
                    </div>

                    <div className="flex items-center gap-2 mb-3">
                      <div className="w-6 h-6 rounded-full bg-slate-800 flex items-center justify-center">
                        <User className="w-3 h-3 text-slate-400" />
                      </div>
                      <p className="text-sm font-bold text-white">
                        {step.userFullName || 'Système'} 
                        <span className="text-slate-500 font-normal ml-2 text-xs">({step.roleName?.replace('ROLE_', '') || 'Auto'})</span>
                      </p>
                    </div>

                    {step.comment && (
                      <div className="mt-4 p-3 bg-slate-950/50 rounded-xl border border-slate-800/50 flex items-start gap-3">
                        <MessageSquare className="w-4 h-4 text-slate-500 mt-0.5 shrink-0" />
                        <p className="text-xs text-slate-300 font-medium italic leading-relaxed">
                          "{step.comment}"
                        </p>
                      </div>
                    )}

                  </div>
                </div>

              </div>
            );
          })}
        </div>
      </div>
      
    </div>
  );
};

export default DocumentTimeline;