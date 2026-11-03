import { DOCUMENT } from '@angular/common';
import { inject, Injectable, signal } from '@angular/core';
import { TranslocoService } from '@jsverse/transloco';
import { AppLanguage, DEFAULT_LANG, LANGUAGES } from './languages';

const KEY = 'dukkan.lang';

@Injectable({ providedIn: 'root' })
export class LanguageService {
  private transloco = inject(TranslocoService);
  private doc = inject(DOCUMENT);

  readonly languages = LANGUAGES;
  readonly current = signal<AppLanguage>(this.find(DEFAULT_LANG));

  init() {
    let saved: string | null = null;
    try { saved = localStorage.getItem(KEY); } catch { /* storage unavailable */ }
    this.use(saved ?? DEFAULT_LANG);
  }

  use(code: string) {
    const lang = this.find(code);
    this.transloco.setActiveLang(lang.code);

    const html = this.doc.documentElement;
    html.setAttribute('lang', lang.code);
    html.setAttribute('dir', lang.dir);

    try { localStorage.setItem(KEY, lang.code); } catch { /* ignore */ }
    this.current.set(lang);
  }

  private find(code: string): AppLanguage {
    return LANGUAGES.find(l => l.code === code)
      ?? LANGUAGES.find(l => l.code === DEFAULT_LANG)!;
  }
}