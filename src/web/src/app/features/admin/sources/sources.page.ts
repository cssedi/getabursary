import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Subscription, firstValueFrom, interval, take } from 'rxjs';
import { AdminApi, AdminSource, IngestionRun, RunStatus } from '../../../core/api/admin-api';
import { PagedResponse } from '../../../core/api/models';
import { ErrorStateComponent } from '../../../shared/ui/error-state/error-state.component';
import { ToastComponent } from '../../../shared/ui/toast/toast.component';
import { ToastService } from '../../../shared/ui/toast/toast.service';

/** After "Run now", the list reloads every 15 s for 2 minutes, then stops. */
export const POLL_MS = 15_000;
export const POLL_WINDOW_MS = 120_000;
export const RUN_REQUESTED = 'Run requested. The worker starts it within a minute.';

const POLL_TICKS = POLL_WINDOW_MS / POLL_MS;
const ERROR_PREVIEW = 80;
const BUTTON =
  'min-h-11 rounded-control border border-line px-3 font-medium hover:bg-bloom active:bg-bloom disabled:opacity-60';

const STATUS_LABELS: Record<RunStatus, string> = {
  Running: 'Running',
  Succeeded: 'Succeeded',
  PartiallyFailed: 'Partly failed',
  Failed: 'Failed',
};

const TRIGGER_LABELS: Record<IngestionRun['trigger'], string> = {
  Scheduled: 'Scheduled',
  Manual: 'Requested by an admin',
};

const RELATIVE = new Intl.RelativeTimeFormat('en-ZA', { numeric: 'auto' });
const UNITS: [Intl.RelativeTimeFormatUnit, number][] = [
  ['day', 86_400],
  ['hour', 3_600],
  ['minute', 60],
];
const DATE_TIME = new Intl.DateTimeFormat('en-ZA', { dateStyle: 'medium', timeStyle: 'short' });

/** "2 hours ago", "yesterday", "30 seconds ago" (`numeric: 'auto'` gives "now" at zero). */
export function relativeTime(iso: string, now = Date.now()): string {
  const seconds = Math.round((Date.parse(iso) - now) / 1000);
  for (const [unit, size] of UNITS) {
    if (Math.abs(seconds) >= size) return RELATIVE.format(Math.round(seconds / size), unit);
  }
  return RELATIVE.format(seconds, 'second');
}

