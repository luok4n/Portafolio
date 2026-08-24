import { Injectable, Provider } from '@angular/core';
import { Translation, TranslocoLoader, provideTransloco } from '@jsverse/transloco';
import { Observable, of } from 'rxjs';

import { DEFAULT_LOCALE, LOCALES } from './locale';

import en from '../../i18n/en.json';
import es from '../../i18n/es.json';

const TRANSLATIONS: Record<string, Translation> = { en, es };

/**
 * Serves the UI strings from the bundle rather than over HTTP.
 *
 * Transloco's default loader fetches a JSON file per language. That breaks during prerendering,
 * where a relative URL has no host and there is no server to answer it, and it would put a network
 * request in front of the first paint in the browser for two small files. Importing them makes
 * prerendering deterministic and offline, and language switching stays instant — which was the
 * actual requirement in ADR-0001, not the fetching.
 */
@Injectable({ providedIn: 'root' })
export class BundledTranslocoLoader implements TranslocoLoader {
  getTranslation(lang: string): Observable<Translation> {
    // Synchronous under the hood: the file is already in the bundle, so this resolves immediately
    // and prerendering never waits on it.
    return of(TRANSLATIONS[lang] ?? TRANSLATIONS[DEFAULT_LOCALE]);
  }
}

export function provideAppTransloco(): Provider[] {
  return [
    provideTransloco({
      config: {
        availableLangs: [...LOCALES],
        defaultLang: DEFAULT_LOCALE,
        fallbackLang: DEFAULT_LOCALE,
        reRenderOnLangChange: true,
        prodMode: true,
        missingHandler: {
          // A missing key must be loud in development and harmless in production: showing the key
          // is more useful than showing an empty element where a button label should be.
          allowEmpty: false,
          useFallbackTranslation: true,
        },
      },
      loader: BundledTranslocoLoader,
    }),
  ];
}
