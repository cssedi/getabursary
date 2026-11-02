import { ChangeDetectionStrategy, Component, inject, input, linkedSignal, output, Signal } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { Observable, catchError, map, of, startWith, switchMap } from 'rxjs';
import { TaxonomyApi } from '../../core/api/taxonomy-api';

export interface DegreeSelection {
  institutionId?: string;
  facultyId?: string;
  qualificationId?: string;
}

interface Loaded<T> {
  loading: boolean;
  items: T[];
}

function loaded<T>(source: Observable<T[]>): Observable<Loaded<T>> {
  return source.pipe(
    map((items): Loaded<T> => ({ loading: false, items })),
    catchError(() => of<Loaded<T>>({ loading: false, items: [] })),
    startWith<Loaded<T>>({ loading: true, items: [] }),
  );
}

/** Three dependent selects: institution → faculty → qualification (FR-01). */
@Component({
  selector: 'app-degree-picker',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="grid gap-3">
      <div class="grid gap-1">
        <label [for]="id + '-institution'" class="font-medium">University</label>
        <select
          [id]="id + '-institution'"
          name="institution"
          class="min-h-11 rounded-control border border-line bg-paper px-2 text-ink" autocomplete="off"
          [disabled]="institutions().loading"
          (change)="onInstitution($any($event.target).value)"
        >
          @if (institutions().loading) {
            <option value="">Loading…</option>
          } @else {
            <option value="">Select a university</option>
            @for (i of institutions().items; track i.id) {
              <option [value]="i.id" [selected]="i.id === selInstitution()">{{ i.name }}</option>
            }
          }
        </select>
      </div>
      <div class="grid gap-1">
        <label [for]="id + '-faculty'" class="font-medium">Faculty</label>
        <select
          [id]="id + '-faculty'"
          name="faculty"
          class="min-h-11 rounded-control border border-line bg-paper px-2 text-ink" autocomplete="off"
          [disabled]="!selInstitution() || faculties().loading"
          (change)="onFaculty($any($event.target).value)"
        >
          @if (faculties().loading) {
            <option value="">Loading…</option>
          } @else {
            <option value="">Select a faculty</option>
            @for (f of faculties().items; track f.id) {
              <option [value]="f.id" [selected]="f.id === selFaculty()">{{ f.name }}</option>
            }
          }
        </select>
      </div>
      <div class="grid gap-1">
        <label [for]="id + '-qualification'" class="font-medium">Qualification</label>
        <select
          [id]="id + '-qualification'"
          name="qualification"
          class="min-h-11 rounded-control border border-line bg-paper px-2 text-ink" autocomplete="off"
          [disabled]="!selFaculty() || qualifications().loading"
          (change)="onQualification($any($event.target).value)"
        >
          @if (qualifications().loading) {
            <option value="">Loading…</option>
          } @else {
            <option value="">Select a qualification</option>
            @for (q of qualifications().items; track q.id) {
              <option [value]="q.id" [selected]="q.id === selQualification()">{{ q.name }}</option>
            }
          }
        </select>
      </div>
    </div>
  `,
})
export class DegreePickerComponent {
  private static nextId = 0;
  private readonly api = inject(TaxonomyApi);

  protected readonly id = `degree-picker-${DegreePickerComponent.nextId++}`;

  /** Pre-fill values; the picker follows the inputs when they change (e.g. a new URL). */
  readonly institutionId = input<string | undefined>();
  readonly facultyId = input<string | undefined>();
  readonly qualificationId = input<string | undefined>();

  readonly selectionChange = output<DegreeSelection>();

  protected readonly selInstitution = linkedSignal(() => this.institutionId());
  protected readonly selFaculty = linkedSignal(() => this.facultyId());
  protected readonly selQualification = linkedSignal(() => this.qualificationId());

  protected readonly institutions = toSignal(loaded(this.api.institutions()), { requireSync: true });
  protected readonly faculties = this.dependent(this.selInstitution, (id) => this.api.faculties(id));
  protected readonly qualifications = this.dependent(this.selFaculty, (id) => this.api.qualifications(id));

  protected onInstitution(value: string): void {
    this.selInstitution.set(value || undefined);
    this.selFaculty.set(undefined);
    this.selQualification.set(undefined);
    this.emit();
  }

  protected onFaculty(value: string): void {
    this.selFaculty.set(value || undefined);
    this.selQualification.set(undefined);
    this.emit();
  }

  protected onQualification(value: string): void {
    this.selQualification.set(value || undefined);
    this.emit();
  }

  private emit(): void {
    this.selectionChange.emit({
      institutionId: this.selInstitution(),
      facultyId: this.selFaculty(),
      qualificationId: this.selQualification(),
    });
  }

  private dependent<T>(key: Signal<string | undefined>, fetch: (id: string) => Observable<T[]>) {
    const idle: Loaded<T> = { loading: false, items: [] };
    return toSignal(
      toObservable(key).pipe(switchMap((id) => (id ? loaded(fetch(id)) : of(idle)))),
      { initialValue: idle },
    );
  }
}
