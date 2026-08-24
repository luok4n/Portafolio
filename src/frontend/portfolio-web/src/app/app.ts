import { Component, ChangeDetectionStrategy } from '@angular/core';
import { RouterOutlet } from '@angular/router';

import { SiteHeader } from './layout/site-header';
import { SiteFooter } from './layout/site-footer';
import { TranslocoDirective } from '@jsverse/transloco';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, SiteHeader, SiteFooter, TranslocoDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ng-container *transloco="let t">
      <a class="skip-link" href="#main">{{ t('nav.skipToContent') }}</a>
    </ng-container>

    <app-site-header />

    <main id="main" tabindex="-1">
      <router-outlet />
    </main>

    <app-site-footer />
  `,
  styles: `
    :host {
      display: flex;
      flex-direction: column;
      min-height: 100dvh;
    }

    main {
      flex: 1;
      outline: none;
    }

    /* Visible only when focused: the first tab stop on the page skips the navigation. */
    .skip-link {
      position: absolute;
      left: -9999px;
      z-index: 100;
      padding: 0.75rem 1rem;
      background: var(--accent);
      color: var(--accent-contrast);
      border-radius: 0 0 var(--radius) 0;
    }

    .skip-link:focus {
      left: 0;
      top: 0;
    }
  `,
})
export class App {}
