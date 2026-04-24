import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import api from '../../api/axios';
import { useAuth } from '../../hooks/useAuth';
import { FailedFileResponse } from '../../types/api';
import { 
  AlertTriangle, 
  Loader2, 
  FileWarning, 
  RefreshCcw, 
  LifeBuoy, 
  HardDrive,
  Clock
} from 'lucide-react';

const FailedQueue: React.FC = () => {
  const { user } = useAuth();
  const navigate = useNavigate();
  const [failedFiles, setFailedFiles] = useState<FailedFileResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const isBO = user?.roles.includes('ROLE_BO');

  const fetchFailed = async () => {
    setLoading(true);
    try {
      // ✅ Routage en minuscules [controller]
      const response = await api.get<FailedFileResponse[]>('/api/v1/document/failed');
      setFailedFiles(response.data);
      setError(null);
    } catch (err: any) {
      setError("Impossible d'accéder au dossier des échecs système.");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (isBO) fetchFailed();
  }, [isBO]);

  if (!isBO) {
    return (
      <div className="p-8 text-center bg-rose-500/5 border border-rose-500/20 rounded-xl text-rose-400">
        <AlertTriangle className="w-12 h-12 mx-auto mb-4" />
        <h2 className="text-xl font-bold">Accès Restreint</h2>
        <p className="mt-2">Seul le Bureau d'Ordre peut effectuer des opérations de récupération système.</p>
      </div>
    );
  }

  return (
    <div className="space-y-6 animate-in fade-in slide-in-from-bottom-4 duration-500">
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-white flex items-center gap-3">
            <LifeBuoy className="w-7 h-7 text-rose-500" />
            Centre de Récupération
          </h1>
          <p className="text-slate-400 text-sm">Fichiers rejetés par le moteur OCR ou corrompus lors du transfert.</p>
        </div>
        
        <button 
          onClick={fetchFailed}
          className="p-2 text-slate-400 hover:text-white transition-colors"
          title="Scanner le dossier failed"
        >
          <RefreshCcw className={`w-5 h-5 ${loading ? 'animate-spin' : ''}`} />
        </button>
      </div>

      {loading ? (
        <div className="flex flex-col items-center justify-center h-64 border border-slate-800 border-dashed rounded-xl">
          <Loader2 className="w-10 h-10 animate-spin text-rose-500 mb-4" />
          <p className="text-slate-500 font-mono text-xs uppercase tracking-widest">Scan du volume /failed...</p>
        </div>
      ) : error ? (
        <div className="p-4 bg-rose-500/10 border border-rose-500/20 rounded-lg text-rose-400 text-sm flex items-center gap-3">
          <AlertTriangle className="w-5 h-5" />
          {error}
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

// Import manquant pour l'état vide (simulé pour la lisibilité)
const CheckCircle2 = ({ className }: { className: string }) => (
  <svg className={className} fill="none" viewBox="0 0 24 24" stroke="currentColor">
    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
  </svg>
);

export default FailedQueue;