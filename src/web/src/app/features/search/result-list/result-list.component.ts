import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { BursarySummaryResponse, PagedResponse } from '../../../core/api/models';
import { EmptyStateComponent } from '../../../shared/ui/empty-state/empty-state.component';
import { ErrorStateComponent } from '../../../shared/ui/error-state/error-state.component';
import { SkeletonCardComponent } from '../../../shared/ui/skeleton-card/skeleton-card.component';
import { BursaryCardComponent } from '../bursary-card/bursary-card.component';
import { PaginationComponent } from '../pagination/pagination.component';

export const AD_SLOT_EVERY = 6;

@Component({
  selector: 'app-result-list',
  imports: [BursaryCardComponent, EmptyStateComponent, ErrorStateComponent, SkeletonCardComponent, PaginationComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section id="results" aria-live="polite">
      @if (loading()) {
        <div class="grid gap-3">
          @for (n of [1, 2, 3]; track n) {
            <app-skeleton-card />
          }
        </div>
      } @else if (error()) {
        <app-error-state (retry)="retry.emit()" />
      } @else if (result(); as r) {
        @if (r.items.length === 0) {
          <app-empty-state
            message="No bursaries match. Try removing the average filter."
            actionLabel="Clear filters"
            (action)="clearFilters.emit()"
          />
        } @else {
          <p class="mb-3 font-medium">
            {{ r.totalCount }} {{ r.totalCount === 1 ? 'bursary matches' : 'bursaries match' }}
          </p>
          <ul class="grid gap-3">
            @for (b of r.items; track b.id; let i = $index) {
              <li><app-bursary-card [bursary]="b" /></li>
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
  readonly result = input<PagedResponse<BursarySummaryResponse> | null>(null);
  readonly loading = input(false);
  readonly error = input(false);
  /** Pagination navigates to `/search`, so pages outside search (field landing) turn it off. */
  readonly showPagination = input(true);
  readonly retry = output<void>();
  readonly clearFilters = output<void>();
}
