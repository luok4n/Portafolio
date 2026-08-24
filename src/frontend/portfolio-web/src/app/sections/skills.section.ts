import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslocoDirective } from '@jsverse/transloco';

import { ContentService } from '../core/content.service';

@Component({
  selector: 'app-skills',
  imports: [TranslocoDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section id="skills" class="band" *transloco="let t">
      <div class="inner">
        <h2>{{ t('section.skills') }}</h2>

        <!--
          No proficiency bars and no "level 4 of 5". They are unverifiable, nobody rates themselves
          honestly, and a technical interviewer reads them as noise. The category labels are
          translated; the technology names never are.
        -->
        <dl class="grid">
          @for (category of skills(); track category.id) {
            <div class="group">
              <dt>{{ category.label }}</dt>
              <dd>
                <ul>
                  @for (item of category.items; track item) {
                    <li>{{ item }}</li>
                  }
                </ul>
              </dd>
            </div>
          }
        </dl>
      </div>
    </section>
  `,
  styles: `
    .grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(260px, 1fr));
      gap: 1.5rem 2rem;
      margin: 0;
    }

    .group { min-width: 0; }

    dt {
      font-size: 0.78rem;
      font-weight: 700;
      text-transform: uppercase;
      letter-spacing: 0.07em;
      color: var(--text-muted);
      margin-bottom: 0.5rem;
    }

    dd { margin: 0; }

    ul {
      display: flex;
      flex-wrap: wrap;
      gap: 0.35rem;
      list-style: none;
      padding: 0;
      margin: 0;
    }

    li {
      font-size: 0.85rem;
      border: 1px solid var(--border);
      border-radius: var(--radius);
      padding: 0.2rem 0.55rem;
    }
  `,
})
export class SkillsSection {
  protected readonly skills = inject(ContentService).skills;
}
