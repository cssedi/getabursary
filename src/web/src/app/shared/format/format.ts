import { Pipe, PipeTransform } from '@angular/core';

export type DateStyle = 'short' | 'long' | 'day' | 'month';

const OPTIONS: Record<DateStyle, Intl.DateTimeFormatOptions> = {
  short: { day: 'numeric', month: 'short', year: 'numeric' },
  long: { day: 'numeric', month: 'long', year: 'numeric' },
  day: { day: 'numeric' },
  month: { month: 'short' },
};

// Calendar dates are formatted in UTC so the viewer's time zone never shifts the day.
const dateFormatters = new Map<DateStyle, Intl.DateTimeFormat>();
function dateFormatter(style: DateStyle): Intl.DateTimeFormat {
  let f = dateFormatters.get(style);
  if (!f) {
    f = new Intl.DateTimeFormat('en-ZA', { ...OPTIONS[style], timeZone: 'UTC' });
    dateFormatters.set(style, f);
  }
  return f;
}

/** Formats an ISO date (`2027-03-31` or a full timestamp) with `Intl.DateTimeFormat('en-ZA')`. */
export function formatDate(iso: string, style: DateStyle = 'short'): string {
  const d = new Date(iso.length === 10 ? `${iso}T00:00:00Z` : iso);
  return dateFormatter(style).format(d);
}

const numberFormatter = new Intl.NumberFormat('en-ZA');
const randFormatter = new Intl.NumberFormat('en-ZA', {
  style: 'currency',
  currency: 'ZAR',
  maximumFractionDigits: 0,
});

export function formatNumber(value: number): string {
  return numberFormatter.format(value);
}

/** Formats rand amounts, e.g. `R 50 000` (with non-breaking spaces). */
export function formatRand(value: number): string {
  return randFormatter.format(value);
}

@Pipe({ name: 'formatDate' })
export class FormatDatePipe implements PipeTransform {
  transform(iso: string | null | undefined, style: DateStyle = 'short'): string {
    return iso ? formatDate(iso, style) : '';
  }
}

@Pipe({ name: 'formatNumber' })
export class FormatNumberPipe implements PipeTransform {
  transform(value: number | null | undefined): string {
    return value == null ? '' : formatNumber(value);
  }
}

@Pipe({ name: 'formatRand' })
export class FormatRandPipe implements PipeTransform {
  transform(value: number | null | undefined): string {
    return value == null ? '' : formatRand(value);
  }
}
