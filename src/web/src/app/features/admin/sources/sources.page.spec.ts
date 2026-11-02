import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { AdminSource, IngestionRun } from '../../../core/api/admin-api';
import { PagedResponse } from '../../../core/api/models';
import { POLL_MS, POLL_WINDOW_MS, RUN_REQUESTED, SourcesPage, relativeTime } from './sources.page';

const SOURCES = '/api/v1/admin/sources';
const RUNS = '/api/v1/admin/ingestion-runs';

const source = (over: Partial<AdminSource> = {}): AdminSource => ({
  id: 'src-1',
  key: 'zabursaries',
  name: 'Zabursaries',
  enabled: true,
  lastRun: {
    startedAt: '2026-11-02T06:00:00Z',
    finishedAt: '2026-11-02T06:02:00Z',
    status: 'Succeeded',
    discovered: 12,
    created: 3,
    updated: 2,
    failed: 1,
    errorSummary: null,
  },
  ...over,
});

const NEVER_RUN = source({ id: 'src-2', name: 'StudyTrust', enabled: false, lastRun: null });

const NO_RUNS: PagedResponse<IngestionRun> = { items: [], page: 1, pageSize: 20, totalCount: 0 };

/** Lets the `firstValueFrom` promises in the page resolve; fake timers don't stop microtasks. */
const microtasks = async () => {
  await Promise.resolve();
  await Promise.resolve();
};

function setup() {
  TestBed.configureTestingModule({
    providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
  });
  const http = TestBed.inject(HttpTestingController);
  const fixture = TestBed.createComponent(SourcesPage);
  fixture.detectChanges();
  const el: HTMLElement = fixture.nativeElement;
  const load = async (list: AdminSource[]) => {
    http.expectOne(SOURCES).flush(list);
    http.expectOne(`${RUNS}?page=1`).flush(NO_RUNS);
    await microtasks();
    fixture.detectChanges();
  };
  return { http, fixture, el, load };
}

const switches = (el: HTMLElement) =>
  Array.from(el.querySelectorAll<HTMLInputElement>('input[role="switch"]'));

const runNowButton = (el: HTMLElement, index: number) =>
  Array.from(el.querySelectorAll<HTMLButtonElement>('tbody button')).filter(
    (b) => b.textContent?.trim() === 'Run now',
  )[index];

describe('SourcesPage', () => {
  afterEach(() => {
    vi.useRealTimers();
    TestBed.inject(HttpTestingController).verify();
  });

  it('sources_listed_rendersRowsSwitchesAndCounts', async () => {
    const { el, load } = setup();
    await load([source(), NEVER_RUN]);

    expect(el.querySelectorAll('tbody tr').length).toBe(2);
    expect(switches(el).map((s) => s.checked)).toEqual([true, false]);
    expect(el.querySelector('tbody tr')!.textContent).toContain('Zabursaries');
    expect(el.querySelector('tbody tr')!.textContent).toContain('12');
    expect(el.querySelector('time')!.getAttribute('title')).toBeTruthy();
    expect(el.textContent).toContain('Never run');
  });

  it('toggle_succeeds_keepsTheNewState', async () => {
    const { http, el, fixture, load } = setup();
    await load([source()]);

    switches(el)[0].click();
    const patch = http.expectOne(`${SOURCES}/src-1`);
    expect(patch.request.method).toBe('PATCH');
    expect(patch.request.body).toEqual({ enabled: false });
    patch.flush(source({ enabled: false }));
    await microtasks();
    fixture.detectChanges();

    expect(switches(el)[0].checked).toBe(false);
  });

  it('toggle_fails_rollsBackAndShowsToast', async () => {
    const { http, el, fixture, load } = setup();
    await load([source()]);

    switches(el)[0].click();
    fixture.detectChanges();
    http.expectOne(`${SOURCES}/src-1`).flush(null, { status: 500, statusText: 'Server Error' });
    await microtasks();
    fixture.detectChanges();

    expect(switches(el)[0].checked).toBe(true);
    expect(el.textContent).toContain('Couldn’t change that source.');
  });

  it('runNow_clicked_postsSourceIdAndShowsMessage', async () => {
    const { http, el, fixture, load } = setup();
    await load([source(), NEVER_RUN]);

    runNowButton(el, 1).click();
    const post = http.expectOne('/api/v1/admin/ingestion-runs');
    expect(post.request.body).toEqual({ sourceId: 'src-2' });
    post.flush(
      { id: 'r-1', sourceId: 'src-2', requestedAt: '2026-11-02T08:00:00Z', pickedUpAt: null },
      { status: 202, statusText: 'Accepted' },
    );
    await microtasks();
    fixture.detectChanges();

    expect(el.textContent).toContain(RUN_REQUESTED);
    http.match((r) => r.url.startsWith(SOURCES) || r.url.startsWith(RUNS));
  });

  it('runAll_clicked_postsWithoutSourceId', async () => {
    const { http, el, load } = setup();
    await load([source()]);

    Array.from(el.querySelectorAll<HTMLButtonElement>('button'))
      .find((b) => b.textContent?.trim() === 'Run all sources now')!
      .click();
    const post = http.expectOne('/api/v1/admin/ingestion-runs');
    expect(post.request.body.sourceId).toBeUndefined();
    post.flush(
      { id: 'r-1', sourceId: null, requestedAt: '2026-11-02T08:00:00Z', pickedUpAt: null },
      { status: 202, statusText: 'Accepted' },
    );
    http.match((r) => r.url.startsWith(SOURCES) || r.url.startsWith(RUNS));
  });

  it('polling_afterRunRequested_reloadsEvery15sForTwoMinutesThenStops', async () => {
    vi.useFakeTimers();
    const { http, el, load } = setup();
    await load([source()]);

    runNowButton(el, 0).click();
    http
      .expectOne('/api/v1/admin/ingestion-runs')
      .flush(
        { id: 'r-1', sourceId: 'src-1', requestedAt: '2026-11-02T08:00:00Z', pickedUpAt: null },
        { status: 202, statusText: 'Accepted' },
      );
    await microtasks();

    vi.advanceTimersByTime(POLL_WINDOW_MS);
    expect(http.match((r) => r.url === SOURCES).length).toBe(POLL_WINDOW_MS / POLL_MS);

    vi.advanceTimersByTime(POLL_MS * 4);
    http.expectNone(SOURCES);
    http.match((r) => r.url.startsWith(RUNS));
  });

  it('lastError_longerThanPreview_truncatesUntilExpanded', async () => {
    const { el, fixture, load } = setup();
    const error = 'Timed out fetching the listing page. '.repeat(4);
    await load([source({ lastRun: { ...source().lastRun!, errorSummary: error } })]);

    const text = () => el.querySelector('tbody td p')!.textContent!.trim();
    expect(text().length).toBeLessThan(error.length);
    expect(text().endsWith('…')).toBe(true);

    const expand = Array.from(el.querySelectorAll<HTMLButtonElement>('tbody button')).find(
      (b) => b.textContent?.trim() === 'Show full error',
    )!;
    expand.click();
    fixture.detectChanges();
    expect(text()).toBe(error.trim());
    expect(expand.getAttribute('aria-expanded')).toBe('true');
  });
});

describe('relativeTime', () => {
  it('relativeTime_twoHoursAgo_readsTwoHoursAgo', () => {
    const now = Date.parse('2026-11-02T08:00:00Z');
    expect(relativeTime('2026-11-02T06:00:00Z', now)).toBe('2 hours ago');
    expect(relativeTime('2026-11-02T07:59:30Z', now)).toBe('30 seconds ago');
  });
});
