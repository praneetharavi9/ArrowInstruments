import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface AdminProductType {
  productTypeId: number;
  productTypeName: string;
  productTypeDescription: string;
  isActive: boolean;
  dateCreated: string;
  dateUpdated?: string;
}

export interface ProductTypeFormData {
  productTypeName: string;
  productTypeDescription: string;
}

@Injectable({ providedIn: 'root' })
export class AdminProductTypeService {
  private apiUrl = `${environment.apiUrl}/producttype`;

  constructor(private http: HttpClient) {}

  getAll(): Observable<AdminProductType[]> {
    return this.http.get<AdminProductType[]>(this.apiUrl);
  }

  create(data: ProductTypeFormData): Observable<AdminProductType> {
    return this.http.post<AdminProductType>(this.apiUrl, data);
  }

  update(id: number, data: ProductTypeFormData): Observable<AdminProductType> {
    return this.http.put<AdminProductType>(`${this.apiUrl}/${id}`, data);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
