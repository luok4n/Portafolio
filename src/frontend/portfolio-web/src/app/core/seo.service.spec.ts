import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';

import { SeoService } from './seo.service';

/**
 * Prerendering only pays off if each generated file carries its own metadata. These cover the part
 * that is easy to get wrong and invisible until a link is shared: tags being appended on every
 * navigation instead of replaced, and hreflang pointing at the wrong page.
 */
describe('SeoService', () => {
  let seo: SeoService;

  const apply = (overrides: Partial<Parameters<SeoService['apply']>[0]> = {}) =>
    seo.apply({
      locale: 'en',
      title: 'A title',
      description: 'A description',
      path: '/en',
      alternates: { en: '/en', es: '/es' },
      ...overrides,
    });

  beforeEach(() => {
    TestBed.configureTestingModule({});
    seo = TestBed.inject(SeoService);
    document.head.querySelectorAll('link[rel="canonical"], link[rel="alternate"]').forEach((l) => l.remove());
    document.getElementById('portfolio-json-ld')?.remove();
  });

  it('sets the title and the document language', () => {
    apply({ locale: 'es', title: 'Un título' });

    expect(document.title).toBe('Un título');
    // Not decorative: a screen reader picks its voice from this attribute.
    expect(document.documentElement.lang).toBe('es');
  });

  it('writes a self-referencing canonical', () => {
    apply({ path: '/en/projects/slang' });

    const canonical = document.head.querySelector<HTMLLinkElement>('link[rel="canonical"]');
    expect(canonical?.href).toContain('/en/projects/slang');
  });

  it('points hreflang at the translated page, not the other home page', () => {
    apply({
      path: '/en/projects/slang',
      alternates: { en: '/en/projects/slang', es: '/es/proyectos/slang' },
    });

    const es = document.head.querySelector<HTMLLinkElement>('link[rel="alternate"][hreflang="es"]');
    const en = document.head.querySelector<HTMLLinkElement>('link[rel="alternate"][hreflang="en"]');

    expect(es?.href).toContain('/es/proyectos/slang');
    expect(en?.href).toContain('/en/projects/slang');
  });

  it('declares an x-default', () => {
    apply();

    const xDefault = document.head.querySelector<HTMLLinkElement>('link[rel="alternate"][hreflang="x-default"]');
    expect(xDefault?.href).toContain('/en');
  });

  it('replaces its tags on navigation instead of appending', () => {
    // Every route change calls this. Appending would leave a page declaring four canonicals, which
    // is worse than declaring none.
    apply({ path: '/en' });
    apply({ path: '/en/engineering' });
    apply({ path: '/en/projects/moa' });

    expect(document.head.querySelectorAll('link[rel="canonical"]')).toHaveLength(1);
    expect(document.head.querySelectorAll('link[rel="alternate"][hreflang="es"]')).toHaveLength(1);
    expect(document.head.querySelector<HTMLLinkElement>('link[rel="canonical"]')?.href)
      .toContain('/en/projects/moa');
  });

  it('sets the social preview tags a shared link depends on', () => {
    apply({ title: 'Shared title', description: 'Shared description' });

    expect(document.head.querySelector('meta[property="og:title"]')?.getAttribute('content'))
      .toBe('Shared title');
    expect(document.head.querySelector('meta[property="og:description"]')?.getAttribute('content'))
      .toBe('Shared description');
    expect(document.head.querySelector('meta[name="description"]')?.getAttribute('content'))
      .toBe('Shared description');
  });

  it('keeps exactly one JSON-LD block and drops it when a page has none', () => {
    apply({ jsonLd: { '@type': 'Person', name: 'Someone' } });
    expect(document.querySelectorAll('#portfolio-json-ld')).toHaveLength(1);

    apply({ jsonLd: { '@type': 'Person', name: 'Someone Else' } });
    expect(document.querySelectorAll('#portfolio-json-ld')).toHaveLength(1);
    expect(document.getElementById('portfolio-json-ld')?.textContent).toContain('Someone Else');

    apply();
    expect(document.getElementById('portfolio-json-ld')).toBeNull();
  });

  it('builds a Person document search engines can read', () => {
    const jsonLd = SeoService.personJsonLd({
      name: 'Sebastián Vélez Ramírez',
      title: 'Senior .NET Developer',
      location: 'Risaralda, Colombia',
      email: 'someone@example.com',
      sameAs: ['https://example.com/in/someone'],
      skills: ['C#', '.NET'],
      alumniOf: 'Universidad Tecnológica de Pereira',
    });

    expect(jsonLd['@type']).toBe('Person');
    expect(jsonLd['jobTitle']).toBe('Senior .NET Developer');
    expect(jsonLd['email']).toBe('mailto:someone@example.com');
    expect(jsonLd['sameAs']).toEqual(['https://example.com/in/someone']);
  });
});
