import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { AdminReportsService, CustomerLedgerRow, LedgerEntryPayload } from '../services/admin-reports.service';
import { AdminCompanyService } from '../services/admin-company.service';
import { AdminReminderService, ReminderFrequency, ReminderSchedule } from '../services/admin-reminder.service';
import { ToastService } from '../services/toast.service';

@Component({
  selector: 'app-reports-customer-detail',
  standalone: false,
  templateUrl: './reports-customer-detail.html',
  styleUrl: './reports-customer-detail.css'
})
export class ReportsCustomerDetailComponent implements OnInit {
  companyId!: number;
  companyName = '';
  openingBalance = 0;
  closingBalance = 0;
  totalDebit = 0;
  totalCredit = 0;
  rows: CustomerLedgerRow[] = [];
  loading = true;
  startDate = '';
  endDate = '';

  // Entry modal
  showEntryModal = false;
  entryMode: 'add' | 'edit' = 'add';
  editingEntryId: number | null = null;
  formDate = '';
  formDescription = '';
  formVoucherType = '';
  formTransNo = '';
  formType: 'debit' | 'credit' | 'opening' = 'debit';
  openingBalanceOnly = false;
  formAmount: number | null = null;
  formLoading = false;
  formError = '';

  // Delete confirm
  showDeleteConfirm = false;
  deletingEntryId: number | null = null;
  deleteLoading = false;

  // Recipient picker (shared by the Send Email + Schedule Reminders modals)
  companyEmails: string[] = [];

  // Send Email Reminder modal
  showSendModal = false;
  sendTo: string[] = [];
  sendCc: string[] = [];
  sendToInput = '';
  sendCcInput = '';
  sendSubject = '';
  sendBody = '';
  sendFiles: File[] = [];
  sendLoading = false;
  sendError = '';

