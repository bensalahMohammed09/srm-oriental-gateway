import React from 'react';
import { AuthProvider, useAuth } from './hooks/useAuth';
import LoginPage from './pages/LoginPage';
import DashboardLayout from './components/DashboardLayout';

/**
 * AppContent - Gère l'affichage conditionnel (Login vs Dashboard)
 */
const AppContent = () => {
  const { user, isLoading } = useAuth();

  // 1. Écran d'attente pendant que l'API vérifie le cookie /me
  if (isLoading) {
    return (
      <div className="flex flex-col items-center justify-center h-screen bg-slate-950 text-white">
        <div className="w-10 h-10 border-4 border-srm-blue border-t-transparent rounded-full animate-spin mb-4"></div>
        <p className="text-slate-400 animate-pulse text-sm font-medium tracking-widest uppercase">SRM Gateway</p>
      </div>
    );
  }

  // 2. Si l'utilisateur n'est pas reconnu -> Page de Connexion
  if (!user) {
    return <LoginPage />;
  }

  // 3. Si l'utilisateur est connecté -> Dashboard complet
  return (
    <DashboardLayout title="Vue d'ensemble">
      <div className="space-y-6">
        <div className="rounded-xl border border-slate-800 bg-slate-900/50 p-8 shadow-sm">
          <h2 className="text-2xl font-bold text-white tracking-tight">
            Bienvenue, <span className="text-srm-blue">{user.userName}</span>
          </h2>
          <p className="mt-2 text-slate-400">
            Vous êtes connecté au portail SRM Gateway avec le profil : 
            <span className="ml-2 font-mono text-xs bg-slate-800 px-2 py-1 rounded text-srm-blue border border-slate-700">
              {user.roles.join(' / ')}
            </span>
          </p>
        </div>

        {/* Grille de stats vide pour l'instant */}
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
          {[1, 2, 3].map((i) => (
            <div key={i} className="h-32 rounded-xl border border-slate-800 bg-slate-900/30 animate-pulse"></div>
          ))}
        </div>
      </div>
    </DashboardLayout>
  );
};

export default function App() {
  return (
    <AuthProvider>
      <AppContent />
    </AuthProvider>
  );
}