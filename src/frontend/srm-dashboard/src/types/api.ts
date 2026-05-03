// Interfaces pour la pagination
export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
}

// 🌟 FIX : Ajout de currentApprovals
export interface DocumentResponse {
  id: string;
  reference: string;
  status: string;
  category?: string;
  createdAt: string;
  currentApprovals?: Record<string, string>; // NOUVEAU
}

export interface DocumentDetailsResponse {
  id: string;
  reference: string;
  status: string;
  category?: string;
  supplierName?: string;
  totalAmount?: number;
  sourceFile?: string;
  metadata: Record<string, any>;
  createdAt: string;
  rowVersion: string;
}

export interface FailedFileResponse {
  fileName: string;
  sizeKb: number;
  creationTime: string;
}

export interface ApprovalRequest {
  comment: string | null;
}

export interface RejectionRequest {
  reason: string;
}

export interface RecentActivityDto {
  id: string; // 🌟 AJOUTER CECI
  reference: string;
  supplierName: string;
  status: string;
  date: string;
}