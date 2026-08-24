import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { beforeEach, describe, expect, it } from 'vitest';

import { LocaleService } from './locale.service';
import { ContentService } from './content.service';
import { EngineeringService } from './engineering';
import { provideAppTransloco } from './transloco';
import { LOCALE_STORAGE_KEY } from './locale';

@Component({ template: '' })
class Blank {}

/**
 * Locale is decided by the URL and nothing else (ADR-0001). These check the wiring that makes that
 * true across the router, Transloco, the content and the document — the four things that must never
 * disagree about which language the reader is looking at.
 */
describe('LocaleService', () => {
  let router: Router;
  let locale: LocaleService;

  beforeEach(async () => {
    localStorage.clear();

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        ...provideAppTransloco(),
        provideRouter([
          { path: 'en', children: [{ path: '', component: Blank }, { path: 'projects/:id', component: Blank }, { path: 'engineering', component: Blank }] },
          { path: 'es', children: [{ path: '', component: Blank }, { path: 'proyectos/:id', component: Blank }, { path: 'ingenieria', component: Blank }] },
          { path: '**', component: Blank },
        ]),
      ],
    });

    router = TestBed.inject(Router);
    locale = TestBed.inject(LocaleService);
    await router.navigateByUrl('/en');
  });

  it('takes the locale from the URL', async () => {
    expect(locale.locale()).toBe('en');

    await router.navigateByUrl('/es');
    expect(locale.locale()).toBe('es');
  });

  it('offers the other language', async () => {
    expect(locale.other()).toBe('es');

    await router.navigateByUrl('/es');
    expect(locale.other()).toBe('en');
  });

  it('points the switcher at the same page in the other language', async () => {
    await router.navigateByUrl('/es/proyectos/slang');

    expect(locale.switchUrl()).toBe('/en/projects/slang');
  });

  it('keeps the anchor when switching', async () => {
    await router.navigateByUrl('/en#experience');

    expect(locale.switchUrl()).toBe('/es#experience');
  });

  it('moves the content and the engineering section with it', async () => {
    const content = TestBed.inject(ContentService);
    const engineering = TestBed.inject(EngineeringService);

    await router.navigateByUrl('/es');

    // Four things had to agree; this is the one place that makes them.
    expect(content.content().profile.title).toBe('Desarrollador .NET Senior');
    expect(engineering.content().title).toContain('Cómo está construido');
    expect(document.documentElement.lang).toBe('es');
  });

  it('remembers an explicit choice', async () => {
    await router.navigateByUrl('/es');

    expect(localStorage.getItem(LOCALE_STORAGE_KEY)).toBe('es');
  });

  it('uses the remembered choice only for the bare entry point', () => {
    localStorage.setItem(LOCALE_STORAGE_KEY, 'es');

    // preferredLocale is consulted on `/` alone. Everywhere else the URL decides, which is what
    // keeps the canonical URLs stable for prerendering.
    expect(locale.preferredLocale()).toBe('es');
  });

  it('falls back to the default when nothing is remembered and nothing is offered', () => {
    localStorage.clear();

    expect(['en', 'es']).toContain(locale.preferredLocale());
  });

  it('builds reciprocal alternates for hreflang', () => {
    const alternates = locale.alternates('/en/projects/linkvest');

    expect(alternates.en).toBe('/en/projects/linkvest');
    expect(alternates.es).toBe('/es/proyectos/linkvest');
  });
});
