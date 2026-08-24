import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';

import { EngineeringService } from '../core/engineering';

/**
 * Hand-written SVG rather than a diagramming library.
 *
 * It is four boxes and some arrows. A runtime library would add a dependency, would not prerender,
 * and would leave a blank rectangle for anyone reading with JavaScript disabled — on the page whose
 * whole argument is that the engineering is sound. Colours come from theme tokens, so it follows
 * light and dark without a second copy.
 *
 * The labels come from the translated content, not from the template. A bilingual site whose only
 * untranslated text is inside the engineering diagram undermines the section it illustrates.
 */
@Component({
  selector: 'app-architecture-diagram',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <figure>
      <svg viewBox="0 0 760 330" role="img" [attr.aria-label]="labels().alt" xmlns="http://www.w3.org/2000/svg">
        <defs>
          <marker id="arch-arrow" viewBox="0 0 10 10" refX="9" refY="5" markerWidth="6" markerHeight="6" orient="auto-start-reverse">
            <path d="M 0 0 L 10 5 L 0 10 z" class="arrow-head" />
          </marker>
        </defs>

        <g class="node">
          <rect x="20" y="20" width="180" height="70" rx="10" />
          <text x="110" y="49" class="title">{{ labels().browser }}</text>
          <text x="110" y="70" class="sub">{{ labels().browserSub }}</text>
        </g>

        <g class="node">
          <rect x="20" y="130" width="180" height="86" rx="10" />
          <text x="110" y="159" class="title">{{ labels().nginx }}</text>
          <text x="110" y="179" class="sub">{{ labels().nginxSub1 }}</text>
          <text x="110" y="197" class="sub">{{ labels().nginxSub2 }}</text>
        </g>

        <g class="node accent">
          <rect x="290" y="130" width="200" height="86" rx="10" />
          <text x="390" y="159" class="title">{{ labels().api }}</text>
          <text x="390" y="179" class="sub">{{ labels().apiSub1 }}</text>
          <text x="390" y="197" class="sub">{{ labels().apiSub2 }}</text>
        </g>

        <g class="node">
          <rect x="570" y="130" width="170" height="86" rx="10" />
          <text x="655" y="159" class="title">{{ labels().database }}</text>
          <text x="655" y="179" class="sub">{{ labels().databaseSub1 }}</text>
          <text x="655" y="197" class="sub">{{ labels().databaseSub2 }}</text>
        </g>

        <g class="node dashed">
          <rect x="290" y="250" width="200" height="60" rx="10" />
          <text x="390" y="275" class="title small">{{ labels().content }}</text>
          <text x="390" y="294" class="sub">{{ labels().contentSub }}</text>
        </g>

        <path d="M 110 90 L 110 128" class="link" marker-end="url(#arch-arrow)" />
        <path d="M 200 173 L 288 173" class="link" marker-end="url(#arch-arrow)" />
        <path d="M 490 173 L 568 173" class="link" marker-end="url(#arch-arrow)" />
        <path d="M 390 250 L 390 218" class="link dashed-link" marker-end="url(#arch-arrow)" />

        <text x="244" y="164" class="edge">{{ labels().edgeRest }}</text>
        <text x="529" y="164" class="edge">{{ labels().edgeOrm }}</text>
        <text x="400" y="238" class="edge start">{{ labels().edgeSeed }}</text>

        <g class="note">
          @for (line of labels().note; track line; let i = $index) {
            <text x="20" [attr.y]="248 + i * 20">{{ line }}</text>
          }
        </g>
      </svg>
    </figure>
  `,
  styles: `
    figure { margin: 0; overflow-x: auto; }

    svg {
      width: 100%;
      min-width: 620px;
      height: auto;
      font-family: var(--font-sans);
    }

    .node rect {
      fill: var(--surface-sunken);
      stroke: var(--border);
      stroke-width: 1.5;
    }

    .node.accent rect {
      stroke: var(--accent);
      fill: color-mix(in srgb, var(--accent) 8%, var(--surface-sunken));
    }

    .node.dashed rect { stroke-dasharray: 5 4; }

    .title {
      fill: var(--text);
      font-size: 15px;
      font-weight: 700;
      text-anchor: middle;
    }

    .title.small { font-size: 13px; font-family: var(--font-mono); }

    .sub {
      fill: var(--text-muted);
      font-size: 11px;
      text-anchor: middle;
    }

    .link {
      stroke: var(--text-muted);
      stroke-width: 1.5;
      fill: none;
    }

    .dashed-link { stroke-dasharray: 5 4; }

    .arrow-head { fill: var(--text-muted); }

    .edge {
      fill: var(--text-muted);
      font-size: 10px;
      font-family: var(--font-mono);
      text-anchor: middle;
    }

    .edge.start { text-anchor: start; }

    .note text {
      fill: var(--text-muted);
      font-size: 11.5px;
    }
  `,
})
export class ArchitectureDiagram {
  private readonly engineering = inject(EngineeringService);

  protected readonly labels = computed(() => this.engineering.content().diagrams.architecture);
}
