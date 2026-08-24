import { Injectable, computed, signal } from '@angular/core';

import { DEFAULT_LOCALE, Locale } from './locale';

import enEngineering from '../../content-snapshot/engineering.en.json';
import esEngineering from '../../content-snapshot/engineering.es.json';
import facts from '../../content-snapshot/engineering-facts.json';

export interface EngineeringDecision {
  id: string;
  adr?: string | null;
  title: string;
  problem: string;
  decision: string;
  rejected: string;
  cost: string;
}

export interface EngineeringFlow {
  id: string;
  title: string;
  description: string;
  steps: string[];
}

export interface EngineeringContent {
  title: string;
  lead: string;
  why: { title: string; paragraphs: string[] };
  focus: { title: string; paragraphs: string[] };
  architecture: { title: string; description: string; notes: string[] };
  decisions: EngineeringDecision[];
  flows: EngineeringFlow[];
  dataModel: { title: string; description: string; notes: string[] };
  testing: { title: string; description: string; items: string[] };
  operations: { title: string; items: string[] };
  stack: { title: string; description: string };
  repository: { label: string; note: string };
}

export interface EngineeringFacts {
  generatedAt: string;
  tests: number;
  tables: number;
  endpoints: number;
  adrs: number;
  languages: number;
  locales: string[];
  roles: number;
  projects: number;
  verifiedProjects: number;
}

const SOURCES: Record<Locale, unknown> = { en: enEngineering, es: esEngineering };

const REPOSITORY_URL = 'https://github.com/luok4n/Portafolio';
const ADR_BASE = `${REPOSITORY_URL}/blob/main/docs/adr`;

/**
 * The "how this site is built" section.
 *
 * Unlike every other piece of content it is compiled into the bundle rather than fetched, because
 * it describes this codebase — see ADR-0005. A deployment can therefore never serve a description
 * of an architecture it is not running.
 *
 * Numbers in the copy are `{placeholders}` resolved from facts measured off the repository at build
 * time. Nothing here is a number someone typed, which is the point: a section boasting fifty tests
 * when there are thirty is the first claim an interviewer checks.
 */
@Injectable({ providedIn: 'root' })
export class EngineeringService {
  private readonly locale = signal<Locale>(DEFAULT_LOCALE);

  readonly facts = facts as unknown as EngineeringFacts;

  readonly content = computed<EngineeringContent>(() =>
    this.resolve(SOURCES[this.locale()] as EngineeringContent),
  );

  readonly repositoryUrl = REPOSITORY_URL;

  setLocale(locale: Locale): void {
    this.locale.set(locale);
  }

  adrUrl(number: string | null | undefined): string | null {
    if (!number) {
      return null;
    }
    const slugs: Record<string, string> = {
      '0001': 'bilingual-content',
      '0002': 'frontend-rendering',
      '0003': 'content-privacy',
      '0004': 'backend-architecture',
      '0005': 'engineering-section',
    };
    const slug = slugs[number];
    return slug ? `${ADR_BASE}/${number}-${slug}.md` : `${ADR_BASE}/`;
  }

  /** Walks the document substituting `{fact}` tokens. Unknown tokens are left alone so they are visible rather than silently blank. */
  private resolve<T>(value: T): T {
    if (typeof value === 'string') {
      return value.replace(/\{([a-zA-Z]+)\}/g, (match, key: string) => {
        const fact = (this.facts as unknown as Record<string, unknown>)[key];
        return fact === undefined ? match : String(fact);
      }) as unknown as T;
    }

    if (Array.isArray(value)) {
      return value.map((item) => this.resolve(item)) as unknown as T;
    }

    if (value && typeof value === 'object') {
      const out: Record<string, unknown> = {};
      for (const [key, item] of Object.entries(value)) {
        if (key.startsWith('$')) continue;
        out[key] = this.resolve(item);
      }
      return out as T;
    }

    return value;
  }
}
