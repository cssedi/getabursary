import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { EMPTY, catchError, debounceTime, switchMap, tap } from 'rxjs';
import { AdminApi, AdminBursarySummary } from '../../core/api/admin-api';
import { FormatDatePipe } from '../../shared/format/format';

const BUTTON =
  'min-h-11 rounded-control border border-line px-4 font-medium hover:bg-bloom active:bg-bloom disabled:opacity-60';

export const SEARCH_DEBOUNCE_MS = 300;

/** `/admin/bursaries`: find any bursary, published or not, and open its editor. */
@Component({
  selector: 'app-admin-bursaries-page',
  imports: [RouterLink, FormatDatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h2 class="text-xl font-bold">Bursaries</h2>
    <div class="mt-3 flex flex-wrap gap-3">
      <div class="min-w-0 flex-1 basis-64">
        <label for="admin-search" class="mb-1 block text-sm font-medium">Name</label>
        <input
          id="admin-search"
          type="search"
          name="query"
          autocomplete="off"
          placeholder="Search by name…"
          class="min-h-11 w-full rounded-control border border-line px-3"
          (input)="setQuery($any($event.target).value)"
        />
      </div>
      <div>
        <label for="admin-published" class="mb-1 block text-sm font-medium">Status</label>
        <select
          id="admin-published"
          name="published"
          autocomplete="off"
          class="min-h-11 rounded-control border border-line px-3"
          (change)="setPublished($any($event.target).value)"
        >
          <option value="">All</option>
          <option value="true">Published</option>
          <option value="false">Unpublished</option>
        </select>
      </div>
    </div>

    @if (error()) {
      <p role="alert" class="mt-3 font-medium">
        Bursaries didn’t load. Check your connection and try again.
      </p>
    }
    @if (rows(); as list) {
      @if (list.length) {
        <div class="mt-3 overflow-x-auto">
          <table class="w-full text-left">
            <thead>
              <tr class="border-b border-line text-sm text-muted">
                <th scope="col" class="py-2 pr-4 font-medium">Bursary</th>
                <th scope="col" class="py-2 pr-4 font-medium">Closing date</th>
                <th scope="col" class="py-2 font-medium">Status</th>
              </tr>
            </thead>
            <tbody>
              @for (b of list; track b.id) {
                <tr class="border-b border-line">
                  <td class="min-w-48 py-3 pr-4 [overflow-wrap:anywhere]">
                    <a
                      class="font-semibold text-jacaranda hover:underline"
                      [routerLink]="['/admin/bursaries', b.id]"
                      >{{ b.name }}</a
                    >
                    @if (b.funderName) {
                      <span class="block text-sm text-muted">{{ b.funderName }}</span>
                    }
                  </td>
                  <td class="py-3 pr-4 whitespace-nowrap tabular-nums">
                    {{ b.closingDate ? (b.closingDate | formatDate) : 'Not set' }}
                  </td>
                  <td class="py-3">{{ b.isPublished ? 'Published' : 'Unpublished' }}</td>
                </tr>
              }
            </tbody>
          </table>
        </div>
        <div class="mt-3 flex items-center gap-3">
          <button
            type="button"
            [class]="button"
            [disabled]="page() === 1"
            (click)="page.set(page() - 1)"
          >
            Previous
          </button>
          <span class="tabular-nums text-muted">Page {{ page() }}, {{ total() }} bursaries</span>
          <button
            type="button"
            [class]="button"
            [disabled]="page() * pageSize >= total()"
            (click)="page.set(page() + 1)"
          >
            Next
          </button>
        </div>
      } @else {
        <p class="empty mt-3 border-y border-line py-6" role="status">
          No bursaries match. Try a shorter name.
        </p>
      }
    } @else if (!error()) {
      <p class="mt-3 text-muted" role="status">Loading bursaries…</p>
    }
  `,
})
export class AdminBursariesPage {
  private readonly api = inject(AdminApi);

  protected readonly button = BUTTON;
  protected readonly pageSize = 20;
  protected readonly query = signal('');
  protected readonly published = signal<boolean | undefined>(undefined);
  protected readonly page = signal(1);
  protected readonly rows = signal<AdminBursarySummary[] | null>(null);
  protected readonly total = signal(0);
  protected readonly error = signal(false);

  constructor() {
    // One debounce for text and filters alike; a page click waits the same 300 ms. Fine for an admin tool.
    toObservable(computed(() => ({ q: this.query(), p: this.published(), n: this.page() })))
      .pipe(
        debounceTime(SEARCH_DEBOUNCE_MS),
        tap(() => this.error.set(false)),
        switchMap(({ q, p, n }) =>
          this.api.bursaries(q.trim(), p, n).pipe(
            catchError(() => {
              this.error.set(true);
              return EMPTY;
            }),
          ),
        ),
        takeUntilDestroyed(),
      )
      .subscribe((page) => {
        this.rows.set(page.items);
        this.total.set(page.totalCount);
      });
  }

  protected setQuery(value: string): void {
    this.query.set(value);
    this.page.set(1);
  }

  protected setPublished(value: string): void {
    this.published.set(value === '' ? undefined : value === 'true');
    this.page.set(1);
  }
}
