import { Locale } from './locale';

const MONTHS: Record<Locale, string[]> = {
  en: ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'],
  es: ['Ene', 'Feb', 'Mar', 'Abr', 'May', 'Jun', 'Jul', 'Ago', 'Sep', 'Oct', 'Nov', 'Dic'],
};

/**
 * Formats the API's inclusive `YYYY-MM` values.
 *
 * Written out rather than delegated to `Intl.DateTimeFormat`, which would need a real Date and
 * therefore a day that the source data does not have. Inventing the first of the month to satisfy a
 * formatter is the kind of small lie that turns into an off-by-one at a timezone boundary.
 */
export function formatYearMonth(value: string, locale: Locale): string {
  const [year, month] = value.split('-').map(Number);
  const name = MONTHS[locale][month - 1] ?? '';
  return `${name} ${year}`;
}

export function formatPeriod(start: string, end: string, locale: Locale): string {
  return `${formatYearMonth(start, locale)} – ${formatYearMonth(end, locale)}`;
}

/** Months as "2 yr 4 mo" / "2 a 4 m", dropping a unit when it is zero. */
export function formatDuration(months: number, locale: Locale): string {
  const years = Math.floor(months / 12);
  const rest = months % 12;
  const y = locale === 'es' ? 'a' : 'yr';
  const m = locale === 'es' ? 'm' : 'mo';

  if (years === 0) return `${rest} ${m}`;
  if (rest === 0) return `${years} ${y}`;
  return `${years} ${y} ${rest} ${m}`;
}

/** ISO date to a short readable one, for the "checked" stamp on a source link. */
export function formatDate(iso: string, locale: Locale): string {
  const [year, month, day] = iso.split('-').map(Number);
  if (!year || !month || !day) return iso;
  return `${day} ${MONTHS[locale][month - 1]} ${year}`;
}
