import { Injectable, signal } from '@angular/core';

/** How long a toast stays on screen before it clears itself. */
export const TOAST_MS = 4000;

/** One message at a time; `ToastComponent` renders it in a polite live region. */
@Injectable({ providedIn: 'root' })
export class ToastService {
  readonly message = signal<string | null>(null);
  private timer: ReturnType<typeof setTimeout> | undefined;

  show(message: string): void {
    clearTimeout(this.timer);
    this.message.set(message);
    this.timer = setTimeout(() => this.message.set(null), TOAST_MS);
  }
}
