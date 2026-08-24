import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslocoDirective } from '@jsverse/transloco';

import { ContentService } from '../core/content.service';

@Component({
  selector: 'app-education',
  imports: [TranslocoDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section id="education" class="band" *transloco="let t">
      <div class="inner">
        <h2>{{ t('section.education') }}</h2>
        <ul>
          @for (entry of education(); track entry.id) {
            <li>
              <h3>{{ entry.degree }}</h3>
              <p>{{ entry.institution }} · {{ entry.location }}</p>
              <p class="year">{{ t('education.graduated', { year: entry.year }) }}</p>
            </li>
          }
        </ul>
      </div>
    </section>
  `,
  styles: `
    ul { list-style: none; padding: 0; margin: 0; }
    h3 { font-size: 1.05rem; margin: 0 0 0.2rem; }
    p { margin: 0; color: var(--text-muted); font-size: 0.92rem; }
    .year { font-family: var(--font-mono); font-size: 0.82rem; margin-top: 0.2rem; }
  `,
})
export class EducationSection {
  protected readonly education = inject(ContentService).education;
}

@Component({
  selector: 'app-contact',
  imports: [TranslocoDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section id="contact" class="band" *transloco="let t">
      <div class="inner">
        <h2>{{ t('section.contact') }}</h2>
        <p class="lead">{{ t('contact.lead') }}</p>

        <!--
          No phone number and no contact form. The number stays off an indexed page by policy
          (ADR-0003); a form would need an email provider and anti-spam handling to do the job a
          mailto link already does.
        -->
        <ul class="links">
          @for (link of links(); track link.id) {
            <li>
              <a [href]="link.url" [attr.rel]="link.id === 'email' ? null : 'noopener'">
                <span class="label">{{ link.label }}</span>
                <span class="value">{{ link.display }}</span>
              </a>
            </li>
          }
        </ul>
      </div>
    </section>
  `,
  styles: `
    .lead { color: var(--text-muted); max-width: var(--measure); }

    .links {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
      gap: 0.75rem;
      list-style: none;
      padding: 0;
      margin: 1.5rem 0 0;
    }

    a {
      display: flex;
      flex-direction: column;
      gap: 0.15rem;
      border: 1px solid var(--border);
      border-radius: var(--radius-lg);
      padding: 0.9rem 1rem;
      text-decoration: none;
      color: inherit;
    }

    a:hover { border-color: var(--accent-soft); }

    .label {
      font-size: 0.72rem;
      text-transform: uppercase;
      letter-spacing: 0.07em;
      color: var(--text-muted);
      font-weight: 700;
    }

    .value { color: var(--accent); word-break: break-word; }
  `,
})
export class ContactSection {
  protected readonly links = inject(ContentService).socialLinks;
}
