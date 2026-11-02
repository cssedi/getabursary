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
  signal,
  untracked,
} from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { catchError, map, of, startWith, switchMap } from 'rxjs';
import { BursaryApi } from '../../core/api/bursary-api';
import { BursarySummaryResponse, PagedResponse } from '../../core/api/models';
import { FilterPanelComponent } from './filter-panel/filter-panel.component';
import { FilterSheetComponent, FilterSheetData } from './filter-sheet/filter-sheet.component';
import { ResultListComponent } from './result-list/result-list.component';
import { SearchStateService } from './search-state.service';

type SearchView =
  | { status: 'loading' }
  | { status: 'error' }
  | { status: 'ok'; result: PagedResponse<BursarySummaryResponse> };

@Component({
  selector: 'app-search-page',
  imports: [FilterPanelComponent, ResultListComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="mx-auto grid max-w-[1040px] gap-4 px-4 py-4 md:px-6 lg:grid-cols-[18rem_1fr]">
      <h1 class="text-2xl font-bold lg:col-span-2">Search bursaries</h1>
      <button
        type="button"
        class="filters-button justify-self-start rounded-control border border-jacaranda px-4 py-2 font-medium text-jacaranda lg:hidden"
        aria-haspopup="dialog"
        (click)="state.setFiltersOpen(true)"
      >
        Filters ({{ state.activeCount() }})
      </button>
      <app-filter-panel class="hidden lg:block" />
      <app-result-list
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

  private readonly request = computed(() => ({ params: this.state.apiParams(), retry: this.retryCount() }));

  // switchMap cancels the in-flight request when the criteria change. Hydration's HTTP transfer cache
  // (provideClientHydration) replays the server's response in the browser, so there is no duplicate request.
  protected readonly view = toSignal(
    toObservable(this.request).pipe(
      switchMap(({ params }) =>
        this.api.search(params).pipe(
          map((result): SearchView => ({ status: 'ok', result })),
          catchError(() => of<SearchView>({ status: 'error' })),
          startWith<SearchView>({ status: 'loading' }),
        ),
      ),
    ),
    { initialValue: { status: 'loading' } as SearchView },
  );

  protected readonly result = computed(() => {
    const v = this.view();
    return v.status === 'ok' ? v.result : null;
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
