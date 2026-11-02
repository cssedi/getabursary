import { Dialog } from '@angular/cdk/dialog';
import { ChangeDetectionStrategy, Component, inject, input } from '@angular/core';
import { ReportDialogComponent, ReportDialogData } from '../../../features/report/report-dialog.component';

/**
 * The flag that opens "Report a problem" for one bursary. Sits inside a stretched-link row, so it
 * stops the click from reaching the row. The dialog returns focus here when it closes.
 */
@Component({
  selector: 'app-report-button',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <button
      type="button"
      class="report-flag relative z-10 inline-flex h-11 w-11 shrink-0 items-center justify-center text-muted hover:text-ink active:text-ink"
      aria-label="Report a problem with this listing"
      (click)="open($event)"
    >
      <svg
        viewBox="0 0 16 16"
        width="16"
        height="16"
        fill="none"
        stroke="currentColor"
        stroke-width="1.5"
        aria-hidden="true"
        focusable="false"
      >
        <path d="M3 14V2m0 1h9l-2 3 2 3H3" />
      </svg>
    </button>
  `,
})
export class ReportButtonComponent {
  private readonly dialog = inject(Dialog);
  readonly bursaryId = input.required<string>();

  protected open(event: Event): void {
    event.preventDefault();
    event.stopPropagation();
    this.dialog.open<unknown, ReportDialogData>(ReportDialogComponent, {
      data: { bursaryId: this.bursaryId() },
      ariaLabel: 'Report a problem',
      width: '480px',
      maxWidth: 'calc(100vw - 32px)',
      panelClass: 'report-dialog-panel',
      backdropClass: 'report-dialog-backdrop',
    });
  }
}
