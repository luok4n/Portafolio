import { describe, expect, it } from 'vitest';

import { formatDate, formatDuration, formatPeriod, formatYearMonth } from './format';

describe('formatYearMonth', () => {
  it('formats in each language', () => {
    expect(formatYearMonth('2019-01', 'en')).toBe('Jan 2019');
    expect(formatYearMonth('2019-01', 'es')).toBe('Ene 2019');
    expect(formatYearMonth('2026-08', 'en')).toBe('Aug 2026');
    expect(formatYearMonth('2026-08', 'es')).toBe('Ago 2026');
  });

  it('handles the December boundary without shifting a month', () => {
    // The reason this does not build a Date: the source has no day, and inventing one to satisfy a
    // formatter is how a period silently becomes off by one at a timezone boundary.
    expect(formatYearMonth('2018-12', 'en')).toBe('Dec 2018');
    expect(formatYearMonth('2019-01', 'en')).toBe('Jan 2019');
  });
});

describe('formatPeriod', () => {
  it('reads the way a CV does', () => {
    expect(formatPeriod('2018-02', '2019-01', 'en')).toBe('Feb 2018 – Jan 2019');
    expect(formatPeriod('2018-02', '2019-01', 'es')).toBe('Feb 2018 – Ene 2019');
  });
});

describe('formatDuration', () => {
  it('drops a unit that is zero', () => {
    expect(formatDuration(10, 'en')).toBe('10 mo');
    expect(formatDuration(24, 'en')).toBe('2 yr');
    expect(formatDuration(10, 'es')).toBe('10 m');
    expect(formatDuration(24, 'es')).toBe('2 a');
  });

  it('shows both units when both are non-zero', () => {
    expect(formatDuration(28, 'en')).toBe('2 yr 4 mo');
    expect(formatDuration(28, 'es')).toBe('2 a 4 m');
  });

  it('handles a single month', () => {
    expect(formatDuration(1, 'en')).toBe('1 mo');
  });
});

describe('formatDate', () => {
  it('formats a source check date', () => {
    expect(formatDate('2026-08-24', 'en')).toBe('24 Aug 2026');
    expect(formatDate('2026-08-24', 'es')).toBe('24 Ago 2026');
  });

  it('returns the input rather than throwing on something unexpected', () => {
    // A malformed date in content should look wrong on the page, not take the page down.
    expect(formatDate('not-a-date', 'en')).toBe('not-a-date');
  });
});
