import { Component, OnInit } from '@angular/core';
import { AdminEnquiryService } from '../services/admin-enquiry.service';
import { Enquiry } from '../models/enquiry.model';
import { ToastService } from '../services/toast.service';

@Component({
  selector: 'app-admin-enquiries',
  standalone: false,
  templateUrl: './enquiries.html',
  styleUrl: './enquiries.css'
})
export class EnquiriesComponent implements OnInit {
  enquiries: Enquiry[] = [];
  loading = true;

  // Detail modal
  showDetailModal = false;
  selectedEnquiry: Enquiry | null = null;

  // Delete confirm
  showDeleteConfirm = false;
  deletingId: number | null = null;
  deleteLoading = false;

  constructor(
    private enquiryService: AdminEnquiryService,
    private toastService: ToastService
  ) {}

  ngOnInit(): void {
    this.loadEnquiries();
  }

  loadEnquiries(): void {
    this.loading = true;
    this.enquiryService.getAll().subscribe({
      next: enquiries => {
        this.enquiries = enquiries;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.toastService.show('Failed to load enquiries.', 'danger');
      }
    });
  }

  viewEnquiry(enquiry: Enquiry): void {
    this.selectedEnquiry = enquiry;
    this.showDetailModal = true;
    if (enquiry.status === 'new') {
      this.updateStatus(enquiry, 'read');
    }
  }

  closeDetailModal(): void {
    this.showDetailModal = false;
    this.selectedEnquiry = null;
  }

  statusBadgeClass(status: string): string {
    switch (status) {
      case 'new':       return 'badge bg-primary';
      case 'read':      return 'badge bg-secondary';
      case 'responded': return 'badge bg-success';
      default:          return 'badge bg-secondary';
    }
  }

  statusLabel(status: string): string {
    switch (status) {
      case 'new':       return 'New';
      case 'read':      return 'Read';
      case 'responded': return 'Responded';
      default:          return status;
    }
  }

  markResponded(enquiry: Enquiry): void {
    this.updateStatus(enquiry, 'responded');
    if (this.showDetailModal) {
      this.closeDetailModal();
    }
  }

  private updateStatus(enquiry: Enquiry, status: string): void {
    this.enquiryService.updateStatus(enquiry.id, status).subscribe({
      next: () => {
        enquiry.status = status;
        if (this.selectedEnquiry?.id === enquiry.id) {
          this.selectedEnquiry.status = status;
        }
        if (status === 'responded') {
          this.toastService.show('Marked as Responded.', 'success');
        }
      },
      error: () => {
        this.toastService.show('Failed to update status.', 'danger');
      }
    });
  }

  confirmDelete(id: number): void {
    this.deletingId = id;
    this.showDeleteConfirm = true;
  }

  cancelDelete(): void {
    this.showDeleteConfirm = false;
    this.deletingId = null;
  }

  doDelete(): void {
    if (!this.deletingId) return;
    this.deleteLoading = true;
    this.enquiryService.delete(this.deletingId).subscribe({
      next: () => {
        this.deleteLoading = false;
        this.showDeleteConfirm = false;
        this.toastService.show('Enquiry deleted.', 'success');
        this.loadEnquiries();
      },
      error: () => {
        this.deleteLoading = false;
        this.toastService.show('Delete failed. Please try again.', 'danger');
      }
    });
  }
}
