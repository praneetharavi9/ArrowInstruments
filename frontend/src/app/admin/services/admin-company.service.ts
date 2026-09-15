import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface CompanyPhone {
  phoneId?: number;
  phoneNumber: string;
  isPrimary: boolean;
}

export interface CompanyEmail {
  emailId?: number;
  emailAddress: string;
  isPrimary: boolean;
}

export interface AdminCompany {
  companyId: number;
  companyName: string;
  gstNumber: string;
  address1?: string;
  address2?: string;
  city?: string;
  state?: string;
  zipcode?: string;
  isActive: boolean;
  dateCreated: string;
  dateUpdated?: string;
  phones: CompanyPhone[];
  emails: CompanyEmail[];
}

export interface CompanySavePayload {
  companyName: string;
  gstNumber: string;
  address1?: string;
  address2?: string;
  city?: string;
  state?: string;
  zipcode?: string;
  phones: { phoneNumber: string; isPrimary: boolean }[];
  emails: { emailAddress: string; isPrimary: boolean }[];
}

@Injectable({ providedIn: 'root' })
export class AdminCompanyService {
  private apiUrl = `${environment.apiUrl}/company`;

  constructor(private http: HttpClient) {}

  getAll(): Observable<AdminCompany[]> {
    return this.http.get<AdminCompany[]>(this.apiUrl);
  }

  getById(id: number): Observable<AdminCompany> {
    return this.http.get<AdminCompany>(`${this.apiUrl}/${id}`);
  }

  create(data: CompanySavePayload): Observable<AdminCompany> {
    return this.http.post<AdminCompany>(this.apiUrl, data);
  }

  update(id: number, data: CompanySavePayload): Observable<AdminCompany> {
    return this.http.put<AdminCompany>(`${this.apiUrl}/${id}`, data);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
