import { DOCUMENT } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { BursarySummaryResponse } from '../../../core/api/models';
import { ClosingDateBadgeComponent } from '../../../shared/ui/closing-date-badge/closing-date-badge.component';
import { ReportButtonComponent } from '../../../shared/ui/report-button/report-button.component';
import { SaveButtonComponent } from '../../../shared/ui/save-button/save-button.component';
import { SourceIconsComponent } from '../../../shared/ui/source-icons/source-icons.component';

const COVERAGE_SHOWN = 3;
/** Only the first rows swipe in, 60 ms apart (06 §6.5). */
export const SWIPE_ROWS = 3;

/** "Tuition" → "tuition", but "NSFAS" stays as written. */
function lowerFirst(text: string): string {
  return /^[A-Z][a-z]/.test(text) ? text[0].toLowerCase() + text.slice(1) : text;
}

/** One ledger row: date column on the left, details on the right (06 §5). */
@Component({
  selector: 'app-bursary-row',
  imports: [RouterLink, ClosingDateBadgeComponent, ReportButtonComponent, SaveButtonComponent, SourceIconsComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <article
      class="relative flex gap-4 border-b border-line py-4"
      [class.swipe-in]="animate()"
      [class.opacity-60]="closed()"
      [style.--i]="index()"
      [attr.aria-disabled]="closed() ? 'true' : null"
    >
      <div class="date-column w-16 shrink-0">
        <app-closing-date-badge [closingDate]="bursary().closingDate" [status]="bursary().status" />
      </div>
      <div class="min-w-0 flex-1">
        <div class="flex items-start gap-2">
          <h2 class="title min-w-0 flex-1 line-clamp-3 text-lg font-semibold [overflow-wrap:anywhere]">
            @if (closed()) {
              {{ bursary().name }}
            } @else {
              <a class="stretched-link text-jacaranda hover:underline" [routerLink]="['/bursaries', bursary().slug]">{{
                bursary().name
              }}</a>
            }
          </h2>
          @if (!closed()) {
            <app-save-button class="-mt-2" [bursaryId]="bursary().id" [name]="bursary().name" />
          }
          <app-report-button class="-mt-2 -mr-2" [bursaryId]="bursary().id" />
        </div>
        @if (bursary().funderName) {
          <p class="funder truncate text-sm text-muted">{{ bursary().funderName }}</p>
        }
        @if (eligibility()) {
          <p class="eligibility text-sm">{{ eligibility() }}</p>
        }
        @if (coverage()) {
          <p class="coverage text-sm">{{ coverage() }}</p>
        }
        <div class="mt-2 flex items-center gap-2">
          <app-source-icons [sources]="bursary().sources" />
          <span class="sources-count text-sm text-muted">found on {{ siteCount() }}</span>
        </div>
      </div>
    </article>
  `,
  styles: `
    .stretched-link::after {
      content: '';
      position: absolute;
      inset: 0;
    }
    /* The date badge is 64 px wide here, so its "closes in" text stacks under the date. */
    .date-column ::ng-deep app-closing-date-badge > div {
      flex-direction: column;
      align-items: flex-start;
      gap: 4px;
    }
    /* The one animation: the highlighter is drawn left to right. Only transform. */
    .swipe-in ::ng-deep .swipe::before {
      transform: scaleX(0);
      animation: draw 300ms ease-out forwards;
      animation-delay: calc(var(--i) * 60ms);
    }
    @keyframes draw {
      to {
        transform: scaleX(1);
      }
    }
    @media (prefers-reduced-motion: reduce) {
      .swipe-in ::ng-deep .swipe::before {
        transform: none;
        animation: none;
      }
    }
  `,
})
export class BursaryRowComponent {
  private readonly window = inject(DOCUMENT).defaultView;
  readonly bursary = input.required<BursarySummaryResponse>();
  /** Position in the results page; the first {@link SWIPE_ROWS} rows animate. */
  readonly index = input(0);

  protected readonly closed = computed(() => this.bursary().status === 'Closed');
  protected readonly animate = computed(
    () => this.index() < SWIPE_ROWS && !this.window?.matchMedia?.('(prefers-reduced-motion: reduce)').matches,
  );
  protected readonly siteCount = computed(() => {
    const n = this.bursary().sources.length;
    return `${n} ${n === 1 ? 'site' : 'sites'}`;
  });
  protected readonly eligibility = computed(() => {
    const { studyLevels, minAverage } = this.bursary();
    const parts = [studyLevels.join(', '), minAverage === null ? '' : `min ${minAverage}% average`];
    return parts.filter(Boolean).join(', ');
  });
  protected readonly coverage = computed(() => {
    const items = this.bursary().coverage.map(lowerFirst);
    if (items.length === 0) return '';
    const shown = items.slice(0, COVERAGE_SHOWN);
    const last = shown.pop()!;
    const list = shown.length ? `${shown.join(', ')} and ${last}` : last;
    return `Covers ${list}${items.length > COVERAGE_SHOWN ? ` +${items.length - COVERAGE_SHOWN} more` : ''}`;
  });
}
