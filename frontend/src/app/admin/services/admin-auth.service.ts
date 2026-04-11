import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap, map, catchError, of } from 'rxjs';
import { Router } from '@angular/router';
import { environment } from '../../../environments/environment';

interface LoginResponse {
  success: boolean;
  data: {
    token: string;
    user: { id: number; email: string; firstName: string; lastName: string; role: string };
  };
}

interface VerifyResponse {
  success: boolean;
  data: { id: string; email: string; firstName: string; lastName: string; role: string };
}

@Injectable({ providedIn: 'root' })
export class AdminAuthService {
  private apiUrl = `${environment.apiUrl}/auth/admin`;

  constructor(private http: HttpClient, private router: Router) {}

  login(email: string, password: string): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.apiUrl}/login`, { email, password }).pipe(
      tap(res => {
        if (res.success) {
          localStorage.setItem('admin_token', res.data.token);
        }
      })
    );
  }

  /**
   * Validates the stored token against the server.
   * Returns true if valid, false otherwise.
   * On failure clears the token and redirects to login.
   */
  verifyToken(): Observable<boolean> {
    const token = localStorage.getItem('admin_token');
    if (!token) {
      return of(false);
    }
    return this.http.get<VerifyResponse>(`${this.apiUrl}/verify`).pipe(
      map(res => res.success === true),
      catchError(() => {
        this.clearToken();
        this.router.navigate(['/admin/login']);
        return of(false);
      })
    );
  }

  logout(): void {
    this.clearToken();
    this.router.navigate(['/admin/login']);
  }

  isLoggedIn(): boolean {
    const token = localStorage.getItem('admin_token');
    if (!token) return false;
    try {
      const parts = token.split('.');
      if (parts.length !== 3) return true;
      const payload = JSON.parse(atob(parts[1]));
      if (payload.exp) {
        return payload.exp * 1000 > Date.now();
      }
      return true;
    } catch {
      return !!token;
    }
  }

  private clearToken(): void {
    localStorage.removeItem('admin_token');
  }
}
