import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface ContactRequest {
  name: string;
  companyName: string;
  email: string;
  phone: string;
  message: string;
}

export interface ContactResponse {
  message: string;
}

@Injectable({
  providedIn: 'root'
})
export class ContactService {
  private apiUrl = `${environment.apiUrl}/Contact`;

  constructor(private http: HttpClient) {}

 submitEnquiry(data: ContactRequest): Observable<ContactResponse> {
  return this.http.post<ContactResponse>(this.apiUrl, data);
}
}