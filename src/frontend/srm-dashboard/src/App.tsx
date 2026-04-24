import React from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'; // <-- NOUVEAU
import { AuthProvider, useAuth } from './hooks/useAuth';

import LoginPage from './pages/LoginPage';
import DashboardHome from './pages/DashboardHome';
import PendingInbox from './pages/BO/PendingInbox';
import ManualUpload from './pages/BO/ManualUpload';
import DocumentIndexation from './pages/BO/DocumentIndexation';
import FailedQueue from './pages/BO/FailedQueue';
import RecoveryForm from './pages/BO/RecoveryForm';
import DashboardLayout from './components/DashboardLayout';

// Initialisation du client React Query (SRE Standard)
const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: 1, // 1 retry en cas d'échec réseau
      refetchOnWindowFocus: true, // Auto-refresh quand l'agent revient sur l'onglet
    },
  },
});

const ProtectedRoute = ({ children, allowedRoles }: { children: React.ReactNode, allowedRoles?: string[] }) => {
  const { user, isLoading } = useAuth();
  if (isLoading) return <div className="h-screen bg-slate-950"></div>; // Écran noir propre pendant le boot
  if (!user) return <Navigate to="/login" replace />;

  if (allowedRoles && !allowedRoles.some(role => user.roles.includes(role))) {
    const isBO = user.roles.includes('ROLE_BO');
    return <Navigate to={isBO ? "/bo/pending" : "/dashboard"} replace />;
  }

  return <DashboardLayout title="SRM Gateway">{children}</DashboardLayout>;
};

const RootRedirect = () => {
  const { user, isLoading } = useAuth();
  if (isLoading) return null;
  if (!user) return <Navigate to="/login" replace />;
  
  if (user.roles.some(r => ['ROLE_ADMIN', 'ROLE_FINANCE'].includes(r))) {
    return <Navigate to="/dashboard" replace />;
  }
  return <Navigate to="/bo/pending" replace />;
};

export default function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <BrowserRouter>
          <Routes>
            <Route path="/login" element={<LoginPage />} />
            <Route path="/" element={<RootRedirect />} />
            
            <Route path="/dashboard" element={<ProtectedRoute allowedRoles={['ROLE_ADMIN', 'ROLE_FINANCE']}><DashboardHome /></ProtectedRoute>} />
            <Route path="/bo/pending" element={<ProtectedRoute allowedRoles={['ROLE_BO']}><PendingInbox /></ProtectedRoute>} />
            <Route path="/bo/manual-upload" element={<ProtectedRoute allowedRoles={['ROLE_BO']}><ManualUpload /></ProtectedRoute>} />
            <Route path="/bo/indexation/:id" element={<ProtectedRoute allowedRoles={['ROLE_BO']}><DocumentIndexation /></ProtectedRoute>} />
            <Route path="/bo/failed" element={<ProtectedRoute allowedRoles={['ROLE_BO']}><FailedQueue /></ProtectedRoute>} />
            <Route path="/bo/recover/:fileName" element={<ProtectedRoute allowedRoles={['ROLE_BO']}><RecoveryForm /></ProtectedRoute>} />
            
            <Route path="*" element={<Navigate to="/" replace />} />
          </Routes>
        </BrowserRouter>
      </AuthProvider>
    </QueryClientProvider>
  );
}