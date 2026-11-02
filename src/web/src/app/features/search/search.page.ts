import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { catchError, map, of, startWith, switchMap } from 'rxjs';
import { BursaryApi } from '../../core/api/bursary-api';
import { BursarySummaryResponse, PagedResponse } from '../../core/api/models';
import { FilterPanelComponent } from './filter-panel/filter-panel.component';
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
    <div class="mx-auto grid max-w-5xl gap-4 p-4 md:grid-cols-[18rem_1fr]">
      <h1 class="text-2xl font-bold md:col-span-2">Search bursaries</h1>
      <app-filter-panel />
      <app-result-list
        [result]="result()"
        [loading]="view().status === 'loading'"
        [error]="view().status === 'error'"
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
}
