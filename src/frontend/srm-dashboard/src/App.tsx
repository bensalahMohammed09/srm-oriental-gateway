import React from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { AuthProvider, useAuth } from './hooks/useAuth';

import LoginPage from './pages/LoginPage';
import DashboardHome from './pages/DashboardHome';
import PendingInbox from './pages/BO/PendingInbox';
import ManualUpload from './pages/BO/ManualUpload';
import DocumentIndexation from './pages/BO/DocumentIndexation';
import FailedQueue from './pages/BO/FailedQueue';
import RecoveryForm from './pages/BO/RecoveryForm';
import DashboardLayout from './components/DashboardLayout';

// Imports pour les Approbateurs
import ApprovalInbox from './pages/Approvals/ApprovalInbox';
import DocumentApproval from './pages/Approvals/DocumentApproval';

// 🌟 NOUVEL IMPORT : La Timeline !
import DocumentTimeline from './pages/DocumentTimeline';

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: 1,
      refetchOnWindowFocus: true,
    },
  },
});

const ProtectedRoute = ({ children, allowedRoles }: { children: React.ReactNode, allowedRoles?: string[] }) => {
  const { user, isLoading } = useAuth();
  if (isLoading) return <div className="h-screen bg-slate-950"></div>;
  if (!user) return <Navigate to="/login" replace />;

  if (allowedRoles && !allowedRoles.some(role => user.roles.includes(role))) {
    const isBO = user.roles.includes('ROLE_BO');
    const isApprover = user.roles.includes('ROLE_FINANCE') || user.roles.includes('ROLE_TECH');
    
    if (isBO) return <Navigate to="/bo/pending" replace />;
    if (isApprover) return <Navigate to="/approvals/inbox" replace />;
    return <Navigate to="/dashboard" replace />;
  }

  return <DashboardLayout title="SRM Gateway">{children}</DashboardLayout>;
};

const RootRedirect = () => {
  const { user, isLoading } = useAuth();
  if (isLoading) return null;
  if (!user) return <Navigate to="/login" replace />;
  
  if (user.roles.includes('ROLE_ADMIN') || user.roles.includes('ROLE_FINANCE') || user.roles.includes('ROLE_TECH')) {
    return user.roles.includes('ROLE_ADMIN') ? <Navigate to="/dashboard" replace /> : <Navigate to="/approvals/inbox" replace />;
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
            
            {/* ROUTES ADMIN */}
            <Route path="/dashboard" element={<ProtectedRoute allowedRoles={['ROLE_ADMIN', 'ROLE_FINANCE']}><DashboardHome /></ProtectedRoute>} />
            
            {/* ROUTES APPROBATEURS (Finance / Tech) */}
            <Route path="/approvals/inbox" element={<ProtectedRoute allowedRoles={['ROLE_FINANCE', 'ROLE_TECH', 'ROLE_ADMIN']}><ApprovalInbox /></ProtectedRoute>} />
            <Route path="/approvals/:id" element={<ProtectedRoute allowedRoles={['ROLE_FINANCE', 'ROLE_TECH', 'ROLE_ADMIN']}><DocumentApproval /></ProtectedRoute>} />
            
            {/* ROUTES BACK OFFICE */}
            <Route path="/bo/pending" element={<ProtectedRoute allowedRoles={['ROLE_BO']}><PendingInbox /></ProtectedRoute>} />
            <Route path="/bo/manual-upload" element={<ProtectedRoute allowedRoles={['ROLE_BO']}><ManualUpload /></ProtectedRoute>} />
            <Route path="/bo/indexation/:id" element={<ProtectedRoute allowedRoles={['ROLE_BO']}><DocumentIndexation /></ProtectedRoute>} />
            <Route path="/bo/failed" element={<ProtectedRoute allowedRoles={['ROLE_BO']}><FailedQueue /></ProtectedRoute>} />
            <Route path="/bo/recover/:fileName" element={<ProtectedRoute allowedRoles={['ROLE_BO']}><RecoveryForm /></ProtectedRoute>} />
            
            {/* 🌟 NOUVELLE ROUTE : Audit Trail (Accessible à tous les rôles internes) */}
            <Route path="/audit/:id" element={<ProtectedRoute allowedRoles={['ROLE_ADMIN', 'ROLE_FINANCE', 'ROLE_TECH', 'ROLE_BO']}><DocumentTimeline /></ProtectedRoute>} />

            <Route path="*" element={<Navigate to="/" replace />} />
          </Routes>
        </BrowserRouter>
      </AuthProvider>
    </QueryClientProvider>
  );
}