import { Dialog, DialogRef } from '@angular/cdk/dialog';
import { Overlay } from '@angular/cdk/overlay';
import { DOCUMENT, isPlatformBrowser } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  PLATFORM_ID,
  computed,
  effect,
  inject,
  linkedSignal,
  signal,
  untracked,
} from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { catchError, map, of, startWith, switchMap, timer } from 'rxjs';
import { BursaryApi } from '../../core/api/bursary-api';
import { BursarySummaryResponse, PagedResponse } from '../../core/api/models';
import { FilterPanelComponent } from './filter-panel/filter-panel.component';
import { FilterSheetComponent, FilterSheetData } from './filter-sheet/filter-sheet.component';
import { ResultListComponent } from './result-list/result-list.component';
import { SearchStateService } from './search-state.service';

export const FILTER_DEBOUNCE_MS = 300;

type SearchView =
  | { status: 'loading' }
  | { status: 'error' }
  | { status: 'ok'; result: PagedResponse<BursarySummaryResponse> };

@Component({
  selector: 'app-search-page',
  imports: [FilterPanelComponent, ResultListComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="mx-auto grid max-w-[1200px] gap-4 px-4 py-4 md:px-6 xl:px-8 lg:grid-cols-[18rem_minmax(0,1fr)] lg:gap-8">
      <h1 class="text-2xl font-bold lg:col-span-2">Search bursaries</h1>
      <button
        type="button"
        class="filters-button justify-self-start rounded-control border border-jacaranda px-4 py-2 font-medium text-jacaranda hover:bg-bloom active:bg-bloom lg:hidden"
        aria-haspopup="dialog"
        (click)="state.setFiltersOpen(true)"
      >
        Filters ({{ state.activeCount() }})
      </button>
      <app-filter-panel
        class="hidden min-w-0 lg:sticky lg:top-20 lg:block lg:max-h-[calc(100dvh-6rem)] lg:self-start lg:overflow-y-auto lg:overscroll-contain lg:pb-4 lg:pr-2"
      />
      <app-result-list
        class="min-w-0"
        [result]="result()"
        [loading]="view().status === 'loading'"
        [error]="view().status === 'error'"
        [sort]="state.criteria().sort"
        [average]="state.criteria().average"
        (sortChange)="state.update({ sort: $event })"
        (retry)="retryCount.update((n) => n + 1)"
        (clearFilters)="state.clear()"
      />
    </div>
  `,
})
export class SearchPage {
  protected readonly state = inject(SearchStateService);
  private readonly api = inject(BursaryApi);
  protected readonly retryCount = signal(0);
  private firstRequest = true;

  private readonly request = computed(() => ({ params: this.state.apiParams(), retry: this.retryCount() }));

  // switchMap cancels the in-flight request when the criteria change. Hydration's HTTP transfer cache
  // (provideClientHydration) replays the server's response in the browser, so there is no duplicate request.
  protected readonly view = toSignal(
    toObservable(this.request).pipe(
      // The first request goes straight out (and is replayed from the server); later ones wait for typing to settle.
      // 'loading' is emitted straight away, so the list dims and aria-busy is set during the wait too.
      switchMap(({ params }) => {
        const wait = this.firstRequest ? of(0) : timer(FILTER_DEBOUNCE_MS);
        this.firstRequest = false;
        return wait.pipe(
          switchMap(() => this.api.search(params)),
          map((result): SearchView => ({ status: 'ok', result })),
          catchError(() => of<SearchView>({ status: 'error' })),
          startWith<SearchView>({ status: 'loading' }),
        );
      }),
    ),
    { initialValue: { status: 'loading' } as SearchView },
  );

  // While a new search loads, the previous results stay on screen (dimmed) instead of flashing skeletons.
  protected readonly result = linkedSignal<SearchView, PagedResponse<BursarySummaryResponse> | null>({
    source: this.view,
    computation: (v, previous) => (v.status === 'ok' ? v.result : v.status === 'loading' ? (previous?.value ?? null) : null),
  });

  private readonly dialog = inject(Dialog);
  private readonly overlay = inject(Overlay);
  private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));
  private readonly window = inject(DOCUMENT).defaultView;
  private sheet?: DialogRef<unknown, FilterSheetComponent>;
  private destroyed = false;

  constructor() {
    // `filters=open` in the URL is the single source of truth for the sheet.
    effect(() => {
      const open = this.state.criteria().filtersOpen;
      untracked(() => {
        // shortcut: no live listener, so resizing past 1024 px with the sheet open leaves it open until closed.
        const desktop = this.window?.matchMedia?.('(min-width: 1024px)').matches;
        if (open && !this.sheet && this.isBrowser && !desktop) this.openSheet();
        else if (!open) this.sheet?.close();
      });
    });
    inject(DestroyRef).onDestroy(() => {
      this.destroyed = true;
      this.sheet?.close();
    });
  }

  private openSheet(): void {
    const data: FilterSheetData = { count: computed(() => this.result()?.totalCount ?? null) };
    const ref = this.dialog.open(FilterSheetComponent, {
      data,
      ariaLabel: 'Filters',
      autoFocus: 'dialog', // no autofocus into a field on mobile
      width: '100%',
      maxWidth: '100vw',
      panelClass: 'filter-sheet-panel',
      backdropClass: 'filter-sheet-backdrop',
      positionStrategy: this.overlay.position().global().bottom('0').centerHorizontally(),
    });
    this.sheet = ref;
    ref.closed.subscribe(() => {
      this.sheet = undefined;
      // Esc, the backdrop or the "Show N bursaries" button closed it: take `filters=open` out of the URL.
      if (!this.destroyed && this.state.criteria().filtersOpen) void this.state.setFiltersOpen(false);
    });
  }
}
