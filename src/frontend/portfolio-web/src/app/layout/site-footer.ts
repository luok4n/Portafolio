import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslocoDirective } from '@jsverse/transloco';

import { ContentService } from '../core/content.service';
import { EngineeringService } from '../core/engineering';

@Component({
  selector: 'app-site-footer',
  imports: [TranslocoDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <footer *transloco="let t">
      <div class="inner">
        <p>{{ t('footer.builtWith') }}</p>
        <p class="links">
          <!--
            The strongest argument on the page is that the repository is public: everything the
            engineering section claims can be checked.
          -->
          <a [href]="engineering.repositoryUrl" rel="noopener">{{ t('footer.source') }}</a>
          <span aria-hidden="true">·</span>
          <span>© {{ year }} {{ content.profile().name }}</span>
        </p>
        <p class="fine">{{ t('footer.rights') }}</p>
      </div>
    </footer>
  `,
  styles: `
    footer {
      border-top: 1px solid var(--border);
      margin-top: 4rem;
      background: var(--surface-sunken);
    }

    .inner {
      max-width: var(--measure-wide);
      margin: 0 auto;
      padding: 2rem var(--gutter);
      display: flex;
      flex-direction: column;
      gap: 0.4rem;
      font-size: 0.85rem;
      color: var(--text-muted);
    }

    .links {
      display: flex;
      gap: 0.5rem;
      align-items: center;
      flex-wrap: wrap;
    }

    a { color: var(--accent); }

    .fine { font-size: 0.78rem; opacity: 0.75; }

    p { margin: 0; }
  `,
})
export class SiteFooter {
  protected readonly content = inject(ContentService);
  protected readonly engineering = inject(EngineeringService);
  protected readonly year = new Date().getFullYear();
}
