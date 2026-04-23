import React from 'react';
import Sidebar from './Sidebar';
import { useAuth } from '../hooks/useAuth';

interface DashboardLayoutProps {
  children: React.ReactNode;
  title?: string;
}

/**
 * DashboardLayout - Composant structurel principal
 * @param children - Le contenu spécifique de la page
 * @param title - Le titre affiché dans le header
 */
export default function DashboardLayout({ children, title = "Tableau de Bord" }: DashboardLayoutProps) {
  const { user } = useAuth();

  return (
    <div className="flex h-screen w-full bg-slate-950 overflow-hidden text-slate-200 font-sans">
      {/* Barre latérale de navigation */}
      <Sidebar />

      {/* Conteneur de droite */}
      <div className="flex flex-1 flex-col overflow-hidden">
        
        {/* Header de la page */}
        <header className="flex h-20 items-center justify-between border-b border-slate-800 bg-slate-900/40 px-8 backdrop-blur-md">
          <div className="flex flex-col">
            <h1 className="text-xl font-bold text-white tracking-tight">{title}</h1>
            <div className="flex items-center space-x-2 text-[10px] text-slate-500 uppercase tracking-widest font-bold">
              <span className="text-srm-blue">SRM Gateway</span>
              <span className="opacity-30">/</span>
              <span>Opérations SRE</span>
            </div>
          </div>
          
          <div className="flex items-center space-x-4">
            <div className="text-right hidden sm:block border-r border-slate-800 pr-4 mr-1">
              <p className="text-sm font-semibold text-white leading-tight">{user?.userName}</p>
              <p className="text-[10px] font-bold text-srm-blue uppercase tracking-tighter">
                {user?.roles[0]?.replace('ROLE_', '') || 'Utilisateur'}
              </p>
            </div>
            {/* Petit indicateur d'état du serveur */}
            <div className="h-2 w-2 rounded-full bg-emerald-500 shadow-[0_0_8px_rgba(16,185,129,0.6)]" title="Backend Connecté"></div>
          </div>
        </header>

        {/* Zone de contenu scrollable */}
        <main className="flex-1 overflow-y-auto bg-slate-950/20 p-6 md:p-10 custom-scrollbar">
          <div className="mx-auto max-w-7xl animate-in fade-in slide-in-from-bottom-4 duration-700">
            {children}
          </div>
        </main>
      </div>
    </div>
  );
}