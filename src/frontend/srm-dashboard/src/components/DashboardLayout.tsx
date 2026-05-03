import React, { useState } from 'react';
import { useAuth } from '../hooks/useAuth';
import { useNavigate, useLocation } from 'react-router-dom';
import { 
  LayoutDashboard, 
  Inbox, 
  UploadCloud, 
  AlertCircle, 
  LogOut, 
  Menu,
  X,
  User as UserIcon,
  ChevronRight,
  CheckSquare // <-- Nouvel import pour l'icône Validations
} from 'lucide-react';

interface DashboardLayoutProps {
  children: React.ReactNode;
  title: string;
}

const DashboardLayout: React.FC<DashboardLayoutProps> = ({ children, title }) => {
  const { user, logout } = useAuth();
  const [isMobileOpen, setIsMobileOpen] = useState(false);
  const navigate = useNavigate();
  const location = useLocation();

  const navItems = [
    { 
      label: 'Tableau de bord', 
      icon: LayoutDashboard, 
      path: '/dashboard', 
      roles: ['ROLE_ADMIN', 'ROLE_FINANCE'] 
    },
    // 🌟 NOUVEAU LIEN : VALIDATIONS
    { 
      label: 'Validations en attente', 
      icon: CheckSquare, 
      path: '/approvals/inbox', 
      roles: ['ROLE_FINANCE', 'ROLE_TECH', 'ROLE_ADMIN'] 
    },
    { 
      label: 'Flux à Indexer', 
      icon: Inbox, 
      path: '/bo/pending', 
      roles: ['ROLE_BO'] 
    },
    { 
      label: 'Saisie Manuelle', 
      icon: UploadCloud, 
      path: '/bo/manual-upload', 
      roles: ['ROLE_BO'] 
    },
    { 
      label: 'Échecs Système', 
      icon: AlertCircle, 
      path: '/bo/failed', 
      roles: ['ROLE_BO'] 
    }
  ];

  const menuItems = navItems.filter(item => 
    item.roles.some(role => user?.roles.includes(role))
  );

  const handleNav = (path: string) => {
    navigate(path);
    setIsMobileOpen(false);
  };

  return (
    <div className="flex h-screen bg-[#020617] text-slate-200 overflow-hidden font-sans">
      <aside className="hidden lg:flex w-72 flex-col border-r border-slate-800/60 bg-[#020617] z-30">
        <div className="p-8">
          <div className="flex items-center gap-3 group cursor-pointer">
            <div className="w-10 h-10 bg-blue-600 rounded-xl flex items-center justify-center shadow-[0_0_20px_rgba(37,99,235,0.3)] group-hover:scale-110 transition-transform">
              <span className="text-white font-black text-xl">S</span>
            </div>
            <div className="flex flex-col">
              <span className="font-bold text-white tracking-tight text-lg">SRM Gateway</span>
              <span className="text-[10px] text-blue-500 font-mono font-bold tracking-[0.2em] uppercase">Operations SRE</span>
            </div>
          </div>
        </div>

        <nav className="flex-1 px-4 space-y-1.5">
          {menuItems.map((item) => {
            const isActive = location.pathname.startsWith(item.path);
            return (
              <button
                key={item.path}
                onClick={() => handleNav(item.path)}
                className={`w-full flex items-center justify-between px-4 py-3.5 rounded-xl transition-all duration-300 group ${
                  isActive 
                  ? 'bg-blue-600/10 text-blue-400 border border-blue-500/20' 
                  : 'text-slate-500 hover:bg-slate-800/40 hover:text-slate-200'
                }`}
              >
                <div className="flex items-center gap-3">
                  <item.icon className={`w-5 h-5 ${isActive ? 'text-blue-400' : 'group-hover:text-blue-400 transition-colors'}`} />
                  <span className="font-semibold text-sm tracking-tight">{item.label}</span>
                </div>
                {isActive && <ChevronRight className="w-4 h-4" />}
              </button>
            );
          })}
        </nav>

        <div className="p-6 border-t border-slate-800/60">
          <div className="flex items-center gap-4 mb-6 px-2">
            <div className="w-10 h-10 rounded-full bg-slate-800 border border-slate-700 flex items-center justify-center text-blue-400 shadow-inner">
              <UserIcon className="w-5 h-5" />
            </div>
            <div className="flex flex-col overflow-hidden">
              <span className="text-sm font-bold text-white truncate">{user?.userName}</span>
              <span className="text-[10px] font-mono text-blue-500 font-bold uppercase">{user?.roles[0]}</span>
            </div>
          </div>
          <button 
            onClick={logout}
            className="w-full flex items-center gap-3 px-4 py-3 rounded-xl text-slate-500 hover:text-rose-400 hover:bg-rose-500/5 transition-all group font-bold text-sm"
          >
            <LogOut className="w-5 h-5 group-hover:-translate-x-1 transition-transform" /> 
            Déconnexion
          </button>
        </div>
      </aside>

      <main className="flex-1 flex flex-col min-w-0 bg-[#020617] relative">
        <header className="h-20 border-b border-slate-800/60 bg-[#020617]/80 backdrop-blur-xl flex items-center justify-between px-8 z-20">
          <div className="flex items-center gap-4">
            <button onClick={() => setIsMobileOpen(true)} className="lg:hidden p-2 text-slate-400">
              <Menu className="w-6 h-6" />
            </button>
            <div className="flex flex-col">
              <h1 className="text-xl font-bold text-white tracking-tight">{title}</h1>
              <div className="flex items-center gap-2 text-[10px] font-mono text-slate-500 uppercase tracking-widest mt-1">
                <span className="text-blue-500">SRM Gateway</span>
                <span className="text-slate-700">/</span>
                <span>{location.pathname.replace('/', '').replace('/', ' ')}</span>
              </div>
            </div>
          </div>
          
          <div className="hidden sm:flex items-center gap-3">
             <div className="flex items-center gap-2 px-3 py-1.5 bg-emerald-500/5 border border-emerald-500/20 rounded-full">
                <div className="w-1.5 h-1.5 rounded-full bg-emerald-500 animate-pulse" />
                <span className="text-[10px] font-mono text-emerald-500 font-bold uppercase">Node_Connected</span>
             </div>
          </div>
        </header>

        <div className="flex-1 overflow-y-auto custom-scrollbar p-8">
          <div className="max-w-[1600px] mx-auto">
            {children}
          </div>
        </div>
      </main>

      {isMobileOpen && (
        <div className="fixed inset-0 z-[100] lg:hidden">
          <div className="absolute inset-0 bg-black/60 backdrop-blur-sm" onClick={() => setIsMobileOpen(false)} />
          <div className="absolute inset-y-0 left-0 w-80 bg-[#020617] border-r border-slate-800 p-8 flex flex-col shadow-2xl animate-in slide-in-from-left duration-300">
             <div className="flex justify-between items-center mb-10">
               <span className="font-bold text-white text-xl">Menu</span>
               <button onClick={() => setIsMobileOpen(false)} className="p-2 bg-slate-900 rounded-lg"><X className="w-5 h-5 text-slate-400" /></button>
             </div>
             <nav className="flex-1 space-y-4">
               {menuItems.map((item) => (
                 <button 
                   key={item.path} 
                   onClick={() => handleNav(item.path)}
                   className="w-full flex items-center gap-4 text-slate-400 hover:text-white p-3 font-semibold"
                 >
                   <item.icon className="w-6 h-6" /> {item.label}
                 </button>
               ))}
             </nav>
          </div>
        </div>
      )}
    </div>
  );
};

export default DashboardLayout;