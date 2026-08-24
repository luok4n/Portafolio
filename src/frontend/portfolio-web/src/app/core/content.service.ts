import { HttpClient } from '@angular/common/http';
import { Injectable, PLATFORM_ID, computed, inject, signal } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { firstValueFrom } from 'rxjs';

import { DEFAULT_LOCALE, Locale } from './locale';
import { ContentOrigin, PortfolioContent, Project } from './content.models';

import enSnapshot from '../../content-snapshot/en.json';
import esSnapshot from '../../content-snapshot/es.json';

const SNAPSHOTS: Record<Locale, PortfolioContent> = {
  en: enSnapshot as unknown as PortfolioContent,
  es: esSnapshot as unknown as PortfolioContent,
};

/**
 * Serves the portfolio content, from the build-time snapshot first and the API second.
 *
 * This inversion is the whole of ADR-0002. The snapshot is imported, not fetched, so prerendering is
 * deterministic and offline: every page is rendered to HTML at build time from content that is
 * already in the bundle. In the browser the service then revalidates against the API and swaps in
 * anything newer.
 *
 * The consequence that matters: there is no loading state and no error state for content. If the
 * API is asleep, unreachable, or simply slow, the page a visitor sees is complete — it just shows a
 * discreet note that the content is cached. A portfolio that displays a spinner or an error while
 * someone reads it during an interview has failed at the only moment that counted.
 */
@Injectable({ providedIn: 'root' })
export class ContentService {
  private readonly http = inject(HttpClient);
  private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));

  private readonly locale = signal<Locale>(DEFAULT_LOCALE);
  private readonly live = signal<Partial<Record<Locale, PortfolioContent>>>({});
  private readonly failed = signal(false);

  /** Never null. Falls back to the snapshot, which is always present. */
  readonly content = computed<PortfolioContent>(
    () => this.live()[this.locale()] ?? SNAPSHOTS[this.locale()],
  );

  readonly origin = computed<ContentOrigin>(() => (this.live()[this.locale()] ? 'api' : 'snapshot'));

  /**
   * True only after a revalidation was attempted and did not succeed. Every prerendered page starts
   * out serving the snapshot, so `origin` alone would flag every first paint as cached and the
   * reader would learn to ignore the notice.
   */
  readonly revalidationFailed = this.failed.asReadonly();

  readonly profile = computed(() => this.content().profile);
  readonly experience = computed(() => this.content().experience);
  readonly projects = computed(() => this.content().projects);
  readonly featuredProjects = computed(() => this.content().projects.filter((p) => p.featured));
  readonly skills = computed(() => this.content().skills);
  readonly education = computed(() => this.content().education);
  readonly socialLinks = computed(() => this.content().socialLinks);

  setLocale(locale: Locale): void {
    this.locale.set(locale);
    void this.revalidate(locale);
  }

  projectById(id: string): Project | undefined {
    return this.content().projects.find((p) => p.id === id);
  }

  /** Every project id, across every locale — what the prerender route generator needs. */
  static allProjectIds(): string[] {
    return [...new Set(Object.values(SNAPSHOTS).flatMap((c) => c.projects.map((p) => p.id)))];
  }

  static snapshotFor(locale: Locale): PortfolioContent {
    return SNAPSHOTS[locale];
  }

  /**
   * Browser-only, and deliberately silent on failure. A failed revalidation is not an error the
   * reader needs to see: they already have the content.
   */
  private async revalidate(locale: Locale): Promise<void> {
    if (!this.isBrowser || this.live()[locale]) {
      return;
    }

    try {
      const fresh = await firstValueFrom(
        this.http.get<PortfolioContent>(`/api/content?lang=${locale}`),
      );

      // Guard against a proxy or an error page returning something that parses but is not content.
      if (fresh?.profile?.name && Array.isArray(fresh.experience)) {
        this.live.update((current) => ({ ...current, [locale]: fresh }));
        this.failed.set(false);
      } else {
        this.failed.set(true);
      }
    } catch {
      // Keep the snapshot. The page stays complete; the notice says the content is cached.
      this.failed.set(true);
    }
  }
}
