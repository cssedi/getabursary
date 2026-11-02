import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ToastService } from './toast.service';

/** Polite live region for {@link ToastService}. Mount it once on each page that shows toasts. */
@Component({
  selector: 'app-toast',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div aria-live="polite" class="pointer-events-none fixed inset-x-4 bottom-24 z-20 flex justify-center">
      @if (toast.message(); as message) {
        <p class="rounded-control bg-ink px-4 py-3 font-medium text-paper">{{ message }}</p>
      }
    </div>
  `,
})
export class ToastComponent {
  protected readonly toast = inject(ToastService);
}
