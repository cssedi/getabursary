import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, input, linkedSignal, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { catchError, of } from 'rxjs';
import { StudyLevel } from '../../../core/api/models';
import { TaxonomyApi } from '../../../core/api/taxonomy-api';
import { DegreePickerComponent, DegreeSelection } from '../../home/degree-picker.component';
import { STUDY_LEVELS, SearchStateService, parseAverage } from '../search-state.service';

const AVERAGE_DEBOUNCE_MS = 300;

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

        <div class="grid gap-2">
          <label [for]="idPrefix() + '-field-query'" class="font-medium">
            Field of study @if (criteria().fieldIds.length) { ({{ criteria().fieldIds.length }} selected) }
          </label>
          <input
            [id]="idPrefix() + '-field-query'"
            name="fieldQuery"
            type="search"
            autocomplete="off"
            spellcheck="false"
            placeholder="Type a field, e.g. law…"
            class="min-h-11 rounded-control border border-line bg-paper-raised px-2 text-ink"
            [value]="fieldQuery()"
            (input)="fieldQuery.set($any($event.target).value)"
          />
          <div class="grid border-t border-line">
            @for (group of visibleGroups(); track group.id) {
              <details class="field-group border-b border-line" [open]="group.open">
                <summary class="flex min-h-11 cursor-pointer items-center font-medium hover:text-jacaranda">
                  <span class="min-w-0 [overflow-wrap:anywhere]">{{ group.name }}</span>
                  @if (group.selected) {
                    <span class="ml-2 tabular-nums text-muted">({{ group.selected }})</span>
                  }
                </summary>
                <div class="grid gap-1 pb-2">
                  <label class="flex min-h-11 items-center gap-2">
                    <input
                      type="checkbox"
                      [checked]="criteria().fieldIds.includes(group.id)"
                      (change)="toggleField(group.id, $any($event.target).checked)"
                    />
                    <span class="min-w-0 [overflow-wrap:anywhere]">All {{ group.name }}</span>
                  </label>
                  @for (child of group.children; track child.id) {
                    <label class="ml-4 flex min-h-11 items-center gap-2">
                      <input
                        type="checkbox"
                        [checked]="criteria().fieldIds.includes(child.id)"
                        (change)="toggleField(child.id, $any($event.target).checked)"
                      />
                      <span class="min-w-0 [overflow-wrap:anywhere]">{{ child.name }}</span>
                    </label>
                  }
                  @if (group.selected) {
                    <button
                      type="button"
                      class="clear-group min-h-11 justify-self-start px-2 text-jacaranda underline underline-offset-[3px] hover:decoration-2 active:decoration-2"
                      (click)="clearGroup(group.id)"
                    >
                      Clear {{ group.name }}
                    </button>
                  }
                </div>
              </details>
            }
            @if (visibleGroups().length === 0 && fieldQuery()) {
              <p class="py-2 text-sm text-muted">No fields match “{{ fieldQuery() }}”.</p>
            }
          </div>
        </div>

        <div class="grid gap-1">
          <label [for]="idPrefix() + '-study-level'" class="font-medium">Study level</label>
          <select
            [id]="idPrefix() + '-study-level'"
            name="studyLevel"
            class="min-h-11 rounded-control border border-line bg-paper px-2 text-ink" autocomplete="off"
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
            name="average"
            type="number"
            min="0"
            max="100"
            inputmode="numeric"
            placeholder="e.g. 72…"
            class="min-h-11 rounded-control border border-line bg-paper px-2 text-ink" autocomplete="off"
            [value]="averageText()"
            (input)="onAverage($any($event.target).value)"
          />
        </div>

        <div class="grid gap-1">
          <label [for]="idPrefix() + '-closing-from'" class="font-medium">Closing from</label>
          <input
            [id]="idPrefix() + '-closing-from'"
            name="closingFrom"
            type="date"
            class="min-h-11 rounded-control border border-line bg-paper px-2 text-ink" autocomplete="off"
            [value]="criteria().closingFrom ?? ''"
            (change)="state.update({ closingFrom: $any($event.target).value || undefined })"
          />
        </div>
        <div class="grid gap-1">
          <label [for]="idPrefix() + '-closing-to'" class="font-medium">Closing to</label>
          <input
            [id]="idPrefix() + '-closing-to'"
            name="closingTo"
            type="date"
            class="min-h-11 rounded-control border border-line bg-paper px-2 text-ink" autocomplete="off"
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
          class="min-h-11 rounded-control border border-jacaranda p-2 font-medium text-jacaranda hover:bg-bloom active:bg-bloom"
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

  protected readonly fieldQuery = signal('');

  /**
   * Groups (and children) matching the filter box. A group opens when it holds a selection or the box is in use.
   * shortcut: unticking a group's last field closes it, upgrade to remembered open state if that annoys.
   */
  protected readonly visibleGroups = computed(() => {
    const query = this.fieldQuery().trim().toLowerCase();
    const selected = this.criteria().fieldIds;
    const has = (name: string) => name.toLowerCase().includes(query);
    return this.fields()
      .map((g) => {
        const children = !query || has(g.name) ? g.children : g.children.filter((c) => has(c.name));
        const count = [g.id, ...g.children.map((c) => c.id)].filter((id) => selected.includes(id)).length;
        return { id: g.id, name: g.name, children, selected: count, open: count > 0 || !!query, show: !query || has(g.name) || children.length > 0 };
      })
      .filter((g) => g.show);
  });

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

  protected clearGroup(groupId: string): void {
    const group = this.fields().find((g) => g.id === groupId);
    if (!group) return;
    const ids = new Set([group.id, ...group.children.map((c) => c.id)]);
    void this.state.update({ fieldIds: this.criteria().fieldIds.filter((id) => !ids.has(id)) });
  }

  protected toggleField(id: string, checked: boolean): void {
    const current = this.criteria().fieldIds;
    const fieldIds = checked ? [...current.filter((f) => f !== id), id] : current.filter((f) => f !== id);
    void this.state.update({ fieldIds });
  }
}
