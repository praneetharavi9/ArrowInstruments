import { Component } from '@angular/core';
import { ContactService, ContactRequest } from '../services/contact.service';

@Component({
  selector: 'app-contact',
  standalone: false,
  templateUrl: './contact.html',
  styleUrl: './contact.css'
})
export class Contact {

  formData: ContactRequest = {
    name: '',
    companyName: '',
    email: '',
    phone: '',
    message: ''
  };

  submitting = false;
  submitted = false;
  errorMessage = '';

  // Validation error messages per field
  errors = {
    name: '',
    email: '',
    phone: ''
  };

  constructor(private contactService: ContactService) {}

  validateEmail(email: string): boolean {
    const emailRegex = /^[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}$/;
    return emailRegex.test(email);
  }

  validatePhoneNumber(phone: string): boolean {
    if (!phone.trim()) return true; // phone is optional
    // Accepts: 10 digits starting with 6-9, optionally prefixed with +91 or 0
    const phoneRegex = /^(\+91|91|0)?[6-9]\d{9}$/;
    return phoneRegex.test(phone.replace(/\s+/g, ''));
  }

  validateForm(): boolean {
    let valid = true;
    this.errors = { name: '', email: '', phone: '' };

    if (!this.formData.name.trim()) {
      this.errors.name = 'Name is required.';
      valid = false;
    }

    if (!this.formData.email.trim()) {
      this.errors.email = 'Email address is required.';
      valid = false;
    } else if (!this.validateEmail(this.formData.email)) {
      this.errors.email = 'Please enter a valid email address.';
      valid = false;
    }

    if (this.formData.phone && !this.validatePhoneNumber(this.formData.phone)) {
      this.errors.phone = 'Please enter a valid mobile number (e.g. 9876543210 or +919876543210).';
      valid = false;
    }

    return valid;
  }

  onSubmit(): void {
    this.errorMessage = '';

    if (!this.validateForm()) return;

    this.submitting = true;

    this.contactService.submitEnquiry(this.formData).subscribe({
      next: () => {
        this.submitted = true;
        this.submitting = false;
        this.formData = { name: '', companyName: '', email: '', phone: '', message: '' };
        this.errors = { name: '', email: '', phone: '' };
      },
      error: () => {
        this.errorMessage = 'Something went wrong. Please try again or call us directly.';
        this.submitting = false;
      }
    });
  }

  submitAnother(): void {
    this.submitted = false;
    this.errorMessage = '';
    this.errors = { name: '', email: '', phone: '' };
  }
}