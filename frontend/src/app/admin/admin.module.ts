import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

import { AdminRoutingModule } from './admin-routing.module';
import { AdminLoginComponent } from './login/login';
import { AdminLayoutComponent } from './layout/admin-layout';
import { DashboardComponent } from './dashboard/dashboard';
import { ProductTypesComponent } from './product-types/product-types';
import { AdminProductsComponent } from './admin-products/admin-products';
import { EnquiriesComponent } from './enquiries/enquiries';
import { Companies } from './companies/companies';
import { DateRangeFilter } from './shared/date-range-filter/date-range-filter';
import { ReportsCustomersComponent } from './reports-customers/reports-customers';
import { ReportsCustomerDetailComponent } from './reports-customer-detail/reports-customer-detail';

@NgModule({
  declarations: [
    AdminLoginComponent,
    AdminLayoutComponent,
    DashboardComponent,
    ProductTypesComponent,
    AdminProductsComponent,
    EnquiriesComponent,
    Companies,
    DateRangeFilter,
    ReportsCustomersComponent,
    ReportsCustomerDetailComponent
  ],
  imports: [
    CommonModule,
    FormsModule,
    AdminRoutingModule
  ]
})
export class AdminModule {}
