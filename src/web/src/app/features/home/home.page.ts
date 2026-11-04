import { ChangeDetectionStrategy, Component, ElementRef, inject, signal, viewChild } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { Router, RouterLink } from '@angular/router';
import { catchError, map, of, startWith, switchMap } from 'rxjs';
import { BursaryApi } from '../../core/api/bursary-api';
import { FacultyQualificationsResponse, StudyLevel } from '../../core/api/models';
import { TaxonomyApi } from '../../core/api/taxonomy-api';
import { ClosingDateBadgeComponent } from '../../shared/ui/closing-date-badge/closing-date-badge.component';
import { STUDY_LEVELS, parseAverage, toQueryParams } from '../search/search-state.service';

interface Degrees {
  loading: boolean;
  groups: FacultyQualificationsResponse[];
}

// shortcut: there is no public sources endpoint, so the enabled sources (data/seed/sources.json) are listed here; replace with an API call when one exists.
const SOURCES = [
  { name: 'ZA Bursaries', url: 'https://www.zabursaries.co.za' },
  { name: 'StudyTrust', url: 'https://studytrust.org.za' },
  { name: 'AllBursaries', url: 'https://allbursaries.co.za' },
];

const STEPS = ['Pick your degree', 'See what you qualify for', 'Track your applications'];

