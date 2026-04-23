/**
 * --- 1. PROFIL & AUTHENTIFICATION ---
 */
export interface UserProfileDto {
  id: string;
  email: string;
  userName: string;
  roles: string[];
  serverTime: string;
}

/**
 * --- 2. DOCUMENTS & RÉPONSES API ---
 */
export interface DocumentResponse {
  id: string; // Guid en C#
  reference: string;
  statusName: string;
  categoryName?: string;
  createdAt: string; // DateTime ISO string
}

export interface OcrMetadata {
  id: string;
  key: string;
  value: string;
  confidence: number;
}

/**
 * --- 3. VALIDATION & CORRECTION (BO -> API) ---
 */
export interface OcrMetadataUpdateDto {
  id: string;
  value: string;
}

export interface DocumentValidationRequest {
  categoryId: string;
  reference: string;
  totalAmount: number;
  metadataCorrections: OcrMetadataUpdateDto[];
}

/**
 * --- 4. DASHBOARD & STATISTIQUES ---
 */
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

/**
 * --- 5. WORKFLOW & ACTIONS ---
 */
export interface WorkflowActionRequest {
  comment?: string;
}

/**
 * --- 6. UPLOAD & RÉCUPÉRATION MANUELLE ---
 */
export interface ManualRecoveryRequest {
  fileName: string;
  reference: string;
  totalAmount?: number;
}

// Pour le ManualUploadRequest, on utilise souvent FormData en React 
// car il contient un fichier physique (IFormFile)