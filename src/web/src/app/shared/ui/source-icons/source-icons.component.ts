import { ChangeDetectionStrategy, Component, input, signal } from '@angular/core';
import { SourceLink } from '../../../core/api/models';

@Component({
  selector: 'app-source-icons',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <span class="flex items-center gap-1">
      @for (s of sources(); track s.key) {
        <a [href]="s.url" target="_blank" rel="noopener nofollow" [attr.title]="s.name" class="relative z-10 inline-flex">
          @if (failed().has(s.key)) {
            <span
              class="source-initials inline-flex h-4 w-4 items-center justify-center rounded-full bg-gray-300 text-[8px] font-bold text-gray-900"
              [attr.aria-label]="s.name"
              >{{ initials(s.name) }}</span
            >
          } @else {
            <img [src]="s.iconUrl" [alt]="s.name" width="16" height="16" loading="lazy" (error)="markFailed(s.key)" />
          }
        </a>
      }
    </span>
  `,
})
export class SourceIconsComponent {
  readonly sources = input.required<SourceLink[]>();
  protected readonly failed = signal<ReadonlySet<string>>(new Set());

  protected markFailed(key: string): void {
    this.failed.update((f) => new Set(f).add(key));
  }

  protected initials(name: string): string {
    return name
      .split(/\s+/)
      .filter(Boolean)
      .map((w) => w[0])
      .join('')
      .slice(0, 2)
      .toUpperCase();
  }
}
