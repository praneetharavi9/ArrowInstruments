import { Component, OnInit } from '@angular/core';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { AdminProductTypeService } from '../services/admin-product-type.service';
import { AdminProductService } from '../services/admin-product.service';
import { AdminEnquiryService } from '../services/admin-enquiry.service';

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
    newEnquiries: 0
  };

  constructor(
    private productTypeService: AdminProductTypeService,
    private productService: AdminProductService,
    private enquiryService: AdminEnquiryService
  ) {}

  ngOnInit(): void {
    forkJoin({
      productTypes: this.productTypeService.getAll().pipe(catchError(() => of([]))),
      products: this.productService.getAll().pipe(catchError(() => of([]))),
      enquiries: this.enquiryService.getAll().pipe(catchError(() => of([])))
    }).subscribe(({ productTypes, products, enquiries }) => {
      this.stats.productTypes = productTypes.length;
      this.stats.products = products.length;
      this.stats.totalEnquiries = enquiries.length;
      this.stats.newEnquiries = enquiries.filter(e => e.status === 'new').length;
      this.loading = false;
    });
  }
}
