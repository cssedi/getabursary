import {
  ChangeDetectionStrategy,
  Component,
  HostListener,
  computed,
  inject,
  signal,
} from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, ValidatorFn, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { firstValueFrom, map } from 'rxjs';
import { AdminApi, AdminBursary, AdminBursaryPatch } from '../../core/api/admin-api';
import { ApiError } from '../../core/api/api-error';
import { CoverageItem, FieldOfStudyResponse, StudyLevel } from '../../core/api/models';
import { TaxonomyApi } from '../../core/api/taxonomy-api';
import { ErrorStateComponent } from '../../shared/ui/error-state/error-state.component';
import { ToastComponent } from '../../shared/ui/toast/toast.component';
import { ToastService } from '../../shared/ui/toast/toast.service';
import { HasUnsavedChanges } from './admin.guards';

const LEVELS: StudyLevel[] = ['Matric', 'Undergraduate', 'Honours', 'Masters', 'Doctoral'];
const COVERAGE: CoverageItem[] = [
  'Tuition',
  'Accommodation',
  'Books',
  'Meals',
  'Laptop',
  'Stipend',
  'Transport',
  'Other',
];

/** Form field to the `locked_fields` name the API uses for it (07 §2). */
export const LOCK_NAMES = {
  name: 'Name',
  funderName: 'FunderId',
  summary: 'Summary',
  applyUrl: 'ApplyUrl',
  openDate: 'OpenDate',
  closingDate: 'ClosingDate',
  minAverage: 'MinAverage',
  studyLevels: 'StudyLevels',
  coverage: 'Coverage',
  fieldCodes: 'Eligibility',
} as const;

type Key = keyof typeof LOCK_NAMES;

const MESSAGES: Record<Key, string> = {
  name: 'Enter a name of 300 characters or fewer.',
  funderName: 'Keep the funder to 300 characters or fewer.',
  summary: 'Keep the summary to 600 characters or fewer.',
  applyUrl: 'Enter a link starting with https:// or http://.',
  openDate: 'Enter a valid date.',
  closingDate: 'Enter a valid date.',
  minAverage: 'Enter an average between 0 and 100.',
  studyLevels: '',
  coverage: '',
  fieldCodes: '',
};

const httpUrl: ValidatorFn = (c) =>
  !c.value || /^https?:\/\/\S+$/i.test(c.value) ? null : { url: true };

const BUTTON =
  'min-h-11 rounded-control border border-line px-4 font-medium hover:bg-bloom active:bg-bloom disabled:opacity-60';
const INPUT = 'min-h-11 w-full rounded-control border border-line px-3';

