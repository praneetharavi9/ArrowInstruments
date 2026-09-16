import { Component, OnInit } from '@angular/core';
import { AdminCompanyService, AdminCompany, CompanySavePayload } from '../services/admin-company.service';
import { ToastService } from '../services/toast.service';

interface PhoneRow {
  phoneNumber: string;
  isPrimary: boolean;
}

interface EmailRow {
  emailAddress: string;
  isPrimary: boolean;
}

type CompanySortColumn = 'companyId' | 'companyName' | 'phone' | 'email' | 'gstNumber' | 'city';

@Component({
  selector: 'app-companies',
  standalone: false,
  templateUrl: './companies.html',
  styleUrl: './companies.css'
})
export class Companies implements OnInit {
  companies: AdminCompany[] = [];
  loading = true;
  sortColumn: CompanySortColumn = 'companyName';
  sortDirection: 'asc' | 'desc' = 'asc';

  // Modal state
  showModal = false;
  modalMode: 'add' | 'edit' = 'add';
  editingId: number | null = null;
  formLoading = false;
  formError = '';

  formCompanyName = '';
  formGstNumber = '';
  formAddress1 = '';
  formAddress2 = '';
  formCity = '';
  formState = '';
  formZipcode = '';
  phoneRows: PhoneRow[] = [];
  emailRows: EmailRow[] = [];

  // Delete confirm
  showDeleteConfirm = false;
  deletingId: number | null = null;
  deletingName = '';
  deleteLoading = false;

