import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { BursarySummaryResponse, PagedResponse, SearchSort } from '../../../core/api/models';
import { EmptyStateComponent } from '../../../shared/ui/empty-state/empty-state.component';
import { ErrorStateComponent } from '../../../shared/ui/error-state/error-state.component';
import { SkeletonCardComponent } from '../../../shared/ui/skeleton-card/skeleton-card.component';
import { BursaryRowComponent } from '../bursary-row/bursary-row.component';
import { PaginationComponent } from '../pagination/pagination.component';
import { SEARCH_SORTS } from '../search-state.service';

export const AD_SLOT_EVERY = 6;

@Component({
  selector: 'app-result-list',
  imports: [BursaryRowComponent, EmptyStateComponent, ErrorStateComponent, SkeletonCardComponent, PaginationComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section id="results">
      @if (loading()) {
        <div>
          @for (n of [1, 2, 3]; track n) {
            <app-skeleton-card />
          }
        </div>
      } @else if (error()) {
        <app-error-state (retry)="retry.emit()" />
      } @else if (result(); as r) {
        @if (r.items.length === 0) {
          <app-empty-state
            [message]="emptyMessage()"
            actionLabel="Clear filters"
            (action)="clearFilters.emit()"
          />
        } @else {
          <div class="mb-1 flex items-center justify-between gap-4">
            <p class="count font-medium tabular-nums" aria-live="polite">
              {{ r.totalCount }} {{ r.totalCount === 1 ? 'bursary matches' : 'bursaries match' }}
            </p>
            <div class="flex items-center gap-2 text-sm">
              <label for="results-sort">Sort:</label>
              <select
                id="results-sort"
                name="sort"
                autocomplete="off"
                class="rounded-control border border-line p-2"
                (change)="sortChange.emit($any($event.target).value || undefined)"
              >
                @for (s of sorts; track s.value) {
                  <option [value]="s.value" [selected]="s.value === (sort() ?? 'BestMatch')">{{ s.label }}</option>
                }
              </select>
            </div>
          </div>
          <ul class="border-t border-line">
            @for (b of r.items; track b.id; let i = $index) {
              <li><app-bursary-row [bursary]="b" [index]="i" /></li>
              @if ((i + 1) % adEvery === 0) {
                <li hidden class="ad-slot" data-slot="results-inline"></li>
              }
            }
          </ul>
          @if (showPagination()) {
            <app-pagination [page]="r.page" [pageSize]="r.pageSize" [totalCount]="r.totalCount" />
          }
        }
      }
    </section>
  `,
})
export class ResultListComponent {
  protected readonly adEvery = AD_SLOT_EVERY;
  protected readonly sorts = SEARCH_SORTS;
  readonly result = input<PagedResponse<BursarySummaryResponse> | null>(null);
  readonly loading = input(false);
  readonly error = input(false);
  /** Pagination navigates to `/search`, so pages outside search (field landing) turn it off. */
  readonly showPagination = input(true);
  readonly sort = input<SearchSort | undefined>();
  /** The average filter, used to make the empty message specific. */
  readonly average = input<number | undefined>();
  readonly retry = output<void>();
  readonly clearFilters = output<void>();
  readonly sortChange = output<SearchSort | undefined>();

  protected emptyMessage(): string {
    const average = this.average();
    return average === undefined
      ? 'No bursaries match. Remove a filter to see more.'
      : `No bursaries match a ${average}% average. Lower it to see more.`;
  }
}
