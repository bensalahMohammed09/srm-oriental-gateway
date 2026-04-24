import React from 'react';
import { DashboardStatsDto } from '../../types/api';
import { FileText, Clock, CheckCircle, AlertCircle } from 'lucide-react';

interface StatsGridProps {
  stats: DashboardStatsDto;
}

const StatsGrid: React.FC<StatsGridProps> = ({ stats }) => {
  const cards = [
    { label: 'Total Documents', value: stats.totalDocuments, icon: FileText, color: 'text-blue-400', bg: 'bg-blue-400/10' },
    { label: 'En Attente BO', value: stats.pendingValidation, icon: Clock, color: 'text-amber-400', bg: 'bg-amber-400/10' },
    { label: 'Montant Approuvé', value: `${stats.approvedAmount.toLocaleString()} DH`, icon: CheckCircle, color: 'text-emerald-400', bg: 'bg-emerald-400/10' },
    { label: 'Rejets', value: stats.rejectedCount, icon: AlertCircle, color: 'text-rose-400', bg: 'bg-rose-400/10' },
  ];

  return (
    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
      {cards.map((card, idx) => (
        <div key={idx} className="p-6 rounded-xl border border-slate-800 bg-slate-900/50 hover:bg-slate-900 transition-colors">
          <div className="flex items-center justify-between mb-4">
            <div className={`p-2 rounded-lg ${card.bg}`}>
              <card.icon className={`w-6 h-6 ${card.color}`} />
            </div>
            <span className="text-xs font-mono text-slate-500 uppercase tracking-tighter">Live</span>
          </div>
          <div className="space-y-1">
            <h3 className="text-3xl font-bold text-white tracking-tight">{card.value}</h3>
            <p className="text-sm text-slate-400 font-medium">{card.label}</p>
          </div>
        </div>
      ))}
    </div>
  );
};

export default StatsGrid;