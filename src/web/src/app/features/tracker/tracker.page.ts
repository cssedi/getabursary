import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { TRACKER_STATUSES, TrackerEntryResponse } from '../../core/api/models';
import { TrackerApi } from '../../core/api/tracker-api';
import { ErrorStateComponent } from '../../shared/ui/error-state/error-state.component';
import { ToastComponent } from '../../shared/ui/toast/toast.component';
import { ToastService } from '../../shared/ui/toast/toast.service';
import { TrackerChange, TrackerRowComponent } from './tracker-row.component';
import { TrackerStore } from './tracker.store';

/** "3 saved, 2 applied, 1 awarded": plain text, statuses with no entries left out. */
export function statusCounts(entries: TrackerEntryResponse[]): string {
  return TRACKER_STATUSES.map((s) => [s, entries.filter((e) => e.status === s).length] as const)
    .filter(([, n]) => n > 0)
    .map(([s, n]) => `${n} ${s.toLowerCase()}`)
    .join(', ');
}

/** `/my-bursaries`: everything the student saved, with status, applied date and notes. */
@Component({
  selector: 'app-tracker-page',
  imports: [RouterLink, ErrorStateComponent, ToastComponent, TrackerRowComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="mx-auto max-w-[720px]">
      <h1 class="text-2xl font-bold">My bursaries</h1>
      @if (entries(); as list) {
        @if (list.length) {
          <p class="counts mt-2 text-muted">{{ counts() }}</p>
          <div class="mt-4 border-t border-line">
            @for (e of list; track e.bursary.id) {
              <app-tracker-row [entry]="e" (edited)="update(e, $event)" (remove)="remove(e)" />
            }
          </div>
        } @else {
          <div class="empty mt-4 border-y border-line py-6" role="status">
            <p class="mb-3">Save bursaries to track them here.</p>
            <a class="text-jacaranda hover:underline" routerLink="/search">Find bursaries</a>
          </div>
        }
      } @else if (failed()) {
        <app-error-state message="Your bursaries didn’t load. Check your connection and try again." (retry)="load()" />
      } @else {
        <p class="mt-4 text-muted" role="status">Loading your bursaries…</p>
      }
      <app-toast />
    </div>
  `,
})
export class TrackerPage {
  private readonly store = inject(TrackerStore);
  private readonly api = inject(TrackerApi);
  private readonly toast = inject(ToastService);

  protected readonly entries = signal<TrackerEntryResponse[] | null>(null);
  protected readonly failed = signal(false);
  protected readonly counts = computed(() => statusCounts(this.entries() ?? []));

  constructor() {
    this.load();
  }

  protected load(): void {
    this.failed.set(false);
    this.store.load().then(
      (list) => this.entries.set(list),
      () => this.failed.set(true),
    );
  }

  /** Optimistic: shows the change at once and puts the old entry back if the API refuses. */
  protected async update(entry: TrackerEntryResponse, change: TrackerChange): Promise<void> {
    const next = { ...entry, ...change };
    this.replace(entry.bursary.id, next);
    try {
      await firstValueFrom(
        this.api.upsert(entry.bursary.id, { status: next.status, appliedOn: next.appliedOn, notes: next.notes }),
      );
    } catch {
      this.replace(entry.bursary.id, entry);
      this.toast.show('Couldn’t save your changes. Check your connection and try again.');
    }
  }

  protected async remove(entry: TrackerEntryResponse): Promise<void> {
    if (await this.store.remove(entry.bursary.id)) {
      this.entries.update((list) => list?.filter((e) => e.bursary.id !== entry.bursary.id) ?? null);
    }
  }

  private replace(id: string, entry: TrackerEntryResponse): void {
    this.entries.update((list) => list?.map((e) => (e.bursary.id === id ? entry : e)) ?? null);
  }
}
