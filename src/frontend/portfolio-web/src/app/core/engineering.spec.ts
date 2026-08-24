import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';

import { EngineeringService } from './engineering';

/**
 * The engineering section's numbers are generated from the repository, never typed. These cover the
 * substitution that makes that true on the page — the failure ADR-0005 exists to prevent is a page
 * confidently claiming more tests than exist.
 */
describe('EngineeringService', () => {
  let service: EngineeringService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(EngineeringService);
  });

  it('carries facts measured from the repository', () => {
    const facts = service.facts;

    expect(facts.tests).toBeGreaterThan(0);
    expect(facts.tables).toBeGreaterThan(0);
    expect(facts.endpoints).toBeGreaterThan(0);
    expect(facts.adrs).toBeGreaterThan(0);
    expect(facts.locales).toContain('en');
    expect(facts.locales).toContain('es');
  });

  it('substitutes every placeholder in the copy', () => {
    for (const locale of ['en', 'es'] as const) {
      service.setLocale(locale);
      const rendered = JSON.stringify(service.content());

      // A leftover {token} renders as literal braces on a public page.
      expect(rendered).not.toMatch(/\{[a-zA-Z]+\}/);
    }
  });

  it('renders the real figures rather than the template', () => {
    service.setLocale('en');
    const content = service.content();

    expect(content.testing.description).toContain(String(service.facts.tests));
    expect(content.dataModel.description).toContain(String(service.facts.tables));
    expect(content.stack.description).toContain(String(service.facts.endpoints));
  });

  it('translates the section', () => {
    service.setLocale('en');
    const en = service.content();
    service.setLocale('es');
    const es = service.content();

    expect(es.title).not.toBe(en.title);
    // Same argument, told twice: the decisions must line up one for one.
    expect(es.decisions.map((d) => d.id)).toEqual(en.decisions.map((d) => d.id));
    expect(es.flows.map((f) => f.id)).toEqual(en.flows.map((f) => f.id));
    expect(es.operations.items.length).toBe(en.operations.items.length);
  });

  it('every decision states what it rejected and what it cost', () => {
    // A decision list without the alternatives and the price is a feature list. The whole point of
    // the section is that each one can be argued about.
    for (const locale of ['en', 'es'] as const) {
      service.setLocale(locale);
      for (const decision of service.content().decisions) {
        expect(decision.problem.length).toBeGreaterThan(20);
        expect(decision.rejected.length).toBeGreaterThan(20);
        expect(decision.cost.length).toBeGreaterThan(20);
      }
    }
  });

  it('links a decision to its record and returns nothing when there is none', () => {
    expect(service.adrUrl('0001')).toContain('0001-bilingual-content.md');
    expect(service.adrUrl('0004')).toContain('0004-backend-architecture.md');
    expect(service.adrUrl(null)).toBeNull();
    expect(service.adrUrl(undefined)).toBeNull();
  });

  it('does not strip the $comment keys into the rendered output', () => {
    service.setLocale('en');

    expect(JSON.stringify(service.content())).not.toContain('$comment');
  });
});