/** `/admin/bursaries/:id`: edit any bursary. Only changed fields are sent, and the API locks them against re-ingestion. */
@Component({
  selector: 'app-bursary-editor-page',
  imports: [NgTemplateOutlet, ReactiveFormsModule, RouterLink, ErrorStateComponent, ToastComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <a routerLink="/admin/bursaries" class="text-jacaranda hover:underline">All bursaries</a>
    @if (bursary(); as b) {
      <div class="mt-2 flex flex-wrap items-center justify-between gap-3">
        <h2 class="min-w-0 text-xl font-bold [overflow-wrap:anywhere]">{{ b.name }}</h2>
        <button
          type="button"
          [class]="button"
          [disabled]="busy()"
          (click)="setPublished(!b.isPublished)"
        >
          {{ b.isPublished ? 'Unpublish' : 'Publish' }}
        </button>
      </div>
      <p class="mt-1 text-muted">{{ b.isPublished ? 'Published' : 'Unpublished' }}</p>

      <ng-template #head let-key="key" let-label="label" let-id="id">
        <div class="mb-1 flex flex-wrap items-center gap-2">
          <label [for]="id" class="font-medium">{{ label }}</label>
          @if (lockOf(key); as lock) {
            <span class="locked-badge rounded-control border border-line px-2 text-sm text-muted"
              >Locked</span
            >
            <button
              type="button"
              class="unlock min-h-11 px-2 text-jacaranda hover:underline"
              (click)="unlock(lock)"
            >
              Unlock<span class="sr-only"> {{ label }}</span>
            </button>
          }
        </div>
      </ng-template>

      <form [formGroup]="form" (ngSubmit)="save()" class="mt-4 grid max-w-[720px] gap-5" novalidate>
        <div>
          <ng-container
            *ngTemplateOutlet="head; context: { key: 'name', label: 'Name', id: 'f-name' }"
          />
          <input
            id="f-name"
            name="name"
            [class]="input"
            autocomplete="off"
            formControlName="name"
            [attr.aria-invalid]="invalid('name')"
          />
          @if (error('name'); as e) {
            <p class="mt-1 text-sm font-medium">{{ e }}</p>
          }
        </div>

        <div>
          <ng-container
            *ngTemplateOutlet="
              head;
              context: { key: 'funderName', label: 'Funder', id: 'f-funder' }
            "
          />
          <input
            id="f-funder"
            name="funderName"
            [class]="input"
            autocomplete="off"
            formControlName="funderName"
            [attr.aria-invalid]="invalid('funderName')"
          />
          @if (error('funderName'); as e) {
            <p class="mt-1 text-sm font-medium">{{ e }}</p>
          }
        </div>

        <div>
          <ng-container
            *ngTemplateOutlet="head; context: { key: 'summary', label: 'Summary', id: 'f-summary' }"
          />
          <textarea
            id="f-summary"
            name="summary"
            rows="4"
            class="w-full rounded-control border border-line p-3"
            autocomplete="off"
            formControlName="summary"
            [attr.aria-invalid]="invalid('summary')"
          ></textarea>
          @if (error('summary'); as e) {
            <p class="mt-1 text-sm font-medium">{{ e }}</p>
          }
        </div>

        <div>
          <ng-container
            *ngTemplateOutlet="head; context: { key: 'applyUrl', label: 'Apply link', id: 'f-url' }"
          />
          <input
            id="f-url"
            name="applyUrl"
            type="url"
            [class]="input"
            autocomplete="off"
            spellcheck="false"
            placeholder="https://example.co.za/apply…"
            formControlName="applyUrl"
            [attr.aria-invalid]="invalid('applyUrl')"
          />
          @if (error('applyUrl'); as e) {
            <p class="mt-1 text-sm font-medium">{{ e }}</p>
          }
        </div>

        <div class="grid gap-5 sm:grid-cols-2">
          <div>
            <ng-container
              *ngTemplateOutlet="
                head;
                context: { key: 'openDate', label: 'Open date', id: 'f-open' }
              "
            />
            <input
              id="f-open"
              name="openDate"
              type="date"
              [class]="input"
              autocomplete="off"
              formControlName="openDate"
            />
          </div>
          <div>
            <ng-container
              *ngTemplateOutlet="
                head;
                context: { key: 'closingDate', label: 'Closing date', id: 'f-close' }
              "
            />
            <input
              id="f-close"
              name="closingDate"
              type="date"
              [class]="input"
              autocomplete="off"
              formControlName="closingDate"
            />
          </div>
        </div>

        <div>
          <ng-container
            *ngTemplateOutlet="
              head;
              context: { key: 'minAverage', label: 'Minimum average (%)', id: 'f-avg' }
            "
          />
          <input
            id="f-avg"
            name="minAverage"
            type="number"
            inputmode="numeric"
            min="0"
            max="100"
            step="any"
            [class]="input"
            autocomplete="off"
            formControlName="minAverage"
            [attr.aria-invalid]="invalid('minAverage')"
          />
          @if (error('minAverage'); as e) {
            <p class="mt-1 text-sm font-medium">{{ e }}</p>
          }
        </div>

        <fieldset>
          <legend class="mb-1 flex flex-wrap items-center gap-2 font-medium">
            Study levels
            @if (lockOf('studyLevels'); as lock) {
              <span
                class="locked-badge rounded-control border border-line px-2 text-sm font-normal text-muted"
                >Locked</span
              >
              <button
                type="button"
                class="unlock min-h-11 px-2 font-normal text-jacaranda hover:underline"
                (click)="unlock(lock)"
              >
                Unlock<span class="sr-only"> study levels</span>
              </button>
            }
          </legend>
          <div class="flex flex-wrap gap-x-5">
            @for (l of levels; track l) {
              <label class="flex min-h-11 items-center gap-2">
                <input
                  type="checkbox"
                  class="size-5 accent-jacaranda"
                  [name]="'level-' + l"
                  [checked]="has('studyLevels', l)"
                  (change)="toggle('studyLevels', l, $any($event.target).checked)"
                />
                {{ l }}
              </label>
            }
          </div>
        </fieldset>

        <fieldset>
          <legend class="mb-1 flex flex-wrap items-center gap-2 font-medium">
            What’s covered
            @if (lockOf('coverage'); as lock) {
              <span
                class="locked-badge rounded-control border border-line px-2 text-sm font-normal text-muted"
                >Locked</span
              >
              <button
                type="button"
                class="unlock min-h-11 px-2 font-normal text-jacaranda hover:underline"
                (click)="unlock(lock)"
              >
                Unlock<span class="sr-only"> what’s covered</span>
              </button>
            }
          </legend>
          <div class="flex flex-wrap gap-x-5">
            @for (c of coverageItems; track c) {
              <label class="flex min-h-11 items-center gap-2">
                <input
                  type="checkbox"
                  class="size-5 accent-jacaranda"
                  [name]="'coverage-' + c"
                  [checked]="has('coverage', c)"
                  (change)="toggle('coverage', c, $any($event.target).checked)"
                />
                {{ c }}
              </label>
            }
          </div>
        </fieldset>

        <div>
          <ng-container
            *ngTemplateOutlet="
              head;
              context: { key: 'fieldCodes', label: 'Fields of study', id: 'f-fields' }
            "
          />
          <select
            id="f-fields"
            name="fieldCodes"
            multiple
            size="8"
            [class]="input"
            autocomplete="off"
            (change)="pickFields($any($event.target).selectedOptions)"
          >
            @for (f of fieldOptions(); track f.code) {
              <option [value]="f.code" [selected]="has('fieldCodes', f.code)">{{ f.name }}</option>
            }
          </select>
          <p class="mt-1 text-sm text-muted">Hold Ctrl or Cmd to pick more than one.</p>
        </div>

        @if (problems().length) {
          <div role="alert" class="font-medium">
            @for (p of problems(); track p) {
              <p>{{ p }}</p>
            }
          </div>
        }

        <div>
          <button
            type="submit"
            class="btn-primary"
            [disabled]="busy()"
          >
            {{ busy() ? 'Saving…' : 'Save changes' }}
          </button>
        </div>
      </form>
    } @else if (failed()) {
      <app-error-state
        message="This bursary didn’t load. Check your connection and try again."
        (retry)="load()"
      />
    } @else {
      <p class="mt-3 text-muted" role="status">Loading the bursary…</p>
    }
    <app-toast />
  `,
})
export class BursaryEditorPage implements HasUnsavedChanges {
  private readonly api = inject(AdminApi);
  private readonly taxonomy = inject(TaxonomyApi);
  private readonly toast = inject(ToastService);
  private readonly id = inject(ActivatedRoute).snapshot.paramMap.get('id') ?? '';

  protected readonly button = BUTTON;
  protected readonly input = INPUT;
  protected readonly levels = LEVELS;
  protected readonly coverageItems = COVERAGE;
  protected readonly bursary = signal<AdminBursary | null>(null);
  protected readonly failed = signal(false);
  protected readonly busy = signal(false);
  protected readonly submitted = signal(false);
  protected readonly problems = signal<string[]>([]);
  protected readonly fieldOptions = signal<{ code: string; name: string }[]>([]);

  protected readonly form = new FormGroup({
    name: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(300)],
    }),
    funderName: new FormControl('', { nonNullable: true, validators: Validators.maxLength(300) }),
    summary: new FormControl('', { nonNullable: true, validators: Validators.maxLength(600) }),
    applyUrl: new FormControl('', {
      nonNullable: true,
      validators: [httpUrl, Validators.maxLength(1000)],
    }),
    openDate: new FormControl('', { nonNullable: true }),
    closingDate: new FormControl('', { nonNullable: true }),
    minAverage: new FormControl<number | null>(null, [Validators.min(0), Validators.max(100)]),
    studyLevels: new FormControl<string[]>([], { nonNullable: true }),
    coverage: new FormControl<string[]>([], { nonNullable: true }),
    fieldCodes: new FormControl<string[]>([], { nonNullable: true }),
  });

  private readonly value = toSignal(
    this.form.valueChanges.pipe(map(() => this.form.getRawValue())),
    {
      initialValue: this.form.getRawValue(),
    },
  );

  /** Only the fields that differ from what the API last returned, in the shape of the PATCH body. */
  protected readonly changes = computed<AdminBursaryPatch>(() => {
    const b = this.bursary();
    if (!b) return {};
    const now = this.value();
    const base = toFormValue(b);
    const out: Record<string, unknown> = {};
    for (const key of Object.keys(LOCK_NAMES) as Key[]) {
      if (JSON.stringify(sortedIfList(now[key])) !== JSON.stringify(sortedIfList(base[key]))) {
        out[key] = patchValue(key, now[key]);
      }
    }
    return out as AdminBursaryPatch;
  });

  constructor() {
    this.load();
    firstValueFrom(this.taxonomy.fields()).then(
      (fields) => this.fieldOptions.set(flattenFields(fields)),
      () => this.toast.show('Fields of study didn’t load. Reload the page to edit them.'),
    );
  }

  hasUnsavedChanges(): boolean {
    return Object.keys(this.changes()).length > 0;
  }

  @HostListener('window:beforeunload', ['$event'])
  protected onBeforeUnload(event: BeforeUnloadEvent): void {
    if (this.hasUnsavedChanges()) event.preventDefault();
  }

  protected load(): void {
    this.failed.set(false);
    firstValueFrom(this.api.bursary(this.id)).then(
      (b) => this.show(b),
      () => this.failed.set(true),
    );
  }

  protected lockOf(key: string): string | null {
    const lock = LOCK_NAMES[key as Key];
    return this.bursary()?.lockedFields.some((f) => f.toLowerCase() === lock.toLowerCase())
      ? lock
      : null;
  }

  protected invalid(key: Key): 'true' | null {
    return this.error(key) ? 'true' : null;
  }

  protected error(key: Key): string | null {
    return this.submitted() && this.form.controls[key].invalid ? MESSAGES[key] : null;
  }

  protected has(key: 'studyLevels' | 'coverage' | 'fieldCodes', item: string): boolean {
    return this.form.controls[key].value.includes(item);
  }

  protected toggle(key: 'studyLevels' | 'coverage', item: string, on: boolean): void {
    const current = this.form.controls[key].value.filter((i) => i !== item);
    this.form.controls[key].setValue(on ? [...current, item] : current);
  }

  protected pickFields(selected: HTMLCollectionOf<HTMLOptionElement>): void {
    this.form.controls.fieldCodes.setValue(Array.from(selected, (o) => o.value));
  }

  protected async save(): Promise<void> {
    this.submitted.set(true);
    this.problems.set([]);
    if (this.form.invalid) {
      document.querySelector<HTMLElement>('[aria-invalid="true"]')?.focus();
      return;
    }
    const changes = this.changes();
    if (Object.keys(changes).length === 0) {
      this.toast.show('No changes to save');
      return;
    }
    if (await this.send(changes)) this.toast.show('Changes saved');
  }

  protected async setPublished(isPublished: boolean): Promise<void> {
    if (await this.send({ isPublished }, false))
      this.toast.show(isPublished ? 'Published' : 'Unpublished');
  }

  protected async unlock(lock: string): Promise<void> {
    this.busy.set(true);
    try {
      this.applyResponse(await firstValueFrom(this.api.unlock(this.id, lock)), false);
      this.toast.show('Unlocked. Ingestion can update this field again');
    } catch {
      this.toast.show('Couldn’t unlock that field. Check your connection and try again.');
    } finally {
      this.busy.set(false);
    }
  }

  private async send(changes: AdminBursaryPatch, resetForm = true): Promise<boolean> {
    this.busy.set(true);
    this.problems.set([]);
    try {
      this.applyResponse(await firstValueFrom(this.api.update(this.id, changes)), resetForm);
      return true;
    } catch (e) {
      const messages =
        e instanceof ApiError && e.errors.length ? e.errors.map((x) => x.message) : [];
      this.problems.set(
        messages.length
          ? messages
          : ['Your changes didn’t save. Check your connection and try again.'],
      );
      return false;
    } finally {
      this.busy.set(false);
    }
  }

  /** Publishing and unlocking leave the user's unsaved edits alone; saving replaces the form with what the API stored. */
  private applyResponse(b: AdminBursary, resetForm: boolean): void {
    if (resetForm) {
      this.show(b);
    } else {
      this.bursary.set(b);
    }
  }

  private show(b: AdminBursary): void {
    this.bursary.set(b);
    this.form.reset(toFormValue(b));
    this.submitted.set(false);
  }
}

function toFormValue(b: AdminBursary) {
  return {
    name: b.name,
    funderName: b.funderName ?? '',
    summary: b.summary ?? '',
    applyUrl: b.applyUrl ?? '',
    openDate: b.openDate ?? '',
    closingDate: b.closingDate ?? '',
    minAverage: b.minAverage,
    studyLevels: b.studyLevels as string[],
    coverage: b.coverage as string[],
    fieldCodes: b.fieldCodes,
  };
}

function sortedIfList(value: unknown): unknown {
  return Array.isArray(value) ? [...value].sort() : value;
}

/** Empty text and dates go to the API as null so the field is cleared. `funderName` null clears the funder. */
function patchValue(key: Key, value: unknown): unknown {
  if (key === 'name') return (value as string).trim();
  return value === '' ? null : value;
}

function flattenFields(fields: FieldOfStudyResponse[]): { code: string; name: string }[] {
  return fields.flatMap((f) => [
    { code: f.code, name: f.name },
    ...flattenFields(f.children).map((c) => ({ ...c, name: `${f.name}: ${c.name}` })),
  ]);
}
