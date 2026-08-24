import { DOCUMENT, Injectable, inject } from '@angular/core';
import { Meta, Title } from '@angular/platform-browser';

import { LOCALES, Locale } from './locale';

export interface SeoInput {
  locale: Locale;
  title: string;
  description: string;
  /** In-app path of the current page, without the origin. */
  path: string;
  /** Same page in every other locale, so hreflang can point at the real equivalent. */
  alternates: Record<Locale, string>;
  type?: 'website' | 'article' | 'profile';
  jsonLd?: Record<string, unknown>;
}

const SITE_ORIGIN = 'https://sebastianvelez.dev';

/**
 * Per-locale document metadata: title, description, Open Graph, canonical, hreflang and JSON-LD.
 *
 * Prerendering only pays off if each generated page carries its own metadata — otherwise every
 * route ships the same title and the same preview card, which is what makes a shared portfolio link
 * look broken in LinkedIn or WhatsApp. `hreflang` is reciprocal and points at the translated path,
 * not at the other locale's home page, because pointing at the wrong page is worse than omitting it.
 */
@Injectable({ providedIn: 'root' })
export class SeoService {
  private readonly title = inject(Title);
  private readonly meta = inject(Meta);
  private readonly document = inject(DOCUMENT);

  apply(input: SeoInput): void {
    this.title.setTitle(input.title);
    this.document.documentElement.lang = input.locale;

    const url = `${SITE_ORIGIN}${input.path}`;

    this.meta.updateTag({ name: 'description', content: input.description });
    this.meta.updateTag({ property: 'og:title', content: input.title });
    this.meta.updateTag({ property: 'og:description', content: input.description });
    this.meta.updateTag({ property: 'og:type', content: input.type ?? 'website' });
    this.meta.updateTag({ property: 'og:url', content: url });
    this.meta.updateTag({ property: 'og:locale', content: input.locale === 'es' ? 'es_CO' : 'en_US' });
    this.meta.updateTag({ name: 'twitter:card', content: 'summary_large_image' });
    this.meta.updateTag({ name: 'twitter:title', content: input.title });
    this.meta.updateTag({ name: 'twitter:description', content: input.description });

    this.setLink('canonical', url, null);
    for (const locale of LOCALES) {
      this.setLink('alternate', `${SITE_ORIGIN}${input.alternates[locale]}`, locale);
    }
    this.setLink('alternate', `${SITE_ORIGIN}${input.alternates.en}`, 'x-default');

    this.setJsonLd(input.jsonLd);
  }

  /** Keyed by rel plus hreflang so a re-render replaces the tag instead of appending another. */
  private setLink(rel: string, href: string, hreflang: string | null): void {
    const selector = hreflang ? `link[rel="${rel}"][hreflang="${hreflang}"]` : `link[rel="${rel}"]:not([hreflang])`;
    let element = this.document.head.querySelector<HTMLLinkElement>(selector);

    if (!element) {
      element = this.document.createElement('link');
      element.setAttribute('rel', rel);
      if (hreflang) {
        element.setAttribute('hreflang', hreflang);
      }
      this.document.head.appendChild(element);
    }

    element.setAttribute('href', href);
  }

  private setJsonLd(data: Record<string, unknown> | undefined): void {
    const id = 'portfolio-json-ld';
    this.document.getElementById(id)?.remove();

    if (!data) {
      return;
    }

    const script = this.document.createElement('script');
    script.id = id;
    script.type = 'application/ld+json';
    script.textContent = JSON.stringify(data);
    this.document.head.appendChild(script);
  }

  static personJsonLd(input: {
    name: string;
    title: string;
    location: string;
    email: string;
    sameAs: string[];
    skills: string[];
    alumniOf: string;
  }): Record<string, unknown> {
    return {
      '@context': 'https://schema.org',
      '@type': 'Person',
      name: input.name,
      jobTitle: input.title,
      email: `mailto:${input.email}`,
      address: { '@type': 'PostalAddress', addressLocality: input.location },
      knowsAbout: input.skills,
      alumniOf: { '@type': 'CollegeOrUniversity', name: input.alumniOf },
      sameAs: input.sameAs,
    };
  }
}
