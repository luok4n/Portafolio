import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

interface Box {
  label: string;
  x: number;
  width: number;
}

const CHAR_WIDTH = 7.1;
const PADDING = 22;
const GAP = 34;
const MIN_WIDTH = 74;

/**
 * A left-to-right chain of steps, used for the three flows in the engineering section.
 *
 * Box widths are computed from the label length rather than fixed, because "PostgreSQL" and
 * "?lang=es" are very different sizes and a fixed grid would either clip the long ones or leave the
 * short ones floating in whitespace. One component renders every flow, so a fourth flow is data.
 */
@Component({
  selector: 'app-flow-diagram',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <figure>
      <svg [attr.viewBox]="viewBox()" role="img" [attr.aria-label]="ariaLabel()" xmlns="http://www.w3.org/2000/svg">
        <defs>
          <marker [attr.id]="markerId()" viewBox="0 0 10 10" refX="9" refY="5" markerWidth="5" markerHeight="5" orient="auto-start-reverse">
            <path d="M 0 0 L 10 5 L 0 10 z" class="arrow-head" />
          </marker>
        </defs>

        @for (box of boxes(); track box.label; let last = $last) {
          <g class="node">
            <rect [attr.x]="box.x" y="12" [attr.width]="box.width" height="44" rx="8" />
            <text [attr.x]="box.x + box.width / 2" y="39">{{ box.label }}</text>
          </g>

          @if (!last) {
            <path
              [attr.d]="'M ' + (box.x + box.width + 6) + ' 34 L ' + (box.x + box.width + 26) + ' 34'"
              class="link"
              [attr.marker-end]="'url(#' + markerId() + ')'"
            />
          }
        }
      </svg>
    </figure>
  `,
  styles: `
    figure { margin: 0.75rem 0 0; overflow-x: auto; }

    svg {
      height: 68px;
      width: 100%;
      font-family: var(--font-mono);
    }

    .node rect {
      fill: var(--surface-sunken);
      stroke: var(--border);
      stroke-width: 1.4;
    }

    .node text {
      fill: var(--text);
      font-size: 12px;
      text-anchor: middle;
    }

    .link { stroke: var(--text-muted); stroke-width: 1.4; fill: none; }

    .arrow-head { fill: var(--text-muted); }
  `,
})
export class FlowDiagram {
  readonly steps = input.required<string[]>();
  readonly flowId = input<string>('flow');

  /** Unique per instance: several flows render on the same page and would otherwise share a marker id. */
  protected readonly markerId = computed(() => `flow-arrow-${this.flowId()}`);

  protected readonly boxes = computed<Box[]>(() => {
    let x = 4;
    return this.steps().map((label) => {
      const width = Math.max(MIN_WIDTH, label.length * CHAR_WIDTH + PADDING);
      const box = { label, x, width };
      x += width + GAP;
      return box;
    });
  });

  protected readonly viewBox = computed(() => {
    const last = this.boxes().at(-1);
    const width = last ? last.x + last.width + 8 : 100;
    return `0 0 ${width} 68`;
  });

  protected readonly ariaLabel = computed(() => this.steps().join(', then '));
}
