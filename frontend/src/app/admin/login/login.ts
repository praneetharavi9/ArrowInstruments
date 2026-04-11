import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { AdminAuthService } from '../services/admin-auth.service';

@Component({
  selector: 'app-admin-login',
  standalone: false,
  templateUrl: './login.html',
  styleUrl: './login.css'
})
export class AdminLoginComponent {
  email = '';
  password = '';
  loading = false;
  errorMessage = '';
  showPassword = false;

  private emailRegex = /^[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}$/;

  constructor(private authService: AdminAuthService, private router: Router) {}

  onSubmit(): void {
    if (!this.email.trim() || !this.password.trim()) {
      this.errorMessage = 'Please enter your email and password.';
      return;
    }
    if (!this.emailRegex.test(this.email.trim())) {
      this.errorMessage = 'Please enter a valid email address.';
      return;
    }
    this.loading = true;
    this.errorMessage = '';

    this.authService.login(this.email, this.password).subscribe({
      next: () => {
        this.router.navigate(['/admin/dashboard']);
      },
      error: (err) => {
        this.loading = false;
        this.errorMessage = err.status === 401
          ? 'Invalid email or password. Please try again.'
          : 'Login failed. Please try again later.';
      }
    });
  }
}
