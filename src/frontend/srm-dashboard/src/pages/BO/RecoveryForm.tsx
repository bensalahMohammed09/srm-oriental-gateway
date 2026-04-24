import React, { useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import api from '../../api/axios';
import DocumentViewer from '../../components/documents/DocumentViewer';
import { 
  LifeBuoy, 
  Hash, 
  DollarSign, 
  Loader2, 
  CheckCircle2, 
  AlertCircle, 
  ArrowLeft,
  SendHorizontal
} from 'lucide-react';

const RecoveryForm: React.FC = () => {
  const { fileName } = useParams<{ fileName: string }>();
  const navigate = useNavigate();
  
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // État aligné sur ManualRecoveryRequest
  const [formData, setFormData] = useState({
    fileName: fileName || '',
    reference: '',
    totalAmount: ''
  });

  // Construction de l'URL de flux pour le fichier en échec
  const failedFileStreamUrl = `/api/v1/document/failed/${encodeURIComponent(fileName || '')}/file`;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsSubmitting(true);
    setError(null);

    try {
      // Endpoint: DocumentController.RecoverFailedDocument
      await api.post('/api/v1/document/failed/recover', {
        fileName: formData.fileName,
        reference: formData.reference,
        totalAmount: parseFloat(formData.totalAmount)
      });
      
      // En SRE, on redirige vers la file d'indexation car le document est maintenant "sauvé"
      navigate('/bo/pending');
    } catch (err: any) {
      setError(err.response?.data?.error || "Échec de la récupération binaire.");
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="flex flex-col gap-6 animate-in fade-in duration-500">
      <div className="flex items-center justify-between">
        <button 
          onClick={() => navigate('/bo/failed')}
          className="flex items-center gap-2 text-slate-500 hover:text-white transition-colors text-sm font-mono"
        >
          <ArrowLeft className="w-4 h-4" /> RECOVERY_CENTER/BACK
        </button>
        <div className="text-right">
          <span className="text-[10px] font-mono text-rose-500 bg-rose-500/10 px-2 py-1 rounded border border-rose-500/20">
            ACTION: MANUAL_RECOVERY
          </span>
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-8 h-[calc(100vh-180px)]">
        {/* Visualisation du fichier corrompu/rejeté */}
        <div className="flex flex-col h-full">
          <DocumentViewer 
            streamUrl={failedFileStreamUrl} 
            title={fileName || "Fichier Inconnu"}
            className="flex-1 border-rose-500/20" 
          />
        </div>

        {/* Formulaire de saisie curative */}
        <div className="flex flex-col justify-center max-w-xl mx-auto w-full space-y-8">
          <div className="space-y-2">
            <div className="p-3 bg-rose-500/10 w-fit rounded-xl mb-4">
              <LifeBuoy className="w-8 h-8 text-rose-500" />
            </div>
            <h1 className="text-2xl font-bold text-white tracking-tight">Récupération Curative</h1>
            <p className="text-slate-400 text-sm leading-relaxed">
              Ce fichier a été rejeté par l'OCR automatique. Veuillez identifier manuellement la référence et le montant pour le réinjecter dans le workflow.
            </p>
          </div>

          <form onSubmit={handleSubmit} className="space-y-5">
            {error && (
              <div className="p-4 bg-rose-500/10 border border-rose-500/20 rounded-lg text-rose-400 text-sm flex items-center gap-3">
                <AlertCircle className="w-5 h-5" />
                {error}
              </div>
            )}

            <div className="space-y-2">
              <label className="text-[10px] font-mono text-slate-500 uppercase tracking-widest flex items-center gap-2">
                <Hash className="w-3 h-3" /> Référence de secours
              </label>
              <input
                required
                type="text"
                className="w-full bg-slate-900 border border-slate-800 rounded-lg p-4 text-white focus:border-rose-500 outline-none transition-all font-mono"
                placeholder="REF-RECOVERY-000"
                value={formData.reference}
                onChange={(e) => setFormData({...formData, reference: e.target.value})}
              />
            </div>

            <div className="space-y-2">
              <label className="text-[10px] font-mono text-slate-500 uppercase tracking-widest flex items-center gap-2">
                <DollarSign className="w-3 h-3" /> Montant Identifié (TTC)
              </label>
              <input
                required
                type="number"
                step="0.01"
                className="w-full bg-slate-900 border border-slate-800 rounded-lg p-4 text-white focus:border-rose-500 outline-none transition-all font-mono"
                placeholder="0.00"
                value={formData.totalAmount}
                onChange={(e) => setFormData({...formData, totalAmount: e.target.value})}
              />
            </div>

            <button
              type="submit"
              disabled={isSubmitting || !formData.reference}
              className="w-full bg-rose-600 hover:bg-rose-500 disabled:opacity-50 text-white font-bold py-4 rounded-xl transition-all flex items-center justify-center gap-3 shadow-lg shadow-rose-600/20"
            >
              {isSubmitting ? (
                <Loader2 className="w-6 h-6 animate-spin" />
              ) : (
                <>
                  <SendHorizontal className="w-5 h-5" />
                  <span>Réinjecter dans le circuit</span>
                </>
              )}
            </button>
          </form>

          <div className="p-4 border border-slate-800 bg-slate-900/30 rounded-xl">
            <h5 className="text-[10px] font-mono text-slate-500 uppercase mb-2">Détails du fichier source</h5>
            <p className="text-xs text-slate-300 truncate font-mono">{fileName}</p>
          </div>
        </div>
      </div>
    </div>
  );
};

export default RecoveryForm;