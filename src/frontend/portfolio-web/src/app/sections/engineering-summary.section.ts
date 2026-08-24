import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslocoDirective } from '@jsverse/transloco';

import { EngineeringService } from '../core/engineering';
import { LocaleService } from '../core/locale.service';
import { pathFor } from '../core/locale';

/**
 * The home page's teaser for the engineering section. The full argument lives on its own route.
 */
@Component({
  selector: 'app-engineering-summary',
  imports: [RouterLink, TranslocoDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section id="engineering" class="band engineering" *transloco="let t">
      <div class="inner">
        <h2>{{ t('section.engineering') }}</h2>
        <p class="lead">{{ engineering.content().lead }}</p>

        <!-- Measured from the repository at build time, never typed. See ADR-0005. -->
        <ul class="facts">
          <li><strong>{{ facts.tests }}</strong><span>{{ t('engineering.facts.tests') }}</span></li>
          <li><strong>{{ facts.tables }}</strong><span>{{ t('engineering.facts.tables') }}</span></li>
          <li><strong>{{ facts.endpoints }}</strong><span>{{ t('engineering.facts.endpoints') }}</span></li>
          <li><strong>{{ facts.adrs }}</strong><span>{{ t('engineering.facts.adrs') }}</span></li>
        </ul>
        <p class="generated">{{ t('engineering.generatedNote') }}</p>

        <p class="actions">
          <a class="button primary" [routerLink]="engineeringPath()">{{ t('engineering.readMore') }} →</a>
          <a class="button" [href]="engineering.repositoryUrl" rel="noopener">{{ t('engineering.viewRepository') }}</a>
        </p>
      </div>
    </section>
  `,
  styles: `
    .engineering .inner {
      border: 1px solid var(--accent-soft);
      border-radius: var(--radius-lg);
      padding: 2rem;
      background: color-mix(in srgb, var(--accent) 5%, var(--surface));
    }

    .lead {
      max-width: var(--measure);
      font-size: 1.05rem;
      line-height: 1.7;
    }

    .facts {
      display: flex;
      flex-wrap: wrap;
      gap: 2rem;
      list-style: none;
      padding: 0;
      margin: 1.75rem 0 0.5rem;
    }

    .facts li {
      display: flex;
      flex-direction: column;
    }

    .facts strong {
      font-size: 1.9rem;
      line-height: 1;
      font-variant-numeric: tabular-nums;
      color: var(--accent);
    }

    .facts span {
      font-size: 0.78rem;
      text-transform: uppercase;
      letter-spacing: 0.07em;
      color: var(--text-muted);
      margin-top: 0.25rem;
    }

    .generated {
      font-size: 0.78rem;
      color: var(--text-muted);
      margin: 0.75rem 0 0;
      font-style: italic;
    }

    .actions {
      display: flex;
      flex-wrap: wrap;
      gap: 0.75rem;
      margin: 1.75rem 0 0;
    }
  `,
})
export class EngineeringSummarySection {
  protected readonly engineering = inject(EngineeringService);
  private readonly localeService = inject(LocaleService);

  protected readonly facts = this.engineering.facts;

  protected engineeringPath(): string {
    return pathFor(this.localeService.locale(), 'engineering');
  }
}
