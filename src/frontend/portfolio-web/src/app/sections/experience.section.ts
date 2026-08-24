import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslocoDirective } from '@jsverse/transloco';

import { ContentService } from '../core/content.service';
import { LocaleService } from '../core/locale.service';
import { formatDuration, formatPeriod } from '../core/format';
import { Experience } from '../core/content.models';

@Component({
  selector: 'app-experience',
  imports: [TranslocoDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section id="experience" class="band" *transloco="let t">
      <div class="inner">
        <h2>{{ t('section.experience') }}</h2>

        <ol class="timeline">
          @for (role of experience(); track role.id) {
            <li>
              <div class="head">
                <h3>{{ role.role }}</h3>
                <p class="company">{{ role.company }}</p>
                <p class="dates">
                  <span>{{ period(role) }}</span>
                  <span class="sep" aria-hidden="true">·</span>
                  <span>{{ duration(role) }}</span>
                </p>

                <!--
                  2022 has two roles running at once. Without saying so plainly, an attentive reader
                  concludes the portfolio has a data error, which is worse than not showing it.
                -->
                @if (role.concurrent) {
                  <p class="concurrent">{{ t('experience.concurrent') }}</p>
                }
              </div>

              @if (role.teams.length > 0) {
                <p class="teams">
                  <span class="label">{{ t('experience.teams') }}:</span> {{ role.teams.join(' · ') }}
                </p>
              }

              <ul class="highlights">
                @for (highlight of role.highlights; track highlight) {
                  <li>{{ highlight }}</li>
                }
              </ul>

              <ul class="tech" [attr.aria-label]="t('experience.technologies')">
                @for (tech of role.technologies; track tech) {
                  <li>{{ tech }}</li>
                }
              </ul>
            </li>
          }
        </ol>
      </div>
    </section>
  `,
  styles: `
    .timeline {
      list-style: none;
      margin: 0;
      padding: 0;
      border-left: 2px solid var(--border);
    }

    .timeline > li {
      position: relative;
      padding: 0 0 2.5rem 1.75rem;
    }

    .timeline > li::before {
      content: '';
      position: absolute;
      left: -0.45rem;
      top: 0.45rem;
      width: 0.75rem;
      height: 0.75rem;
      border-radius: 50%;
      background: var(--accent);
      box-shadow: 0 0 0 4px var(--surface);
    }

    h3 {
      font-size: 1.15rem;
      margin: 0;
    }

    .company {
      color: var(--accent);
      font-weight: 600;
      margin: 0.15rem 0 0.2rem;
    }

    .dates {
      display: flex;
      gap: 0.5rem;
      font-size: 0.85rem;
      color: var(--text-muted);
      font-family: var(--font-mono);
      margin: 0;
    }

    .concurrent {
      display: inline-block;
      font-size: 0.78rem;
      color: var(--warning);
      background: color-mix(in srgb, var(--warning) 12%, transparent);
      border: 1px solid color-mix(in srgb, var(--warning) 30%, transparent);
      border-radius: var(--radius);
      padding: 0.15rem 0.5rem;
      margin: 0.5rem 0 0;
    }

    .teams {
      font-size: 0.85rem;
      color: var(--text-muted);
      margin: 0.75rem 0 0;
    }

    .label { font-weight: 600; color: var(--text); }

    .highlights {
      margin: 0.85rem 0 0;
      padding-left: 1.1rem;
      max-width: var(--measure);
    }

    .highlights li {
      margin-bottom: 0.35rem;
      line-height: 1.6;
    }

    .tech {
      display: flex;
      flex-wrap: wrap;
      gap: 0.35rem;
      list-style: none;
      padding: 0;
      margin: 1rem 0 0;
    }

    .tech li {
      font-size: 0.75rem;
      font-family: var(--font-mono);
      color: var(--text-muted);
      background: var(--surface-sunken);
      border-radius: var(--radius);
      padding: 0.15rem 0.45rem;
    }
  `,
})
export class ExperienceSection {
  private readonly localeService = inject(LocaleService);

  protected readonly experience = inject(ContentService).experience;

  protected period(role: Experience): string {
    return formatPeriod(role.start, role.end, this.localeService.locale());
  }

  protected duration(role: Experience): string {
    return formatDuration(role.durationMonths, this.localeService.locale());
  }
}
