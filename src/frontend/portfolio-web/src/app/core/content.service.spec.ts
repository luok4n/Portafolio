import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { beforeEach, describe, expect, it } from 'vitest';

import { ContentService } from './content.service';
import { PortfolioContent } from './content.models';

/**
 * The behaviour these cover is the whole argument of ADR-0002: the page is never empty, never shows
 * a spinner, and never shows an error — because the content is already in the bundle and the API can
 * only improve it.
 */
describe('ContentService', () => {
  let service: ContentService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });

    service = TestBed.inject(ContentService);
    http = TestBed.inject(HttpTestingController);
  });

  it('has content before any request is made', () => {
    // Prerendering depends on this: no fetch happens during the server render, so the snapshot has
    // to be enough on its own.
    expect(service.content().profile.name).toBeTruthy();
    expect(service.experience().length).toBeGreaterThan(0);
    expect(service.projects().length).toBeGreaterThan(0);
    expect(service.origin()).toBe('snapshot');
  });

  it('serves the requested language from the snapshot', () => {
    service.setLocale('es');
    http.expectOne('/api/content?lang=es').flush(null, { status: 500, statusText: 'boom' });

    const es = service.content();

    service.setLocale('en');
    http.expectOne('/api/content?lang=en').flush(null, { status: 500, statusText: 'boom' });

    expect(service.content().profile.summary).not.toBe(es.profile.summary);
  });

  it('replaces the snapshot once the API answers', async () => {
    service.setLocale('en');

    const fresh = structuredClone(service.content()) as PortfolioContent;
    fresh.profile = { ...fresh.profile, name: 'Newer Name' };

    http.expectOne('/api/content?lang=en').flush(fresh);
    await Promise.resolve();

    expect(service.content().profile.name).toBe('Newer Name');
    expect(service.origin()).toBe('api');
    expect(service.revalidationFailed()).toBe(false);
  });

  it('keeps the snapshot and flags the failure when the API is unreachable', async () => {
    service.setLocale('en');
    const before = service.content().profile.name;

    http.expectOne('/api/content?lang=en').error(new ProgressEvent('network error'));
    await Promise.resolve();

    // The reader still sees a complete page; only the discreet "cached" notice appears.
    expect(service.content().profile.name).toBe(before);
    expect(service.origin()).toBe('snapshot');
    expect(service.revalidationFailed()).toBe(true);
  });

  it('rejects a response that parses but is not content', async () => {
    // A captive portal or an error page can return valid JSON. Trusting it would blank the site.
    service.setLocale('en');
    const before = service.content().profile.name;

    http.expectOne('/api/content?lang=en').flush({ error: 'gateway timeout' });
    await Promise.resolve();

    expect(service.content().profile.name).toBe(before);
    expect(service.revalidationFailed()).toBe(true);
  });

  it('does not flag a failure before anything has been tried', () => {
    // Every prerendered page starts on the snapshot. Flagging that as "cached" would show the notice
    // on every first paint and train the reader to ignore it.
    expect(service.revalidationFailed()).toBe(false);
  });

  it('does not refetch a language it already has', async () => {
    service.setLocale('en');
    http.expectOne('/api/content?lang=en').flush(structuredClone(service.content()));
    await Promise.resolve();

    service.setLocale('es');
    http.expectOne('/api/content?lang=es').flush(structuredClone(service.content()));
    await Promise.resolve();

    service.setLocale('en');
    http.expectNone('/api/content?lang=en');
  });

  it('finds a project by id and reports nothing for an unknown one', () => {
    const known = service.projects()[0];

    expect(service.projectById(known.id)?.name).toBe(known.name);
    expect(service.projectById('no-such-project')).toBeUndefined();
  });

  it('exposes every project id for the prerender route generator', () => {
    const ids = ContentService.allProjectIds();

    expect(ids.length).toBeGreaterThan(0);
    expect(new Set(ids).size).toBe(ids.length);
    // A route generated for a project the bundle does not contain would prerender an empty page.
    for (const id of ids) {
      expect(ContentService.snapshotFor('en').projects.some((p) => p.id === id)).toBe(true);
    }
  });

  it('serves the same facts in both languages', () => {
    const en = ContentService.snapshotFor('en');
    const es = ContentService.snapshotFor('es');

    expect(es.experience.map((e) => e.id)).toEqual(en.experience.map((e) => e.id));
    expect(es.experience.map((e) => e.start)).toEqual(en.experience.map((e) => e.start));
    expect(es.profile.monthsOfExperience).toBe(en.profile.monthsOfExperience);
  });
});
