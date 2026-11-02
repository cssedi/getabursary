import { DOCUMENT } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { Params, RouterLink } from '@angular/router';
import { SearchStateService, toQueryParams } from '../search-state.service';

@Component({
  selector: 'app-pagination',
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (totalPages() > 1) {
      <nav class="mt-4 flex items-center justify-between" aria-label="Pagination">
        @if (page() > 1) {
          <a class="prev link" routerLink="/search" [queryParams]="params(page() - 1)" (click)="scrollToResults()">Previous</a>
        } @else {
          <span class="prev" aria-disabled="true">Previous</span>
        }
        <span class="tabular-nums">Page {{ page() }} of {{ totalPages() }}</span>
        @if (page() < totalPages()) {
          <a class="next link" routerLink="/search" [queryParams]="params(page() + 1)" (click)="scrollToResults()">Next</a>
        } @else {
          <span class="next" aria-disabled="true">Next</span>
        }
      </nav>
    }
  `,
  styles: `
    .prev,
    .next {
      display: inline-flex;
      align-items: center;
      min-height: 44px;
      padding: 8px 16px;
      border: 1px solid var(--line);
      border-radius: var(--radius-control);
      font-weight: 500;
    }
    span[aria-disabled='true'] {
      opacity: 0.5;
    }
    .link {
      color: var(--jacaranda);
    }
    .link:hover,
    .link:active {
      background: var(--bloom);
    }
  `,
})
export class PaginationComponent {
  private readonly state = inject(SearchStateService);
  private readonly document = inject(DOCUMENT);
  readonly page = input.required<number>();
  readonly pageSize = input.required<number>();
  readonly totalCount = input.required<number>();
  protected readonly totalPages = computed(() =>
    Math.max(1, Math.ceil(this.totalCount() / Math.max(1, this.pageSize()))),
  );

  /** Real links, so Cmd/Ctrl/middle-click work (06 §7). */
  protected params(page: number): Params {
    return toQueryParams({ ...this.state.criteria(), page });
  }

  protected scrollToResults(): void {
    this.document.getElementById('results')?.scrollIntoView?.();
  }
}
