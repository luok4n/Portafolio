import { ChangeDetectionStrategy, Component, PLATFORM_ID, inject } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { Meta } from '@angular/platform-browser';
import { Router } from '@angular/router';

import { LocaleService } from '../core/locale.service';
import { DEFAULT_LOCALE, LOCALE_NAMES, LOCALES } from '../core/locale';

/**
 * The bare `/` entry point, and anything else without a locale prefix.
 *
 * This is the one place where the reader's preference is consulted, because the URL says nothing:
 * their last explicit choice, then the browser's, then English. Everywhere else the URL decides,
 * which is what keeps the canonical URLs stable for prerendering (ADR-0001).
 *
 * The page is prerendered as a static file, so it cannot issue an HTTP redirect. It ships a meta
 * refresh for readers without JavaScript, plain links as the last resort, and `noindex` so search
 * engines index the real localised pages rather than this hop.
 */
@Component({
  selector: 'app-locale-redirect-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="band">
      <div class="inner">
        <p>Choose a language · Elige un idioma</p>
        <ul>
          @for (locale of locales; track locale) {
            <li><a [href]="'/' + locale">{{ names[locale] }}</a></li>
          }
        </ul>
      </div>
    </section>
  `,
  styles: `
    ul { list-style: none; padding: 0; margin: 1rem 0 0; display: flex; gap: 1rem; }
    a { font-weight: 600; }
  `,
})
export class LocaleRedirectPage {
  private readonly router = inject(Router);
  private readonly meta = inject(Meta);
  private readonly localeService = inject(LocaleService);
  private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));

  protected readonly locales = LOCALES;
  protected readonly names = LOCALE_NAMES;

  constructor() {
    this.meta.updateTag({ name: 'robots', content: 'noindex, follow' });
    this.meta.updateTag({ httpEquiv: 'refresh', content: `0; url=/${DEFAULT_LOCALE}` }, 'http-equiv="refresh"');

    if (this.isBrowser) {
      void this.router.navigateByUrl(`/${this.localeService.preferredLocale()}`, { replaceUrl: true });
    }
  }
}
