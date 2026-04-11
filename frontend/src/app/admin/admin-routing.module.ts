import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AdminLoginComponent } from './login/login';
import { AdminLayoutComponent } from './layout/admin-layout';
import { DashboardComponent } from './dashboard/dashboard';
import { ProductTypesComponent } from './product-types/product-types';
import { AdminProductsComponent } from './admin-products/admin-products';
import { EnquiriesComponent } from './enquiries/enquiries';
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
      { path: 'enquiries', component: EnquiriesComponent }
    ]
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class AdminRoutingModule {}
