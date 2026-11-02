import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

@Component({
  selector: 'app-empty-state',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="rounded-[var(--radius)] bg-[var(--color-surface)] p-6 text-center" role="status">
      <p class="mb-3">{{ message() }}</p>
      @if (actionLabel()) {
        <button
          type="button"
          class="rounded bg-[var(--color-primary)] px-4 py-2 text-[var(--color-primary-ink)]"
          (click)="action.emit()"
        >
          {{ actionLabel() }}
        </button>
      }
    </div>
  `,
})
export class EmptyStateComponent {
  readonly message = input.required<string>();
  readonly actionLabel = input<string>();
  readonly action = output<void>();
}
