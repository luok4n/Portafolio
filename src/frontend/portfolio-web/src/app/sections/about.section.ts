import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslocoDirective } from '@jsverse/transloco';

import { ContentService } from '../core/content.service';

@Component({
  selector: 'app-about',
  imports: [TranslocoDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section id="about" class="band" *transloco="let t">
      <div class="inner">
        <h2>{{ t('section.about') }}</h2>
        <!-- The same sentence the generated CV prints, by construction: one template, one source. -->
        <p class="summary">{{ profile().summary }}</p>

        <ul class="languages">
          @for (language of profile().languages; track language.language) {
            <li><strong>{{ language.language }}</strong> {{ language.level }}</li>
          }
          <li><strong>{{ profile().location }}</strong></li>
        </ul>
      </div>
    </section>
  `,
  styles: `
    .summary {
      max-width: var(--measure);
      font-size: 1.05rem;
      line-height: 1.7;
      color: var(--text);
    }

    .languages {
      display: flex;
      flex-wrap: wrap;
      gap: 1.25rem;
      list-style: none;
      padding: 0;
      margin: 1.5rem 0 0;
      font-size: 0.9rem;
      color: var(--text-muted);
    }

    .languages strong {
      color: var(--text);
      font-weight: 600;
      margin-right: 0.35rem;
    }
  `,
})
export class AboutSection {
  protected readonly profile = inject(ContentService).profile;
}
