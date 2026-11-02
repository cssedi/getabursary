import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

@Component({
  selector: 'app-empty-state',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="border-y border-line py-6" role="status">
      <p class="mb-3">{{ message() }}</p>
      @if (actionLabel()) {
        <button
          type="button"
          class="rounded-control bg-jacaranda px-4 py-2 text-paper"
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
