import { DOCUMENT } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { SearchStateService } from '../search-state.service';

@Component({
  selector: 'app-pagination',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (totalPages() > 1) {
      <nav class="mt-4 flex items-center justify-between" aria-label="Pagination">
        <button
          type="button"
          class="prev rounded border px-3 py-2 disabled:opacity-50"
          [disabled]="page() <= 1"
          (click)="go(page() - 1)"
        >
          Previous
        </button>
        <span>Page {{ page() }} of {{ totalPages() }}</span>
        <button
          type="button"
          class="next rounded border px-3 py-2 disabled:opacity-50"
          [disabled]="page() >= totalPages()"
          (click)="go(page() + 1)"
        >
          Next
        </button>
      </nav>
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

  protected async go(page: number): Promise<void> {
    await this.state.update({ page });
    this.document.getElementById('results')?.scrollIntoView?.();
  }
}
