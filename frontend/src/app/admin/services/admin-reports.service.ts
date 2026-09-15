import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface CustomerSummaryRow {
  companyId: number;
  companyName: string;
  open: number;
  debit: number;
  credit: number;
  balance: number;
}

export interface CustomerSummaryResponse {
  startDate: string;
  endDate: string;
  rows: CustomerSummaryRow[];
}

export interface CustomerLedgerRow {
  entryId: number;
  date: string;
  description: string | null;
  type: string | null;
  transNo: string | null;
  open: number;
  debit: number;
  credit: number;
  balance: number;
}

export interface CustomerLedgerResponse {
  companyId: number;
  companyName: string;
  startDate: string;
  endDate: string;
  openingBalance: number;
  closingBalance: number;
  rows: CustomerLedgerRow[];
}

export interface LedgerEntryPayload {
  entryDate: string;
  description: string;
  type: string;
  transNo: string;
  debit: number;
  credit: number;
}

export interface OpeningBalanceResponse {
  companyId: number;
  openingBalance: number;
}

@Injectable({ providedIn: 'root' })
export class AdminReportsService {
  private apiUrl = `${environment.apiUrl}/reports`;

  constructor(private http: HttpClient) {}

  getCustomerSummary(startDate: string, endDate: string): Observable<CustomerSummaryResponse> {
    const params = new HttpParams().set('startDate', startDate).set('endDate', endDate);
    return this.http.get<CustomerSummaryResponse>(`${this.apiUrl}/customers`, { params });
  }

  getCustomerLedger(companyId: number, startDate: string, endDate: string): Observable<CustomerLedgerResponse> {
    const params = new HttpParams().set('startDate', startDate).set('endDate', endDate);
    return this.http.get<CustomerLedgerResponse>(`${this.apiUrl}/customers/${companyId}`, { params });
  }

  addEntry(companyId: number, payload: LedgerEntryPayload): Observable<CustomerLedgerRow> {
    return this.http.post<CustomerLedgerRow>(`${this.apiUrl}/customers/${companyId}/entries`, payload);
  }

  updateEntry(companyId: number, entryId: number, payload: LedgerEntryPayload): Observable<CustomerLedgerRow> {
    return this.http.put<CustomerLedgerRow>(`${this.apiUrl}/customers/${companyId}/entries/${entryId}`, payload);
  }

  deleteEntry(companyId: number, entryId: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/customers/${companyId}/entries/${entryId}`);
  }

  addOpeningBalance(companyId: number, amount: number): Observable<OpeningBalanceResponse> {
    return this.http.post<OpeningBalanceResponse>(`${this.apiUrl}/customers/${companyId}/opening-balance`, { amount });
  }

  setOpeningBalance(companyId: number, amount: number): Observable<OpeningBalanceResponse> {
    return this.http.put<OpeningBalanceResponse>(`${this.apiUrl}/customers/${companyId}/opening-balance`, { amount });
  }
}
