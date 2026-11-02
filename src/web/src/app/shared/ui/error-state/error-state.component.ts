import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

@Component({
  selector: 'app-error-state',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="rounded-[var(--radius)] bg-[var(--color-surface)] p-6 text-center" role="alert">
      <p class="mb-3">{{ message() }}</p>
      <button
        type="button"
        class="rounded bg-[var(--color-primary)] px-4 py-2 text-[var(--color-primary-ink)]"
        (click)="retry.emit()"
      >
        Try again
      </button>
    </div>
  `,
})
export class ErrorStateComponent {
  readonly message = input('Something went wrong loading bursaries.');
  readonly retry = output<void>();
}
