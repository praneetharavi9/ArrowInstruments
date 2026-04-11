import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { Enquiry } from '../models/enquiry.model';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class AdminEnquiryService {
  private apiUrl = `${environment.apiUrl}/enquiries`;

  constructor(private http: HttpClient) {}

  getAll(): Observable<Enquiry[]> {
    return this.http.get<any[]>(this.apiUrl).pipe(
      map(items => items.map(item => ({
        ...item,
        status: item.status ?? (item.isRead ? 'read' : 'new')
      })))
    );
  }

  updateStatus(id: number, status: string): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}`, {
      status,
      isRead: status !== 'new'
    });
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
