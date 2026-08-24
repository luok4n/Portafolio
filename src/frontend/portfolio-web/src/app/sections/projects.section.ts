import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslocoDirective } from '@jsverse/transloco';

import { ContentService } from '../core/content.service';
import { LocaleService } from '../core/locale.service';
import { pathFor } from '../core/locale';

@Component({
  selector: 'app-projects',
  imports: [RouterLink, TranslocoDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section id="projects" class="band" *transloco="let t">
      <div class="inner">
        <h2>{{ t('section.projects') }}</h2>

        <ul class="grid">
          @for (project of projects(); track project.id) {
            <li class="card">
              <a class="cover" [routerLink]="detail(project.id)">
                <h3>
                  {{ project.name }}
                  @if (project.featured) {
                    <span class="featured">{{ t('projects.featured') }}</span>
                  }
                </h3>
              </a>

              <p class="meta">
                <span>{{ project.client }}</span>
                @if (project.sector) {
                  <span class="sep" aria-hidden="true">·</span>
                  <span>{{ project.sector }}</span>
                }
              </p>

              <p class="summary">{{ project.summary }}</p>

              <ul class="tech">
                @for (tech of project.technologies; track tech) {
                  <li>{{ tech }}</li>
                }
              </ul>

              <p class="more">
                <a [routerLink]="detail(project.id)">{{ t('projects.viewDetail') }} →</a>
              </p>
            </li>
          }
        </ul>
      </div>
    </section>
  `,
  styles: `
    .grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
      gap: 1.25rem;
      list-style: none;
      padding: 0;
      margin: 0;
    }

    .card {
      display: flex;
      flex-direction: column;
      border: 1px solid var(--border);
      border-radius: var(--radius-lg);
      padding: 1.25rem;
      background: var(--surface);
      transition: border-color 120ms ease, transform 120ms ease;
    }

    .card:hover { border-color: var(--accent-soft); transform: translateY(-2px); }

    @media (prefers-reduced-motion: reduce) {
      .card { transition: none; }
      .card:hover { transform: none; }
    }

    .cover { text-decoration: none; color: inherit; }

    h3 {
      font-size: 1.05rem;
      margin: 0 0 0.25rem;
      display: flex;
      align-items: center;
      gap: 0.5rem;
      flex-wrap: wrap;
    }

    .featured {
      font-size: 0.68rem;
      text-transform: uppercase;
      letter-spacing: 0.06em;
      color: var(--accent);
      border: 1px solid var(--accent-soft);
      border-radius: 999px;
      padding: 0.1rem 0.45rem;
      font-weight: 700;
    }

    .meta {
      display: flex;
      gap: 0.4rem;
      font-size: 0.82rem;
      color: var(--text-muted);
      margin: 0 0 0.6rem;
    }

    .summary {
      font-size: 0.92rem;
      line-height: 1.6;
      margin: 0 0 1rem;
      display: -webkit-box;
      -webkit-line-clamp: 4;
      -webkit-box-orient: vertical;
      overflow: hidden;
    }

    .tech {
      display: flex;
      flex-wrap: wrap;
      gap: 0.3rem;
      list-style: none;
      padding: 0;
      margin: auto 0 0.9rem;
    }

    .tech li {
      font-size: 0.72rem;
      font-family: var(--font-mono);
      color: var(--text-muted);
      background: var(--surface-sunken);
      border-radius: var(--radius);
      padding: 0.12rem 0.4rem;
    }

    .more { margin: 0; font-size: 0.88rem; }
    .more a { font-weight: 600; }
  `,
})
export class ProjectsSection {
  private readonly localeService = inject(LocaleService);

  protected readonly projects = inject(ContentService).projects;

  protected detail(id: string): string {
    return pathFor(this.localeService.locale(), 'projects', id);
  }
}
