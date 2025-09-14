import { Component, NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { Home } from './home/home';
import { Contact } from './contact/contact';
import { About } from './about/about';
import { Products } from './products/products';

const routes: Routes = [
  { path : '' , component : Home},
  { path : 'contact', component : Contact},
  { path : 'about', component : About},
  { path : 'products', component : Products}
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
