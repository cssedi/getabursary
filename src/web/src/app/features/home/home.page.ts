import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { StudyLevel } from '../../core/api/models';
import { STUDY_LEVELS, parseAverage, toQueryParams } from '../search/search-state.service';
import { DegreePickerComponent, DegreeSelection } from './degree-picker.component';

@Component({
  selector: 'app-home-page',
  imports: [DegreePickerComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="mx-auto grid max-w-xl gap-4 p-4">
      <h1 class="text-2xl font-bold">Find bursaries for your degree</h1>
      <form class="grid gap-4" (submit)="$event.preventDefault(); search()">
        <app-degree-picker (selectionChange)="degree.set($event)" />
        <div class="grid gap-1">
          <label for="home-study-level" class="font-medium">Study level</label>
          <select
            id="home-study-level"
            class="rounded-[var(--radius)] border border-gray-400 p-2"
            (change)="studyLevel.set($any($event.target).value || undefined)"
          >
            <option value="">Any level</option>
            @for (level of levels; track level) {
              <option [value]="level">{{ level }}</option>
            }
          </select>
        </div>
        <div class="grid gap-1">
          <label for="home-average" class="font-medium">Your average (optional)</label>
          <input
            id="home-average"
            type="number"
            min="0"
            max="100"
            inputmode="numeric"
            class="rounded-[var(--radius)] border border-gray-400 p-2"
            (input)="average.set($any($event.target).value)"
          />
        </div>
        <button
          type="submit"
          class="rounded-[var(--radius)] bg-[var(--color-primary)] p-3 font-medium text-[var(--color-primary-ink)]"
        >
          Search
        </button>
      </form>
    </section>
  `,
})
export class HomePage {
  private readonly router = inject(Router);

  protected readonly levels = STUDY_LEVELS;
  protected readonly degree = signal<DegreeSelection>({});
  protected readonly studyLevel = signal<StudyLevel | undefined>(undefined);
  protected readonly average = signal('');

  protected search(): void {
    const queryParams = toQueryParams({
      ...this.degree(),
      studyLevel: this.studyLevel(),
      average: parseAverage(this.average()),
    });
    void this.router.navigate(['/search'], { queryParams });
  }
}
