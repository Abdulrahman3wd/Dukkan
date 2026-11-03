export interface AppLanguage {
  code: string;                 // used as the translation file name
  nativeName: string;           // shown in the language picker
  dir: 'ltr' | 'rtl';
  locale: string;               // for Intl (dates, numbers, currency)
}

export const LANGUAGES: AppLanguage[] = [
  { code: 'en', nativeName: 'English', dir: 'ltr', locale: 'en-US' },
  { code: 'ar', nativeName: 'العربية', dir: 'rtl', locale: 'ar-EG' },
  { code: 'fr', nativeName: 'Français', dir: 'ltr', locale: 'fr-FR' }
];

export const DEFAULT_LANG = 'ar';
export const FALLBACK_LANG = 'en';