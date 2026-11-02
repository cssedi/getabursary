import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { BursarySummaryResponse } from '../../core/api/models';
import { formatDate } from '../../shared/format/format';
import { SourceIconsComponent } from '../../shared/ui/source-icons/source-icons.component';

/**
 * Summary-only view for a closed bursary (API `410 Gone`). It shows no eligibility details.
 * `fieldIds` drives the "similar fields" link; the 410 summary does not carry fields, so the link
 * appears only when a caller supplies them.
 */
@Component({
  selector: 'app-closed-bursary',
  imports: [RouterLink, SourceIconsComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="space-y-4 rounded-control bg-paper p-4 shadow-sm">
      <h1 class="text-2xl font-bold">{{ summary().name }}</h1>
      @if (summary().funderName) {
        <p class="text-muted">{{ summary().funderName }}</p>
      }
      <p class="font-semibold" role="status">{{ closedMessage() }}</p>
      <app-source-icons [sources]="summary().sources" />
      @if (fieldIds().length) {
        <p>
          <a
            class="text-jacaranda underline"
            [routerLink]="['/search']"
            [queryParams]="{ fieldIds: fieldIds().join(',') }"
            >Find open bursaries for similar fields</a
          >
        </p>
      }
    </section>
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
