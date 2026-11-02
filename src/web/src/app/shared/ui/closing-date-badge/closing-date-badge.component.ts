import { ChangeDetectionStrategy, Component, InjectionToken, computed, inject, input } from '@angular/core';
import { BursaryStatus } from '../../../core/api/models';

/** Supplies "now" so tests can be deterministic. */
export const NOW = new InjectionToken<() => Date>('NOW', { providedIn: 'root', factory: () => () => new Date() });

const MONTHS = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
const DAY_MS = 86_400_000;
export const CLOSING_SOON_DAYS = 14;

/** Formats `2027-03-31` as `31 Mar 2027`. */
export function formatDate(iso: string): string {
  const [y, m, d] = iso.split('-').map(Number);
  return `${d} ${MONTHS[m - 1]} ${y}`;
}

@Component({
  selector: 'app-closing-date-badge',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (view(); as v) {
      @if (v.closed) {
        <span class="badge-closed rounded bg-gray-200 px-2 py-0.5 text-xs font-semibold text-gray-800">Closed</span>
      } @else {
        <span class="text-sm text-[var(--color-muted)]">{{ v.dateText }}</span>
        @if (v.soonText) {
          <span
            class="badge-soon ml-2 rounded bg-[var(--color-accent)] px-2 py-0.5 text-xs font-semibold text-[var(--color-ink)]"
            >{{ v.soonText }}</span
          >
        }
      }
    }
  `,
})
export class ClosingDateBadgeComponent {
  private readonly now = inject(NOW);
  readonly closingDate = input<string | null>(null);
  readonly status = input<BursaryStatus>('Open');

  protected readonly view = computed(() => {
    const date = this.closingDate();
    if (this.status() === 'Closed') return { closed: true, dateText: '', soonText: null };
    if (!date) return null;
    const today = this.now().toISOString().slice(0, 10);
    const days = Math.round((Date.parse(`${date}T00:00:00Z`) - Date.parse(`${today}T00:00:00Z`)) / DAY_MS);
    if (days < 0) return { closed: true, dateText: '', soonText: null };
    let soonText: string | null = null;
    if (days === 0) soonText = 'Closes today';
    else if (days <= CLOSING_SOON_DAYS) soonText = `Closes in ${days} ${days === 1 ? 'day' : 'days'}`;
    return { closed: false, dateText: `Closes ${formatDate(date)}`, soonText };
  });
}
