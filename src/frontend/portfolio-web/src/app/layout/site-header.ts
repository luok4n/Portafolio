import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslocoDirective } from '@jsverse/transloco';

import { LocaleService } from '../core/locale.service';
import { LOCALE_NAMES, pathFor } from '../core/locale';

@Component({
  selector: 'app-site-header',
  imports: [RouterLink, TranslocoDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <header *transloco="let t">
      <nav [attr.aria-label]="t('nav.sections')">
        <a class="mark" [routerLink]="home()">SV</a>

        <ul class="links">
          @for (item of sections; track item.anchor) {
            <li><a [href]="home() + '#' + item.anchor">{{ t('section.' + item.key) }}</a></li>
          }
        </ul>

        <!--
          The switcher shows the language it goes to, not the current one, because that is the
          question the reader is asking. It keeps the route and the anchor, so switching while
          reading a project lands on that project rather than on the home page.
        -->
        <a
          class="lang"
          [routerLink]="locale.switchUrl()"
          [attr.hreflang]="locale.other()"
          [attr.lang]="locale.other()"
          [attr.aria-label]="t('nav.switchTo')"
        >{{ otherName() }}</a>
      </nav>
    </header>
  `,
  styles: `
    header {
      position: sticky;
      top: 0;
      z-index: 20;
      backdrop-filter: blur(10px);
      background: color-mix(in srgb, var(--surface) 85%, transparent);
      border-bottom: 1px solid var(--border);
    }

    nav {
      display: flex;
      align-items: center;
      gap: 1.5rem;
      max-width: var(--measure-wide);
      margin: 0 auto;
      padding: 0.75rem var(--gutter);
    }

    .mark {
      font-weight: 700;
      letter-spacing: 0.08em;
      color: var(--text);
      text-decoration: none;
      border: 1px solid var(--border);
      border-radius: var(--radius);
      padding: 0.2rem 0.5rem;
    }

    .links {
      display: flex;
      gap: 1.25rem;
      list-style: none;
      margin: 0;
      padding: 0;
      flex: 1;
      overflow-x: auto;
      scrollbar-width: none;
    }

    .links::-webkit-scrollbar { display: none; }

    .links a {
      color: var(--text-muted);
      text-decoration: none;
      font-size: 0.9rem;
      white-space: nowrap;
    }

    .links a:hover,
    .links a:focus-visible { color: var(--text); }

    .lang {
      font-size: 0.85rem;
      font-weight: 600;
      color: var(--accent);
      text-decoration: none;
      border: 1px solid var(--accent-soft);
      border-radius: var(--radius);
      padding: 0.3rem 0.6rem;
      white-space: nowrap;
    }

    @media (max-width: 640px) {
      .links { display: none; }
    }
  `,
})
export class SiteHeader {
  protected readonly locale = inject(LocaleService);

  protected readonly sections = [
    { key: 'about', anchor: 'about' },
    { key: 'experience', anchor: 'experience' },
    { key: 'projects', anchor: 'projects' },
    { key: 'skills', anchor: 'skills' },
    { key: 'engineering', anchor: 'engineering' },
    { key: 'contact', anchor: 'contact' },
  ];

  protected home(): string {
    return pathFor(this.locale.locale(), 'home');
  }

  protected otherName(): string {
    return LOCALE_NAMES[this.locale.other()];
  }
}
