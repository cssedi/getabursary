import { ChangeDetectionStrategy, Component, DestroyRef, inject, input, linkedSignal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { catchError, of } from 'rxjs';
import { StudyLevel } from '../../../core/api/models';
import { TaxonomyApi } from '../../../core/api/taxonomy-api';
import { DegreePickerComponent, DegreeSelection } from '../../home/degree-picker.component';
import { STUDY_LEVELS, SearchStateService, parseAverage } from '../search-state.service';

const AVERAGE_DEBOUNCE_MS = 400;

@Component({
  selector: 'app-filter-panel',
  imports: [DegreePickerComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section aria-label="Filters" class="grid gap-3">
      <div class="grid gap-4">
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
          <label [for]="idPrefix() + '-study-level'" class="font-medium">Study level</label>
          <select
            [id]="idPrefix() + '-study-level'"
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
          <label [for]="idPrefix() + '-average'" class="font-medium">Your average (%)</label>
          <input
            [id]="idPrefix() + '-average'"
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
          <label [for]="idPrefix() + '-closing-from'" class="font-medium">Closing from</label>
          <input
            [id]="idPrefix() + '-closing-from'"
            type="date"
            class="rounded-[var(--radius)] border border-gray-400 p-2"
            [value]="criteria().closingFrom ?? ''"
            (change)="state.update({ closingFrom: $any($event.target).value || undefined })"
          />
        </div>
        <div class="grid gap-1">
          <label [for]="idPrefix() + '-closing-to'" class="font-medium">Closing to</label>
          <input
            [id]="idPrefix() + '-closing-to'"
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

        <button
          type="button"
          class="rounded-control border border-jacaranda p-2 font-medium text-jacaranda"
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
  /** Prefix for control ids, so the page panel and the mobile sheet copy never share ids. */
  readonly idPrefix = input('filter');
  protected readonly fields = toSignal(this.taxonomy.fields().pipe(catchError(() => of([]))), { initialValue: [] });

  /** Text in the average box; follows the URL (e.g. when filters are cleared). */
  protected readonly averageText = linkedSignal(() => this.criteria().average?.toString() ?? '');

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
