/**
 * SRM Gateway - API Type Definitions (SRE Alignment)
 * Ces interfaces sont synchronisées avec le fichier Dtos.cs du Backend.
 * Standard de nommage : camelCase (aligné sur JsonSerializerOptions du Middleware)
 */

// --- UTILISATEURS & AUTH ---
export interface UserProfileDto {
  id: string;
  email: string;
  userName: string;
  roles: string[];
  serverTime: string; // ISO String pour synchronisation horaire
}

// --- DOCUMENTS & RÉPONSES ---
export interface DocumentResponse {
  id: string;
  reference: string;
  status: string;
  category: string | null;
  createdAt: string;
}

export interface FailedFileResponse {
  fileName: string;
  sizeKb: number;
  creationTime: string;
}

export interface Category {
  id: string;
  name: string;
}

// --- WORKFLOW & HISTORIQUE ---
export interface WorkflowStepResponse {
  stepName: string;
  action: string;
  userFullName: string | null;
  roleName: string | null;
  date: string | null;
  comment: string | null;
  isCompleted: boolean;
}

// --- VALIDATION & INDEXATION (Payloads) ---
export interface DocumentValidationRequest {
  categoryId: string;
  reference: string;
  totalAmount: number;
  metadataCorrections?: Record<string, string>; // Dictionary<string, string> en C#
}

export interface ManualRecoveryRequest {
  fileName: string;
  reference: string;
  totalAmount: number;
}

// --- TABLEAUX DE BORD (DASHBOARDS) ---
export interface CategoryDistributionDto {
  name: string;
  value: number;
}

export interface RecentActivityDto {
  reference: string;
  supplierName: string;
  status: string;
  date: string;
}

export interface DashboardStatsDto {
  totalDocuments: number;
  pendingValidation: number;
  approvedAmount: number;
  rejectedCount: number;
  distribution: CategoryDistributionDto[];
  recentActivity: RecentActivityDto[];
}

// --- ACTIONS WORKFLOW ---
export interface ApprovalRequest {
  comment?: string;
}

export interface RejectionRequest {
  reason: string;
}