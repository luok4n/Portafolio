import { Routes } from '@angular/router';

import { LOCALES, ROUTE_SEGMENTS } from './core/locale';
import { HomePage } from './pages/home.page';
import { ProjectDetailPage } from './pages/project-detail.page';
import { EngineeringPage } from './pages/engineering.page';
import { NotFoundPage } from './pages/not-found.page';
import { LocaleRedirectPage } from './pages/locale-redirect.page';

/**
 * Routes are generated from the locale map rather than written twice.
 *
 * ADR-0001 translates the path segment — `/en/projects/x` and `/es/proyectos/x` — and the only way
 * that stays consistent between the router, the language switcher, the prerender route generator
 * and the sitemap is for all of them to read one table. Adding a language means adding a column to
 * `ROUTE_SEGMENTS`, not another block here.
 */
const localeRoutes: Routes = LOCALES.map((locale) => ({
  path: locale,
  data: { locale },
  children: [
    { path: '', component: HomePage, data: { locale } },
    {
      path: `${ROUTE_SEGMENTS.projects[locale]}/:id`,
      component: ProjectDetailPage,
      data: { locale },
    },
    { path: ROUTE_SEGMENTS.engineering[locale], component: EngineeringPage, data: { locale } },
    // An explicit route so the page is prerendered to a real file. nginx serves it as the 404
    // document, which a wildcard-only route would never produce.
    { path: ROUTE_SEGMENTS.notFound[locale], component: NotFoundPage, data: { locale } },
    { path: '**', component: NotFoundPage, data: { locale } },
  ],
}));

export const routes: Routes = [
  ...localeRoutes,
  // Anything without a locale prefix lands here and is sent to a language.
  { path: '', pathMatch: 'full', component: LocaleRedirectPage },
  { path: '**', component: LocaleRedirectPage },
];
