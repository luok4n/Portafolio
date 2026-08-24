import { describe, expect, it } from 'vitest';

import { DEFAULT_LOCALE, LOCALES, isLocale, pathFor, translateUrl } from './locale';

/**
 * These two functions have already produced two shipped bugs — a dropped leading slash that turned
 * every navigation link relative, and a canonical URL of `https://sebastianvelez.deven`. Both were
 * found by reading generated HTML, which is a slow way to find out. These are the cheap way.
 */
describe('pathFor', () => {
  it('always returns an absolute path', () => {
    // The bug: filtering blank segments out of the array also removed the empty leading element,
    // so the path came back as "en" and every link became relative to the current route.
    for (const locale of LOCALES) {
      expect(pathFor(locale, 'home').startsWith('/')).toBe(true);
      expect(pathFor(locale, 'engineering').startsWith('/')).toBe(true);
      expect(pathFor(locale, 'projects', 'slang').startsWith('/')).toBe(true);
    }
  });

  it('builds the home path as just the locale', () => {
    expect(pathFor('en', 'home')).toBe('/en');
    expect(pathFor('es', 'home')).toBe('/es');
  });

  it('translates the segment but never the slug', () => {
    expect(pathFor('en', 'projects', 'argos-one')).toBe('/en/projects/argos-one');
    expect(pathFor('es', 'projects', 'argos-one')).toBe('/es/proyectos/argos-one');
  });

  it('translates the engineering segment', () => {
    expect(pathFor('en', 'engineering')).toBe('/en/engineering');
    expect(pathFor('es', 'engineering')).toBe('/es/ingenieria');
  });

  it('never emits a double slash', () => {
    for (const locale of LOCALES) {
      expect(pathFor(locale, 'home')).not.toContain('//');
      expect(pathFor(locale, 'projects', 'x')).not.toContain('//');
    }
  });
});

describe('translateUrl', () => {
  it('keeps the reader on the same page', () => {
    // Switching language while reading about Linkvest must show Linkvest, not the home page.
    expect(translateUrl('/en/projects/linkvest', 'es')).toBe('/es/proyectos/linkvest');
    expect(translateUrl('/es/proyectos/linkvest', 'en')).toBe('/en/projects/linkvest');
  });

  it('keeps the anchor', () => {
    expect(translateUrl('/en#experience', 'es')).toBe('/es#experience');
    expect(translateUrl('/es/ingenieria#top', 'en')).toBe('/en/engineering#top');
  });

  it('keeps the query string', () => {
    expect(translateUrl('/en/projects/slang?ref=cv', 'es')).toBe('/es/proyectos/slang?ref=cv');
  });

  it('returns an absolute path', () => {
    // The same missing-slash bug produced hreflang="https://sebastianvelez.deves/proyectos/slang".
    expect(translateUrl('/en/projects/slang', 'es').startsWith('/')).toBe(true);
    expect(translateUrl('/en', 'es').startsWith('/')).toBe(true);
    expect(translateUrl('/', 'es').startsWith('/')).toBe(true);
  });

  it('sends a URL with no locale to that locale home', () => {
    expect(translateUrl('/', 'es')).toBe('/es');
    expect(translateUrl('', 'en')).toBe('/en');
    expect(translateUrl('/something-odd', 'es')).toBe('/es');
  });

  it('round-trips', () => {
    for (const url of ['/en', '/en/engineering', '/en/projects/moa', '/en#skills']) {
      expect(translateUrl(translateUrl(url, 'es'), 'en')).toBe(url);
    }
  });

  it('passes an unrecognised segment through rather than dropping it', () => {
    // Losing the rest of the path is a worse answer than an imperfect translation of it.
    expect(translateUrl('/en/some-future-page/detail', 'es')).toBe('/es/some-future-page/detail');
  });
});

describe('isLocale', () => {
  it('accepts what is supported and nothing else', () => {
    expect(isLocale('en')).toBe(true);
    expect(isLocale('es')).toBe(true);
    expect(isLocale('de')).toBe(false);
    expect(isLocale('')).toBe(false);
    expect(isLocale(null)).toBe(false);
    expect(isLocale(undefined)).toBe(false);
  });

  it('agrees with the default', () => {
    expect(isLocale(DEFAULT_LOCALE)).toBe(true);
  });
});
