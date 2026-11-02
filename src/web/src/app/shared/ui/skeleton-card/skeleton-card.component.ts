import { ChangeDetectionStrategy, Component } from '@angular/core';

/** Placeholder ledger row: a date column and three lines. Static, so nothing animates while loading. */
@Component({
  selector: 'app-skeleton-card',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="skeleton-card flex gap-4 border-b border-line py-4" aria-hidden="true">
      <div class="h-12 w-16 shrink-0 bg-line"></div>
      <div class="min-w-0 flex-1">
        <div class="mb-2 h-5 w-2/3 bg-line"></div>
        <div class="mb-2 h-4 w-1/3 bg-line"></div>
        <div class="h-4 w-1/2 bg-line"></div>
      </div>
    </div>
  `,
})
export class SkeletonCardComponent {}
