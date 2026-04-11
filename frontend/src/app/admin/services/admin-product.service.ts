import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface AdminProduct {
  productId: number;
  productTypeId: number;
  productName: string;
  productDescription: string;
  imagePath: string;
  isActive: boolean;
  dateCreated: string;
  specs: { specId?: number; specName: string; specValue: string; displayOrder: number }[];
}

export interface ProductFormData {
  productName: string;
  productDescription: string;
  productTypeId: number;
  isActive: boolean;
}

export interface ProductSavePayload extends ProductFormData {
  specs: { specName: string; specValue: string; displayOrder: number }[];
}

@Injectable({ providedIn: 'root' })
export class AdminProductService {
  private apiUrl = `${environment.apiUrl}/products`;

  constructor(private http: HttpClient) {}

  getAll(): Observable<AdminProduct[]> {
    return this.http.get<AdminProduct[]>(this.apiUrl);
  }

  getByTypeId(typeId: number): Observable<AdminProduct[]> {
    return this.http.get<AdminProduct[]>(`${this.apiUrl}/GetProductsByProductId/${typeId}`);
  }

  create(data: ProductSavePayload): Observable<AdminProduct> {
    return this.http.post<AdminProduct>(this.apiUrl, data);
  }

  update(id: number, data: ProductSavePayload): Observable<AdminProduct> {
    return this.http.put<AdminProduct>(`${this.apiUrl}/${id}`, data);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  uploadImage(id: number, formData: FormData): Observable<AdminProduct> {
    return this.http.post<AdminProduct>(`${this.apiUrl}/${id}/image`, formData);
  }
}
