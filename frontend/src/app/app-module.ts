import { NgModule, provideBrowserGlobalErrorListeners } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';

import { AppRoutingModule } from './app-routing-module';
import { App } from './app';
import { Home } from './home/home';
import { AppNavbar } from './navbar/navbar';
import { Contact } from './contact/contact';
import { Products } from './products/products';
import { About } from './about/about';
import { HttpClientModule } from '@angular/common/http';

@NgModule({
  declarations: [
    App,
    Home,
    AppNavbar,
    Contact,
    Products,
    About
  ],
  imports: [
    BrowserModule,
    AppRoutingModule,
    HttpClientModule
  ],
  providers: [
    provideBrowserGlobalErrorListeners()
  ],
  bootstrap: [App]
})
export class AppModule { }
