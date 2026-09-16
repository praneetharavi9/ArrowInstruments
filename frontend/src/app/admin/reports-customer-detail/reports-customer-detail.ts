import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Subscription, timeout } from 'rxjs';
import { AdminReportsService, CustomerLedgerRow, LedgerEntryPayload } from '../services/admin-reports.service';
import { AdminCompanyService } from '../services/admin-company.service';
import { AdminReminderService, ReminderFrequency, ReminderSchedule } from '../services/admin-reminder.service';
import { ToastService } from '../services/toast.service';

// Client-side ceiling on the send/schedule request so a stalled SMTP
// connection surfaces as an error instead of leaving the button stuck
// on "Sending..." indefinitely.
const EMAIL_REQUEST_TIMEOUT_MS = 30000;

@Component({
  selector: 'app-reports-customer-detail',
  standalone: false,
  templateUrl: './reports-customer-detail.html',
  styleUrl: './reports-customer-detail.css'
})
export class ReportsCustomerDetailComponent implements OnInit, OnDestroy {
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
  dateSortDirection: 'asc' | 'desc' = 'asc';
  downloadingExcel = false;

  // Entry modal
  showEntryModal = false;
  entryMode: 'add' | 'edit' = 'add';
  editingEntryId: number | null = null;
  formDate = '';
  formDescription = '';
  formParticularsChoice: 'to' | 'by' | null = null;
  formByType: 'bank' | 'cash' | null = null;
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
  sendAttachLedger = false;
  sendLoading = false;
  sendError = '';
  private sendSub?: Subscription;

  // Zero-balance confirm (guards both the Send Email Reminder and Schedule Reminders modals)
  showZeroBalanceConfirm = false;
  private zeroBalancePendingAction: 'send' | 'schedule' | null = null;

  // Schedule Reminders modal
  showScheduleModal = false;
  scheduleFrequency: ReminderFrequency = 'monthly';
  scheduleStartDate = '';
  scheduleEndDate = '';
  scheduleSendTime = '09:00';
  scheduleTo: string[] = [];
  scheduleCc: string[] = [];
  scheduleToInput = '';
  scheduleCcInput = '';
  scheduleSubject = '';
  scheduleBody = '';
  scheduleFiles: File[] = [];
  scheduleAttachLedger = false;
  scheduleLoading = false;
  scheduleError = '';
  schedules: ReminderSchedule[] = [];
  schedulesLoading = false;
  private scheduleSub?: Subscription;

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

