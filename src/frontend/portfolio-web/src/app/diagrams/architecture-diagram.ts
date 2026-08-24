import { ChangeDetectionStrategy, Component } from '@angular/core';

/**
 * Hand-written SVG rather than a diagramming library.
 *
 * It is four boxes and some arrows. A runtime library would add a dependency, would not prerender,
 * and would leave a blank rectangle for anyone reading with JavaScript disabled — on the page whose
 * whole argument is that the engineering is sound. Colours come from theme tokens, so it follows
 * light and dark without a second copy.
 */
@Component({
  selector: 'app-architecture-diagram',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <figure>
      <svg viewBox="0 0 760 330" role="img" [attr.aria-label]="label" xmlns="http://www.w3.org/2000/svg">
        <defs>
          <marker id="arch-arrow" viewBox="0 0 10 10" refX="9" refY="5" markerWidth="6" markerHeight="6" orient="auto-start-reverse">
            <path d="M 0 0 L 10 5 L 0 10 z" class="arrow-head" />
          </marker>
        </defs>

        <g class="node">
          <rect x="20" y="20" width="180" height="70" rx="10" />
          <text x="110" y="49" class="title">Browser</text>
          <text x="110" y="70" class="sub">reader · crawler · preview bot</text>
        </g>

        <g class="node">
          <rect x="20" y="130" width="180" height="86" rx="10" />
          <text x="110" y="159" class="title">nginx</text>
          <text x="110" y="179" class="sub">prerendered HTML</text>
          <text x="110" y="197" class="sub">one file per route × locale</text>
        </g>

        <g class="node accent">
          <rect x="290" y="130" width="200" height="86" rx="10" />
          <text x="390" y="159" class="title">Portfolio API</text>
          <text x="390" y="179" class="sub">ASP.NET Core · .NET 10</text>
          <text x="390" y="197" class="sub">stateless · read-only</text>
        </g>

        <g class="node">
          <rect x="570" y="130" width="170" height="86" rx="10" />
          <text x="655" y="159" class="title">PostgreSQL</text>
          <text x="655" y="179" class="sub">base tables +</text>
          <text x="655" y="197" class="sub">translations</text>
        </g>

        <g class="node dashed">
          <rect x="290" y="250" width="200" height="60" rx="10" />
          <text x="390" y="275" class="title small">content/</text>
          <text x="390" y="294" class="sub">reviewed files · seed source</text>
        </g>

        <path d="M 110 90 L 110 128" class="link" marker-end="url(#arch-arrow)" />
        <path d="M 200 173 L 288 173" class="link" marker-end="url(#arch-arrow)" />
        <path d="M 490 173 L 568 173" class="link" marker-end="url(#arch-arrow)" />
        <path d="M 390 250 L 390 218" class="link dashed-link" marker-end="url(#arch-arrow)" />

        <text x="244" y="164" class="edge">REST</text>
        <text x="529" y="164" class="edge">EF Core</text>
        <text x="400" y="238" class="edge start">seed</text>

        <g class="note">
          <text x="20" y="248">The frontend embeds a build-time snapshot of the content.</text>
          <text x="20" y="268">If the API is unreachable the page still renders — it shows</text>
          <text x="20" y="288">cached content, never a spinner and never an error.</text>
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
  protected readonly label =
    'Browser to nginx serving prerendered HTML, to the ASP.NET Core API, to PostgreSQL. ' +
    'Reviewed content files seed the database.';
}
