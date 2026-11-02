import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

@Component({
  selector: 'app-error-state',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="border-y border-line py-6" role="alert">
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
  readonly message = input("Bursaries didn't load. Check your connection and try again.");
  readonly retry = output<void>();
}
