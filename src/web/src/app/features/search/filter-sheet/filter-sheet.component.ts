import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import { ChangeDetectionStrategy, Component, Signal, ViewEncapsulation, computed, inject } from '@angular/core';
import { FilterPanelComponent } from '../filter-panel/filter-panel.component';

export interface FilterSheetData {
  /** Total matches for the current filters; null while loading. */
  count: Signal<number | null>;
}

/** Mobile filters in a bottom sheet (CDK dialog). Closing it is handled by the page. */
@Component({
  selector: 'app-filter-sheet',
  imports: [FilterPanelComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  // None: the dialog backdrop and panel live outside this component's view.
  encapsulation: ViewEncapsulation.None,
  template: `
    <h2 class="mb-4 text-xl">Filters</h2>
    <app-filter-panel idPrefix="sheet" />
    <button
      type="button"
      class="show-results sticky bottom-0 mt-4 w-full rounded-control bg-jacaranda px-4 py-3 font-semibold text-paper"
      (click)="ref.close()"
    >
      {{ label() }}
    </button>
  `,
  styles: `
    .filter-sheet-backdrop {
      background: rgb(30 26 46 / 0.5);
    }
    .filter-sheet-panel app-filter-sheet {
      display: block;
      box-sizing: border-box;
      max-height: 85dvh;
      overflow-y: auto;
      overscroll-behavior: contain;
      padding: 24px 16px calc(16px + env(safe-area-inset-bottom));
      border-radius: 16px 16px 0 0;
      background: var(--paper);
    }
  `,
})
export class FilterSheetComponent {
  protected readonly ref = inject(DialogRef);
  private readonly count = inject<FilterSheetData>(DIALOG_DATA).count;
  protected readonly label = computed(() => {
    const n = this.count();
    return n === null ? 'Show bursaries' : `Show ${n} ${n === 1 ? 'bursary' : 'bursaries'}`;
  });
}
