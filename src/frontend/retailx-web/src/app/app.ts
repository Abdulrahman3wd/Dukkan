import { Component, signal } from '@angular/core';

@Component({
  selector: 'app-root',
  templateUrl: './app.html'
})
export class App {
  dark = signal(false);
  rtl = signal(false);

  toggleTheme() {
    this.dark.update(v => !v);
    document.documentElement.setAttribute('data-theme', this.dark() ? 'dark' : 'light');
  }

  toggleDir() {
    this.rtl.update(v => !v);
    document.documentElement.setAttribute('dir', this.rtl() ? 'rtl' : 'ltr');
    document.documentElement.setAttribute('lang', this.rtl() ? 'ar' : 'en');
  }
}