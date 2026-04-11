import { NgModule, provideBrowserGlobalErrorListeners } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { HTTP_INTERCEPTORS } from '@angular/common/http';
import { provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';

import { AppRoutingModule } from './app-routing-module';
import { App } from './app';
import { Home } from './home/home';
import { AppNavbar } from './navbar/navbar';
import { Contact } from './contact/contact';
import { Products } from './products/products';
import { About } from './about/about';
import { Footer } from './footer/footer';
import { MegaMenu } from './mega-menu/mega-menu';
import { FormsModule } from '@angular/forms';
import { AuthInterceptor } from './admin/interceptors/auth.interceptor';

@NgModule({
  declarations: [
    App,
    Home,
    AppNavbar,
    Contact,
    Products,
    About,
    Footer,
    MegaMenu
  ],
  imports: [
    BrowserModule,
    AppRoutingModule,
    FormsModule
  ],
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideHttpClient(withInterceptorsFromDi()),
    { provide: HTTP_INTERCEPTORS, useClass: AuthInterceptor, multi: true }
  ],
  bootstrap: [App]
})
export class AppModule { }
