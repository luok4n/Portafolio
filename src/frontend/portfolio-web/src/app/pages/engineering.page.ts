import { ChangeDetectionStrategy, Component, effect, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslocoDirective } from '@jsverse/transloco';

import { EngineeringService } from '../core/engineering';
import { LocaleService } from '../core/locale.service';
import { SeoService } from '../core/seo.service';
import { pathFor } from '../core/locale';
import { ArchitectureDiagram } from '../diagrams/architecture-diagram';
import { FlowDiagram } from '../diagrams/flow-diagram';
import { DataModelDiagram } from '../diagrams/data-model-diagram';

/**
 * The full "how this site is built" page.
 *
 * It is the reason the project exists. Everything else on the site is a claim about work done
 * elsewhere; this is the one part a reader can check line by line against a public repository.
 */
@Component({
  selector: 'app-engineering-page',
  imports: [RouterLink, TranslocoDirective, ArchitectureDiagram, FlowDiagram, DataModelDiagram],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <article *transloco="let t">
      <header class="band">
        <div class="inner">
          <p class="back"><a [routerLink]="home()">← {{ profileName() }}</a></p>
          <h1>{{ content().title }}</h1>
          <p class="lead">{{ content().lead }}</p>

          <ul class="facts">
            <li><strong>{{ facts.tests }}</strong><span>{{ t('engineering.facts.tests') }}</span></li>
            <li><strong>{{ facts.tables }}</strong><span>{{ t('engineering.facts.tables') }}</span></li>
            <li><strong>{{ facts.endpoints }}</strong><span>{{ t('engineering.facts.endpoints') }}</span></li>
            <li><strong>{{ facts.adrs }}</strong><span>{{ t('engineering.facts.adrs') }}</span></li>
          </ul>
          <p class="generated">{{ t('engineering.generatedNote') }}</p>
        </div>
      </header>

      <section class="band">
        <div class="inner prose">
          <h2>{{ content().why.title }}</h2>
          @for (paragraph of content().why.paragraphs; track paragraph) {
            <p>{{ paragraph }}</p>
          }

          <h2>{{ content().focus.title }}</h2>
          @for (paragraph of content().focus.paragraphs; track paragraph) {
            <p>{{ paragraph }}</p>
          }
        </div>
      </section>

      <section class="band">
        <div class="inner">
          <h2>{{ content().architecture.title }}</h2>
          <p class="prose">{{ content().architecture.description }}</p>
          <app-architecture-diagram />
          <ul class="notes">
            @for (note of content().architecture.notes; track note) {
              <li>{{ note }}</li>
            }
          </ul>
        </div>
      </section>

      <section class="band">
        <div class="inner">
          <h2>{{ t('engineering.decision') }}s</h2>
          <ul class="decisions">
            @for (decision of content().decisions; track decision.id) {
              <li>
                <h3>{{ decision.title }}</h3>
                <dl>
                  <div><dt>{{ t('engineering.problem') }}</dt><dd>{{ decision.problem }}</dd></div>
                  <div><dt>{{ t('engineering.decision') }}</dt><dd>{{ decision.decision }}</dd></div>
                  <div><dt>{{ t('engineering.rejected') }}</dt><dd>{{ decision.rejected }}</dd></div>
                  <div><dt>{{ t('engineering.cost') }}</dt><dd>{{ decision.cost }}</dd></div>
                </dl>
                @if (adrUrl(decision.adr); as url) {
                  <p class="adr"><a [href]="url" rel="noopener">{{ t('engineering.readAdr') }} →</a></p>
                }
              </li>
            }
          </ul>
        </div>
      </section>

      <section class="band">
        <div class="inner">
          <h2>{{ content().flows[0].title }}</h2>
          @for (flow of content().flows; track flow.id; let first = $first) {
            <div class="flow">
              @if (!first) {
                <h3>{{ flow.title }}</h3>
              }
              <p class="prose">{{ flow.description }}</p>
              <app-flow-diagram [steps]="flow.steps" [flowId]="flow.id" />
            </div>
          }
        </div>
      </section>

      <section class="band">
        <div class="inner">
          <h2>{{ content().dataModel.title }}</h2>
          <p class="prose">{{ content().dataModel.description }}</p>
          <app-data-model-diagram />
          <ul class="notes">
            @for (note of content().dataModel.notes; track note) {
              <li>{{ note }}</li>
            }
          </ul>
        </div>
      </section>

      <section class="band">
        <div class="inner">
          <h2>{{ content().testing.title }}</h2>
          <p class="prose">{{ content().testing.description }}</p>
          <ul class="notes">
            @for (item of content().testing.items; track item) {
              <li>{{ item }}</li>
            }
          </ul>
        </div>
      </section>

      <section class="band">
        <div class="inner">
          <h2>{{ content().operations.title }}</h2>
          <ul class="notes">
            @for (item of content().operations.items; track item) {
              <li>{{ item }}</li>
            }
          </ul>
        </div>
      </section>

      <section class="band">
        <div class="inner closing">
          <p class="prose">{{ content().repository.note }}</p>
          <p>
            <a class="button primary" [href]="engineering.repositoryUrl" rel="noopener">
              {{ content().repository.label }} →
            </a>
          </p>
        </div>
      </section>
    </article>
  `,
  styles: `
    h1 {
      font-size: clamp(1.9rem, 5vw, 2.9rem);
      line-height: 1.1;
      letter-spacing: -0.02em;
      margin: 0.5rem 0 1rem;
    }

    .back { font-size: 0.85rem; margin: 0; }

    .lead {
      max-width: var(--measure);
      font-size: 1.15rem;
      line-height: 1.7;
      color: var(--text-muted);
    }

    .prose { max-width: var(--measure); line-height: 1.75; }

    .facts {
      display: flex;
      flex-wrap: wrap;
      gap: 2.25rem;
      list-style: none;
      padding: 0;
      margin: 2rem 0 0.5rem;
    }

    .facts li { display: flex; flex-direction: column; }

    .facts strong {
      font-size: 2.1rem;
      line-height: 1;
      font-variant-numeric: tabular-nums;
      color: var(--accent);
    }

    .facts span {
      font-size: 0.78rem;
      text-transform: uppercase;
      letter-spacing: 0.07em;
      color: var(--text-muted);
      margin-top: 0.3rem;
    }

    .generated {
      font-size: 0.78rem;
      color: var(--text-muted);
      font-style: italic;
      margin: 0.6rem 0 0;
    }

    .notes {
      max-width: var(--measure);
      margin: 1.25rem 0 0;
      padding-left: 1.1rem;
      color: var(--text-muted);
    }

    .notes li { margin-bottom: 0.5rem; line-height: 1.65; }

    .decisions { list-style: none; padding: 0; margin: 0; display: grid; gap: 1rem; }

    .decisions > li {
      border: 1px solid var(--border);
      border-radius: var(--radius-lg);
      padding: 1.25rem 1.5rem;
      background: var(--surface);
    }

    .decisions h3 { margin: 0 0 0.9rem; font-size: 1.05rem; }

    .decisions dl { margin: 0; display: grid; gap: 0.6rem; }

    .decisions dt {
      font-size: 0.7rem;
      text-transform: uppercase;
      letter-spacing: 0.08em;
      font-weight: 700;
      color: var(--text-muted);
    }

    .decisions dd {
      margin: 0.15rem 0 0;
      max-width: var(--measure);
      line-height: 1.6;
      font-size: 0.94rem;
    }

    .adr { margin: 1rem 0 0; font-size: 0.85rem; }

    .flow { margin-bottom: 2rem; }

    .flow h3 { font-size: 1.05rem; margin: 0 0 0.5rem; }

    .closing { display: flex; flex-direction: column; gap: 1rem; align-items: flex-start; }
  `,
})
export class EngineeringPage {
  protected readonly engineering = inject(EngineeringService);
  private readonly localeService = inject(LocaleService);
  private readonly seo = inject(SeoService);

  protected readonly content = this.engineering.content;
  protected readonly facts = this.engineering.facts;

  constructor() {
    effect(() => {
      const locale = this.localeService.locale();
      const path = pathFor(locale, 'engineering');
      const doc = this.content();

      this.seo.apply({
        locale,
        title: `${doc.title} — Sebastián Vélez Ramírez`,
        description: doc.lead,
        path,
        alternates: this.localeService.alternates(path),
        type: 'article',
      });
    });
  }

  protected profileName(): string {
    return 'Sebastián Vélez Ramírez';
  }

  protected home(): string {
    return pathFor(this.localeService.locale(), 'home');
  }

  protected adrUrl(adr: string | null | undefined): string | null {
    return this.engineering.adrUrl(adr);
  }
}
