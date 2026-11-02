import { ChangeDetectionStrategy, Component, ElementRef, inject, signal, viewChild } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { Router, RouterLink } from '@angular/router';
import { catchError, defaultIfEmpty, forkJoin, map, of, startWith, switchMap } from 'rxjs';
import { FacultyResponse, QualificationResponse, StudyLevel } from '../../core/api/models';
import { TaxonomyApi } from '../../core/api/taxonomy-api';
import { STUDY_LEVELS, parseAverage, toQueryParams } from '../search/search-state.service';

interface DegreeGroup {
  faculty: FacultyResponse;
  degrees: QualificationResponse[];
}

interface Degrees {
  loading: boolean;
  groups: DegreeGroup[];
}

/** Home: the search is the hero, a fill-in-the-blanks sentence (specs/06-design §5). */
@Component({
  selector: 'app-home-page',
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="mx-auto grid max-w-xl gap-6 p-4">
      <form class="grid gap-4" novalidate (submit)="$event.preventDefault(); search()">
        <h1 class="text-[31px] font-bold leading-[1.15]">Find bursaries for</h1>
        <div class="flex flex-wrap items-center gap-x-2 gap-y-3 text-[20px]">
          <label for="home-degree" class="sr-only">Degree</label>
          <select
            id="home-degree"
            name="degree"
            class="min-h-11 min-w-0 max-w-full rounded-control border border-line bg-paper px-2 text-ink touch-manipulation disabled:text-muted"
            [disabled]="!institutionId() || degrees().loading"
            (change)="degreeId.set($any($event.target).value || undefined)"
          >
            @if (!institutionId()) {
              <option value="">Choose a university first…</option>
            } @else if (degrees().loading) {
              <option value="">Loading degrees…</option>
            } @else {
              <option value="">Any degree</option>
              @for (group of degrees().groups; track group.faculty.id) {
                <optgroup [label]="group.faculty.name">
                  @for (d of group.degrees; track d.id) {
                    <option [value]="d.id">{{ d.name }}</option>
                  }
                </optgroup>
              }
            }
          </select>
          <span>at</span>
          <label for="home-university" class="sr-only">University</label>
          <select
            #university
            id="home-university"
            name="university"
            class="min-h-11 min-w-0 max-w-full rounded-control border border-line bg-paper px-2 text-ink touch-manipulation"
            [attr.aria-invalid]="universityError() ? 'true' : null"
            [attr.aria-describedby]="universityError() ? 'home-university-error' : null"
            (change)="onUniversity($any($event.target).value)"
          >
            <option value="">Choose a university…</option>
            @for (i of institutions(); track i.id) {
              <option [value]="i.id">{{ i.name }}</option>
            }
          </select>
          <span>in</span>
          <label for="home-study-level" class="sr-only">Study level</label>
          <select
            id="home-study-level"
            name="studyLevel"
            class="min-h-11 rounded-control border border-line bg-paper px-2 text-ink touch-manipulation"
            (change)="studyLevel.set($any($event.target).value || undefined)"
          >
            <option value="">any level</option>
            @for (level of levels; track level) {
              <option [value]="level">{{ level }}</option>
            }
          </select>
          <span>with a</span>
          <label for="home-average" class="sr-only">Your average</label>
          <input
            #averageInput
            id="home-average"
            name="average"
            type="text"
            inputmode="numeric"
            autocomplete="off"
            placeholder="e.g. 72…"
            class="min-h-11 w-24 rounded-control border border-line bg-paper px-2 text-ink touch-manipulation"
            [attr.aria-invalid]="averageError() ? 'true' : null"
            [attr.aria-describedby]="averageError() ? 'home-average-error' : null"
            (input)="average.set($any($event.target).value)"
          />
          <span>% average.</span>
        </div>
        <div aria-live="polite" class="grid gap-1 text-ink">
          @if (universityError()) {
            <p id="home-university-error" class="font-medium">{{ universityError() }}</p>
          }
          @if (averageError()) {
            <p id="home-average-error" class="font-medium">{{ averageError() }}</p>
          }
        </div>
        <div>
          <button
            type="submit"
            class="min-h-11 rounded-control bg-jacaranda px-4 font-medium text-paper touch-manipulation hover:bg-ink active:bg-ink"
          >
            Show bursaries
          </button>
        </div>
      </form>
      @if (fields().length) {
        <p class="border-t border-line pt-4">
          Browse by field:
          @for (field of fields(); track field.id; let last = $last) {
            <a [routerLink]="['/bursaries/field', field.slug]" class="text-jacaranda underline">{{ field.name }}</a
            >{{ last ? '' : ', ' }}
          }
        </p>
      }
    </section>
  `,
})
export class HomePage {
  private readonly router = inject(Router);
  private readonly api = inject(TaxonomyApi);

  private readonly universityEl = viewChild.required<ElementRef<HTMLSelectElement>>('university');
  private readonly averageEl = viewChild.required<ElementRef<HTMLInputElement>>('averageInput');

  protected readonly levels = STUDY_LEVELS;
  protected readonly institutionId = signal<string | undefined>(undefined);
  protected readonly degreeId = signal<string | undefined>(undefined);
  protected readonly studyLevel = signal<StudyLevel | undefined>(undefined);
  protected readonly average = signal('');
  protected readonly universityError = signal('');
  protected readonly averageError = signal('');

  protected readonly institutions = toSignal(this.api.institutions().pipe(catchError(() => of([]))), {
    initialValue: [],
  });
  protected readonly fields = toSignal(this.api.fields().pipe(catchError(() => of([]))), { initialValue: [] });

  // shortcut: one request per faculty (no "all degrees at a university" endpoint); add one if it gets slow.
  protected readonly degrees = toSignal(
    toObservable(this.institutionId).pipe(
      switchMap((id) =>
        id
          ? this.api.faculties(id).pipe(
              switchMap((faculties) =>
                forkJoin(
                  faculties.map((faculty) =>
                    this.api.qualifications(faculty.id).pipe(map((degrees): DegreeGroup => ({ faculty, degrees }))),
                  ),
                ).pipe(defaultIfEmpty([] as DegreeGroup[])),
              ),
              map((groups): Degrees => ({ loading: false, groups: groups.filter((g) => g.degrees.length) })),
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
    if (value) this.universityError.set('');
  }

  protected search(): void {
    const institutionId = this.institutionId();
    const raw = this.average().trim();
    const average = parseAverage(raw);
    this.universityError.set(institutionId ? '' : 'Choose your university to see its degrees.');
    this.averageError.set(raw && average === undefined ? 'Enter your average as a number from 0 to 100, like 72.' : '');
    if (!institutionId) {
      this.universityEl().nativeElement.focus();
      return;
    }
    if (this.averageError()) {
      this.averageEl().nativeElement.focus();
      return;
    }
    const group = this.degrees().groups.find((g) => g.degrees.some((d) => d.id === this.degreeId()));
    const queryParams = toQueryParams({
      institutionId,
      facultyId: group?.faculty.id,
      qualificationId: this.degreeId(),
      studyLevel: this.studyLevel(),
      average,
    });
    void this.router.navigate(['/search'], { queryParams });
  }
}
