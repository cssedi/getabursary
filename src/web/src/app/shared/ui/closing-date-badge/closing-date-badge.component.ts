import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { BursaryStatus } from '../../../core/api/models';
import { formatDate } from '../../format/format';
import { TODAY } from '../../format/today';

const DAY_MS = 86_400_000;
export const CLOSING_SOON_DAYS = 14;

/** The ledger date block: big day number, month beneath, highlighter swipe when closing soon (06 §4–§5). */
@Component({
  selector: 'app-closing-date-badge',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (view(); as v) {
      <div class="flex items-center gap-3">
        @if (v.closed) {
          <span class="badge-closed w-16 shrink-0 text-lg font-bold text-closed">Closed</span>
        } @else {
          <span class="date-block w-16 shrink-0 leading-none">
            <span class="sr-only">Closes </span>
            <span class="day-swipe relative inline-block" [class.swipe]="v.soon">
              <span class="day relative text-3xl font-extrabold tabular-nums">{{ v.day }}</span>
            </span>&ngsp;<span class="month mt-1 block text-xs font-semibold tabular-nums">{{ v.month }}</span>
          </span>
          @if (v.soonText) {
            &ngsp;<span class="badge-soon text-sm text-ink">{{ v.soonText }}</span>
          }
        }
      </div>
    }
  `,
  styles: `
    .swipe::before {
      content: '';
      position: absolute;
      inset: 55% -4px 0 -4px;
      background: var(--highlighter);
      transform-origin: left;
    }
  `,
})
export class ClosingDateBadgeComponent {
  private readonly today = inject(TODAY);
  readonly closingDate = input<string | null>(null);
  readonly status = input<BursaryStatus>('Open');

  protected readonly view = computed(() => {
    const date = this.closingDate();
    if (this.status() === 'Closed') return { closed: true as const };
    if (!date) return null;
    const days = Math.round((Date.parse(`${date}T00:00:00Z`) - Date.parse(`${this.today}T00:00:00Z`)) / DAY_MS);
    if (days < 0) return { closed: true as const };
    const soon = days <= CLOSING_SOON_DAYS;
    let soonText: string | null = null;
    if (days === 0) soonText = 'closes today';
    else if (soon) soonText = `closes in ${days} ${days === 1 ? 'day' : 'days'}`;
    const sameYear = date.slice(0, 4) === this.today.slice(0, 4);
    return {
      closed: false as const,
      soon,
      soonText,
      day: formatDate(date, 'day'),
      month: formatDate(date, 'month') + (sameYear ? '' : ` ${date.slice(0, 4)}`),
    };
  });
}
