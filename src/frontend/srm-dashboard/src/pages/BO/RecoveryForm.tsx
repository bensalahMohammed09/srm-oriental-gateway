import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import * as z from 'zod';
import { useQuery } from '@tanstack/react-query';
import api from '../../api/axios';
import { 
  LifeBuoy, Hash, DollarSign, Loader2, AlertCircle, 
  ArrowLeft, SendHorizontal, Eye, FileWarning, Plus, Trash2, Landmark, CheckCircle2, Folder
} from 'lucide-react';

const recoverySchema = z.object({
  categoryId: z.string().min(1, "Veuillez sélectionner une destination métier"),
  reference: z.string().min(3, "La référence est obligatoire"),
  supplierName: z.string().min(2, "Le fournisseur est obligatoire"),
  totalAmount: z.string()
    .min(1, "Le montant est obligatoire")
    .refine((val) => !isNaN(parseFloat(val)) && parseFloat(val) >= 0, {
      message: "Montant invalide",
    })
});

type RecoveryFormValues = z.infer<typeof recoverySchema>;

interface MetaRow {
  id: string;
  key: string;
  value: string;
}

const RecoveryForm: React.FC = () => {
  const { fileName } = useParams<{ fileName: string }>();
  const navigate = useNavigate();
  
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [errorStatus, setErrorStatus] = useState<string | null>(null);
  const [showSuccess, setShowSuccess] = useState(false);
  
  // 🌟 RESTAURÉ : Le state pour la table de métadonnées
  const [metaList, setMetaList] = useState<MetaRow[]>([]);
  
  const [blobUrl, setBlobUrl] = useState<string | null>(null);
  const [blobLoading, setBlobLoading] = useState(true);

  const { data: categories = [] } = useQuery({
    queryKey: ['categories'],
    queryFn: async () => (await api.get<any[]>('/api/v1/category')).data,
    staleTime: 5 * 60 * 1000
  });

  const { register, handleSubmit, formState: { errors } } = useForm<RecoveryFormValues>({
    resolver: zodResolver(recoverySchema),
  });

  useEffect(() => {
    if (!fileName) return;
    setBlobLoading(true);
    const encodedName = encodeURIComponent(fileName);
    
    // ⚠️ Si ceci échoue (404), c'est que le backend n'a pas la route [HttpGet("failed/{fileName}/file")] !
    api.get(`/api/v1/document/failed/${encodedName}/file`, { responseType: 'blob' })
      .then(res => {
        const url = URL.createObjectURL(res.data);
        setBlobUrl(url);
      })
      .catch(err => {
        console.error("Erreur flux failed:", err);
        setErrorStatus("Impossible de charger le fichier binaire. Vérifiez que l'API expose bien la route de téléchargement.");
      })
      .finally(() => setBlobLoading(false));

    return () => { if (blobUrl) URL.revokeObjectURL(blobUrl); };
  }, [fileName]);

  // --- CRUD METADATA ---
  const addRow = () => setMetaList([...metaList, { id: Math.random().toString(36).substr(2, 9), key: '', value: '' }]);
  const removeRow = (id: string) => setMetaList(metaList.filter(r => r.id !== id));
  const updateRow = (id: string, f: 'key' | 'value', v: string) => 
    setMetaList(metaList.map(r => r.id === id ? { ...r, [f]: v } : r));

  const onSubmit = async (data: RecoveryFormValues) => {
    setIsSubmitting(true);
    setErrorStatus(null);

    // Injection des métadonnées dynamiques
    const metadataPayload: Record<string, any> = {};
    metaList.forEach(m => {
      if (m.key.trim()) {
        metadataPayload[m.key.trim()] = { value: m.value, confidence: 1.0 };
      }
    });

    try {
      await api.post('/api/v1/document/failed/recover', {
        fileName: fileName,
        categoryId: data.categoryId,
        reference: data.reference,
        supplierName: data.supplierName,
        totalAmount: parseFloat(data.totalAmount),
        metadata: metadataPayload // 🌟 RESTAURÉ : Envoi au backend
      });
      
      setShowSuccess(true);
    } catch (err: any) {
      setErrorStatus(err.response?.data?.detail || "Échec de la récupération chirurgicale.");
    } finally {
      setIsSubmitting(false);
    }
  };

  const isPdf = fileName?.toLowerCase().endsWith('.pdf');

  return (
    <>
      <div className="flex flex-col h-[calc(100vh-120px)] animate-in fade-in duration-500">
        <div className="flex items-center justify-between mb-6">
          <button 
            onClick={() => navigate('/bo/failed')}
            className="flex items-center gap-2 text-slate-500 hover:text-white transition-all text-xs font-black uppercase"
          >
            <ArrowLeft className="w-4 h-4" /> Retour Centre Échecs
          </button>
          <div className="flex items-center gap-2">
            <span className="text-[10px] font-black font-mono text-rose-500 bg-rose-500/10 px-3 py-1.5 rounded-full border border-rose-500/20 shadow-lg shadow-rose-500/5">
              CRITICAL_RECOVERY_MODE
            </span>
          </div>
        </div>

        <div className="flex flex-col lg:flex-row gap-8 flex-1 overflow-hidden">
          
          <div className="lg:w-[45%] flex flex-col h-full overflow-y-auto pr-3 custom-scrollbar">
            <div className="space-y-2 mb-8">
              <div className="p-3 bg-rose-500/10 w-fit rounded-2xl border border-rose-500/20 mb-4">
                <LifeBuoy className="w-8 h-8 text-rose-500" />
              </div>
              <h1 className="text-2xl font-black text-white tracking-tighter uppercase">Sauvetage Documentaire</h1>
              <p className="text-slate-400 text-sm font-medium leading-relaxed">
                L'IA n'a pas pu traiter ce fichier. Veuillez reconstruire manuellement la fiche pour l'injecter dans le circuit de validation.
              </p>
            </div>

            <form onSubmit={handleSubmit(onSubmit)} className="space-y-6 pb-20">
              {errorStatus && (
                <div className="p-4 bg-rose-500/10 border border-rose-500/20 rounded-2xl text-rose-400 text-xs flex items-center gap-3 animate-shake">
                  <AlertCircle className="w-5 h-5 shrink-0" />
                  {errorStatus}
                </div>
              )}

              <div className="p-6 rounded-3xl border border-slate-800 bg-slate-900/50 space-y-5 shadow-2xl">
                <h3 className="text-[10px] font-black text-slate-500 uppercase tracking-widest border-b border-slate-800 pb-2">Données de base</h3>
                
                <div className="space-y-1">
                  <label className="text-[10px] font-black text-slate-500 uppercase flex items-center gap-2">
                    <Folder className="w-3 h-3" /> Destination Métier
                  </label>
                  <select
                    {...register('categoryId')}
                    className={`w-full bg-slate-950 border rounded-xl p-3.5 text-white focus:border-rose-500 outline-none transition-all ${errors.categoryId ? 'border-rose-500' : 'border-slate-800'}`}
                  >
                    <option value="">Sélectionner une catégorie...</option>
                    {categories.map((c: any) => <option key={c.id} value={c.id}>{c.name}</option>)}
                  </select>
                </div>

                <div className="space-y-1">
                  <label className="text-[10px] font-black text-slate-500 uppercase flex items-center gap-2">
                    <Hash className="w-3 h-3" /> Référence
                  </label>
                  <input
                    {...register('reference')}
                    className={`w-full bg-slate-950 border rounded-xl p-3.5 text-white font-mono focus:border-rose-500 outline-none transition-all ${errors.reference ? 'border-rose-500' : 'border-slate-800'}`}
                    placeholder="REF-SAVED-001"
                  />
                </div>

                <div className="space-y-1">
                  <label className="text-[10px] font-black text-slate-500 uppercase flex items-center gap-2">
                    <Landmark className="w-3 h-3" /> Fournisseur / Tiers
                  </label>
                  <input
                    {...register('supplierName')}
                    className={`w-full bg-slate-950 border rounded-xl p-3.5 text-white focus:border-rose-500 outline-none transition-all ${errors.supplierName ? 'border-rose-500' : 'border-slate-800'}`}
                    placeholder="NOM DU TIERS"
                  />
                </div>

                <div className="space-y-1">
                  <label className="text-[10px] font-black text-slate-500 uppercase flex items-center gap-2">
                    <DollarSign className="w-3 h-3" /> Montant Identifié (DH)
                  </label>
                  <input
                    {...register('totalAmount')}
                    type="number"
                    step="0.01"
                    className={`w-full bg-slate-950 border rounded-xl p-3.5 text-white font-mono focus:border-rose-500 outline-none transition-all ${errors.totalAmount ? 'border-rose-500' : 'border-slate-800'}`}
                    placeholder="0.00"
                  />
                </div>
              </div>

              {/* 🌟 RESTAURÉ : Tableau des métadonnées dynamiques */}
              <div className="p-6 rounded-3xl border border-slate-800 bg-slate-900/50 space-y-4 shadow-2xl">
                <div className="flex justify-between items-center border-b border-slate-800 pb-2">
                  <h3 className="text-[10px] font-black text-slate-500 uppercase tracking-widest">Métadonnées Supplémentaires</h3>
                  <button type="button" onClick={addRow} className="p-2 bg-rose-500/10 text-rose-500 rounded-xl hover:bg-rose-500 hover:text-white transition-all"><Plus className="w-4 h-4" /></button>
                </div>

                {metaList.length === 0 ? (
                  <p className="text-center text-slate-600 text-[10px] uppercase font-bold py-4 italic">Aucun champ additionnel</p>
                ) : (
                  <div className="space-y-3">
                    {metaList.map(row => (
                      <div key={row.id} className="flex gap-2">
                        <input 
                          placeholder="Clé" 
                          value={row.key}
                          onChange={e => updateRow(row.id, 'key', e.target.value)} 
                          className="w-1/3 bg-slate-950 border border-slate-800 rounded-xl p-3 text-white text-[10px] font-black uppercase outline-none focus:border-rose-500" 
                        />
                        <input 
                          placeholder="Valeur" 
                          value={row.value}
                          onChange={e => updateRow(row.id, 'value', e.target.value)} 
                          className="flex-1 bg-rose-500/5 border-2 border-rose-500/20 text-rose-400 rounded-xl p-3 text-xs font-bold outline-none focus:border-rose-500 transition-all" 
                        />
                        <button type="button" onClick={() => removeRow(row.id)} className="p-3 text-slate-600 hover:text-rose-500 hover:bg-rose-500/10 rounded-xl transition-colors">
                          <Trash2 className="w-5 h-5"/>
                        </button>
                      </div>
                    ))}
                  </div>
                )}
              </div>

              <button
                type="submit"
                disabled={isSubmitting}
                className="w-full bg-rose-600 hover:bg-rose-500 disabled:opacity-50 text-white font-black uppercase py-5 rounded-2xl transition-all flex items-center justify-center gap-3 shadow-2xl shadow-rose-600/20 active:scale-95"
              >
                {isSubmitting ? <Loader2 className="animate-spin" /> : <SendHorizontal className="w-5 h-5" />}
                <span>Réinjecter dans le flux</span>
              </button>
            </form>
          </div>

          <div className="lg:w-[55%] flex flex-col h-full bg-slate-950 border border-slate-800 rounded-[2.5rem] overflow-hidden shadow-2xl relative">
            <div className="h-14 bg-slate-900 border-b border-slate-800 flex items-center justify-between px-6 z-10">
              <div className="flex items-center gap-2 text-slate-300">
                <Eye className="w-4 h-4 text-rose-500" />
                <span className="text-xs font-black uppercase truncate max-w-[250px] font-mono tracking-tighter">{fileName}</span>
              </div>
              <div className="flex gap-2">
                <span className="text-[10px] font-black text-rose-500 bg-rose-500/10 px-3 py-1.5 rounded-full border border-rose-500/20 uppercase">
                  FAILED_STORAGE_STREAM
                </span>
              </div>
            </div>

            <div className="flex-1 relative bg-slate-900/50">
              {blobLoading ? (
                <div className="absolute inset-0 flex flex-col items-center justify-center text-slate-700 bg-slate-950">
                  <Loader2 className="w-12 h-12 animate-spin mb-4 opacity-20" />
                  <p className="text-[10px] font-black uppercase tracking-widest animate-pulse">Streaming Authentifié...</p>
                </div>
              ) : !blobUrl ? (
                <div className="absolute inset-0 flex flex-col items-center justify-center text-slate-600">
                  <FileWarning className="w-12 h-12 mb-2 opacity-50" />
                  <p className="text-[10px] font-black uppercase">Le flux binaire est inaccessible</p>
                </div>
              ) : isPdf ? (
                <iframe src={blobUrl} className="w-full h-full border-0" title="Source PDF" />
              ) : (
                <div className="w-full h-full overflow-auto p-4 flex items-center justify-center bg-slate-950">
                  <img src={blobUrl} alt="Failed Source" className="max-w-full h-auto rounded-xl shadow-2xl border border-slate-800" />
                </div>
              )}
            </div>
          </div>
        </div>
      </div>

      {showSuccess && (
        <div className="fixed inset-0 bg-slate-950/80 backdrop-blur-sm flex items-center justify-center z-50 animate-in fade-in duration-200">
          <div className="bg-slate-900 border border-slate-800 p-8 rounded-[2rem] max-w-sm w-full text-center shadow-2xl shadow-rose-900/20 animate-in zoom-in-95 duration-300">
            <div className="w-20 h-20 bg-emerald-500/10 text-emerald-500 rounded-full flex items-center justify-center mx-auto mb-6 shadow-inner border border-emerald-500/20">
              <CheckCircle2 className="w-10 h-10" />
            </div>
            <h2 className="text-2xl font-black text-white mb-2">Sauvetage Réussi !</h2>
            <p className="text-slate-400 text-sm mb-8 leading-relaxed">
              Le fichier a été récupéré, indexé manuellement, et injecté avec succès dans le circuit de validation.
            </p>
            <button 
              onClick={() => navigate('/bo/pending')}
              className="w-full bg-blue-600 hover:bg-blue-500 text-white font-black uppercase tracking-wider text-xs py-4 rounded-xl transition-all shadow-lg shadow-blue-600/20 active:scale-95"
            >
              Retour à l'Accueil
            </button>
          </div>
        </div>
      )}
    </>
  );
};

export default RecoveryForm;