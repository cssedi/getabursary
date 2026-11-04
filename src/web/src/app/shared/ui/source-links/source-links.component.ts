import { ChangeDetectionStrategy, Component, input, signal } from '@angular/core';
import { SourceLink } from '../../../core/api/models';

/** "Found on A, B, C": every source by name, each linking to that site's page for the bursary (06 §5). */
@Component({
  selector: 'app-source-links',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (sources().length) {
    <p class="found-on text-sm text-muted [overflow-wrap:anywhere]">
      Found on
      @for (s of sources(); track s.key; let last = $last) {
        &ngsp;<a
          [href]="s.url"
          target="_blank"
          rel="noopener nofollow"
          class="relative z-10 text-jacaranda underline underline-offset-[3px] hover:decoration-2 active:decoration-2"
          >@if (s.iconUrl && !failed().has(s.key)) {
            <img
              class="mr-1 inline-block align-[-3px]"
              [src]="s.iconUrl"
              alt=""
              width="16"
              height="16"
              loading="lazy"
              (error)="markFailed(s.key)"
            />
          }{{ s.name }}</a
        >@if (!last) {,}
      }
    </p>
    }
  `,
})
export class SourceLinksComponent {
  readonly sources = input.required<SourceLink[]>();
  protected readonly failed = signal<ReadonlySet<string>>(new Set());

  protected markFailed(key: string): void {
    this.failed.update((f) => new Set(f).add(key));
  }
}
