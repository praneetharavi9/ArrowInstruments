export interface Enquiry {
  id: number;
  name: string;
  companyName?: string;
  email: string;
  phone?: string;
  message: string;
  isRead: boolean;
  status: string; // 'new' | 'read' | 'responded'
  submittedAt: string;
}
