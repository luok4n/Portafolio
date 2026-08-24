import { DOCUMENT, Injectable, PLATFORM_ID, computed, inject, signal } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { NavigationEnd, Router } from '@angular/router';
import { TranslocoService } from '@jsverse/transloco';
import { filter } from 'rxjs';

import {
  DEFAULT_LOCALE,
  LOCALE_STORAGE_KEY,
  Locale,
  isLocale,
  translateUrl,
} from './locale';
import { ContentService } from './content.service';
import { EngineeringService } from './engineering';

/**
 * Keeps one idea of "the current language" for the router, Transloco, the content and the document.
 *
 * The locale is decided by the URL and nothing else — no `Accept-Language` redirect, per ADR-0001,
 * because guessing destabilises the canonical URLs that prerendering depends on and takes the choice
 * away from the reader. What the browser reports and what the reader last chose only matter on the
 * bare `/` path, where there is nothing else to go on.
 */
@Injectable({ providedIn: 'root' })
export class LocaleService {
  private readonly router = inject(Router);
  private readonly transloco = inject(TranslocoService);
  private readonly content = inject(ContentService);
  private readonly engineering = inject(EngineeringService);
  private readonly document = inject(DOCUMENT);
  private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));

  private readonly current = signal<Locale>(DEFAULT_LOCALE);
  private readonly url = signal<string>('/');

  readonly locale = this.current.asReadonly();

  /** The locale the switcher offers. With two languages this is simply the other one. */
  readonly other = computed<Locale>(() => (this.current() === 'en' ? 'es' : 'en'));

  /** Where the switcher goes: the same page in the other language, anchor and all. */
  readonly switchUrl = computed(() => translateUrl(this.url(), this.other()));

  constructor() {
    this.apply(this.fromUrl(this.router.url) ?? DEFAULT_LOCALE);
    this.url.set(this.router.url);

    this.router.events
      .pipe(filter((event): event is NavigationEnd => event instanceof NavigationEnd))
      .subscribe((event) => {
        this.url.set(event.urlAfterRedirects);
        const locale = this.fromUrl(event.urlAfterRedirects);
        if (locale && locale !== this.current()) {
          this.apply(locale);
        }
      });
  }

  /** Path in the current locale translated into the other one — used to build hreflang alternates. */
  alternates(path: string): Record<Locale, string> {
    return {
      en: translateUrl(path, 'en'),
      es: translateUrl(path, 'es'),
    };
  }

  /**
   * Only for the bare `/` entry point: last explicit choice, then the browser's preference, then
   * English. Never applied to a URL that already names a locale.
   */
  preferredLocale(): Locale {
    if (!this.isBrowser) {
      return DEFAULT_LOCALE;
    }

    const stored = localStorage.getItem(LOCALE_STORAGE_KEY);
    if (isLocale(stored)) {
      return stored;
    }

    for (const tag of navigator.languages ?? []) {
      const primary = tag.split('-')[0];
      if (isLocale(primary)) {
        return primary;
      }
    }

    return DEFAULT_LOCALE;
  }

  private fromUrl(url: string): Locale | null {
    const first = url.split('?')[0].split('#')[0].split('/').filter(Boolean)[0];
    return isLocale(first) ? first : null;
  }

  private apply(locale: Locale): void {
    this.current.set(locale);
    this.transloco.setActiveLang(locale);
    this.content.setLocale(locale);
    this.engineering.setLocale(locale);

    // Screen readers pick their voice from this attribute, so it is not decorative.
    this.document.documentElement.lang = locale;

    if (this.isBrowser) {
      localStorage.setItem(LOCALE_STORAGE_KEY, locale);
    }
  }
}
