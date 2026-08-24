import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslocoDirective } from '@jsverse/transloco';

import { ContentService } from '../core/content.service';
import { LocaleService } from '../core/locale.service';
import { pathFor } from '../core/locale';

@Component({
  selector: 'app-hero',
  imports: [RouterLink, TranslocoDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="hero" *transloco="let t">
      <!--
        Everything a recruiter needs in ten seconds, above the fold: who, what, whether he is
        available, and how to reach him.
      -->
      @if (profile().openToWork) {
        <p class="badge"><span class="dot" aria-hidden="true"></span>{{ t('hero.openToWork') }}</p>
      }

      <h1>{{ profile().name }}</h1>
      <p class="title">{{ profile().title }}</p>

      <!-- The number is computed by the API from the real dates; it is never a stored string. -->
      <p class="years">{{ t('hero.yearsAcross', { years: profile().yearsOfExperience }) }}</p>

      <ul class="stack" aria-label="Core technologies">
        @for (tech of coreStack; track tech) {
          <li>{{ tech }}</li>
        }
      </ul>

      <div class="actions">
        <a class="button primary" [href]="home() + '#contact'">{{ t('hero.contact') }}</a>
        <a class="button" [href]="cvUrl()" download>{{ t('hero.downloadCv') }}</a>
        <a class="button ghost" [routerLink]="engineeringPath()">{{ t('engineering.readMore') }}</a>
      </div>
    </section>
  `,
  styles: `
    .hero {
      max-width: var(--measure-wide);
      margin: 0 auto;
      padding: clamp(3rem, 9vw, 6rem) var(--gutter) 3rem;
    }

    .badge {
      display: inline-flex;
      align-items: center;
      gap: 0.5rem;
      font-size: 0.82rem;
      font-weight: 600;
      color: var(--positive);
      background: color-mix(in srgb, var(--positive) 12%, transparent);
      border: 1px solid color-mix(in srgb, var(--positive) 35%, transparent);
      border-radius: 999px;
      padding: 0.3rem 0.75rem;
      margin: 0 0 1.25rem;
    }

    .dot {
      width: 0.5rem;
      height: 0.5rem;
      border-radius: 50%;
      background: var(--positive);
    }

    h1 {
      font-size: clamp(2.25rem, 6vw, 3.75rem);
      line-height: 1.05;
      letter-spacing: -0.02em;
      margin: 0 0 0.4rem;
    }

    .title {
      font-size: clamp(1.1rem, 2.5vw, 1.4rem);
      color: var(--accent);
      font-weight: 600;
      margin: 0 0 1rem;
    }

    .years {
      max-width: var(--measure);
      color: var(--text-muted);
      font-size: 1.05rem;
      margin: 0 0 1.5rem;
    }

    .stack {
      display: flex;
      flex-wrap: wrap;
      gap: 0.4rem;
      list-style: none;
      padding: 0;
      margin: 0 0 2rem;
    }

    .stack li {
      font-size: 0.8rem;
      font-family: var(--font-mono);
      color: var(--text-muted);
      border: 1px solid var(--border);
      border-radius: var(--radius);
      padding: 0.2rem 0.55rem;
    }

    .actions {
      display: flex;
      flex-wrap: wrap;
      gap: 0.75rem;
    }
  `,
})
export class HeroSection {
  private readonly content = inject(ContentService);
  private readonly localeService = inject(LocaleService);

  protected readonly profile = this.content.profile;

  /**
   * A curated subset, not the full skills list. The hero answers "can this person do the job";
   * eleven categories of technology answer a different question further down the page.
   */
  protected readonly coreStack = ['C#', '.NET', 'ASP.NET Core', 'Azure', 'PostgreSQL', 'Angular'];

  protected readonly cvUrl = computed(
    () => `/cv/Sebastian_Velez_CV_${this.localeService.locale().toUpperCase()}.pdf`,
  );

  protected home(): string {
    return pathFor(this.localeService.locale(), 'home');
  }

  protected engineeringPath(): string {
    return pathFor(this.localeService.locale(), 'engineering');
  }
}
