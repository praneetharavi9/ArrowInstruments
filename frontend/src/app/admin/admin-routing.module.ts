import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AdminLoginComponent } from './login/login';
import { AdminLayoutComponent } from './layout/admin-layout';
import { DashboardComponent } from './dashboard/dashboard';
import { ProductTypesComponent } from './product-types/product-types';
import { AdminProductsComponent } from './admin-products/admin-products';
import { EnquiriesComponent } from './enquiries/enquiries';
import { Companies } from './companies/companies';
import { ReportsCustomersComponent } from './reports-customers/reports-customers';
import { ReportsCustomerDetailComponent } from './reports-customer-detail/reports-customer-detail';
import { authGuard } from './guards/auth.guard';

const routes: Routes = [
  { path: 'login', component: AdminLoginComponent },
  {
    path: '',
    component: AdminLayoutComponent,
    canActivate: [authGuard],
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      { path: 'dashboard', component: DashboardComponent },
      { path: 'product-types', component: ProductTypesComponent },
      { path: 'products', component: AdminProductsComponent },
      { path: 'enquiries', component: EnquiriesComponent },
      { path: 'companies', component: Companies },
      { path: 'reports/customers', component: ReportsCustomersComponent },
      { path: 'reports/customers/:id', component: ReportsCustomerDetailComponent }
    ]
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class AdminRoutingModule {}
