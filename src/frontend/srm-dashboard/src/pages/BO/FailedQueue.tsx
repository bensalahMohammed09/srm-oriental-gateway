import React from 'react';
import { useNavigate } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import api from '../../api/axios';
import { FailedFileResponse } from '../../types/api';
import { 
  AlertTriangle, 
  FileWarning, 
  RefreshCcw, 
  LifeBuoy, 
  HardDrive,
  Clock,
  CheckCircle2
} from 'lucide-react';

const FailedQueue: React.FC = () => {
  const navigate = useNavigate();

  // 🔥 SRE Standard: Utilisation de TanStack Query avec Polling toutes les 15s
  const { data: failedFiles = [], isLoading, isError, refetch, isFetching } = useQuery({
    queryKey: ['failed-files'],
    queryFn: async () => {
      const response = await api.get<FailedFileResponse[]>('/api/v1/document/failed');
      return response.data;
    },
    refetchInterval: 15000, // L'agent sera notifié presque en temps réel d'un crash OCR
  });

  return (
    <div className="space-y-6 animate-in fade-in slide-in-from-bottom-4 duration-500">
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-white flex items-center gap-3">
            <LifeBuoy className="w-7 h-7 text-rose-500" />
            Centre de Récupération
            {isFetching && !isLoading && (
              <span className="flex h-2 w-2 relative ml-2">
                <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-rose-400 opacity-75"></span>
                <span className="relative inline-flex rounded-full h-2 w-2 bg-rose-500"></span>
              </span>
            )}
          </h1>
          <p className="text-slate-400 text-sm">Fichiers rejetés par le moteur OCR ou corrompus lors du transfert.</p>
        </div>
        
        <button 
          onClick={() => refetch()}
          className="p-2 text-slate-400 hover:text-white transition-colors"
          title="Scanner le dossier failed"
        >
          <RefreshCcw className={`w-5 h-5 ${isFetching ? 'animate-spin text-rose-500' : ''}`} />
        </button>
      </div>

      {/* 🛡️ SRE Standard: Skeleton State au lieu du spinner bloquant */}
      {isLoading ? (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {[1, 2, 3].map((i) => (
            <div key={i} className="h-40 bg-slate-800/50 animate-pulse rounded-xl border border-slate-800/30"></div>
          ))}
        </div>
      ) : isError ? (
        <div className="p-4 bg-rose-500/10 border border-rose-500/20 rounded-lg text-rose-400 text-sm flex items-center gap-3">
          <AlertTriangle className="w-5 h-5" />
          Impossible d'accéder au dossier des échecs système partagé.
        </div>
      ) : failedFiles.length === 0 ? (
        <div className="p-16 text-center border border-slate-800 border-dashed rounded-xl bg-slate-900/10">
          <CheckCircle2 className="w-16 h-16 mx-auto mb-4 text-emerald-500/20" />
          <h3 className="text-white font-semibold">Système Nominal</h3>
          <p className="text-slate-500 text-sm mt-1">Aucun échec technique détecté dans le stockage partagé.</p>
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {failedFiles.map((file, idx) => (
            <div key={idx} className="group p-5 rounded-xl border border-slate-800 bg-slate-900/50 hover:border-rose-500/30 transition-all">
              <div className="flex items-start justify-between mb-4">
                <div className="p-2 bg-rose-500/10 rounded-lg">
                  <FileWarning className="w-6 h-6 text-rose-500" />
                </div>
                <span className="text-[10px] font-mono text-slate-600 bg-slate-950 px-2 py-1 rounded uppercase">
                  Err: OCR_REJECT
                </span>
              </div>
              
              <div className="space-y-3">
                <h4 className="text-white font-medium truncate text-sm" title={file.fileName}>
                  {file.fileName}
                </h4>
                
                <div className="flex items-center gap-4 text-[10px] font-mono text-slate-500">
                  <div className="flex items-center gap-1">
                    <HardDrive className="w-3 h-3" />
                    {file.sizeKb.toFixed(1)} KB
                  </div>
                  <div className="flex items-center gap-1">
                    <Clock className="w-3 h-3" />
                    {new Date(file.creationTime).toLocaleDateString()}
                  </div>
                </div>

                <button 
                  onClick={() => navigate(`/bo/recover/${encodeURIComponent(file.fileName)}`)}
                  className="w-full mt-2 py-2 bg-slate-800 hover:bg-rose-600 text-slate-300 hover:text-white rounded-lg text-xs font-bold transition-all flex items-center justify-center gap-2"
                >
                  Récupération Manuelle
                </button>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
};

export default FailedQueue;