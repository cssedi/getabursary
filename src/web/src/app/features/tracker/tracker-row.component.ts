import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TRACKER_STATUSES, TrackerEntryResponse, TrackerStatus } from '../../core/api/models';
import { ClosingDateBadgeComponent } from '../../shared/ui/closing-date-badge/closing-date-badge.component';

export const NOTES_DEBOUNCE_MS = 600;
export const NOTES_MAX = 2000;

export type TrackerChange = Partial<{ status: TrackerStatus; appliedOn: string | null; notes: string | null }>;

/** One tracked bursary: the ledger date block on the left, status, applied date and notes on the right. */
@Component({
  selector: 'app-tracker-row',
  imports: [RouterLink, ClosingDateBadgeComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <article class="flex gap-4 border-b border-line py-4" [class.opacity-60]="closed()">
      <div class="w-16 shrink-0">
        <app-closing-date-badge [closingDate]="entry().bursary.closingDate" [status]="entry().bursary.status" />
      </div>
      <div class="min-w-0 flex-1 space-y-3">
        <div>
          <h2 class="title text-lg font-semibold [overflow-wrap:anywhere]">
            @if (closed()) {
              {{ entry().bursary.name }}
            } @else {
              <a class="text-jacaranda hover:underline" [routerLink]="['/bursaries', entry().bursary.slug]">{{
                entry().bursary.name
              }}</a>
            }
          </h2>
          @if (entry().bursary.funderName) {
            <p class="funder truncate text-sm text-muted">{{ entry().bursary.funderName }}</p>
          }
        </div>
        <div class="flex flex-wrap items-end gap-3">
          <label class="grid gap-1 text-sm font-medium">
            Status
            <select
              class="status min-h-11 rounded-control border border-line bg-paper px-2 text-ink"
              name="status"
              autocomplete="off"
              [value]="entry().status"
              (change)="edited.emit({ status: asStatus($event) })"
            >
              @for (s of statuses; track s) {
                <option [value]="s" [selected]="s === entry().status">{{ s }}</option>
              }
            </select>
          </label>
          <label class="grid gap-1 text-sm font-medium">
            Applied on
            <input
              type="date"
              class="applied-on min-h-11 rounded-control border border-line bg-paper px-2 text-ink"
              name="appliedOn"
              autocomplete="off"
              [value]="entry().appliedOn ?? ''"
              (change)="edited.emit({ appliedOn: asText($event) || null })"
            />
          </label>
        </div>
        <label class="grid gap-1 text-sm font-medium">
          Notes
          <textarea
            class="notes min-h-20 rounded-control border border-line bg-paper px-2 py-1 font-normal text-ink"
            name="notes"
            autocomplete="off"
            rows="2"
            [attr.maxlength]="notesMax"
            placeholder="Documents to gather, contacts, reminders…"
            [value]="entry().notes ?? ''"
            (input)="queueNotes($event)"
            (blur)="flushNotes()"
          ></textarea>
        </label>
        <button
          type="button"
          class="remove min-h-11 rounded-control px-2 text-sm text-jacaranda hover:bg-bloom active:bg-bloom"
          (click)="remove.emit()"
        >
          Remove<span class="sr-only"> {{ entry().bursary.name }}</span>
        </button>
      </div>
    </article>
  `,
})
export class TrackerRowComponent {
  readonly entry = input.required<TrackerEntryResponse>();
  readonly edited = output<TrackerChange>();
  readonly remove = output<void>();

  protected readonly statuses = TRACKER_STATUSES;
  protected readonly notesMax = NOTES_MAX;
  private pending: string | undefined;
  private timer: ReturnType<typeof setTimeout> | undefined;

  protected closed(): boolean {
    return this.entry().bursary.status === 'Closed';
  }

  protected asStatus(event: Event): TrackerStatus {
    return (event.target as HTMLSelectElement).value as TrackerStatus;
  }

  protected asText(event: Event): string {
    return (event.target as HTMLInputElement).value;
  }

  protected queueNotes(event: Event): void {
    this.pending = (event.target as HTMLTextAreaElement).value.slice(0, NOTES_MAX);
    clearTimeout(this.timer);
    this.timer = setTimeout(() => this.flushNotes(), NOTES_DEBOUNCE_MS);
  }

  protected flushNotes(): void {
    clearTimeout(this.timer);
    if (this.pending === undefined) return;
    const notes = this.pending;
    this.pending = undefined;
    this.edited.emit({ notes: notes || null });
  }
}