  ngOnDestroy(): void {
    this.sendSub?.unsubscribe();
    this.scheduleSub?.unsubscribe();
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
        this.sortRowsByDate();
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

  toggleDateSort(): void {
    this.dateSortDirection = this.dateSortDirection === 'asc' ? 'desc' : 'asc';
    this.sortRowsByDate();
  }

  private sortRowsByDate(): void {
    const dir = this.dateSortDirection === 'asc' ? 1 : -1;
    this.rows = [...this.rows].sort((a, b) => (new Date(a.date).getTime() - new Date(b.date).getTime()) * dir);
  }

  backToList(): void {
    this.router.navigate(['/admin/reports/customers']);
  }

  downloadExcel(): void {
    this.downloadingExcel = true;
    this.reportsService.downloadLedgerExcel(this.companyId, this.startDate, this.endDate).subscribe({
      next: (blob) => {
        this.downloadingExcel = false;
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `${this.companyName} Ledger Statement ${this.startDate}-${this.endDate}.xlsx`;
        a.click();
        window.URL.revokeObjectURL(url);
      },
      error: () => {
        this.downloadingExcel = false;
        this.toastService.show('Failed to download ledger.', 'danger');
      }
    });
  }

  openAddEntry(): void {
    this.entryMode = 'add';
    this.editingEntryId = null;
    this.formDate = this.toDateString(new Date());
    this.formDescription = '';
    this.formParticularsChoice = null;
    this.formByType = null;
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

    const desc = this.formDescription.trim().toLowerCase();
    const voucherType = this.formVoucherType.trim().toLowerCase();
    if (desc === 'to') {
      this.formParticularsChoice = 'to';
      this.formByType = null;
    } else if (desc === 'by') {
      this.formParticularsChoice = 'by';
      this.formByType = voucherType === 'bank' ? 'bank' : voucherType === 'cash' ? 'cash' : null;
    } else {
      // Existing entry doesn't match one of the To/By options — leave unselected
      // so the admin has to pick one before saving over it.
      this.formParticularsChoice = null;
      this.formByType = null;
    }

    this.showEntryModal = true;
  }

  onParticularsChoiceChange(choice: 'to' | 'by'): void {
    this.formParticularsChoice = choice;
    this.formDescription = choice === 'to' ? 'To' : 'By';
    if (choice === 'to') {
      this.formType = 'debit';
      this.formVoucherType = 'Sales';
      this.formByType = null;
    } else {
      this.formType = 'credit';
      this.formByType = null;
      this.formVoucherType = '';
    }
  }

  onByTypeChange(byType: 'bank' | 'cash'): void {
    this.formByType = byType;
    this.formVoucherType = byType === 'bank' ? 'Bank' : 'Cash';
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
    if (!this.formParticularsChoice) {
      this.formError = 'Select To or By for Particulars.';
      return;
    }
    if (this.formParticularsChoice === 'by' && !this.formByType) {
      this.formError = 'Select Bank or Cash for Type.';
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
    this.sendSub?.unsubscribe();
    this.sendLoading = false;
    this.sendTo = [];
    this.sendCc = [];
    this.sendToInput = '';
    this.sendCcInput = '';
    this.sendSubject = `Payment Reminder — ${this.companyName}`;
    this.sendBody = this.buildReminderBody();
    this.sendFiles = [];
    this.sendAttachLedger = false;
    this.sendError = '';
    this.showSendModal = true;
  }

  closeSendModal(): void {
    this.showSendModal = false;
    this.sendSub?.unsubscribe();
    this.sendLoading = false;
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

    if (this.closingBalance <= 0) {
      this.zeroBalancePendingAction = 'send';
      this.showZeroBalanceConfirm = true;
      return;
    }

    this.doSendReminder();
  }

  cancelZeroBalanceConfirm(): void {
    this.showZeroBalanceConfirm = false;
    this.zeroBalancePendingAction = null;
  }

  confirmZeroBalanceAction(): void {
    this.showZeroBalanceConfirm = false;
    const action = this.zeroBalancePendingAction;
    this.zeroBalancePendingAction = null;

    if (action === 'schedule') {
      this.doCreateSchedule();
    } else {
      this.doSendReminder();
    }
  }

  private doSendReminder(): void {
    this.sendLoading = true;
    this.sendSub?.unsubscribe();
    this.sendSub = this.reminderService.sendReminder(this.companyId, {
      to: this.sendTo,
      cc: this.sendCc,
      subject: this.sendSubject.trim(),
      body: this.sendBody,
      files: this.sendFiles,
      attachLedgerStatement: this.sendAttachLedger,
      ledgerStartDate: this.startDate,
      ledgerEndDate: this.endDate
    }).pipe(timeout(EMAIL_REQUEST_TIMEOUT_MS)).subscribe({
      next: () => {
        this.sendLoading = false;
        this.showSendModal = false;
        this.toastService.show('Reminder email sent.', 'success');
      },
      error: (err) => {
        this.sendLoading = false;
        this.sendError = err?.name === 'TimeoutError'
          ? 'Sending the email is taking too long. Check the SMTP settings and try again.'
          : err?.error?.message || 'Failed to send email.';
      }
    });
  }

  // ───────────────────────────── Schedule Reminders modal ─────────────────────────────

  openScheduleReminders(): void {
    this.scheduleSub?.unsubscribe();
    this.scheduleLoading = false;
    this.scheduleFrequency = 'monthly';
    this.scheduleStartDate = this.toDateString(new Date());
    this.scheduleEndDate = '';
    this.scheduleSendTime = '09:00';
    this.scheduleTo = [];
    this.scheduleCc = [];
    this.scheduleToInput = '';
    this.scheduleCcInput = '';
    this.scheduleSubject = `Payment Reminder — ${this.companyName}`;
    this.scheduleBody = this.buildReminderBody();
    this.scheduleFiles = [];
    this.scheduleAttachLedger = false;
    this.scheduleError = '';
    this.showScheduleModal = true;
    this.loadSchedules();
  }

  closeScheduleModal(): void {
    this.showScheduleModal = false;
    this.scheduleSub?.unsubscribe();
    this.scheduleLoading = false;
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
    if (!this.scheduleSendTime) {
      this.scheduleError = 'Send time is required.';
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

    if (this.closingBalance === 0) {
      this.zeroBalancePendingAction = 'schedule';
      this.showZeroBalanceConfirm = true;
      return;
    }

    this.doCreateSchedule();
  }

  private doCreateSchedule(): void {
    this.scheduleLoading = true;
    this.scheduleSub?.unsubscribe();
    this.scheduleSub = this.reminderService.createSchedule(this.companyId, {
      frequency: this.scheduleFrequency,
      startDate: this.scheduleStartDate,
      endDate: this.scheduleEndDate || null,
      sendTime: this.scheduleSendTime,
      attachLedgerStatement: this.scheduleAttachLedger,
      to: this.scheduleTo,
      cc: this.scheduleCc,
      subject: this.scheduleSubject.trim(),
      body: this.scheduleBody,
      files: this.scheduleFiles
    }).pipe(timeout(EMAIL_REQUEST_TIMEOUT_MS)).subscribe({
      next: () => {
        this.scheduleLoading = false;
        this.toastService.show('Reminder schedule created.', 'success');
        this.scheduleTo = [];
        this.scheduleCc = [];
        this.scheduleSubject = '';
        this.scheduleBody = '';
        this.scheduleFiles = [];
        this.scheduleAttachLedger = false;
        this.loadSchedules();
      },
      error: (err) => {
        this.scheduleLoading = false;
        this.scheduleError = err?.name === 'TimeoutError'
          ? 'Creating the schedule is taking too long. Check the FTP/SMTP settings and try again.'
          : err?.error?.message || 'Failed to create reminder schedule.';
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
