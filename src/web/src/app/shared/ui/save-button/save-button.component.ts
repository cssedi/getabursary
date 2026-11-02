import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { TrackerStore } from '../../../features/tracker/tracker.store';

/** The bookmark that saves a bursary to "My bursaries". Hosts must not render it for closed bursaries. */
@Component({
  selector: 'app-save-button',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <button
      type="button"
      class="save-button relative z-10 inline-flex h-11 w-11 shrink-0 items-center justify-center rounded-control text-jacaranda hover:bg-bloom active:bg-bloom"
      [attr.aria-pressed]="saved()"
      (click)="toggle($event)"
    >
      <svg
        viewBox="0 0 16 16"
        width="16"
        height="16"
        [attr.fill]="saved() ? 'currentColor' : 'none'"
        stroke="currentColor"
        stroke-width="1.5"
        aria-hidden="true"
        focusable="false"
      >
        <path d="M3.5 2h9v12L8 10.5 3.5 14z" />
      </svg>
      <span class="sr-only">{{ saved() ? 'Saved' : 'Save ' + name() }}</span>
    </button>
  `,
})
export class SaveButtonComponent {
  private readonly store = inject(TrackerStore);
  readonly bursaryId = input.required<string>();
  readonly name = input.required<string>();

  protected readonly saved = computed(() => this.store.ids().has(this.bursaryId()));

  protected toggle(event: Event): void {
    // Sits inside a stretched-link row, so the click must not reach the link.
    event.preventDefault();
    event.stopPropagation();
    this.store.toggle(this.bursaryId());
  }
}