  // Schedule Reminders modal
  showScheduleModal = false;
  scheduleFrequency: ReminderFrequency = 'monthly';
  scheduleStartDate = '';
  scheduleEndDate = '';
  scheduleTo: string[] = [];
  scheduleCc: string[] = [];
  scheduleToInput = '';
  scheduleCcInput = '';
  scheduleSubject = '';
  scheduleBody = '';
  scheduleFiles: File[] = [];
  scheduleLoading = false;
  scheduleError = '';
  schedules: ReminderSchedule[] = [];
  schedulesLoading = false;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private reportsService: AdminReportsService,
    private companyService: AdminCompanyService,
    private reminderService: AdminReminderService,
    private toastService: ToastService
  ) {}

  ngOnInit(): void {
    this.companyId = Number(this.route.snapshot.paramMap.get('id'));
    const qp = this.route.snapshot.queryParamMap;
    const today = new Date();
    this.startDate = qp.get('start') || `${today.getFullYear()}-01-01`;
    this.endDate = qp.get('end') || this.toDateString(today);
    this.loadData();
    this.loadCompanyEmails();
  }

  private loadCompanyEmails(): void {
    this.companyService.getById(this.companyId).subscribe({
      next: (company) => {
        this.companyEmails = (company.emails || []).map(e => e.emailAddress);
      },
      error: () => {
        // Non-fatal — the recipient picker just falls back to "add new" only.
      }
    });
  }

  private toDateString(d: Date): string {
    const y = d.getFullYear();
    const m = (d.getMonth() + 1).toString().padStart(2, '0');
    const day = d.getDate().toString().padStart(2, '0');
    return `${y}-${m}-${day}`;
  }

  loadData(): void {
    this.loading = true;
    this.reportsService.getCustomerLedger(this.companyId, this.startDate, this.endDate).subscribe({
      next: (res) => {
        this.companyName = res.companyName;
        this.openingBalance = res.openingBalance;
        this.closingBalance = res.closingBalance;
        this.rows = res.rows;
        this.totalDebit = this.rows.reduce((sum, r) => sum + (r.debit || 0), 0);
        this.totalCredit = this.rows.reduce((sum, r) => sum + (r.credit || 0), 0);
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.toastService.show('Failed to load customer ledger.', 'danger');
      }
    });
  }

  onRangeChange(range: { start: string; end: string }): void {
    this.startDate = range.start;
    this.endDate = range.end;
    this.loadData();
  }

  backToList(): void {
    this.router.navigate(['/admin/reports/customers']);
  }

  openAddEntry(): void {
    this.entryMode = 'add';
    this.editingEntryId = null;
    this.formDate = this.toDateString(new Date());
    this.formDescription = '';
    this.formVoucherType = '';
    this.formTransNo = '';
    this.formType = 'debit';
    this.formAmount = null;
    this.formError = '';
    this.openingBalanceOnly = false;
    this.showEntryModal = true;
  }

  openEditEntry(row: CustomerLedgerRow): void {
    this.entryMode = 'edit';
    this.editingEntryId = row.entryId;
    this.formDate = row.date.substring(0, 10);
    this.formDescription = row.description || '';
    this.formVoucherType = row.type || '';
    this.formTransNo = row.transNo || '';
    this.formType = row.debit > 0 ? 'debit' : 'credit';
    this.formAmount = row.debit > 0 ? row.debit : row.credit;
    this.formError = '';
    this.openingBalanceOnly = false;
    this.showEntryModal = true;
  }

  openEditOpeningBalance(): void {
    this.entryMode = 'edit';
    this.editingEntryId = null;
    this.formType = 'opening';
    this.formDate = '';
    this.formDescription = '';
    this.formAmount = this.openingBalance;
    this.formError = '';
    this.openingBalanceOnly = true;
    this.showEntryModal = true;
  }

  closeEntryModal(): void {
    this.showEntryModal = false;
  }

  saveEntry(): void {
    this.formError = '';

    if (this.formType === 'opening') {
      if (this.formAmount === null || (this.entryMode === 'add' && this.formAmount === 0)) {
        this.formError = 'Enter an amount.';
        return;
      }

      this.formLoading = true;
      const obs = this.entryMode === 'add'
        ? this.reportsService.addOpeningBalance(this.companyId, this.formAmount)
        : this.reportsService.setOpeningBalance(this.companyId, this.formAmount);

      obs.subscribe({
        next: () => {
          this.formLoading = false;
          this.showEntryModal = false;
          this.toastService.show('Opening balance updated.', 'success');
          this.loadData();
        },
        error: (err) => {
          this.formLoading = false;
          this.formError = err?.error?.message || 'Failed to update opening balance.';
        }
      });
      return;
    }

    if (!this.formDate) {
      this.formError = 'Date is required.';
      return;
    }
    if (!this.formAmount || this.formAmount <= 0) {
      this.formError = 'Enter a valid amount.';
      return;
    }

    this.formLoading = true;

    const payload: LedgerEntryPayload = {
      entryDate: this.formDate,
      description: this.formDescription.trim(),
      type: this.formVoucherType.trim(),
      transNo: this.formTransNo.trim(),
      debit: this.formType === 'debit' ? this.formAmount : 0,
      credit: this.formType === 'credit' ? this.formAmount : 0
    };

    const obs = this.entryMode === 'add'
      ? this.reportsService.addEntry(this.companyId, payload)
      : this.reportsService.updateEntry(this.companyId, this.editingEntryId!, payload);

    obs.subscribe({
      next: () => {
        this.formLoading = false;
        this.showEntryModal = false;
        this.toastService.show(this.entryMode === 'add' ? 'Entry added.' : 'Entry updated.', 'success');
        this.loadData();
      },
      error: (err) => {
        this.formLoading = false;
        this.formError = err?.error?.message || 'Failed to save entry.';
      }
    });
  }

  confirmDelete(row: CustomerLedgerRow): void {
    this.deletingEntryId = row.entryId;
    this.showDeleteConfirm = true;
  }

  cancelDelete(): void {
    this.showDeleteConfirm = false;
    this.deletingEntryId = null;
  }

  doDelete(): void {
    if (!this.deletingEntryId) return;
    this.deleteLoading = true;
    this.reportsService.deleteEntry(this.companyId, this.deletingEntryId).subscribe({
      next: () => {
        this.deleteLoading = false;
        this.showDeleteConfirm = false;
        this.toastService.show('Entry deleted.', 'success');
        this.loadData();
      },
      error: () => {
        this.deleteLoading = false;
        this.toastService.show('Delete failed.', 'danger');
      }
    });
  }

  // ───────────────────────────── Recipient picker (shared) ─────────────────────────────

  toggleEmail(list: string[], email: string): void {
    const idx = list.indexOf(email);
    if (idx >= 0) {
      list.splice(idx, 1);
    } else {
      list.push(email);
    }
  }

  addEmailFromInput(list: string[], rawInput: string): string {
    const email = rawInput.trim();
    if (!email) return rawInput;
    if (!this.isValidEmail(email)) {
      this.toastService.show(`'${email}' is not a valid email address.`, 'danger');
      return rawInput;
    }
    if (!list.some(e => e.toLowerCase() === email.toLowerCase())) {
      list.push(email);
    }
    if (!this.companyEmails.some(e => e.toLowerCase() === email.toLowerCase())) {
      this.companyEmails.push(email);
    }
    return '';
  }

  private isValidEmail(email: string): boolean {
    return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
  }

  private buildReminderBody(): string {
    return `Dear ${this.companyName},

This is a courteous reminder regarding your account with us. As of today, your outstanding balance stands at ₹${this.closingBalance.toFixed(2)}.

We kindly request that any applicable payment be arranged at your earliest convenience. Should you have any questions regarding this notice, please do not hesitate to contact us.

Thank you for your continued business.

Best regards,
Arrow Instruments`;
  }

  onFilesSelected(event: Event, target: File[]): void {
    const input = event.target as HTMLInputElement;
    if (input.files) {
      target.push(...Array.from(input.files));
    }
    input.value = '';
  }

  removeFile(target: File[], index: number): void {
    target.splice(index, 1);
  }

  // ───────────────────────────── Send Email Reminder modal ─────────────────────────────

  openSendReminder(): void {
    this.sendTo = [];
    this.sendCc = [];
    this.sendToInput = '';
    this.sendCcInput = '';
    this.sendSubject = `Payment Reminder — ${this.companyName}`;
    this.sendBody = this.buildReminderBody();
    this.sendFiles = [];
    this.sendError = '';
    this.showSendModal = true;
  }

  closeSendModal(): void {
    this.showSendModal = false;
  }

  submitSendReminder(): void {
    this.sendError = '';

    if (this.sendTo.length === 0) {
      this.sendError = 'Add at least one recipient in To.';
      return;
    }
    if (!this.sendSubject.trim()) {
      this.sendError = 'Subject is required.';
      return;
    }

    this.sendLoading = true;
    this.reminderService.sendReminder(this.companyId, {
      to: this.sendTo,
      cc: this.sendCc,
      subject: this.sendSubject.trim(),
      body: this.sendBody,
      files: this.sendFiles
    }).subscribe({
      next: () => {
        this.sendLoading = false;
        this.showSendModal = false;
        this.toastService.show('Reminder email sent.', 'success');
      },
      error: (err) => {
        this.sendLoading = false;
        this.sendError = err?.error?.message || 'Failed to send email.';
      }
    });
  }

  // ───────────────────────────── Schedule Reminders modal ─────────────────────────────

  openScheduleReminders(): void {
    this.scheduleFrequency = 'monthly';
    this.scheduleStartDate = this.toDateString(new Date());
    this.scheduleEndDate = '';
    this.scheduleTo = [];
    this.scheduleCc = [];
    this.scheduleToInput = '';
    this.scheduleCcInput = '';
    this.scheduleSubject = `Payment Reminder — ${this.companyName}`;
    this.scheduleBody = this.buildReminderBody();
    this.scheduleFiles = [];
    this.scheduleError = '';
    this.showScheduleModal = true;
    this.loadSchedules();
  }

  closeScheduleModal(): void {
    this.showScheduleModal = false;
  }

  private loadSchedules(): void {
    this.schedulesLoading = true;
    this.reminderService.getSchedules(this.companyId).subscribe({
      next: (schedules) => {
        this.schedules = schedules;
        this.schedulesLoading = false;
      },
      error: () => {
        this.schedulesLoading = false;
        this.toastService.show('Failed to load reminder schedules.', 'danger');
      }
    });
  }

  submitCreateSchedule(): void {
    this.scheduleError = '';

    if (this.scheduleTo.length === 0) {
      this.scheduleError = 'Add at least one recipient in To.';
      return;
    }
    if (!this.scheduleStartDate) {
      this.scheduleError = 'Start date is required.';
      return;
    }
    if (this.scheduleEndDate && this.scheduleEndDate < this.scheduleStartDate) {
      this.scheduleError = 'End date cannot be before the start date.';
      return;
    }
    if (!this.scheduleSubject.trim()) {
      this.scheduleError = 'Subject is required.';
      return;
    }
    if (!this.scheduleBody.trim()) {
      this.scheduleError = 'Body is required.';
      return;
    }

    this.scheduleLoading = true;
    this.reminderService.createSchedule(this.companyId, {
      frequency: this.scheduleFrequency,
      startDate: this.scheduleStartDate,
      endDate: this.scheduleEndDate || null,
      to: this.scheduleTo,
      cc: this.scheduleCc,
      subject: this.scheduleSubject.trim(),
      body: this.scheduleBody,
      files: this.scheduleFiles
    }).subscribe({
      next: () => {
        this.scheduleLoading = false;
        this.toastService.show('Reminder schedule created.', 'success');
        this.scheduleTo = [];
        this.scheduleCc = [];
        this.scheduleSubject = '';
        this.scheduleBody = '';
        this.scheduleFiles = [];
        this.loadSchedules();
      },
      error: (err) => {
        this.scheduleLoading = false;
        this.scheduleError = err?.error?.message || 'Failed to create reminder schedule.';
      }
    });
  }

  toggleScheduleActive(schedule: ReminderSchedule): void {
    this.reminderService.toggleSchedule(schedule.id).subscribe({
      next: (res) => {
        schedule.isActive = res.isActive;
        this.toastService.show(res.isActive ? 'Schedule resumed.' : 'Schedule paused.', 'success');
      },
      error: () => {
        this.toastService.show('Failed to update schedule.', 'danger');
      }
    });
  }

  deleteScheduleRow(schedule: ReminderSchedule): void {
    this.reminderService.deleteSchedule(schedule.id).subscribe({
      next: () => {
        this.schedules = this.schedules.filter(s => s.id !== schedule.id);
        this.toastService.show('Schedule deleted.', 'success');
      },
      error: () => {
        this.toastService.show('Failed to delete schedule.', 'danger');
      }
    });
  }
}
