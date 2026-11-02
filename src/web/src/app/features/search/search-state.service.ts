import { Injectable, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Params, Router } from '@angular/router';
import { BursarySearchParams, SearchSort, StudyLevel } from '../../core/api/models';

export const STUDY_LEVELS: readonly StudyLevel[] = ['Matric', 'Undergraduate', 'Honours', 'Masters', 'Doctoral'];

export const SEARCH_SORTS: readonly { value: SearchSort; label: string }[] = [
  { value: 'BestMatch', label: 'Best match' },
  { value: 'ClosingSoonest', label: 'Closing soonest' },
  { value: 'Newest', label: 'Newest' },
];

/** The validated search state held in the URL (specs/05-web §4). */
export interface SearchCriteria {
  institutionId?: string;
  /** UI-only: restores the degree picker; never sent to the API. */
  facultyId?: string;
  qualificationId?: string;
  fieldIds: string[];
  studyLevel?: StudyLevel;
  average?: number;
  closingFrom?: string;
  closingTo?: string;
  includeClosed: boolean;
  sort?: SearchSort;
  page: number;
  /** UI-only: the mobile filter sheet is open (`filters=open`); never sent to the API. */
  filtersOpen: boolean;
}

const GUID = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
const ISO_DATE = /^\d{4}-\d{2}-\d{2}$/;

function guid(value: string | null): string | undefined {
  return value !== null && GUID.test(value) ? value : undefined;
}

function isoDate(value: string | null): string | undefined {
  if (value === null || !ISO_DATE.test(value)) return undefined;
  const parsed = new Date(`${value}T00:00:00Z`);
  return Number.isNaN(parsed.getTime()) || parsed.toISOString().slice(0, 10) !== value ? undefined : value;
}

/** Parses an average typed by a student; anything outside 0–100 is dropped. */
export function parseAverage(value: string | null | undefined): number | undefined {
  if (value === null || value === undefined || value.trim() === '') return undefined;
  const n = Number(value);
  return Number.isFinite(n) && n >= 0 && n <= 100 ? n : undefined;
}

/** Reads raw query params, silently dropping unknown or invalid values (§4). */
export function parseCriteria(get: (key: string) => string | null): SearchCriteria {
  const studyLevel = get('studyLevel');
  const sort = get('sort');
  const page = Number(get('page'));
  return {
    institutionId: guid(get('institutionId')),
    facultyId: guid(get('facultyId')),
    qualificationId: guid(get('qualificationId')),
    fieldIds: (get('fieldIds') ?? '')
      .split(',')
      .map((id) => id.trim())
      .filter((id) => GUID.test(id)),
    studyLevel: STUDY_LEVELS.find((l) => l === studyLevel),
    average: parseAverage(get('average')),
    closingFrom: isoDate(get('closingFrom')),
    closingTo: isoDate(get('closingTo')),
    includeClosed: get('includeClosed') === 'true',
    sort: SEARCH_SORTS.find((s) => s.value === sort)?.value,
    page: Number.isInteger(page) && page >= 1 ? page : 1,
    filtersOpen: get('filters') === 'open',
  };
}

/** Serialises criteria to URL query params, leaving out defaults and empty values. */
export function toQueryParams(c: Partial<SearchCriteria>): Params {
  const params: Params = {};
  if (c.institutionId) params['institutionId'] = c.institutionId;
  if (c.facultyId) params['facultyId'] = c.facultyId;
  if (c.qualificationId) params['qualificationId'] = c.qualificationId;
  if (c.fieldIds?.length) params['fieldIds'] = c.fieldIds.join(',');
  if (c.studyLevel) params['studyLevel'] = c.studyLevel;
  if (c.average !== undefined) params['average'] = c.average;
  if (c.closingFrom) params['closingFrom'] = c.closingFrom;
  if (c.closingTo) params['closingTo'] = c.closingTo;
  if (c.includeClosed) params['includeClosed'] = 'true';
  if (c.sort) params['sort'] = c.sort;
  if (c.page !== undefined && c.page > 1) params['page'] = c.page;
  if (c.filtersOpen) params['filters'] = 'open';
  return params;
}

/** URL ⇄ signals for the search page. The URL is the single source of truth. */
@Injectable({ providedIn: 'root' })
export class SearchStateService {
  private readonly router = inject(Router);
  private readonly queryParams = toSignal(inject(ActivatedRoute).queryParamMap);

  readonly criteria = computed<SearchCriteria>(() => {
    const map = this.queryParams();
    return parseCriteria((key) => map?.get(key) ?? null);
  });

  /** Parameters for the search API call (no `facultyId`). */
  readonly apiParams = computed<BursarySearchParams>(() => {
    const c = this.criteria();
    const params: BursarySearchParams = {};
    if (c.qualificationId) params.qualificationId = c.qualificationId;
    if (c.institutionId) params.institutionId = c.institutionId;
    if (c.fieldIds.length) params.fieldIds = c.fieldIds;
    if (c.studyLevel) params.studyLevel = c.studyLevel;
    if (c.average !== undefined) params.average = c.average;
    if (c.closingFrom) params.closingFrom = c.closingFrom;
    if (c.closingTo) params.closingTo = c.closingTo;
    if (c.includeClosed) params.includeClosed = true;
    if (c.sort) params.sort = c.sort;
    if (c.page > 1) params.page = c.page;
    return params;
    // Same params → same object, so toggling the filter sheet does not refetch.
  }, { equal: (a, b) => JSON.stringify(a) === JSON.stringify(b) });

  /** How many filters are set; shown on the mobile "Filters (3)" button. */
  readonly activeCount = computed(() => {
    const c = this.criteria();
    return [
      c.institutionId || c.qualificationId,
      c.fieldIds.length > 0,
      c.studyLevel,
      c.average !== undefined,
      c.closingFrom,
      c.closingTo,
      c.includeClosed,
    ].filter(Boolean).length;
  });

  /** Writes changes to the URL (replacing history). Any change other than `page` resets `page` to 1. */
  update(partial: Partial<SearchCriteria>): Promise<boolean> {
    const onlyPage = Object.keys(partial).every((key) => key === 'page');
    const next = { ...this.criteria(), ...partial, ...(onlyPage ? {} : { page: 1 }) };
    return this.router.navigate(['/search'], { queryParams: toQueryParams(next), replaceUrl: true });
  }

  /** Opens or closes the mobile filter sheet, keeping everything else in the URL. */
  setFiltersOpen(filtersOpen: boolean): Promise<boolean> {
    return this.router.navigate(['/search'], {
      queryParams: toQueryParams({ ...this.criteria(), filtersOpen }),
      replaceUrl: true,
    });
  }

  /** Empties the URL: every filter, the sort and the page (an open filter sheet stays open). */
  clear(): Promise<boolean> {
    const queryParams = toQueryParams({ filtersOpen: this.criteria().filtersOpen });
    return this.router.navigate(['/search'], { queryParams, replaceUrl: true });
  }
}
