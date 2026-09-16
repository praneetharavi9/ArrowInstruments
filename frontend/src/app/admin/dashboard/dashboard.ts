import { Component, OnInit } from '@angular/core';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { AdminProductTypeService } from '../services/admin-product-type.service';
import { AdminProductService } from '../services/admin-product.service';
import { AdminEnquiryService } from '../services/admin-enquiry.service';
import { AdminReportsService } from '../services/admin-reports.service';

@Component({
  selector: 'app-admin-dashboard',
  standalone: false,
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css'
})
export class DashboardComponent implements OnInit {
  loading = true;
  stats = {
    productTypes: 0,
    products: 0,
    totalEnquiries: 0,
    newEnquiries: 0,
    totalBalance: 0
  };

  constructor(
    private productTypeService: AdminProductTypeService,
    private productService: AdminProductService,
    private enquiryService: AdminEnquiryService,
    private reportsService: AdminReportsService
  ) {}

  ngOnInit(): void {
    const today = new Date();
    const startDate = `${today.getFullYear()}-01-01`;
    const endDate = this.toDateString(today);

    forkJoin({
      productTypes: this.productTypeService.getAll().pipe(catchError(() => of([]))),
      products: this.productService.getAll().pipe(catchError(() => of([]))),
      enquiries: this.enquiryService.getAll().pipe(catchError(() => of([]))),
      customerSummary: this.reportsService.getCustomerSummary(startDate, endDate).pipe(catchError(() => of({ startDate, endDate, rows: [] })))
    }).subscribe(({ productTypes, products, enquiries, customerSummary }) => {
      this.stats.productTypes = productTypes.length;
      this.stats.products = products.length;
      this.stats.totalEnquiries = enquiries.length;
      this.stats.newEnquiries = enquiries.filter(e => e.status === 'new').length;
      this.stats.totalBalance = customerSummary.rows.reduce((sum, r) => sum + (r.balance || 0), 0);
      this.loading = false;
    });
  }

  private toDateString(d: Date): string {
    const y = d.getFullYear();
    const m = (d.getMonth() + 1).toString().padStart(2, '0');
    const day = d.getDate().toString().padStart(2, '0');
    return `${y}-${m}-${day}`;
  }
}
