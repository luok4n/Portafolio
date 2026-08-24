import { ChangeDetectionStrategy, Component, computed, effect, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { TranslocoDirective } from '@jsverse/transloco';

import { ContentService } from '../core/content.service';
import { LocaleService } from '../core/locale.service';
import { SeoService } from '../core/seo.service';
import { pathFor } from '../core/locale';
import { formatDate } from '../core/format';

@Component({
  selector: 'app-project-detail-page',
  imports: [RouterLink, TranslocoDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <article class="band" *transloco="let t">
      <div class="inner">
        <p class="back"><a [routerLink]="homePath()" fragment="projects">← {{ t('projects.back') }}</a></p>

        @if (project(); as p) {
          <h1>{{ p.name }}</h1>

          <dl class="meta">
            <div><dt>{{ t('projects.client') }}</dt><dd>{{ p.client }}</dd></div>
            @if (p.sector) {
              <div><dt>{{ t('projects.sector') }}</dt><dd>{{ p.sector }}</dd></div>
            }
            @if (p.company) {
              <div><dt>{{ t('projects.partOf') }}</dt><dd>{{ p.company }}</dd></div>
            }
          </dl>

          <p class="summary">{{ p.summary }}</p>

          @if (p.contribution) {
            <h2>{{ t('projects.contribution') }}</h2>
            <p class="summary">{{ p.contribution }}</p>
          }

          @if (p.technologies.length > 0) {
            <ul class="tech">
              @for (tech of p.technologies; track tech) {
                <li>{{ tech }}</li>
              }
            </ul>
          }

          <!--
            Sources are what separate "I worked on a multinational platform" from a claim the reader
            can confirm in one click. A project with none says so plainly rather than wearing a
            negative badge.
          -->
          @if (p.sources.length > 0) {
            <h2>{{ t('projects.sources') }}</h2>
            <ul class="sources">
              @for (source of p.sources; track source.url) {
                <li>
                  <a [href]="source.url" rel="noopener nofollow">{{ hostOf(source.url) }}</a>
                  <span class="checked">{{ t('projects.checked', { date: checked(source.checked) }) }}</span>
                </li>
              }
            </ul>
          } @else {
            <p class="unsourced">{{ t('projects.noSources') }}</p>
          }
        } @else {
          <h1>{{ t('error.notFoundTitle') }}</h1>
          <p class="summary">{{ t('error.notFoundBody') }}</p>
        }
      </div>
    </article>
  `,
  styles: `
    .back { font-size: 0.85rem; margin: 0 0 1rem; }

    h1 {
      font-size: clamp(1.8rem, 4.5vw, 2.6rem);
      line-height: 1.15;
      margin: 0 0 1.25rem;
    }

    h2 {
      font-size: 1rem;
      text-transform: uppercase;
      letter-spacing: 0.07em;
      color: var(--text-muted);
      margin: 2rem 0 0.5rem;
    }

    .meta {
      display: flex;
      flex-wrap: wrap;
      gap: 1.75rem;
      margin: 0 0 1.75rem;
      padding-bottom: 1.25rem;
      border-bottom: 1px solid var(--border);
    }

    .meta dt {
      font-size: 0.7rem;
      text-transform: uppercase;
      letter-spacing: 0.08em;
      color: var(--text-muted);
      font-weight: 700;
    }

    .meta dd { margin: 0.2rem 0 0; font-weight: 600; }

    .summary { max-width: var(--measure); line-height: 1.75; }

    .tech {
      display: flex;
      flex-wrap: wrap;
      gap: 0.35rem;
      list-style: none;
      padding: 0;
      margin: 1.5rem 0 0;
    }

    .tech li {
      font-size: 0.78rem;
      font-family: var(--font-mono);
      color: var(--text-muted);
      border: 1px solid var(--border);
      border-radius: var(--radius);
      padding: 0.18rem 0.5rem;
    }

    .sources { list-style: none; padding: 0; margin: 0; }

    .sources li {
      display: flex;
      gap: 0.6rem;
      align-items: baseline;
      flex-wrap: wrap;
      margin-bottom: 0.4rem;
    }

    .checked { font-size: 0.78rem; color: var(--text-muted); font-family: var(--font-mono); }

    .unsourced {
      max-width: var(--measure);
      font-size: 0.9rem;
      color: var(--text-muted);
      border-left: 2px solid var(--border);
      padding-left: 0.9rem;
      margin-top: 1.5rem;
    }
  `,
})
export class ProjectDetailPage {
  private readonly route = inject(ActivatedRoute);
  private readonly content = inject(ContentService);
  private readonly localeService = inject(LocaleService);
  private readonly seo = inject(SeoService);

  private readonly params = toSignal(this.route.paramMap, { requireSync: true });

  protected readonly project = computed(() => {
    const id = this.params().get('id');
    return id ? this.content.projectById(id) : undefined;
  });

  constructor() {
    effect(() => {
      const locale = this.localeService.locale();
      const project = this.project();
      if (!project) {
        return;
      }

      const path = pathFor(locale, 'projects', project.id);
      this.seo.apply({
        locale,
        title: `${project.name} · ${project.client} — Sebastián Vélez Ramírez`,
        description: project.summary.slice(0, 180),
        path,
        alternates: this.localeService.alternates(path),
        type: 'article',
      });
    });
  }

  /**
   * The fragment goes through routerLink's own input. Concatenating "#projects" into the link
   * string gets it percent-encoded, producing /en%23projects — a link that silently 404s.
   */
  protected homePath(): string {
    return pathFor(this.localeService.locale(), 'home');
  }

  /** Shows the domain rather than a 90-character URL, which would wrap badly and read as noise. */
  protected hostOf(url: string): string {
    try {
      return new URL(url).hostname.replace(/^www\./, '');
    } catch {
      return url;
    }
  }

  protected checked(iso: string): string {
    return formatDate(iso, this.localeService.locale());
  }
}
