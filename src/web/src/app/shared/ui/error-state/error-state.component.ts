import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

@Component({
  selector: 'app-error-state',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="rounded-control bg-paper p-6 text-center" role="alert">
      <p class="mb-3">{{ message() }}</p>
      <button
        type="button"
        class="rounded-control bg-jacaranda px-4 py-2 text-paper"
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
