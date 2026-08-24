import { ChangeDetectionStrategy, Component } from '@angular/core';

/**
 * The shape that makes the site bilingual: facts on the base table, translated text in a table keyed
 * by entity and language.
 *
 * Drawn rather than described because the one thing a reader needs to see is that adding a third
 * language adds rows and not columns — which a paragraph states and a picture proves.
 */
@Component({
  selector: 'app-data-model-diagram',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <figure>
      <svg viewBox="0 0 700 250" role="img" [attr.aria-label]="label" xmlns="http://www.w3.org/2000/svg">
        <defs>
          <marker id="dm-arrow" viewBox="0 0 10 10" refX="9" refY="5" markerWidth="6" markerHeight="6" orient="auto-start-reverse">
            <path d="M 0 0 L 10 5 L 0 10 z" class="arrow-head" />
          </marker>
        </defs>

        <g class="table">
          <rect x="20" y="30" width="230" height="150" rx="8" />
          <rect x="20" y="30" width="230" height="30" rx="8" class="header" />
          <text x="135" y="50" class="table-name">experiences</text>
          <text x="36" y="82" class="col key">id</text>
          <text x="36" y="104" class="col">company</text>
          <text x="36" y="126" class="col">start_year, start_month</text>
          <text x="36" y="148" class="col">end_year, end_month</text>
          <text x="36" y="170" class="col">ordinal</text>
        </g>

        <g class="table accent">
          <rect x="400" y="30" width="280" height="150" rx="8" />
          <rect x="400" y="30" width="280" height="30" rx="8" class="header" />
          <text x="540" y="50" class="table-name">experience_translations</text>
          <text x="416" y="82" class="col key">experience_id</text>
          <text x="416" y="104" class="col key">language_code</text>
          <text x="416" y="126" class="col">role</text>
          <text x="416" y="148" class="col">employment_type</text>
          <text x="416" y="170" class="col hint">one complete row per language</text>
        </g>

        <path d="M 250 105 L 398 105" class="link" marker-end="url(#dm-arrow)" />
        <text x="324" y="96" class="edge">1 : n</text>

        <g class="note">
          <text x="20" y="212">Facts live on the base table. Translated text lives beside it, keyed by</text>
          <text x="20" y="232">entity and language — so a third language is rows, not a migration.</text>
        </g>
      </svg>
    </figure>
  `,
  styles: `
    figure { margin: 0; overflow-x: auto; }

    svg {
      width: 100%;
      min-width: 560px;
      height: auto;
      font-family: var(--font-mono);
    }

    .table rect {
      fill: var(--surface-sunken);
      stroke: var(--border);
      stroke-width: 1.5;
    }

    .table rect.header { fill: color-mix(in srgb, var(--text) 8%, transparent); }

    .table.accent rect { stroke: var(--accent); }

    .table.accent rect.header { fill: color-mix(in srgb, var(--accent) 14%, transparent); }

    .table-name {
      fill: var(--text);
      font-size: 13px;
      font-weight: 700;
      text-anchor: middle;
    }

    .col { fill: var(--text-muted); font-size: 12px; }

    .col.key { fill: var(--accent); font-weight: 700; }

    .col.hint {
      fill: var(--text-muted);
      font-size: 10.5px;
      font-style: italic;
      font-family: var(--font-sans);
    }

    .link { stroke: var(--text-muted); stroke-width: 1.5; fill: none; }

    .arrow-head { fill: var(--text-muted); }

    .edge {
      fill: var(--text-muted);
      font-size: 11px;
      text-anchor: middle;
    }

    .note text {
      fill: var(--text-muted);
      font-size: 11.5px;
      font-family: var(--font-sans);
    }
  `,
})
export class DataModelDiagram {
  protected readonly label =
    'The experiences table holds facts; experience_translations holds one complete row per language, ' +
    'keyed by experience id and language code.';
}
