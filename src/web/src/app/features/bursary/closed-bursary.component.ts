import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { BursarySummaryResponse } from '../../core/api/models';
import { formatDate } from '../../shared/format/format';
import { ClosingDateBadgeComponent } from '../../shared/ui/closing-date-badge/closing-date-badge.component';
import { SourceLinksComponent } from '../../shared/ui/source-links/source-links.component';

/**
 * Summary-only view for a closed bursary (API `410 Gone`). It shows no eligibility details and no apply button.
 * `fieldIds` drives the "similar fields" link; the 410 summary does not carry fields, so the link
 * appears only when a caller supplies them.
 */
@Component({
  selector: 'app-closed-bursary',
  imports: [ClosingDateBadgeComponent, RouterLink, SourceLinksComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="space-y-3">
      <app-closing-date-badge [closingDate]="summary().closingDate" [status]="'Closed'" />
      <h1 class="text-2xl font-bold">{{ summary().name }}</h1>
      @if (summary().funderName) {
        <p class="text-muted">{{ summary().funderName }}</p>
      }
      <p class="text-lg font-semibold" role="status">{{ closedMessage() }}</p>
      <app-source-links [sources]="summary().sources" />
      @if (fieldIds().length) {
        <p>
          <a
            class="text-jacaranda underline underline-offset-4 hover:decoration-2"
            [routerLink]="['/search']"
            [queryParams]="{ fieldIds: fieldIds().join(',') }"
            >Find open bursaries for similar fields</a
          >
        </p>
      }
    </div>
  `,
})
export class ClosedBursaryComponent {
  readonly summary = input.required<BursarySummaryResponse>();
  readonly fieldIds = input<string[]>([]);

  protected closedMessage(): string {
    const date = this.summary().closingDate;
    return date ? `This bursary closed on ${formatDate(date)}.` : 'This bursary has closed.';
  }
}
