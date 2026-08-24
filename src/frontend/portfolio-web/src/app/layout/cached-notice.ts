import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslocoDirective } from '@jsverse/transloco';

import { ContentService } from '../core/content.service';

/**
 * The only thing the reader ever sees about content loading.
 *
 * There is no spinner and no error screen anywhere on this site: the content is already in the
 * prerendered HTML, so the API can only improve it. When the API cannot be reached this says so
 * quietly and the page carries on being complete — which is the difference between a portfolio that
 * survives a cold start during an interview and one that does not.
 */
@Component({
  selector: 'app-cached-notice',
  imports: [TranslocoDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (showing()) {
      <p class="notice" role="status" *transloco="let t">{{ t('state.cachedContent') }}</p>
    }
  `,
  styles: `
    .notice {
      max-width: var(--measure-wide);
      margin: 0 auto;
      padding: 0.5rem var(--gutter);
      font-size: 0.8rem;
      color: var(--text-muted);
      border-bottom: 1px dashed var(--border);
    }
  `,
})
export class CachedNotice {
  private readonly content = inject(ContentService);

  /**
   * Hidden during prerendering and on first paint. Every prerendered page is technically "cached",
   * and flashing that notice on every load would train the reader to ignore it — so it appears only
   * once a revalidation has actually been attempted and failed.
   */
  protected readonly showing = this.content.revalidationFailed;
}
