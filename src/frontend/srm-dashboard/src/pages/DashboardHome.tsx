import React, { useEffect, useState, useCallback } from 'react';
import api from '../api/axios';
import { useAuth } from '../hooks/useAuth';
import { DashboardStatsDto } from '../types/api';
import StatsGrid from '../components/dashboard/StatsGrid';
import RecentActivityTable from '../components/dashboard/RecentActivityTable';
import { Loader2, AlertTriangle, RefreshCcw, ShieldCheck } from 'lucide-react';

const DashboardHome: React.FC = () => {
  const { user } = useAuth();
  const [stats, setStats] = useState<DashboardStatsDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [lastUpdated, setLastUpdated] = useState<Date>(new Date());

  // Vérification RBAC pour l'accès aux stats globales
  const canSeeStats = user?.roles.some(r => ['ROLE_ADMIN', 'ROLE_FINANCE'].includes(r));

  const fetchStats = useCallback(async (isAuto = false) => {
    if (!canSeeStats) return;
    
    if (isAuto) setRefreshing(true);
    else setLoading(true);

    try {
      const response = await api.get<DashboardStatsDto>('/api/v1/profile/stats');
      setStats(response.data);
      setLastUpdated(new Date());
      setError(null);
    } catch (err: any) {
      setError(err.response?.data?.detail || "Erreur lors de la synchronisation des métriques.");
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }, [canSeeStats]);

  useEffect(() => {
    fetchStats();

    // 🕒 AUTO-REFRESH SRE : On rafraîchit toutes les 30 secondes si l'onglet est actif
    const interval = setInterval(() => {
      if (document.visibilityState === 'visible') {
        fetchStats(true);
      }
    }, 30000);

    return () => clearInterval(interval);
  }, [fetchStats]);

  if (!canSeeStats) {
    return (
      <div className="flex flex-col items-center justify-center h-96 text-slate-500 animate-in fade-in zoom-in duration-300">
        <ShieldCheck className="w-16 h-16 mb-4 opacity-20 text-blue-500" />
        <h2 className="text-xl font-bold text-white">Accès Restreint</h2>
        <p className="max-w-md text-center mt-2">
          Votre profil (<span className="text-blue-400 font-mono">{user?.roles.join(', ')}</span>) ne dispose pas des privilèges 
          nécessaires pour consulter les statistiques globales de performance.
        </p>
      </div>
    );
  }

  if (loading && !stats) {
    return (
      <div className="flex flex-col items-center justify-center h-64 text-slate-400">
        <Loader2 className="w-8 h-8 animate-spin mb-2 text-blue-500" />
        <p className="text-sm font-mono uppercase tracking-widest">Initialisation de la stack de monitoring...</p>
      </div>
    );
  }

  return (
    <div className="space-y-8 animate-in fade-in duration-500">
      <div className="flex flex-col md:flex-row md:items-end justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-white tracking-tight">Tableau de Bord Stratégique</h1>
          <p className="text-slate-400 text-sm">Vision consolidée du flux financier et documentaire SRM.</p>
        </div>
        
        <div className="flex items-center gap-4">
          <div className="text-right">
            <p className="text-[10px] uppercase font-mono text-slate-500">Dernière synchro</p>
            <p className="text-xs font-mono text-slate-300">{lastUpdated.toLocaleTimeString()}</p>
          </div>
          <button 
            onClick={() => fetchStats(true)}
            disabled={refreshing}
            className={`p-2.5 rounded-lg border border-slate-700 bg-slate-800 hover:bg-slate-700 transition-all ${refreshing ? 'opacity-50 cursor-not-allowed' : ''}`}
            title="Rafraîchir les données"
          >
            <RefreshCcw className={`w-5 h-5 text-blue-400 ${refreshing ? 'animate-spin' : ''}`} />
          </button>
        </div>
      </div>

      {error && (
        <div className="p-4 rounded-lg border border-rose-500/20 bg-rose-500/5 text-rose-400 flex items-center gap-3 text-sm">
          <AlertTriangle className="w-5 h-5 shrink-0" />
          <p>{error}</p>
        </div>
      )}

      {stats && (
        <>
          <StatsGrid stats={stats} />
          
          <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
            <div className="lg:col-span-2">
              <RecentActivityTable activities={stats.recentActivity} />
            </div>
            
            <div className="p-8 rounded-xl border border-slate-800 bg-slate-900/50 flex flex-col justify-center items-center text-center">
              <div className="relative w-24 h-24 mb-6">
                <div className="absolute inset-0 rounded-full border-4 border-slate-800"></div>
                <div className="absolute inset-0 rounded-full border-4 border-t-blue-500 border-r-blue-400 animate-[spin_3s_linear_infinite]"></div>
                <div className="absolute inset-0 flex items-center justify-center font-mono text-xs text-blue-400">
                  {stats.distribution.length} CAT
                </div>
              </div>
              <h4 className="text-white font-semibold mb-2">Répartition Catégories</h4>
              <p className="text-xs text-slate-500 leading-relaxed font-mono">
                {stats.distribution.map(d => `${d.name}: ${d.value}`).join(' | ')}
              </p>
            </div>
          </div>
        </>
      )}
    </div>
  );
};

export default DashboardHome;