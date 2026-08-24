import { ChangeDetectionStrategy, Component, effect, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Meta, Title } from '@angular/platform-browser';
import { TranslocoDirective, TranslocoService } from '@jsverse/transloco';

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
  private readonly transloco = inject(TranslocoService);
  private readonly title = inject(Title);
  private readonly meta = inject(Meta);

  constructor() {
    effect(() => {
      const locale = this.localeService.locale();

      // The prerendered /en/404 file is what nginx returns as the error document, so it needs its
      // own title — otherwise a not-found response is titled as if it were the home page, which is
      // what a browser tab and a shared link would show.
      this.title.setTitle(`${this.transloco.translate('error.notFoundTitle', {}, locale)} — 404`);

      // Deliberately not indexed. It is a real prerendered file, so without this a crawler would
      // happily add /en/404 to the index as a normal page.
      this.meta.updateTag({ name: 'robots', content: 'noindex, follow' });
    });
  }

  protected home(): string {
    return pathFor(this.localeService.locale(), 'home');
  }
}
