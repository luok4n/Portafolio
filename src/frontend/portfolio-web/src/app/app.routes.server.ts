import { RenderMode, ServerRoute } from '@angular/ssr';

import { ContentService } from './core/content.service';
import { LOCALES, ROUTE_SEGMENTS } from './core/locale';

/**
 * Everything is prerendered — ADR-0002. There is no server in production; the output is static
 * files behind nginx.
 *
 * The project detail routes need their parameters enumerated at build time. They come from the
 * content snapshot that is already compiled into the bundle, so route generation needs no API and
 * no network, and a route can never be generated for a project the bundle does not contain.
 */
const projectParams = async () => ContentService.allProjectIds().map((id) => ({ id }));

const projectRoutes: ServerRoute[] = LOCALES.map((locale) => ({
  path: `${locale}/${ROUTE_SEGMENTS.projects[locale]}/:id`,
  renderMode: RenderMode.Prerender,
  getPrerenderParams: projectParams,
}));

export const serverRoutes: ServerRoute[] = [
  ...projectRoutes,
  {
    path: '**',
    renderMode: RenderMode.Prerender,
  },
];
