import { Component, OnInit } from '@angular/core';
import { AdminAuthService } from '../services/admin-auth.service';
import { ToastService, Toast } from '../services/toast.service';

@Component({
  selector: 'app-admin-layout',
  standalone: false,
  templateUrl: './admin-layout.html',
  styleUrl: './admin-layout.css'
})
export class AdminLayoutComponent implements OnInit {
  toasts: Toast[] = [];
  sidebarCollapsed = false;

  constructor(
    private authService: AdminAuthService,
    private toastService: ToastService
  ) {}

  ngOnInit(): void {
    this.toastService.toasts$.subscribe(toasts => {
      this.toasts = toasts;
    });

    // Server-side token validation on shell load.
    // If the token is expired or revoked, the service clears it and
    // redirects to /admin/login automatically.
    this.authService.verifyToken().subscribe();
  }

  logout(): void {
    this.authService.logout();
  }

  dismissToast(id: number): void {
    this.toastService.remove(id);
  }
}
