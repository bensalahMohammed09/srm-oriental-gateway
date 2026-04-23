import React, { useState } from 'react';
import { useAuth } from '../hooks/useAuth';

export default function LoginPage() {
  const { login } = useAuth();
  
  // États locaux pour le formulaire
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setIsSubmitting(true);

    try {
      await login({ email, password });
      // Si réussi, App.tsx détectera le changement d'utilisateur et affichera le Dashboard
    } catch (err: any) {
      // On récupère le message "detail" formaté par notre intercepteur axios
      setError(err.userFriendlyMessage || "Identifiants invalides.");
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="flex min-h-screen items-center justify-center bg-background p-4">
      <div className="w-full max-w-md space-y-8 rounded-xl border border-slate-800 bg-slate-900/50 p-8 shadow-2xl backdrop-blur-sm">
        
        {/* Header avec Logo Placeholder (remplace par ton image plus tard) */}
        <div className="text-center">
          <div className="mx-auto flex h-20 w-20 items-center justify-center rounded-full bg-white p-2 shadow-lg">
            {/* Si tu as mis ton logo dans src/assets/logo.png, utilise <img src="/src/assets/logo.png" /> */}
            <span className="text-2xl font-bold text-srm-blue">SRM</span>
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
            <div className="rounded-md bg-destructive/10 p-3 text-sm text-destructive border border-destructive/20 animate-shake">
              {error}
            </div>
          )}

          <div className="space-y-4">
            <div>
              <label className="block text-sm font-medium text-slate-300">Email</label>
              <input
                type="email"
                required
                className="mt-1 block w-full rounded-lg border border-slate-700 bg-slate-800 px-4 py-3 text-white placeholder-slate-500 focus:border-srm-blue focus:ring-2 focus:ring-srm-blue/20 outline-none transition-all"
                placeholder="nom@srm.ma"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-slate-300">Mot de passe</label>
              <input
                type="password"
                required
                className="mt-1 block w-full rounded-lg border border-slate-700 bg-slate-800 px-4 py-3 text-white placeholder-slate-500 focus:border-srm-blue focus:ring-2 focus:ring-srm-blue/20 outline-none transition-all"
                placeholder="••••••••"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
              />
            </div>
          </div>

          <button
            type="submit"
            disabled={isSubmitting}
            className="group relative flex w-full justify-center rounded-lg bg-srm-blue px-4 py-3 text-sm font-semibold text-white hover:bg-srm-blue/90 focus:outline-none focus:ring-2 focus:ring-srm-blue/50 disabled:opacity-50 transition-all shadow-lg shadow-srm-blue/20"
          >
            {isSubmitting ? (
              <div className="h-5 w-5 border-2 border-white border-t-transparent rounded-full animate-spin"></div>
            ) : (
              "Se connecter"
            )}
          </button>
        </form>

        <p className="text-center text-xs text-slate-500 mt-8">
          Système Régional de Multiservices - Oriental SA<br/>
          © {new Date().getFullYear()} - Tous droits réservés
        </p>
      </div>
    </div>
  );
}