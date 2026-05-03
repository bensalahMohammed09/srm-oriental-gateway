import React from 'react';
import { useNavigate } from 'react-router-dom';
import { RecentActivityDto } from '../../types/api';
import { Route } from 'lucide-react';
import { useAuth } from '../../hooks/useAuth'; // Assure-toi que ce chemin correspond bien à ton dossier hooks

const RecentActivityTable: React.FC<{ activities: RecentActivityDto[] }> = ({ activities }) => {
  const navigate = useNavigate();
  const { user } = useAuth(); // 🌟 On récupère l'utilisateur connecté

  // 🌟 On vérifie s'il possède le rôle Admin
  const isAdmin = user?.roles.includes('ROLE_ADMIN');

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
            <tr 
              key={idx} 
              // 🌟 Le clic ne fonctionne QUE pour l'Admin
              onClick={isAdmin ? () => navigate(`/audit/${act.id}`) : undefined}
              // 🌟 Le design s'adapte (Curseur pointer et hover stylé uniquement pour l'Admin)
              className={`border-b border-slate-800/50 transition-colors ${
                isAdmin ? 'hover:bg-slate-800/60 cursor-pointer group' : 'hover:bg-slate-800/30'
              }`}
              title={isAdmin ? "Cliquer pour voir l'Audit Trail" : undefined}
            >
              <td className={`p-4 font-mono font-medium ${isAdmin ? 'text-blue-400 group-hover:text-blue-300 transition-colors' : 'text-slate-300'}`}>
                {act.reference}
              </td>
              <td className="p-4">{act.supplierName || '---'}</td>
              <td className="p-4">
                <span className={`px-2 py-0.5 rounded-full text-[10px] font-bold uppercase ${
                  act.status === 'APPROVED' ? 'bg-emerald-500/10 text-emerald-400' : 
                  act.status === 'REJECTED' ? 'bg-rose-500/10 text-rose-400' : 'bg-amber-500/10 text-amber-400'
                }`}>
                  {act.status}
                </span>
              </td>
              <td className="p-4 text-right text-slate-500 flex items-center justify-end gap-3">
                <span>{new Date(act.date).toLocaleDateString()}</span>
                {/* 🌟 L'icône n'est insérée dans le DOM QUE pour l'Admin */}
                {isAdmin && (
                  <div className="w-6 h-6 rounded bg-blue-500/10 text-blue-500 flex items-center justify-center opacity-0 group-hover:opacity-100 transition-opacity">
                    <Route className="w-3 h-3" />
                  </div>
                )}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
};

export default RecentActivityTable;