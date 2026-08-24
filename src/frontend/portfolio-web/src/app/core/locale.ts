/**
 * The single source of truth for locales and for how a route looks in each one.
 *
 * ADR-0001 puts the locale in the URL and translates the path segment: `/en/projects/argos-one`
 * and `/es/proyectos/argos-one`. That only stays coherent if the router, the prerender route
 * generator, the language switcher and the sitemap all read the same map — which is this file.
 */

export const LOCALES = ['en', 'es'] as const;

export type Locale = (typeof LOCALES)[number];

export const DEFAULT_LOCALE: Locale = 'en';

export const LOCALE_STORAGE_KEY = 'portfolio.locale';

/** Human names, each written in its own language rather than translated. */
export const LOCALE_NAMES: Record<Locale, string> = {
  en: 'English',
  es: 'Español',
};

/**
 * Route ids and their path segment per locale. A project slug is a proper noun and is never
 * translated — only the segment around it is.
 */
export const ROUTE_SEGMENTS = {
  home: { en: '', es: '' },
  projects: { en: 'projects', es: 'proyectos' },
  engineering: { en: 'engineering', es: 'ingenieria' },
  notFound: { en: '404', es: '404' },
} as const satisfies Record<string, Record<Locale, string>>;

export type RouteId = keyof typeof ROUTE_SEGMENTS;

export function isLocale(value: string | null | undefined): value is Locale {
  return value !== null && value !== undefined && (LOCALES as readonly string[]).includes(value);
}

/** Builds an absolute in-app path, e.g. `pathFor('es', 'projects', 'linkvest')` → `/es/proyectos/linkvest`. */
export function pathFor(locale: Locale, route: RouteId, slug?: string): string {
  const segment = ROUTE_SEGMENTS[route][locale];
  // The leading slash is prepended rather than carried as an empty first element: filtering blanks
  // out of the array would drop it too, and a path that quietly loses its slash produces a relative
  // link that only misbehaves on nested routes.
  const parts = [locale, segment, slug].filter((part): part is string => !!part);
  return `/${parts.join('/')}`;
}

/**
 * Rewrites a URL from one locale to another, keeping the route and the slug.
 *
 * The switcher must not dump the reader back on the home page: someone reading about Linkvest in
 * English who switches to Spanish wants that same project in Spanish. The route id is recovered by
 * matching the segment, so `/en/projects/linkvest` becomes `/es/proyectos/linkvest`.
 */
export function translateUrl(url: string, to: Locale): string {
  const [pathAndQuery, fragment] = url.split('#');
  const [path, query] = pathAndQuery.split('?');
  const segments = path.split('/').filter(Boolean);

  // No locale prefix at all: send the reader to that locale's home.
  if (segments.length === 0 || !isLocale(segments[0])) {
    return `/${to}`;
  }

  const from = segments[0];
  const rest = segments.slice(1);
  let translated = `/${to}`;

  if (rest.length > 0) {
    const routeId = (Object.keys(ROUTE_SEGMENTS) as RouteId[]).find(
      (id) => ROUTE_SEGMENTS[id][from] === rest[0],
    );

    // An unrecognised segment is passed through unchanged rather than dropped. Losing the rest of
    // the path would be a worse answer than an imperfect translation of it.
    const head = routeId ? ROUTE_SEGMENTS[routeId][to] : rest[0];
    const parts = [to, head, ...rest.slice(1)].filter((p): p is string => !!p);
    translated = `/${parts.join('/')}`;
  }

  return translated + (query ? `?${query}` : '') + (fragment ? `#${fragment}` : '');
}
