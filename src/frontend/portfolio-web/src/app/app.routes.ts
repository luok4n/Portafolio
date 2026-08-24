import { Routes } from '@angular/router';

import { LOCALES, ROUTE_SEGMENTS } from './core/locale';
import { HomePage } from './pages/home.page';

/**
 * Routes are generated from the locale map rather than written twice.
 *
 * ADR-0001 translates the path segment — `/en/projects/x` and `/es/proyectos/x` — and the only way
 * that stays consistent between the router, the language switcher, the prerender route generator
 * and the sitemap is for all of them to read one table. Adding a language means adding a column to
 * `ROUTE_SEGMENTS`, not another block here.
 *
 * The home page is imported directly because it is what every visitor loads first; everything else
 * is lazy, so reading the home page does not download the engineering diagrams and the project
 * detail template it may never open. Prerendering means each page ships complete HTML either way —
 * this is about the JavaScript that follows it.
 */
const localeRoutes: Routes = LOCALES.map((locale) => ({
  path: locale,
  data: { locale },
  children: [
    { path: '', component: HomePage, data: { locale } },
    {
      path: `${ROUTE_SEGMENTS.projects[locale]}/:id`,
      loadComponent: () => import('./pages/project-detail.page').then((m) => m.ProjectDetailPage),
      data: { locale },
    },
    {
      path: ROUTE_SEGMENTS.engineering[locale],
      loadComponent: () => import('./pages/engineering.page').then((m) => m.EngineeringPage),
      data: { locale },
    },
    // An explicit route so the page is prerendered to a real file. nginx serves it as the 404
    // document, which a wildcard-only route would never produce.
    {
      path: ROUTE_SEGMENTS.notFound[locale],
      loadComponent: () => import('./pages/not-found.page').then((m) => m.NotFoundPage),
      data: { locale },
    },
    {
      path: '**',
      loadComponent: () => import('./pages/not-found.page').then((m) => m.NotFoundPage),
      data: { locale },
    },
  ],
}));

export const routes: Routes = [
  ...localeRoutes,
  // Anything without a locale prefix lands here and is sent to a language.
  {
    path: '',
    pathMatch: 'full',
    loadComponent: () => import('./pages/locale-redirect.page').then((m) => m.LocaleRedirectPage),
  },
  {
    path: '**',
    loadComponent: () => import('./pages/locale-redirect.page').then((m) => m.LocaleRedirectPage),
  },
];
