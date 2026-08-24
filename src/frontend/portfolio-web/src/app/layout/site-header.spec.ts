import { Component } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { beforeEach, describe, expect, it } from 'vitest';

import { SiteHeader } from './site-header';
import { provideAppTransloco } from '../core/transloco';

@Component({ template: '' })
class Blank {}

/**
 * The language switcher, rendered.
 *
 * It has already produced two bugs that only appeared in the output — a link that lost its leading
 * slash and a fragment that got percent-encoded. Asserting on the rendered href is the level where
 * those are visible.
 */
describe('SiteHeader', () => {
  let fixture: ComponentFixture<SiteHeader>;
  let router: Router;

  const hrefs = () =>
    Array.from(fixture.nativeElement.querySelectorAll('a')).map((a) => (a as HTMLAnchorElement).getAttribute('href'));

  beforeEach(async () => {
    localStorage.clear();

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        ...provideAppTransloco(),
        provideRouter([
          { path: 'en', children: [{ path: '', component: Blank }, { path: 'projects/:id', component: Blank }] },
          { path: 'es', children: [{ path: '', component: Blank }, { path: 'proyectos/:id', component: Blank }] },
          { path: '**', component: Blank },
        ]),
      ],
    });

    router = TestBed.inject(Router);
    await router.navigateByUrl('/en');

    fixture = TestBed.createComponent(SiteHeader);
    await fixture.whenStable();
  });

  it('shows the language it goes to, not the one you are on', async () => {
    // The reader is asking "how do I get Spanish", not "what am I reading".
    const link = fixture.nativeElement.querySelector('a.lang') as HTMLAnchorElement;

    expect(link.textContent?.trim()).toBe('Español');
    expect(link.getAttribute('hreflang')).toBe('es');
  });

  it('every link is absolute', async () => {
    // A relative href resolves against the current route, so on /en/projects/slang the nav would
    // point at /en/projects/slang/en#about. That shipped once.
    for (const href of hrefs()) {
      expect(href?.startsWith('/')).toBe(true);
    }
  });

  it('anchors point at the current locale home', () => {
    expect(hrefs()).toContain('/en#about');
    expect(hrefs()).toContain('/en#experience');
    expect(hrefs()).toContain('/en#engineering');
  });

  it('follows the locale when the route changes', async () => {
    await router.navigateByUrl('/es');
    await fixture.whenStable();
    fixture.detectChanges();

    const link = fixture.nativeElement.querySelector('a.lang') as HTMLAnchorElement;

    expect(link.textContent?.trim()).toBe('English');
    expect(hrefs()).toContain('/es#about');
  });

  it('keeps the reader on the same page when switching from a detail route', async () => {
    await router.navigateByUrl('/es/proyectos/linkvest');
    await fixture.whenStable();
    fixture.detectChanges();

    const link = fixture.nativeElement.querySelector('a.lang') as HTMLAnchorElement;

    expect(link.getAttribute('href')).toBe('/en/projects/linkvest');
  });

  it('translates the navigation labels', async () => {
    const labels = () =>
      Array.from(fixture.nativeElement.querySelectorAll('.links a')).map((a) => (a as HTMLElement).textContent?.trim());

    expect(labels()).toContain('Experience');

    await router.navigateByUrl('/es');
    await fixture.whenStable();
    fixture.detectChanges();

    expect(labels()).toContain('Experiencia');
  });
});
