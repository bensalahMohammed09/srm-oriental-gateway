import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import * as z from 'zod';
import api from '../../api/axios';
import { 
  Upload, FileText, Landmark, Hash, DollarSign, 
  Loader2, AlertCircle, Tag, Zap, Database, Plus, Trash2, CheckCircle2
} from 'lucide-react';

const manualSchema = z.object({
  reference: z.string().min(3, "La référence doit contenir au moins 3 caractères"),
  categoryId: z.string().min(1, "Veuillez sélectionner une catégorie"),
  supplierName: z.string().min(2, "Le nom du fournisseur est requis"),
  totalAmount: z.string()
    .min(1, "Le montant est requis")
    .refine((val) => !isNaN(parseFloat(val)) && parseFloat(val) > 0, {
      message: "Le montant doit être un nombre positif valide",
    })
});

type ManualFormValues = z.infer<typeof manualSchema>;

interface Category {
  id: string;
  name: string;
}

// Interface pour le tableau dynamique des métadonnées optionnelles
interface MetaRow {
  id: string;
  key: string;
  value: string;
}

const MAX_FILE_SIZE = 10 * 1024 * 1024; // 10MB

const ManualUpload: React.FC = () => {
  const navigate = useNavigate();
  const [activeTab, setActiveTab] = useState<'ocr' | 'manual'>('ocr');
  
  // OCR State
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  
  // Manual State (Métadonnées dynamiques)
  const [metaList, setMetaList] = useState<MetaRow[]>([]);
  
  // Shared State
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [errorStatus, setErrorStatus] = useState<string | null>(null);
  
  // 🌟 NOUVEAU : État pour le popup de succès
  const [showSuccess, setShowSuccess] = useState(false);

  const { data: categories = [], isLoading: loadingCategories } = useQuery({
    queryKey: ['categories'],
    queryFn: async () => {
      const res = await api.get<Category[]>('/api/v1/category');
      return res.data;
    },
    staleTime: 5 * 60 * 1000,
  });

  const { register, handleSubmit, formState: { errors }, reset } = useForm<ManualFormValues>({
    resolver: zodResolver(manualSchema),
  });

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setErrorStatus(null);
    if (e.target.files && e.target.files[0]) {
      const file = e.target.files[0];
      if (file.size > MAX_FILE_SIZE) {
        setErrorStatus("Fichier trop volumineux. La limite SRE est fixée à 10 MB.");
        setSelectedFile(null);
        return;
      }
      setSelectedFile(file);
    }
  };

  // --- LOGIQUE METADATA (CRUD MANUEL) ---
  const addMetaRow = () => {
    setMetaList([...metaList, { id: Math.random().toString(36).substr(2, 9), key: '', value: '' }]);
  };

  const updateMetaRow = (id: string, field: 'key' | 'value', newValue: string) => {
    setMetaList(metaList.map(row => row.id === id ? { ...row, [field]: newValue } : row));
  };

  const removeMetaRow = (id: string) => {
    setMetaList(metaList.filter(row => row.id !== id));
  };

  // ✅ ACTION 1: Fast Track (OCR)
  const submitOcr = async () => {
    if (!selectedFile) return;
    setIsSubmitting(true);
    setErrorStatus(null);

    const payload = new FormData();
    payload.append('file', selectedFile);

    try {
      await api.post('/api/v1/document/upload', payload, {
        headers: { 'Content-Type': 'multipart/form-data' }
      });
      // 🌟 Afficher le modal au lieu de naviguer
      setShowSuccess(true);
    } catch (err: any) {
      setErrorStatus(err.response?.data?.detail || "Échec de la transmission vers le moteur OCR.");
    } finally {
      setIsSubmitting(false);
    }
  };

  // ✅ ACTION 2: Saisie Manuelle Complète (Data-Only avec Métadonnées Dynamiques)
  const submitManual = async (data: ManualFormValues) => {
    setIsSubmitting(true);
    setErrorStatus(null);

    // Transformation du tableau React en Dictionnaire JSON pour le C#
    const metadataPayload: Record<string, any> = {};
    metaList.forEach(row => {
      if (row.key.trim() !== '') {
        // 🌟 RÈGLE D'OR : Saisie Humaine = 100% de confiance (1.0)
        metadataPayload[row.key.trim()] = { value: row.value, confidence: 1.0 };
      }
    });

    const payload = {
      reference: data.reference,
      supplierName: data.supplierName,
      totalAmount: parseFloat(data.totalAmount),
      categoryId: data.categoryId,
      metadata: metadataPayload // Injection du dictionnaire dynamique
    };

    try {
      await api.post('/api/v1/document/manual-upload', payload);
      // 🌟 Afficher le modal au lieu de naviguer
      setShowSuccess(true);
    } catch (err: any) {
      setErrorStatus(err.response?.data?.detail || "Échec de la création manuelle du document.");
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <>
      <div className="max-w-4xl mx-auto space-y-6 animate-in fade-in duration-500 pb-20">
        <div className="flex items-center justify-between mb-8">
          <div className="flex items-center gap-4">
            <div className="p-3 bg-blue-600/10 rounded-xl">
              <Upload className="w-6 h-6 text-blue-500" />
            </div>
            <div>
              <h1 className="text-2xl font-bold text-white tracking-tight">Ingestion Documentaire</h1>
              <p className="text-slate-400 text-sm">Alimentation du pipeline de validation SRM.</p>
            </div>
          </div>
        </div>

        <div className="flex p-1 bg-slate-900 border border-slate-800 rounded-xl mb-6">
          <button 
            onClick={() => { setActiveTab('ocr'); setErrorStatus(null); }}
            className={`flex-1 flex items-center justify-center gap-2 py-3 rounded-lg text-sm font-bold transition-all ${activeTab === 'ocr' ? 'bg-blue-600 text-white shadow-lg' : 'text-slate-400 hover:text-white'}`}
          >
            <Zap className="w-4 h-4" /> Injection Rapide (Scanner OCR)
          </button>
          <button 
            onClick={() => { setActiveTab('manual'); setErrorStatus(null); reset(); setMetaList([]); }}
            className={`flex-1 flex items-center justify-center gap-2 py-3 rounded-lg text-sm font-bold transition-all ${activeTab === 'manual' ? 'bg-emerald-600 text-white shadow-lg' : 'text-slate-400 hover:text-white'}`}
          >
            <Database className="w-4 h-4" /> Saisie Manuelle (Data-Only)
          </button>
        </div>

        {errorStatus && (
          <div className="p-4 rounded-xl flex items-center gap-3 border bg-rose-500/10 text-rose-400 border-rose-500/20 animate-shake">
            <AlertCircle className="w-5 h-5 shrink-0" />
            <p className="text-sm font-medium">{errorStatus}</p>
          </div>
        )}

        {/* --- TAB 1: OCR --- */}
        {activeTab === 'ocr' && (
          <div className="space-y-6">
            <label className={`block p-10 border-2 border-dashed rounded-2xl transition-all cursor-pointer group ${selectedFile ? 'border-blue-500/50 bg-blue-500/5' : 'border-slate-800 bg-slate-900/30 hover:bg-slate-900/50 hover:border-blue-500/50'}`}>
              <input type="file" className="hidden" onChange={handleFileChange} accept=".pdf,.png,.jpg,.jpeg" />
              <div className="flex flex-col items-center text-center">
                <div className={`w-14 h-14 rounded-full flex items-center justify-center mb-4 transition-transform group-hover:scale-110 ${selectedFile ? 'bg-blue-600' : 'bg-slate-800'}`}>
                  <FileText className={`w-7 h-7 ${selectedFile ? 'text-white' : 'text-slate-400 group-hover:text-blue-400'}`} />
                </div>
                <p className="text-white font-medium text-lg">
                  {selectedFile ? selectedFile.name : "Sélectionner le flux binaire à envoyer au Worker"}
                </p>
                <p className="text-slate-500 text-[10px] uppercase font-mono mt-2 tracking-widest flex items-center gap-2">
                  <span>PDF / PNG / JPG</span>
                  <span>•</span>
                  <span className={selectedFile && selectedFile.size > MAX_FILE_SIZE ? 'text-rose-500 font-bold' : ''}>MAX 10 MB</span>
                </p>
              </div>
            </label>

            <button
              onClick={submitOcr}
              disabled={isSubmitting || !selectedFile}
              className="w-full bg-blue-600 hover:bg-blue-500 disabled:opacity-50 text-white font-bold py-4 rounded-xl transition-all flex items-center justify-center gap-3 shadow-lg shadow-blue-600/20"
            >
              {isSubmitting ? <Loader2 className="w-6 h-6 animate-spin" /> : <><Upload className="w-5 h-5" /> <span>Transmettre au moteur OCR</span></>}
            </button>
          </div>
        )}

        {/* --- TAB 2: MANUAL SAISIE (JSON ONLY) --- */}
        {activeTab === 'manual' && (
          <form onSubmit={handleSubmit(submitManual)} className="space-y-6">
            {/* Bloc 1 : Champs Obligatoires */}
            <div className="grid grid-cols-1 md:grid-cols-2 gap-6 bg-slate-900/40 border border-slate-800 rounded-2xl p-6 backdrop-blur-md">
              <div className="md:col-span-2 border-b border-slate-800 pb-2">
                <h3 className="text-xs font-mono text-slate-500 uppercase tracking-widest">Informations Principales (Obligatoires)</h3>
              </div>

              <div className="space-y-2">
                <label className="text-[10px] font-mono text-slate-500 uppercase flex items-center gap-2">
                  <Hash className="w-3 h-3" /> Référence Interne
                </label>
                <input
                  {...register('reference')}
                  className={`w-full bg-slate-950 border rounded-lg p-3 text-white focus:border-emerald-500 outline-none font-mono ${errors.reference ? 'border-rose-500' : 'border-slate-800'}`}
                  placeholder="REF-MANUAL-000"
                />
                {errors.reference && <p className="text-rose-400 text-xs mt-1">{errors.reference.message}</p>}
              </div>

              <div className="space-y-2">
                <label className="text-[10px] font-mono text-slate-500 uppercase flex items-center gap-2">
                  <Tag className="w-3 h-3" /> Catégorie de Workflow
                </label>
                <select
                  {...register('categoryId')}
                  disabled={loadingCategories}
                  className={`w-full bg-slate-950 border rounded-lg p-3 text-white focus:border-emerald-500 outline-none appearance-none cursor-pointer ${errors.categoryId ? 'border-rose-500' : 'border-slate-800'}`}
                >
                  <option value="">{loadingCategories ? "Chargement..." : "Choisir la destination..."}</option>
                  {categories.map(cat => (
                    <option key={cat.id} value={cat.id}>{cat.name}</option>
                  ))}
                </select>
                {errors.categoryId && <p className="text-rose-400 text-xs mt-1">{errors.categoryId.message}</p>}
              </div>

              <div className="space-y-2">
                <label className="text-[10px] font-mono text-slate-500 uppercase flex items-center gap-2">
                  <Landmark className="w-3 h-3" /> Tiers / Fournisseur
                </label>
                <input
                  {...register('supplierName')}
                  className={`w-full bg-slate-950 border rounded-lg p-3 text-white focus:border-emerald-500 outline-none ${errors.supplierName ? 'border-rose-500' : 'border-slate-800'}`}
                  placeholder="Entité émettrice"
                />
                {errors.supplierName && <p className="text-rose-400 text-xs mt-1">{errors.supplierName.message}</p>}
              </div>

              <div className="space-y-2">
                <label className="text-[10px] font-mono text-slate-500 uppercase flex items-center gap-2">
                  <DollarSign className="w-3 h-3" /> Montant Identifié (DH)
                </label>
                <input
                  {...register('totalAmount')}
                  type="number"
                  step="0.01"
                  className={`w-full bg-slate-950 border rounded-lg p-3 text-white focus:border-emerald-500 outline-none font-mono ${errors.totalAmount ? 'border-rose-500' : 'border-slate-800'}`}
                  placeholder="0.00"
                />
                {errors.totalAmount && <p className="text-rose-400 text-xs mt-1">{errors.totalAmount.message}</p>}
              </div>
            </div>

            {/* Bloc 2 : Métadonnées Dynamiques (Optionnel) */}
            <div className="bg-slate-900/40 border border-slate-800 rounded-2xl p-6 backdrop-blur-md space-y-4">
              <div className="flex justify-between items-center border-b border-slate-800 pb-2">
                <h3 className="text-xs font-mono text-slate-500 uppercase tracking-widest flex items-center gap-2">
                  Métadonnées Optionnelles <CheckCircle2 className="w-3 h-3 text-emerald-500"/>
                </h3>
                <button 
                  type="button" 
                  onClick={addMetaRow}
                  className="flex items-center gap-1 text-[10px] uppercase font-bold text-emerald-400 hover:text-emerald-300 bg-emerald-500/10 px-2 py-1 rounded transition-colors"
                >
                  <Plus className="w-3 h-3" /> Ajouter un champ
                </button>
              </div>

              {metaList.length === 0 ? (
                <div className="py-6 text-center text-slate-500 text-sm">Aucun champ supplémentaire (Optionnel).</div>
              ) : (
                <div className="space-y-3">
                  {metaList.map((row) => (
                    <div key={row.id} className="flex items-center gap-2">
                      <input
                        type="text"
                        placeholder="Nom du champ (ex: Email, IBAN)"
                        className="w-1/3 bg-slate-950 border border-slate-700 rounded-lg p-3 text-white outline-none focus:border-emerald-500 font-mono text-xs"
                        value={row.key}
                        onChange={(e) => updateMetaRow(row.id, 'key', e.target.value)}
                      />
                      <input
                        type="text"
                        placeholder="Valeur"
                        className="flex-1 bg-emerald-500/5 border-2 border-emerald-500/50 focus:border-emerald-500 text-emerald-400 rounded-lg p-3 outline-none font-mono text-sm transition-colors"
                        value={row.value}
                        onChange={(e) => updateMetaRow(row.id, 'value', e.target.value)}
                      />
                      <button
                        type="button"
                        onClick={() => removeMetaRow(row.id)}
                        className="p-3 text-slate-500 hover:text-rose-500 hover:bg-rose-500/10 rounded-lg transition-colors"
                      >
                        <Trash2 className="w-5 h-5" />
                      </button>
                    </div>
                  ))}
                </div>
              )}
            </div>

            <div>
              <button
                type="submit"
                disabled={isSubmitting}
                className="w-full bg-emerald-600 hover:bg-emerald-500 disabled:opacity-50 text-white font-bold py-4 rounded-xl transition-all flex items-center justify-center gap-3 shadow-lg shadow-emerald-600/20"
              >
                {isSubmitting ? <Loader2 className="w-6 h-6 animate-spin text-white" /> : <><Database className="w-5 h-5" /> <span>Créer la Fiche Documentaire</span></>}
              </button>
            </div>
          </form>
        )}
      </div>

      {/* 🌟 NOUVEAU : POPUP MODAL DE SUCCÈS */}
      {showSuccess && (
        <div className="fixed inset-0 bg-slate-950/80 backdrop-blur-sm flex items-center justify-center z-50 animate-in fade-in duration-200">
          <div className="bg-slate-900 border border-slate-800 p-8 rounded-[2rem] max-w-sm w-full text-center shadow-2xl shadow-blue-900/20 animate-in zoom-in-95 duration-300">
            <div className="w-20 h-20 bg-blue-500/10 text-blue-500 rounded-full flex items-center justify-center mx-auto mb-6 shadow-inner border border-blue-500/20">
              <CheckCircle2 className="w-10 h-10" />
            </div>
            <h2 className="text-2xl font-black text-white mb-2">Opération Réussie</h2>
            <p className="text-slate-400 text-sm mb-8 leading-relaxed">
              {activeTab === 'ocr' 
                ? "Le document a été transmis au moteur OCR. Il apparaîtra bientôt dans la file d'attente d'indexation."
                : "La fiche documentaire a été créée manuellement et injectée dans le circuit."}
            </p>
            <button 
              onClick={() => navigate('/bo/pending')}
              className="w-full bg-blue-600 hover:bg-blue-500 text-white font-black uppercase tracking-wider text-xs py-4 rounded-xl transition-all shadow-lg shadow-blue-600/20 active:scale-95"
            >
              Aller au Centre d'Indexation
            </button>
          </div>
        </div>
      )}
    </>
  );
};

export default ManualUpload;