  constructor(
    private companyService: AdminCompanyService,
    private toastService: ToastService
  ) {}

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    this.loading = true;
    this.companyService.getAll().subscribe({
      next: (companies) => {
        this.companies = companies;
        this.applySort();
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.toastService.show('Failed to load companies.', 'danger');
      }
    });
  }

  sortBy(column: CompanySortColumn): void {
    if (this.sortColumn === column) {
      this.sortDirection = this.sortDirection === 'asc' ? 'desc' : 'asc';
    } else {
      this.sortColumn = column;
      this.sortDirection = 'asc';
    }
    this.applySort();
  }

  private applySort(): void {
    const dir = this.sortDirection === 'asc' ? 1 : -1;
    const valueOf = (c: AdminCompany): string | number => {
      switch (this.sortColumn) {
        case 'companyId': return c.companyId;
        case 'companyName': return c.companyName.toLowerCase();
        case 'phone': return this.primaryPhone(c).toLowerCase();
        case 'email': return this.primaryEmail(c).toLowerCase();
        case 'gstNumber': return (c.gstNumber || '').toLowerCase();
        case 'city': return (c.city || '').toLowerCase();
      }
    };

    this.companies = [...this.companies].sort((a, b) => {
      const valA = valueOf(a);
      const valB = valueOf(b);
      if (valA < valB) return -1 * dir;
      if (valA > valB) return 1 * dir;
      return 0;
    });
  }

  primaryPhone(company: AdminCompany): string {
    const primary = company.phones?.find(p => p.isPrimary);
    return primary ? primary.phoneNumber : (company.phones?.[0]?.phoneNumber || '—');
  }

  primaryEmail(company: AdminCompany): string {
    const primary = company.emails?.find(e => e.isPrimary);
    return primary ? primary.emailAddress : (company.emails?.[0]?.emailAddress || '—');
  }

  openAddModal(): void {
    this.modalMode = 'add';
    this.editingId = null;
    this.formError = '';
    this.formCompanyName = '';
    this.formGstNumber = '';
    this.formAddress1 = '';
    this.formAddress2 = '';
    this.formCity = '';
    this.formState = '';
    this.formZipcode = '';
    this.phoneRows = [];
    this.emailRows = [{ emailAddress: '', isPrimary: true }];
    this.showModal = true;
  }

  openEditModal(company: AdminCompany): void {
    this.modalMode = 'edit';
    this.editingId = company.companyId;
    this.formError = '';
    this.formCompanyName = company.companyName;
    this.formGstNumber = company.gstNumber;
    this.formAddress1 = company.address1 || '';
    this.formAddress2 = company.address2 || '';
    this.formCity = company.city || '';
    this.formState = company.state || '';
    this.formZipcode = company.zipcode || '';
    this.phoneRows = (company.phones || []).map(p => ({ phoneNumber: p.phoneNumber, isPrimary: p.isPrimary }));
    this.emailRows = (company.emails || []).map(e => ({ emailAddress: e.emailAddress, isPrimary: e.isPrimary }));
    if (this.emailRows.length === 0) {
      this.emailRows = [{ emailAddress: '', isPrimary: true }];
    }
    this.showModal = true;
  }

  closeModal(): void {
    this.showModal = false;
  }

  addPhoneRow(): void {
    this.phoneRows.push({ phoneNumber: '', isPrimary: this.phoneRows.length === 0 });
  }

  removePhoneRow(index: number): void {
    const wasPrimary = this.phoneRows[index].isPrimary;
    this.phoneRows.splice(index, 1);
    if (wasPrimary && this.phoneRows.length > 0) {
      this.phoneRows[0].isPrimary = true;
    }
  }

  setPrimaryPhone(index: number): void {
    this.phoneRows.forEach((p, i) => p.isPrimary = i === index);
  }

  addEmailRow(): void {
    this.emailRows.push({ emailAddress: '', isPrimary: this.emailRows.length === 0 });
  }

  removeEmailRow(index: number): void {
    const wasPrimary = this.emailRows[index].isPrimary;
    this.emailRows.splice(index, 1);
    if (wasPrimary && this.emailRows.length > 0) {
      this.emailRows[0].isPrimary = true;
    }
  }

  setPrimaryEmail(index: number): void {
    this.emailRows.forEach((e, i) => e.isPrimary = i === index);
  }

  private validate(): string | null {
    if (!this.formCompanyName.trim()) return 'Company name is required.';
    if (!this.formGstNumber.trim()) return 'GST number is required.';

    const validEmails = this.emailRows.filter(e => e.emailAddress.trim());
    if (validEmails.length === 0) return 'At least one email address is required.';
    if (!validEmails.some(e => e.isPrimary)) return 'Please mark one email address as primary.';

    return null;
  }

  saveCompany(): void {
    const error = this.validate();
    if (error) {
      this.formError = error;
      return;
    }
    this.formError = '';
    this.formLoading = true;

    const payload: CompanySavePayload = {
      companyName: this.formCompanyName.trim(),
      gstNumber: this.formGstNumber.trim(),
      address1: this.formAddress1.trim(),
      address2: this.formAddress2.trim(),
      city: this.formCity.trim(),
      state: this.formState.trim(),
      zipcode: this.formZipcode.trim(),
      phones: this.phoneRows
        .filter(p => p.phoneNumber.trim())
        .map(p => ({ phoneNumber: p.phoneNumber.trim(), isPrimary: p.isPrimary })),
      emails: this.emailRows
        .filter(e => e.emailAddress.trim())
        .map(e => ({ emailAddress: e.emailAddress.trim(), isPrimary: e.isPrimary }))
    };

    const obs = this.modalMode === 'add'
      ? this.companyService.create(payload)
      : this.companyService.update(this.editingId!, payload);

    obs.subscribe({
      next: () => {
        this.formLoading = false;
        this.showModal = false;
        this.toastService.show(
          this.modalMode === 'add' ? 'Company added successfully.' : 'Company updated.',
          'success'
        );
        this.loadData();
      },
      error: (err) => {
        this.formLoading = false;
        this.formError = err?.error?.message || 'Failed to save. Please try again.';
      }
    });
  }

  confirmDelete(company: AdminCompany): void {
    this.deletingId = company.companyId;
    this.deletingName = company.companyName;
    this.showDeleteConfirm = true;
  }

  cancelDelete(): void {
    this.showDeleteConfirm = false;
    this.deletingId = null;
    this.deletingName = '';
  }

  doDelete(): void {
    if (!this.deletingId) return;
    this.deleteLoading = true;
    this.companyService.delete(this.deletingId).subscribe({
      next: () => {
        this.deleteLoading = false;
        this.showDeleteConfirm = false;
        this.toastService.show('Company deleted.', 'success');
        this.loadData();
      },
      error: () => {
        this.deleteLoading = false;
        this.toastService.show('Delete failed. Please try again.', 'danger');
      }
    });
  }
}