/** `/admin/sources`: scraper health, enabled switches, "Run now", and recent runs. */
@Component({
  selector: 'app-sources-page',
  imports: [ErrorStateComponent, ToastComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="flex flex-wrap items-end justify-between gap-3">
      <h2 class="text-xl font-bold">Sources</h2>
      <button
        type="button"
        class="min-h-11 rounded-control bg-jacaranda px-4 font-semibold text-paper hover:bg-ink active:bg-ink"
        (click)="requestRun()"
      >
        Run all sources now
      </button>
    </div>
    <p class="mt-2 min-h-6 font-medium" role="status">{{ runMessage() }}</p>

    @if (sources(); as list) {
      @if (list.length) {
        <div class="mt-3 overflow-x-auto">
          <table class="w-full text-left">
            <thead>
              <tr class="border-b border-line text-sm text-muted">
                <th scope="col" class="py-2 pr-4 font-medium">Source</th>
                <th scope="col" class="py-2 pr-4 font-medium">Enabled</th>
                <th scope="col" class="py-2 pr-4 font-medium">Last run</th>
                <th scope="col" class="py-2 pr-4 text-right font-medium">Discovered</th>
                <th scope="col" class="py-2 pr-4 text-right font-medium">Created</th>
                <th scope="col" class="py-2 pr-4 text-right font-medium">Updated</th>
                <th scope="col" class="py-2 pr-4 text-right font-medium">Failed</th>
                <th scope="col" class="py-2 pr-4 font-medium">Last error</th>
                <th scope="col" class="py-2 font-medium"><span class="sr-only">Run</span></th>
              </tr>
            </thead>
            <tbody>
              @for (source of list; track source.id) {
                <tr class="border-b border-line align-top">
                  <th
                    scope="row"
                    class="min-w-40 py-3 pr-4 text-left font-semibold [overflow-wrap:anywhere]"
                  >
                    {{ source.name }}
                  </th>
                  <td class="py-3 pr-4">
                    <label class="flex min-h-11 items-center gap-2">
                      <input
                        type="checkbox"
                        role="switch"
                        class="size-5 accent-jacaranda"
                        [checked]="source.enabled"
                        (change)="setEnabled(source, $any($event.target).checked)"
                      />
                      <span class="sr-only">Enabled for {{ source.name }}</span>
                      <span aria-hidden="true">{{ source.enabled ? 'On' : 'Off' }}</span>
                    </label>
                  </td>
                  <td class="py-3 pr-4">
                    @if (source.lastRun; as run) {
                      <time
                        [attr.datetime]="run.startedAt"
                        [attr.title]="absolute(run.startedAt)"
                        class="tabular-nums"
                        >{{ relative(run.startedAt) }}</time
                      >
                      <span class="block text-sm text-muted">{{ statusLabels[run.status] }}</span>
                    } @else {
                      <span class="text-muted">Never run</span>
                    }
                  </td>
                  <td class="py-3 pr-4 text-right tabular-nums">
                    {{ source.lastRun?.discovered ?? '–' }}
                  </td>
                  <td class="py-3 pr-4 text-right tabular-nums">
                    {{ source.lastRun?.created ?? '–' }}
                  </td>
                  <td class="py-3 pr-4 text-right tabular-nums">
                    {{ source.lastRun?.updated ?? '–' }}
                  </td>
                  <td class="py-3 pr-4 text-right tabular-nums">
                    {{ source.lastRun?.failed ?? '–' }}
                  </td>
                  <td class="max-w-64 py-3 pr-4 text-sm">
                    @if (source.lastRun?.errorSummary; as error) {
                      <p class="[overflow-wrap:anywhere]">{{ errorText(source.id, error) }}</p>
                      @if (error.length > errorPreview) {
                        <button
                          type="button"
                          class="mt-1 min-h-11 font-medium text-jacaranda hover:underline"
                          [attr.aria-expanded]="expandedErrors().has(source.id)"
                          (click)="toggleError(source.id)"
                        >
                          {{ expandedErrors().has(source.id) ? 'Show less' : 'Show full error' }}
                        </button>
                      }
                    } @else {
                      <span class="text-muted">None</span>
                    }
                  </td>
                  <td class="py-3">
                    <button
                      type="button"
                      [class]="button"
                      [attr.aria-label]="'Run ' + source.name + ' now'"
                      (click)="requestRun(source.id)"
                    >
                      Run now
                    </button>
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      } @else {
        <p class="empty mt-3 border-y border-line py-6" role="status">No sources are set up yet.</p>
      }
    } @else if (sourcesFailed()) {
      <app-error-state
        message="Sources didn’t load. Check your connection and try again."
        (retry)="loadSources()"
      />
    } @else {
      <p class="mt-3 text-muted" role="status">Loading sources…</p>
    }

    <h3 class="mt-8 text-lg font-bold">Recent runs</h3>
    @if (runs(); as page) {
      @if (page.items.length) {
        <ul class="mt-2 border-t border-line">
          @for (run of page.items; track run.id) {
            <li class="border-b border-line py-3">
              <p>
                <time
                  [attr.datetime]="run.startedAt"
                  [attr.title]="absolute(run.startedAt)"
                  class="font-medium tabular-nums"
                  >{{ relative(run.startedAt) }}</time
                >
              </p>
              <p class="text-sm text-muted">{{ triggerLabels[run.trigger] }}</p>
              <p class="text-sm">{{ statusLabels[run.status] }}</p>
              <ul class="mt-1 text-sm tabular-nums">
                @for (s of run.sources; track s.sourceId) {
                  <li class="[overflow-wrap:anywhere]">
                    {{ s.sourceName }}: {{ s.discovered }} discovered, {{ s.created }} created,
                    {{ s.updated }} updated, {{ s.failed }} failed
                  </li>
                }
              </ul>
            </li>
          }
        </ul>
        <div class="mt-3 flex flex-wrap items-center gap-3">
          <button
            type="button"
            [class]="button"
            [disabled]="page.page <= 1"
            (click)="goToRuns(page.page - 1)"
          >
            Previous
          </button>
          <span class="tabular-nums text-muted"
            >Page {{ page.page }}, {{ page.totalCount }} runs</span
          >
          <button
            type="button"
            [class]="button"
            [disabled]="page.page * page.pageSize >= page.totalCount"
            (click)="goToRuns(page.page + 1)"
          >
            Next
          </button>
        </div>
      } @else {
        <p class="empty mt-2 border-y border-line py-6" role="status">
          No runs yet. Run all sources now to start one.
        </p>
      }
    } @else if (runsFailed()) {
      <app-error-state
        message="Run history didn’t load. Check your connection and try again."
        (retry)="loadRuns()"
      />
    } @else {
      <p class="mt-2 text-muted" role="status">Loading run history…</p>
    }
    <app-toast />
  `,
})
export class SourcesPage {
  private readonly api = inject(AdminApi);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);
  private polling?: Subscription;

  protected readonly button = BUTTON;
  protected readonly errorPreview = ERROR_PREVIEW;
  protected readonly statusLabels = STATUS_LABELS;
  protected readonly triggerLabels = TRIGGER_LABELS;
  protected readonly sources = signal<AdminSource[] | null>(null);
  protected readonly sourcesFailed = signal(false);
  protected readonly runs = signal<PagedResponse<IngestionRun> | null>(null);
  protected readonly runsFailed = signal(false);
  protected readonly runsPageNo = signal(1);
  protected readonly runMessage = signal('');
  protected readonly expandedErrors = signal<ReadonlySet<string>>(new Set());

  constructor() {
    this.loadSources();
    this.loadRuns();
  }

  protected loadSources(): void {
    this.sourcesFailed.set(false);
    firstValueFrom(this.api.sources()).then(
      (list) => this.sources.set(list),
      () => this.sourcesFailed.set(true),
    );
  }

  protected loadRuns(): void {
    this.runsFailed.set(false);
    firstValueFrom(this.api.ingestionRuns(this.runsPageNo())).then(
      (page) => this.runs.set(page),
      () => this.runsFailed.set(true),
    );
  }

  protected goToRuns(page: number): void {
    this.runsPageNo.set(page);
    this.runs.set(null);
    this.loadRuns();
  }

  /** No `sourceId` runs every enabled source. */
  protected requestRun(sourceId?: string): void {
    firstValueFrom(this.api.requestRun(sourceId)).then(
      () => {
        this.runMessage.set(RUN_REQUESTED);
        this.poll();
      },
      () => this.toast.show('Run wasn’t requested. Check your connection and try again.'),
    );
  }

  /** Reloads the sources and runs every 15 s for 2 minutes, so the new run shows up without a refresh. */
  private poll(): void {
    this.polling?.unsubscribe();
    this.polling = interval(POLL_MS)
      .pipe(take(POLL_TICKS), takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.loadSources();
        this.loadRuns();
      });
  }

  /** Flips the switch at once and puts it back if the API refuses. */
  protected async setEnabled(source: AdminSource, enabled: boolean): Promise<void> {
    this.replaceSource(source.id, { enabled });
    try {
      this.replaceSource(
        source.id,
        await firstValueFrom(this.api.setSourceEnabled(source.id, enabled)),
      );
    } catch {
      this.replaceSource(source.id, { enabled: !enabled });
      this.toast.show('Couldn’t change that source. Check your connection and try again.');
    }
  }

  private replaceSource(id: string, change: Partial<AdminSource>): void {
    this.sources.update(
      (list) => list?.map((s) => (s.id === id ? { ...s, ...change } : s)) ?? null,
    );
  }

  protected toggleError(id: string): void {
    this.expandedErrors.update((set) => {
      const next = new Set(set);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  }

  protected errorText(id: string, error: string): string {
    if (error.length <= ERROR_PREVIEW || this.expandedErrors().has(id)) return error;
    return `${error.slice(0, ERROR_PREVIEW).trimEnd()}…`;
  }

  // shortcut: relative times are re-read on each render (a poll or a click), not on a timer; upgrade if admins keep the tab open.
  protected relative(iso: string): string {
    return relativeTime(iso);
  }

  protected absolute(iso: string): string {
    return DATE_TIME.format(new Date(iso));
  }
}
