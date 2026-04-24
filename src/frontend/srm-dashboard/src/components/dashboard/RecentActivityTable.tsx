import React from 'react';
import { RecentActivityDto } from '../../types/api';

const RecentActivityTable: React.FC<{ activities: RecentActivityDto[] }> = ({ activities }) => {
  return (
    <div className="rounded-xl border border-slate-800 bg-slate-900/50 overflow-hidden">
      <div className="p-4 border-b border-slate-800 bg-slate-900/80">
        <h3 className="font-semibold text-white">Flux Documentaire Récent</h3>
      </div>
      <table className="w-full text-left border-collapse">
        <thead>
          <tr className="text-xs uppercase text-slate-500 bg-slate-900/30 font-mono">
            <th className="p-4 border-b border-slate-800">Référence</th>
            <th className="p-4 border-b border-slate-800">Fournisseur</th>
            <th className="p-4 border-b border-slate-800">Statut</th>
            <th className="p-4 border-b border-slate-800 text-right">Date</th>
          </tr>
        </thead>
        <tbody className="text-sm text-slate-300">
          {activities.map((act, idx) => (
            <tr key={idx} className="hover:bg-slate-800/30 transition-colors border-b border-slate-800/50">
              <td className="p-4 font-mono font-medium text-blue-400">{act.reference}</td>
              <td className="p-4">{act.supplierName || '---'}</td>
              <td className="p-4">
                <span className={`px-2 py-0.5 rounded-full text-[10px] font-bold uppercase ${
                  act.status === 'APPROVED' ? 'bg-emerald-500/10 text-emerald-400' : 
                  act.status === 'REJECTED' ? 'bg-rose-500/10 text-rose-400' : 'bg-amber-500/10 text-amber-400'
                }`}>
                  {act.status}
                </span>
              </td>
              <td className="p-4 text-right text-slate-500">{new Date(act.date).toLocaleDateString()}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
};

export default RecentActivityTable;