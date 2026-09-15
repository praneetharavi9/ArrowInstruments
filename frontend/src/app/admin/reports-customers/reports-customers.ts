import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { AdminReportsService, CustomerSummaryRow } from '../services/admin-reports.service';
import { ToastService } from '../services/toast.service';

@Component({
  selector: 'app-reports-customers',
  standalone: false,
  templateUrl: './reports-customers.html',
  styleUrl: './reports-customers.css'
})
export class ReportsCustomersComponent implements OnInit {
  rows: CustomerSummaryRow[] = [];
  loading = true;
  startDate = '';
  endDate = '';

  constructor(
    private reportsService: AdminReportsService,
    private router: Router,
    private toastService: ToastService
  ) {}

  ngOnInit(): void {
    const today = new Date();
    this.startDate = `${today.getFullYear()}-01-01`;
    this.endDate = this.toDateString(today);
    this.loadData();
  }

  private toDateString(d: Date): string {
    const y = d.getFullYear();
    const m = (d.getMonth() + 1).toString().padStart(2, '0');
    const day = d.getDate().toString().padStart(2, '0');
    return `${y}-${m}-${day}`;
  }

  loadData(): void {
    this.loading = true;
    this.reportsService.getCustomerSummary(this.startDate, this.endDate).subscribe({
      next: (res) => {
        this.rows = res.rows;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.toastService.show('Failed to load customer report.', 'danger');
      }
    });
  }

  onRangeChange(range: { start: string; end: string }): void {
    this.startDate = range.start;
    this.endDate = range.end;
    this.loadData();
  }

  openDetail(row: CustomerSummaryRow): void {
    this.router.navigate(['/admin/reports/customers', row.companyId], {
      queryParams: { start: this.startDate, end: this.endDate }
    });
  }
}
