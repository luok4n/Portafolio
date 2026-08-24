import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslocoDirective } from '@jsverse/transloco';

import { LocaleService } from '../core/locale.service';
import { pathFor } from '../core/locale';

@Component({
  selector: 'app-not-found-page',
  imports: [RouterLink, TranslocoDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="band" *transloco="let t">
      <div class="inner">
        <p class="code">404</p>
        <h1>{{ t('error.notFoundTitle') }}</h1>
        <p class="body">{{ t('error.notFoundBody') }}</p>
        <p><a class="button primary" [routerLink]="home()">{{ t('error.backHome') }}</a></p>
      </div>
    </section>
  `,
  styles: `
    .code {
      font-family: var(--font-mono);
      font-size: 3rem;
      color: var(--accent);
      margin: 0;
      line-height: 1;
    }

    h1 { margin: 0.5rem 0 0.75rem; }

    .body { color: var(--text-muted); max-width: var(--measure); }
  `,
})
export class NotFoundPage {
  private readonly localeService = inject(LocaleService);

  protected home(): string {
    return pathFor(this.localeService.locale(), 'home');
  }
}
