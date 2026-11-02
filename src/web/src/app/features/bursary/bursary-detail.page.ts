import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { map } from 'rxjs';
import { BursaryDetailResponse, Citizenship } from '../../core/api/models';
import {
  ClosingDateBadgeComponent,
  formatDate,
} from '../../shared/ui/closing-date-badge/closing-date-badge.component';
import { ErrorStateComponent } from '../../shared/ui/error-state/error-state.component';
import { SourceIconsComponent } from '../../shared/ui/source-icons/source-icons.component';
import { NotFoundPage } from '../static/not-found.page';
import { SeoService } from '../seo/seo.service';
import { ClosedBursaryComponent } from './closed-bursary.component';
import { BursaryPageData } from './bursary-detail.resolver';
import {
  CITIZENSHIP_LABELS,
  PROVINCE_LABELS,
  grantJsonLd,
  truncateDescription,
} from './bursary-labels';

/** `/bursaries/:slug`. The resolver has already loaded the bursary (see the route entry in app.routes.ts). */
@Component({
  selector: 'app-bursary-detail-page',
  imports: [
    ClosingDateBadgeComponent,
    ClosedBursaryComponent,
    ErrorStateComponent,
    NotFoundPage,
    SourceIconsComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="mx-auto max-w-3xl space-y-6 p-4">
      @if (open(); as b) {
        <header class="space-y-2">
          <h1 class="text-2xl font-bold">{{ b.name }}</h1>
          @if (b.funderName) {
            <p class="text-[var(--color-muted)]">{{ b.funderName }}</p>
          }
          <div class="flex flex-wrap items-center gap-3">
            <app-closing-date-badge [closingDate]="b.closingDate" [status]="b.status" />
            <app-source-icons [sources]="b.sources" />
          </div>
        </header>

        <section class="space-y-2" aria-labelledby="about-heading">
          <h2 id="about-heading" class="text-lg font-semibold">About</h2>
          <p>{{ b.summary }}</p>
        </section>

        <section class="space-y-2" aria-labelledby="who-heading">
          <h2 id="who-heading" class="text-lg font-semibold">Who can apply</h2>
          <dl class="grid gap-2 text-sm sm:grid-cols-[10rem_1fr]">
            <dt class="font-medium">Study levels</dt>
            <dd>{{ b.studyLevels.length ? b.studyLevels.join(', ') : 'Not specified' }}</dd>
            <dt class="font-medium">Minimum average</dt>
            <dd>{{ b.minAverage !== null ? b.minAverage + '%' : 'Not specified' }}</dd>
            <dt class="font-medium">Citizenship</dt>
            <dd>{{ citizenshipLabel(b.citizenship) }}</dd>
            <dt class="font-medium">Provinces</dt>
            <dd>{{ provincesLabel(b) }}</dd>
          </dl>
          @if (b.minSubjectRequirements.length) {
            <h3 class="pt-2 font-medium">Subject requirements (for information only)</h3>
            <ul class="list-inside list-disc text-sm">
              @for (r of b.minSubjectRequirements; track r.subject) {
                <li>{{ r.subject }}: {{ r.minPercent }}%</li>
              }
            </ul>
          }
        </section>

        <section class="space-y-2" aria-labelledby="fields-heading">
          <h2 id="fields-heading" class="text-lg font-semibold">
            Fields of study and qualifications
          </h2>
          @if (b.fields.length) {
            <h3 class="font-medium">Fields</h3>
            <ul class="list-inside list-disc text-sm">
              @for (f of b.fields; track f.code) {
                <li>{{ f.name }}</li>
              }
            </ul>
          }
          @if (b.qualifications.length) {
            <h3 class="font-medium">Qualifications</h3>
            <ul class="list-inside list-disc text-sm">
              @for (q of b.qualifications; track q.id) {
                <li>{{ q.name }} ({{ q.institutionShortName }})</li>
              }
            </ul>
          }
          @if (!b.fields.length && !b.qualifications.length) {
            <p class="text-sm">Open to all fields of study.</p>
          }
        </section>

        <section class="space-y-2" aria-labelledby="covered-heading">
          <h2 id="covered-heading" class="text-lg font-semibold">What's covered</h2>
          @if (b.coverage.length) {
            <ul class="list-inside list-disc text-sm">
              @for (item of b.coverage; track item) {
                <li>{{ item }}</li>
              }
            </ul>
          }
          @if (b.valueSummary) {
            <p>{{ b.valueSummary }}</p>
          }
        </section>

        @if (b.workBackRequired || b.workBackDetails) {
          <section class="space-y-2" aria-labelledby="workback-heading">
            <h2 id="workback-heading" class="text-lg font-semibold">Work-back obligation</h2>
            <p>
              {{
                b.workBackDetails ??
                  'A work-back obligation applies. Check the funder’s site for the details.'
              }}
            </p>
          </section>
        }

        @if (b.applyUrl) {
          <a
            class="inline-block rounded-[var(--radius)] bg-[var(--color-primary)] px-6 py-3 text-lg font-semibold text-[var(--color-primary-ink)]"
            [href]="b.applyUrl"
            target="_blank"
            rel="noopener nofollow"
            >Apply on funder's site</a
          >
        }

        <p class="text-sm text-[var(--color-muted)]">Last updated {{ lastUpdated(b) }}</p>
        <div class="ad-slot" data-slot="detail" hidden></div>
      } @else if (closed(); as c) {
        <app-closed-bursary [summary]="c" />
      } @else if (page().kind === 'notFound') {
        <app-not-found-page />
      } @else {
        <app-error-state message="We couldn't load this bursary." (retry)="retry()" />
      }
    </div>
  `,
})
export class BursaryDetailPage {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly seo = inject(SeoService);

  protected readonly page = toSignal(
    this.route.data.pipe(map((data) => data['bursary'] as BursaryPageData)),
    {
      requireSync: true,
    },
  );
  protected readonly open = computed(() => {
    const p = this.page();
    return p.kind === 'open' ? p.bursary : null;
  });
  protected readonly closed = computed(() => {
    const p = this.page();
    return p.kind === 'closed' ? p.summary : null;
  });

  constructor() {
    this.route.data
      .pipe(
        map((data) => data['bursary'] as BursaryPageData),
        takeUntilDestroyed(),
      )
      .subscribe((p) => this.applySeo(p));
  }

  protected citizenshipLabel(citizenship: Citizenship): string {
    return CITIZENSHIP_LABELS[citizenship];
  }

  protected provincesLabel(b: BursaryDetailResponse): string {
    return b.provinces.length
      ? b.provinces.map((p) => PROVINCE_LABELS[p]).join(', ')
      : 'Any province';
  }

  protected lastUpdated(b: BursaryDetailResponse): string {
    return formatDate(b.updatedAt.slice(0, 10));
  }

  /** Reloads the current URL. The route sets `runGuardsAndResolvers: 'always'`, so the resolver runs again. */
  protected retry(): void {
    void this.router.navigateByUrl(this.router.url, { onSameUrlNavigation: 'reload' });
  }

  private applySeo(p: BursaryPageData): void {
    if (p.kind === 'open') {
      const b = p.bursary;
      this.seo.set({
        title: b.name,
        description: truncateDescription(b.summary),
        canonicalPath: `/bursaries/${b.slug}`,
        jsonLd: grantJsonLd(b),
      });
    } else if (p.kind === 'closed') {
      const s = p.summary;
      this.seo.set({
        title: s.name,
        description: s.closingDate
          ? `${s.name} closed on ${formatDate(s.closingDate)}. Find other open bursaries on GetABursary.`
          : `${s.name} has closed. Find other open bursaries on GetABursary.`,
        canonicalPath: `/bursaries/${s.slug}`,
        robots: 'noindex',
      });
    }
  }
}
