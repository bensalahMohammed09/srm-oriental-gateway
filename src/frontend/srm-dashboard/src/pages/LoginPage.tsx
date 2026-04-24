import React, { useState } from 'react';
import { useNavigate, Navigate } from 'react-router-dom'; // <-- NOUVEAUX IMPORTS
import { useAuth } from '../hooks/useAuth';

export default function LoginPage() {
  const { login, user } = useAuth(); // On récupère "user" en plus
  const navigate = useNavigate(); // Hook pour naviguer
  
  // États locaux pour le formulaire
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  // ✅ SÉCURITÉ: Si l'utilisateur est DÉJÀ connecté, on le sort de la page de login
  if (user) {
    return <Navigate to="/" replace />;
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setIsSubmitting(true);

    try {
      await login({ email, password });
      // ✅ SUCCÈS: On redirige vers la racine, App.tsx fera le routage RBAC
      navigate('/', { replace: true }); 
    } catch (err: any) {
      // On récupère le message "detail" formaté par notre intercepteur axios
      setError(err.userFriendlyMessage || "Identifiants invalides.");
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="flex min-h-screen items-center justify-center bg-[#020617] p-4">
      <div className="w-full max-w-md space-y-8 rounded-xl border border-slate-800 bg-slate-900/50 p-8 shadow-2xl backdrop-blur-sm">
        
        {/* Header avec Logo Placeholder */}
        <div className="text-center">
          <div className="mx-auto flex h-20 w-20 items-center justify-center rounded-xl bg-blue-600 shadow-[0_0_20px_rgba(37,99,235,0.3)] group-hover:scale-110 transition-transform">
            <span className="text-3xl font-black text-white">S</span>
          </div>
          <h2 className="mt-6 text-3xl font-bold tracking-tight text-white">
            SRM Gateway
          </h2>
          <p className="mt-2 text-sm text-slate-400">
            Accès sécurisé au portail de gestion
          </p>
        </div>

        <form className="mt-8 space-y-6" onSubmit={handleSubmit}>
          {error && (
            <div className="rounded-md bg-rose-500/10 p-4 text-sm text-rose-400 border border-rose-500/20 animate-in fade-in slide-in-from-top-2">
              {error}
            </div>
          )}

          <div className="space-y-4">
            <div>
              <label className="block text-sm font-medium text-slate-300 mb-1.5">Email</label>
              <input
                type="email"
                required
                className="block w-full rounded-lg border border-slate-700 bg-slate-800 px-4 py-3 text-white placeholder-slate-500 focus:border-blue-500 focus:ring-2 focus:ring-blue-500/20 outline-none transition-all"
                placeholder="nom@srm.ma"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-slate-300 mb-1.5">Mot de passe</label>
              <input
                type="password"
                required
                className="block w-full rounded-lg border border-slate-700 bg-slate-800 px-4 py-3 text-white placeholder-slate-500 focus:border-blue-500 focus:ring-2 focus:ring-blue-500/20 outline-none transition-all"
                placeholder="••••••••"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
              />
            </div>
          </div>

          <button
            type="submit"
            disabled={isSubmitting}
            className="group relative flex w-full justify-center rounded-lg bg-blue-600 px-4 py-3.5 text-sm font-bold text-white hover:bg-blue-500 focus:outline-none focus:ring-2 focus:ring-blue-500/50 disabled:opacity-50 transition-all shadow-lg shadow-blue-600/20"
          >
            {isSubmitting ? (
              <div className="h-5 w-5 border-2 border-white border-t-transparent rounded-full animate-spin"></div>
            ) : (
              "Initialiser la session"
            )}
          </button>
        </form>

        <p className="text-center text-xs text-slate-500 mt-8 font-mono">
          Système Régional de Multiservices<br/>
          NODE_SECURE // {new Date().getFullYear()}
        </p>
      </div>
    </div>
  );
}