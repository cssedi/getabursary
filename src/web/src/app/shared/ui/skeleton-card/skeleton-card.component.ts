import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-skeleton-card',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="skeleton-card animate-pulse rounded-control bg-paper p-4" aria-hidden="true">
      <div class="mb-2 h-5 w-2/3 rounded bg-gray-200"></div>
      <div class="mb-2 h-4 w-1/3 rounded bg-gray-200"></div>
      <div class="h-4 w-1/2 rounded bg-gray-200"></div>
    </div>
  `,
})
export class SkeletonCardComponent {}
