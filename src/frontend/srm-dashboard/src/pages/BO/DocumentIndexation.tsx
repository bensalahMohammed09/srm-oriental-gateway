import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import api from '../../api/axios';
import DocumentViewer from '../../components/documents/DocumentViewer';
import { 
  Loader2, 
  AlertCircle, 
  Save, 
  XCircle, 
  ChevronRight,
  Database,
  Info
} from 'lucide-react';

interface Category {
  id: string;
  name: string;
}

/**
 * Interface d'indexation pour le Bureau d'Ordre.
 * Aligne le flux binaire (viewer) avec la validation métier (formulaire).
 */
const DocumentIndexation: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [categories, setCategories] = useState<Category[]>([]);
  const [error, setError] = useState<string | null>(null);

  // Payload aligné sur DocumentValidationRequest (C#)
  const [formData, setFormData] = useState({
    reference: '',
    totalAmount: 0,
    categoryId: '',
    metadataCorrections: {} as Record<string, string>
  });

  // ✅ URL dynamique pour le stream binaire
  const currentStreamUrl = `http://localhost:5050/api/v1/document/${id}/view`;

  useEffect(() => {
    const fetchData = async () => {
      if (!id) return;
      setLoading(true);
      try {
        // Chargement simultané des référentiels et du document
        const [catRes, docRes] = await Promise.all([
          api.get<Category[]>('/api/v1/category'),
          api.get(`/api/v1/document/${id}`)
        ]);

        setCategories(catRes.data);
        const doc = docRes.data;

        setFormData({
          reference: doc.reference || '',
          totalAmount: doc.totalAmount || 0,
          categoryId: doc.categoryId || '',
          metadataCorrections: {} // On initialise les corrections à vide
        });
      } catch (err: any) {
        setError(err.response?.data?.detail || "Impossible de monter le plan de travail documentaire.");
      } finally {
        setLoading(false);
      }
    };
    fetchData();
  }, [id]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!id) return;

    setSubmitting(true);
    try {
      await api.post(`/api/v1/document/${id}/confirm-indexation`, formData);
      // On retourne à la liste après succès
      navigate('/bo/pending');
    } catch (err: any) {
      setError(err.response?.data?.detail || "Erreur lors de la validation du workflow.");
    } finally {
      setSubmitting(false);
    }
  };

  if (loading) {
    return (
      <div className="flex flex-col items-center justify-center h-96 text-slate-400">
        <Loader2 className="w-10 h-10 animate-spin text-blue-500 mb-4" />
        <p className="font-mono text-[10px] uppercase tracking-widest animate-pulse">Handshaking with stream node...</p>
      </div>
    );
  }

  return (
    <div className="flex flex-col lg:flex-row gap-6 h-[calc(100vh-140px)] animate-in fade-in duration-500">
      {/* SECTION GAUCHE : Visualisation du flux binaire */}
      <div className="lg:w-1/2 flex flex-col h-full overflow-hidden">
        <div className="mb-3 flex items-center justify-between px-1">
          <div className="flex items-center gap-2">
            <ChevronRight className="w-4 h-4 text-blue-500" />
            <h2 className="text-sm font-bold text-white uppercase tracking-tighter">Source Binaire (Inline)</h2>
          </div>
          <span className="text-[9px] font-mono text-slate-500 bg-slate-900 border border-slate-800 px-2 py-0.5 rounded">
            ID: {id?.substring(0, 8)}
          </span>
        </div>
        
        {/* ✅ FIX: Utilisation correcte de streamUrl comme requis par le nouveau DocumentViewer */}
        <DocumentViewer 
          streamUrl={currentStreamUrl} 
          title="Validation Courrier"
          className="flex-1" 
        />
      </div>

      {/* SECTION DROITE : Indexation métier */}
      <div className="lg:w-1/2 flex flex-col h-full overflow-y-auto pr-2 custom-scrollbar">
        <div className="mb-3 px-1 flex items-center gap-2">
          <Database className="w-4 h-4 text-emerald-500" />
          <h2 className="text-sm font-bold text-white uppercase tracking-tighter">Indexation & Validation Workflow</h2>
        </div>

        <form onSubmit={handleSubmit} className="bg-slate-900/40 border border-slate-800 rounded-2xl p-6 space-y-6 backdrop-blur-md">
          {error && (
            <div className="p-4 bg-rose-500/10 border border-rose-500/20 rounded-lg text-rose-400 text-xs flex items-center gap-3">
              <AlertCircle className="w-5 h-5 shrink-0" />
              <p>{error}</p>
            </div>
          )}

          <div className="grid grid-cols-1 gap-5">
            <div className="space-y-2">
              <label className="text-[10px] font-mono text-slate-500 uppercase tracking-widest">Référence du Courrier</label>
              <input
                required
                type="text"
                className="w-full bg-slate-950 border border-slate-800 rounded-xl p-4 text-white focus:border-blue-500 focus:ring-1 focus:ring-blue-500/20 outline-none transition-all font-mono text-sm"
                value={formData.reference}
                onChange={(e) => setFormData({...formData, reference: e.target.value})}
              />
            </div>

            <div className="space-y-2">
              <label className="text-[10px] font-mono text-slate-500 uppercase tracking-widest">Catégorie / Affectation</label>
              <select
                required
                className="w-full bg-slate-950 border border-slate-800 rounded-xl p-4 text-white focus:border-blue-500 outline-none appearance-none cursor-pointer text-sm"
                value={formData.categoryId}
                onChange={(e) => setFormData({...formData, categoryId: e.target.value})}
              >
                <option value="">Sélectionner la destination...</option>
                {categories.map(cat => (
                  <option key={cat.id} value={cat.id}>{cat.name}</option>
                ))}
              </select>
            </div>

            <div className="space-y-2">
              <label className="text-[10px] font-mono text-slate-500 uppercase tracking-widest">Montant Total Identifié</label>
              <div className="relative">
                <input
                  required
                  type="number"
                  step="0.01"
                  className="w-full bg-slate-950 border border-slate-800 rounded-xl p-4 text-white focus:border-blue-500 outline-none font-mono text-sm"
                  value={formData.totalAmount}
                  onChange={(e) => setFormData({...formData, totalAmount: parseFloat(e.target.value)})}
                />
                <span className="absolute right-4 top-1/2 -translate-y-1/2 text-slate-600 font-mono text-xs italic">DH</span>
              </div>
            </div>
          </div>

          <div className="pt-6 border-t border-slate-800 flex items-center gap-3">
            <button
              type="submit"
              disabled={submitting}
              className="flex-1 bg-emerald-600 hover:bg-emerald-500 disabled:opacity-50 text-white font-bold py-4 rounded-xl transition-all flex items-center justify-center gap-3 shadow-lg shadow-emerald-600/10"
            >
              {submitting ? <Loader2 className="w-5 h-5 animate-spin" /> : <Save className="w-5 h-5" />}
              <span>Valider l'Indexation</span>
            </button>
            
            <button
              type="button"
              onClick={() => navigate('/bo/pending')}
              className="px-6 py-4 border border-slate-800 text-slate-400 hover:text-white hover:bg-slate-800 rounded-xl transition-all flex items-center gap-2"
            >
              <XCircle className="w-5 h-5" />
            </button>
          </div>
        </form>

        <div className="mt-4 p-4 bg-blue-500/5 border border-blue-500/10 rounded-xl flex gap-3">
          <Info className="w-4 h-4 text-blue-400 shrink-0 mt-0.5" />
          <p className="text-[10px] text-slate-500 leading-relaxed font-mono">
            SRE ALERT: La validation déclenchera le circuit d'approbation spécifique à la catégorie sélectionnée.
          </p>
        </div>
      </div>
    </div>
  );
};

export default DocumentIndexation;