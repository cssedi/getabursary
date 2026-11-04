import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import {
  AdminApi,
  REVIEW_TYPES,
  ReviewAction,
  ReviewItem,
  ReviewType,
} from '../../core/api/admin-api';
import { ClosingDateBadgeComponent } from '../../shared/ui/closing-date-badge/closing-date-badge.component';
import { ErrorStateComponent } from '../../shared/ui/error-state/error-state.component';
import { ToastComponent } from '../../shared/ui/toast/toast.component';
import { ToastService } from '../../shared/ui/toast/toast.service';

const BUTTON =
  'min-h-11 rounded-control border border-line px-3 font-medium hover:bg-bloom active:bg-bloom';

/** "confidence: 0.5, candidate: …" — the ingestion details, as plain text. */
export function reasonText(reason: ReviewItem['reason']): string {
  return Object.entries(reason ?? {})
    .map(([k, v]) => `${k}: ${typeof v === 'object' ? JSON.stringify(v) : v}`)
    .join(', ');
}

/** `/admin`: open review items, newest first. Actions remove the row at once and put it back if the API refuses. */
@Component({
  selector: 'app-review-queue-page',
  imports: [RouterLink, ClosingDateBadgeComponent, ErrorStateComponent, ToastComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="flex flex-wrap items-end justify-between gap-3">
      <h2 class="text-xl font-bold">Review queue</h2>
      <div>
        <label for="review-type" class="mb-1 block text-sm font-medium">Type</label>
        <select
          id="review-type"
          name="type"
          autocomplete="off"
          class="min-h-11 rounded-control border border-line px-3"
          (change)="setType($any($event.target).value)"
        >
          <option value="">All types</option>
          @for (t of types; track t.value) {
            <option [value]="t.value">{{ t.label }}</option>
          }
        </select>
      </div>
    </div>

    @if (items(); as list) {
      <p class="open-count mt-3 tabular-nums text-muted" role="status">{{ total() }} open</p>
      @if (list.length) {
        <ul class="mt-2 border-t border-line">
          @for (item of list; track item.id) {
            <li class="queue-item flex flex-wrap gap-4 border-b border-line py-4">
              <app-closing-date-badge
                [closingDate]="item.closingDate"
                [status]="item.bursaryStatus ?? 'Open'"
              />
              <div class="min-w-0 flex-1 basis-64">
                <p class="text-sm text-muted">{{ typeLabel(item.type) }}</p>
                <p class="font-semibold [overflow-wrap:anywhere]">
                  {{ item.bursaryName ?? 'No bursary attached' }}
                </p>
                @if (item.funderName) {
                  <p class="text-muted [overflow-wrap:anywhere]">{{ item.funderName }}</p>
                }
                @if (reason(item); as text) {
                  <p class="mt-1 text-sm [overflow-wrap:anywhere]">{{ text }}</p>
                }
                @if (item.reportComment) {
                  <p class="mt-1 text-sm [overflow-wrap:anywhere]">“{{ item.reportComment }}”</p>
                }
                @if (dismissing() === item.id) {
                  <form
                    class="mt-3 flex flex-wrap items-end gap-2"
                    (submit)="$event.preventDefault(); dismiss(item, note.value)"
                  >
                    <div class="min-w-0 flex-1 basis-48">
                      <label [for]="'note-' + item.id" class="mb-1 block text-sm font-medium"
                        >Note (optional)</label
                      >
                      <input
                        #note
                        [id]="'note-' + item.id"
                        name="notes"
                        maxlength="2000"
                        autocomplete="off"
                        placeholder="Why dismiss this…"
                        class="min-h-11 w-full rounded-control border border-line px-3"
                      />
                    </div>
                    <button
                      type="submit"
                      class="btn-primary"
                    >
                      Dismiss item
                    </button>
                    <button type="button" [class]="button" (click)="dismissing.set(null)">
                      Cancel
                    </button>
                  </form>
                }
              </div>
              <div class="flex flex-wrap content-start gap-2">
                @if (item.bursaryId) {
                  <button type="button" [class]="button" (click)="resolve(item, 'Publish')">
                    Publish
                  </button>
                  <a
                    [class]="button + ' flex items-center'"
                    [routerLink]="['/admin/bursaries', item.bursaryId]"
                    >Edit</a
                  >
                }
                <button type="button" [class]="button" (click)="dismissing.set(item.id)">
                  Dismiss
                </button>
                @if (item.bursaryId) {
                  <button type="button" [class]="button" (click)="resolve(item, 'MarkClosed')">
                    Mark closed
                  </button>
                }
              </div>
            </li>
          }
        </ul>
      } @else {
        <p class="empty mt-3 border-y border-line py-6" role="status">Nothing to review.</p>
      }
    } @else if (failed()) {
      <app-error-state
        message="The review queue didn’t load. Check your connection and try again."
        (retry)="load()"
      />
    } @else {
      <p class="mt-3 text-muted" role="status">Loading the review queue…</p>
    }
    <app-toast />
  `,
})
export class ReviewQueuePage {
  private readonly api = inject(AdminApi);
  private readonly toast = inject(ToastService);

  protected readonly types = REVIEW_TYPES;
  protected readonly button = BUTTON;
  protected readonly items = signal<ReviewItem[] | null>(null);
  protected readonly total = signal(0);
  protected readonly failed = signal(false);
  protected readonly dismissing = signal<string | null>(null);
  private type: ReviewType | undefined;

  constructor() {
    this.load();
  }

  protected typeLabel(type: ReviewType): string {
    return REVIEW_TYPES.find((t) => t.value === type)?.label ?? type;
  }

  protected reason(item: ReviewItem): string {
    return reasonText(item.reason);
  }

  protected setType(value: string): void {
    this.type = (value || undefined) as ReviewType | undefined;
    this.items.set(null);
    this.load();
  }

  protected load(): void {
    this.failed.set(false);
    // shortcut: shows the first page (20) only; clearing it loads the next, so no paging controls.
    firstValueFrom(this.api.reviewItems(this.type)).then(
      (page) => {
        this.items.set(page.items);
        this.total.set(page.totalCount);
      },
      () => this.failed.set(true),
    );
  }

  protected dismiss(item: ReviewItem, note: string): Promise<void> {
    return this.resolve(item, 'Dismiss', note.trim() || undefined);
  }

  protected async resolve(item: ReviewItem, action: ReviewAction, notes?: string): Promise<void> {
    const list = this.items() ?? [];
    const index = list.indexOf(item);
    this.dismissing.set(null);
    this.items.set(list.filter((i) => i !== item));
    this.total.update((n) => n - 1);
    try {
      await firstValueFrom(this.api.resolve(item.id, action, notes));
    } catch {
      this.items.update((l) => (l ? [...l.slice(0, index), item, ...l.slice(index)] : l));
      this.total.update((n) => n + 1);
      this.toast.show('Couldn’t update that item. Check your connection and try again.');
      return;
    }
    if (this.items()?.length === 0 && this.total() > 0) this.load();
  }
}
