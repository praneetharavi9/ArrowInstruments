import { Component, signal } from '@angular/core';

@Component({
  selector: 'app-root',
  templateUrl: './app.html',
  standalone: false,
  styleUrl: './app.css'
})
export class App {
  protected readonly title = signal('frontend');
   ngOnInit() {
      const splash = document.getElementById('splash-screen');
    if (splash) {
      setTimeout(() => splash.style.display = 'none', 0);
    }
  }
}
