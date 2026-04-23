import React from 'react';
import { useAuth } from '../hooks/useAuth';
import { 
  LayoutDashboard, 
  FileText, 
  ClipboardCheck, 
  Search, 
  LogOut,
  User
} from 'lucide-react';

/**
 * Sous-composant pour un élément du menu
 */
const NavItem = ({ icon: Icon, label, active, onClick }: { 
  icon: any, 
  label: string, 
  active?: boolean, 
  onClick: () => void 
}) => (
  <button
    onClick={onClick}
    className={`flex w-full items-center space-x-3 rounded-lg px-4 py-3 text-sm font-medium transition-all ${
      active 
        ? 'bg-srm-blue text-white shadow-lg shadow-srm-blue/20' 
        : 'text-slate-400 hover:bg-slate-800 hover:text-white'
    }`}
  >
    <Icon size={20} />
    <span>{label}</span>
  </button>
);

export default function Sidebar() {
  const { user, logout } = useAuth();

  // Fonction utilitaire pour vérifier les permissions
  const hasRole = (role: string) => user?.roles.includes(role);

  return (
    <div className="flex h-full w-64 flex-col border-r border-slate-800 bg-slate-950">
      {/* Zone Logo SRM */}
      <div className="flex h-20 items-center border-b border-slate-800 px-6">
        <div className="flex items-center space-x-3">
          <div className="flex h-8 w-8 items-center justify-center rounded bg-white">
             <span className="text-xs font-bold text-srm-blue uppercase">SRM</span>
          </div>
          <span className="text-lg font-bold text-white tracking-tight">Gateway</span>
        </div>
      </div>

      {/* Navigation Dynamique basée sur les Rôles */}
      <nav className="flex-1 space-y-2 p-4">
        <NavItem icon={LayoutDashboard} label="Tableau de bord" onClick={() => {}} active />
        
        {/* Visible pour les validateurs (TECH, FINANCE, etc.) */}
        <NavItem icon={ClipboardCheck} label="Mes Approbations" onClick={() => {}} />

        {/* Spécifique Agent Bureau d'Ordre */}
        {hasRole('ROLE_BO') && (
          <NavItem icon={FileText} label="Indexation OCR" onClick={() => {}} />
        )}

        {/* Spécifique Administrateur */}
        {hasRole('ROLE_ADMIN') && (
          <NavItem icon={Search} label="Audit & Recherche" onClick={() => {}} />
        )}
      </nav>

      {/* Profil Utilisateur & Déconnexion */}
      <div className="border-t border-slate-800 p-4 bg-slate-900/50">
        <div className="mb-4 flex items-center space-x-3 px-2">
          <div className="flex h-9 w-9 items-center justify-center rounded-full bg-srm-blue/20 border border-srm-blue/30 text-srm-blue">
            <User size={18} />
          </div>
          <div className="flex flex-col overflow-hidden text-left">
            <span className="truncate text-sm font-semibold text-white">{user?.userName}</span>
            <span className="truncate text-[10px] uppercase font-bold text-slate-500 tracking-wider">
              {user?.roles[0]?.replace('ROLE_', '')}
            </span>
          </div>
        </div>
        
        <button
          onClick={logout}
          className="flex w-full items-center space-x-3 rounded-lg px-4 py-2 text-sm font-medium text-red-400 hover:bg-red-400/10 transition-colors"
        >
          <LogOut size={18} />
          <span>Déconnexion</span>
        </button>
      </div>
    </div>
  );
}