import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, linkedSignal, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { catchError, of } from 'rxjs';
import { SearchSort, StudyLevel } from '../../../core/api/models';
import { TaxonomyApi } from '../../../core/api/taxonomy-api';
import { DegreePickerComponent, DegreeSelection } from '../../home/degree-picker.component';
import { SEARCH_SORTS, STUDY_LEVELS, SearchStateService, parseAverage } from '../search-state.service';

const AVERAGE_DEBOUNCE_MS = 400;

@Component({
  selector: 'app-filter-panel',
  imports: [DegreePickerComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section aria-label="Filters" class="grid gap-3">
      <button
        type="button"
        class="rounded-[var(--radius)] border border-gray-400 p-2 text-left font-medium md:hidden"
        aria-controls="filter-panel-body"
        [attr.aria-expanded]="open()"
        (click)="open.set(!open())"
      >
        Filters — {{ activeCount() }} {{ activeCount() === 1 ? 'filter' : 'filters' }} active
      </button>

      <div id="filter-panel-body" class="gap-4 md:grid" [class.grid]="open()" [class.hidden]="!open()">
        <app-degree-picker
          [institutionId]="criteria().institutionId"
          [facultyId]="criteria().facultyId"
          [qualificationId]="criteria().qualificationId"
          (selectionChange)="onDegree($event)"
        />

        <details class="rounded-[var(--radius)] border border-gray-400 p-2">
          <summary class="cursor-pointer font-medium">
            Field of study @if (criteria().fieldIds.length) { ({{ criteria().fieldIds.length }} selected) }
          </summary>
          <div class="mt-2 grid gap-3">
            @for (group of fields(); track group.id) {
              <fieldset class="grid gap-1">
                <legend class="font-medium">{{ group.name }}</legend>
                <label class="flex items-center gap-2">
                  <input
                    type="checkbox"
                    [checked]="criteria().fieldIds.includes(group.id)"
                    (change)="toggleField(group.id, $any($event.target).checked)"
                  />
                  All {{ group.name }}
                </label>
                @for (child of group.children; track child.id) {
                  <label class="ml-4 flex items-center gap-2">
                    <input
                      type="checkbox"
                      [checked]="criteria().fieldIds.includes(child.id)"
                      (change)="toggleField(child.id, $any($event.target).checked)"
                    />
                    {{ child.name }}
                  </label>
                }
              </fieldset>
            }
          </div>
        </details>

        <div class="grid gap-1">
          <label for="filter-study-level" class="font-medium">Study level</label>
          <select
            id="filter-study-level"
            class="rounded-[var(--radius)] border border-gray-400 p-2"
            (change)="state.update({ studyLevel: $any($event.target).value || undefined })"
          >
            <option value="" [selected]="!criteria().studyLevel">Any level</option>
            @for (level of levels; track level) {
              <option [value]="level" [selected]="level === criteria().studyLevel">{{ level }}</option>
            }
          </select>
        </div>

        <div class="grid gap-1">
          <label for="filter-average" class="font-medium">Your average (%)</label>
          <input
            id="filter-average"
            type="number"
            min="0"
            max="100"
            inputmode="numeric"
            class="rounded-[var(--radius)] border border-gray-400 p-2"
            [value]="averageText()"
            (input)="onAverage($any($event.target).value)"
          />
        </div>

        <div class="grid gap-1">
          <label for="filter-closing-from" class="font-medium">Closing from</label>
          <input
            id="filter-closing-from"
            type="date"
            class="rounded-[var(--radius)] border border-gray-400 p-2"
            [value]="criteria().closingFrom ?? ''"
            (change)="state.update({ closingFrom: $any($event.target).value || undefined })"
          />
        </div>
        <div class="grid gap-1">
          <label for="filter-closing-to" class="font-medium">Closing to</label>
          <input
            id="filter-closing-to"
            type="date"
            class="rounded-[var(--radius)] border border-gray-400 p-2"
            [value]="criteria().closingTo ?? ''"
            (change)="state.update({ closingTo: $any($event.target).value || undefined })"
          />
        </div>

        <label class="flex items-center gap-2">
          <input
            type="checkbox"
            [checked]="criteria().includeClosed"
            (change)="state.update({ includeClosed: $any($event.target).checked })"
          />
          Show closed bursaries
        </label>

        <div class="grid gap-1">
          <label for="filter-sort" class="font-medium">Sort by</label>
          <select
            id="filter-sort"
            class="rounded-[var(--radius)] border border-gray-400 p-2"
            (change)="state.update({ sort: $any($event.target).value || undefined })"
          >
            @for (s of sorts; track s.value) {
              <option [value]="s.value" [selected]="s.value === (criteria().sort ?? 'BestMatch')">{{ s.label }}</option>
            }
          </select>
        </div>

        <button
          type="button"
          class="rounded-[var(--radius)] border border-[var(--color-primary)] p-2 font-medium text-[var(--color-primary)]"
          (click)="state.clear()"
        >
          Clear filters
        </button>
      </div>
    </section>
  `,
})
export class FilterPanelComponent {
  protected readonly state = inject(SearchStateService);
  private readonly taxonomy = inject(TaxonomyApi);

  protected readonly criteria = this.state.criteria;
  protected readonly levels: readonly StudyLevel[] = STUDY_LEVELS;
  protected readonly sorts: readonly { value: SearchSort; label: string }[] = SEARCH_SORTS;
  protected readonly open = signal(false);
  protected readonly fields = toSignal(this.taxonomy.fields().pipe(catchError(() => of([]))), { initialValue: [] });

  /** Text in the average box; follows the URL (e.g. when filters are cleared). */
  protected readonly averageText = linkedSignal(() => this.criteria().average?.toString() ?? '');

  protected readonly activeCount = computed(() => {
    const c = this.criteria();
    return [
      c.institutionId || c.qualificationId,
      c.fieldIds.length > 0,
      c.studyLevel,
      c.average !== undefined,
      c.closingFrom,
      c.closingTo,
      c.includeClosed,
    ].filter(Boolean).length;
  });

  private averageTimer: ReturnType<typeof setTimeout> | undefined;

  constructor() {
    inject(DestroyRef).onDestroy(() => clearTimeout(this.averageTimer));
  }

  protected onDegree(selection: DegreeSelection): void {
    void this.state.update(selection);
  }

  protected onAverage(text: string): void {
    clearTimeout(this.averageTimer);
    this.averageTimer = setTimeout(
      () => void this.state.update({ average: parseAverage(text) }),
      AVERAGE_DEBOUNCE_MS,
    );
  }

  protected toggleField(id: string, checked: boolean): void {
    const current = this.criteria().fieldIds;
    const fieldIds = checked ? [...current.filter((f) => f !== id), id] : current.filter((f) => f !== id);
    void this.state.update({ fieldIds });
  }
}