/** Home: headline, degree search, closing soon, how it works, browse by field, sources (specs/06-design §5). */
@Component({
  selector: 'app-home-page',
  imports: [RouterLink, ClosingDateBadgeComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styles: `
    /* The date column is 64 px wide, so the badge's "closes in" text stacks under the date. */
    .date-column ::ng-deep app-closing-date-badge > div {
      flex-direction: column;
      align-items: flex-start;
      gap: 4px;
    }
    .swipe-draw {
      transform-origin: left center;
      transform-box: fill-box;
      animation: draw 500ms ease-out both;
    }
    @keyframes draw {
      from {
        transform: scaleX(0);
      }
      to {
        transform: scaleX(1);
      }
    }
    @media (prefers-reduced-motion: reduce) {
      .swipe-draw {
        animation: none;
      }
    }
  `,
  template: `
    <div class="mx-auto grid max-w-[1200px] gap-16 px-4 pb-16 pt-8 md:px-6 md:pt-16 xl:px-8 [&>section]:min-w-0 md:gap-24">
      <section class="grid gap-8 md:grid-cols-[1fr_auto] md:items-center md:gap-12">
        <div class="grid min-w-0 gap-6">
          <h1
            class="text-[clamp(2.75rem,8vw,6.5rem)] font-bold leading-[0.95] tracking-[-0.03em] [font-stretch:80%] [text-wrap:balance]"
          >
            Find the bursaries that fit your degree.
          </h1>
          <p class="reading">
            We read South Africa’s bursary sites so you don’t have to. Pick your degree and see what you qualify for,
            sorted by closing date.
          </p>
        </div>
        <svg
          class="mx-auto h-auto w-48 md:w-64"
          width="256"
          height="256"
          viewBox="0 0 256 256"
          fill="none"
          aria-hidden="true"
          focusable="false"
        >
          <rect class="swipe-draw" x="58" y="132" width="128" height="30" fill="var(--highlighter)" />
          <rect x="32" y="40" width="192" height="184" rx="8" stroke="var(--ink)" stroke-width="3" stroke-linejoin="round" />
          <path d="M32 84h192" stroke="var(--ink)" stroke-width="3" stroke-linecap="round" />
          <path d="M80 24v32M176 24v32" stroke="var(--ink)" stroke-width="3" stroke-linecap="round" />
          <path
            d="M64 112h20M118 112h20M172 112h20M64 146h20M118 146h20M172 146h20M64 180h20M118 180h20"
            stroke="var(--ink)"
            stroke-width="3"
            stroke-linecap="round"
          />
        </svg>
      </section>

      <form
        class="grid min-w-0 gap-4 rounded-control border border-line bg-paper-raised p-4 md:grid-cols-2 md:p-6"
        novalidate
        aria-label="Find bursaries"
        (submit)="$event.preventDefault(); search()"
      >
        <div class="grid min-w-0 gap-1">
          <label for="home-university" class="font-medium">University</label>
          <select
            id="home-university"
            name="university"
            autocomplete="off"
            (change)="onUniversity($any($event.target).value)"
          >
            <option value="">Any university</option>
            @for (i of institutions(); track i.id) {
              <option [value]="i.id">{{ i.name }}</option>
            }
          </select>
        </div>
        <div class="grid min-w-0 gap-1">
          <label for="home-qualification" class="font-medium">Qualification</label>
          <select
            id="home-qualification"
            name="qualification"
            autocomplete="off"
            [disabled]="!institutionId() || degrees().loading"
            (change)="degreeId.set($any($event.target).value || undefined)"
          >
            @if (!institutionId()) {
              <option value="">Pick a university first</option>
            } @else if (degrees().loading) {
              <option value="">Loading qualifications…</option>
            } @else {
              <option value="">Any qualification</option>
              @for (group of degrees().groups; track group.id) {
                <optgroup [label]="group.name">
                  @for (d of group.qualifications; track d.id) {
                    <option [value]="d.id">{{ d.name }}</option>
                  }
                </optgroup>
              }
            }
          </select>
        </div>
        <div class="grid min-w-0 gap-1">
          <label for="home-study-level" class="font-medium">Year of study</label>
          <select
            id="home-study-level"
            name="studyLevel"
            autocomplete="off"
            (change)="studyLevel.set($any($event.target).value || undefined)"
          >
            <option value="">Any year</option>
            @for (level of levels; track level) {
              <option [value]="level">{{ level }}</option>
            }
          </select>
        </div>
        <div class="grid min-w-0 gap-1">
          <label for="home-average" class="font-medium">Your average</label>
          <input
            #averageInput
            id="home-average"
            name="average"
            type="text"
            inputmode="numeric"
            autocomplete="off"
            placeholder="For example 72…"
            [attr.aria-invalid]="averageError() ? 'true' : null"
            [attr.aria-describedby]="averageError() ? 'home-average-error' : null"
            (input)="average.set($any($event.target).value)"
          />
          <p id="home-average-error" aria-live="polite" class="font-medium empty:hidden">{{ averageError() }}</p>
        </div>
        <div class="md:col-span-2 md:justify-self-end">
          <button type="submit" class="btn-primary w-full md:w-auto">Show bursaries</button>
        </div>
      </form>

      <section class="grid min-w-0 gap-4" aria-labelledby="closing-soon-heading">
        <div class="flex items-baseline justify-between gap-4">
          <h2 id="closing-soon-heading" class="text-3xl">Closing soon</h2>
          <a routerLink="/search" [queryParams]="{ sort: 'ClosingSoonest' }" class="text-jacaranda underline"
            >See all closing soon</a
          >
        </div>
        @if (closingSoon().length) {
          <ul class="min-h-[22rem]">
            @for (b of closingSoon(); track b.id) {
              <li class="relative flex gap-4 border-b border-line py-4 hover:bg-bloom">
                <div class="date-column w-16 shrink-0">
                  <app-closing-date-badge [closingDate]="b.closingDate" [status]="b.status" />
                </div>
                <div class="min-w-0 flex-1">
                  <a
                    class="text-lg font-semibold text-jacaranda [overflow-wrap:anywhere] hover:underline after:absolute after:inset-0 after:content-['']"
                    [routerLink]="['/bursaries', b.slug]"
                    >{{ b.name }}</a
                  >
                  @if (b.funderName) {
                    <p class="truncate text-sm text-muted">{{ b.funderName }}</p>
                  }
                </div>
              </li>
            }
          </ul>
        } @else {
          <p class="reading">No bursaries close in the next while. Search by your degree instead.</p>
        }
      </section>

      @defer (on viewport; hydrate on viewport) {
        <section class="grid gap-8" aria-labelledby="how-heading">
          <h2 id="how-heading" class="text-3xl">How it works</h2>
          <ol class="grid gap-6 md:grid-cols-3">
            @for (step of steps; track step; let n = $index) {
              <li class="flex items-baseline gap-4">
                <span class="text-4xl font-extrabold tabular-nums">{{ n + 1 }}</span>
                <span class="text-lg font-semibold">{{ step }}</span>
              </li>
            }
          </ol>
        </section>
      } @placeholder {
        <div class="min-h-48" aria-hidden="true"></div>
      }

      @defer (on viewport; hydrate on viewport) {
        <section class="grid gap-6" aria-labelledby="browse-heading">
          <h2 id="browse-heading" class="text-3xl">Browse by field</h2>
          @if (fields().length) {
            <ul class="grid gap-x-8 gap-y-3 sm:grid-cols-2 lg:grid-cols-3">
              @for (field of fields(); track field.id) {
                <li class="min-w-0">
                  <a
                    class="text-2xl font-bold text-ink [overflow-wrap:anywhere] hover:text-jacaranda hover:underline"
                    [routerLink]="['/bursaries/field', field.slug]"
                    >{{ field.name }}</a
                  >
                </li>
              }
            </ul>
          }
        </section>
      } @placeholder {
        <div class="min-h-96" aria-hidden="true"></div>
      }

      @defer (on viewport; hydrate on viewport) {
        <section class="grid gap-2" aria-labelledby="sources-heading">
          <h2 id="sources-heading" class="text-2xl">Where the listings come from</h2>
          <p class="reading">
            @for (s of sources; track s.name; let last = $last) {
              <a [href]="s.url" target="_blank" rel="noopener nofollow" class="text-jacaranda underline">{{ s.name }}</a
              >{{ last ? '.' : ', ' }}
            }
            Always apply on the funder’s own site.
          </p>
        </section>
      } @placeholder {
        <div class="min-h-32" aria-hidden="true"></div>
      }
    </div>
  `,
})
export class HomePage {
  private readonly router = inject(Router);
  private readonly api = inject(TaxonomyApi);
  private readonly bursaries = inject(BursaryApi);

  private readonly averageEl = viewChild.required<ElementRef<HTMLInputElement>>('averageInput');

  protected readonly levels = STUDY_LEVELS;
  protected readonly steps = STEPS;
  protected readonly sources = SOURCES;
  protected readonly institutionId = signal<string | undefined>(undefined);
  protected readonly degreeId = signal<string | undefined>(undefined);
  protected readonly studyLevel = signal<StudyLevel | undefined>(undefined);
  protected readonly average = signal('');
  protected readonly averageError = signal('');

  protected readonly institutions = toSignal(this.api.institutions().pipe(catchError(() => of([]))), {
    initialValue: [],
  });
  protected readonly fields = toSignal(this.api.fields().pipe(catchError(() => of([]))), { initialValue: [] });
  protected readonly closingSoon = toSignal(
    this.bursaries.search({ sort: 'ClosingSoonest', pageSize: 5 }).pipe(
      map((r) => r.items),
      catchError(() => of([])),
    ),
    { initialValue: [] },
  );

  protected readonly degrees = toSignal(
    toObservable(this.institutionId).pipe(
      switchMap((id) =>
        id
          ? this.api.qualificationsByInstitution(id).pipe(
              map((groups): Degrees => ({ loading: false, groups: groups.filter((g) => g.qualifications.length) })),
              catchError(() => of<Degrees>({ loading: false, groups: [] })),
              startWith<Degrees>({ loading: true, groups: [] }),
            )
          : of<Degrees>({ loading: false, groups: [] }),
      ),
    ),
    { initialValue: { loading: false, groups: [] } as Degrees },
  );

  protected onUniversity(value: string): void {
    this.institutionId.set(value || undefined);
    this.degreeId.set(undefined);
  }

  protected search(): void {
    const raw = this.average().trim();
    const average = parseAverage(raw);
    this.averageError.set(raw && average === undefined ? 'Enter your average as a number from 0 to 100, like 72.' : '');
    if (this.averageError()) {
      this.averageEl().nativeElement.focus();
      return;
    }
    const group = this.degrees().groups.find((g) => g.qualifications.some((d) => d.id === this.degreeId()));
    const queryParams = toQueryParams({
      institutionId: this.institutionId(),
      facultyId: group?.id,
      qualificationId: this.degreeId(),
      studyLevel: this.studyLevel(),
      average,
    });
    void this.router.navigate(['/search'], { queryParams });
  }
}
