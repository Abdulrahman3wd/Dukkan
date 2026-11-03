import { Component, inject, signal } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { LanguageService } from './core/i18n/language.service';

@Component({
  selector: 'app-root',
  imports: [TranslocoPipe],
  templateUrl: './app.html'
})
export class App {
  lang = inject(LanguageService);
  dark = signal(false);

  toggleTheme() {
    this.dark.update(v => !v);
    document.documentElement.setAttribute('data-theme', this.dark() ? 'dark' : 'light');
  }

  onLang(e: Event) {
    this.lang.use((e.target as HTMLSelectElement).value);
  }
}