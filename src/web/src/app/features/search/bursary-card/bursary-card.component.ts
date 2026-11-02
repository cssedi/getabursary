import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { BursarySummaryResponse } from '../../../core/api/models';
import { ClosingDateBadgeComponent } from '../../../shared/ui/closing-date-badge/closing-date-badge.component';
import { SourceIconsComponent } from '../../../shared/ui/source-icons/source-icons.component';

const COVERAGE_SHOWN = 3;

@Component({
  selector: 'app-bursary-card',
  imports: [RouterLink, ClosingDateBadgeComponent, SourceIconsComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <article
      class="relative rounded-control bg-paper p-4 shadow-sm"
      [class.opacity-60]="closed()"
      [class.grayscale]="closed()"
      [attr.aria-disabled]="closed() ? 'true' : null"
    >
      <h3 class="text-lg font-semibold">
        @if (closed()) {
          {{ bursary().name }}
        } @else {
          <a class="stretched-link text-jacaranda" [routerLink]="['/bursaries', bursary().slug]">{{
            bursary().name
          }}</a>
        }
      </h3>
      @if (bursary().funderName) {
        <p class="text-sm text-muted">{{ bursary().funderName }}</p>
      }
      <div class="mt-2">
        <app-closing-date-badge [closingDate]="bursary().closingDate" [status]="bursary().status" />
      </div>
      <dl class="mt-2 grid gap-1 text-sm">
        @if (bursary().studyLevels.length) {
          <div>
            <dt class="inline font-medium">Levels:</dt>
            <dd class="ml-1 inline">{{ bursary().studyLevels.join(', ') }}</dd>
          </div>
        }
        @if (bursary().minAverage !== null) {
          <div>
            <dt class="inline font-medium">Min average:</dt>
            <dd class="ml-1 inline">{{ bursary().minAverage }}%</dd>
          </div>
        }
        @if (coverage()) {
          <div>
            <dt class="inline font-medium">Covers:</dt>
            <dd class="coverage ml-1 inline">{{ coverage() }}</dd>
          </div>
        }
      </dl>
      <div class="mt-3"><app-source-icons [sources]="bursary().sources" /></div>
    </article>
  `,
  styles: `
    .stretched-link::after {
      content: '';
      position: absolute;
      inset: 0;
    }
  `,
})
export class BursaryCardComponent {
  readonly bursary = input.required<BursarySummaryResponse>();
  protected readonly closed = computed(() => this.bursary().status === 'Closed');
  protected readonly coverage = computed(() => {
    const items = this.bursary().coverage;
    const text = items.slice(0, COVERAGE_SHOWN).join(', ');
    return items.length > COVERAGE_SHOWN ? `${text} +${items.length - COVERAGE_SHOWN}` : text;
  });
}
