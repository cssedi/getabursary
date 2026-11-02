import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { map } from 'rxjs';
import { BursaryDetailResponse, Citizenship } from '../../core/api/models';
import { formatDate } from '../../shared/format/format';
import { ClosingDateBadgeComponent } from '../../shared/ui/closing-date-badge/closing-date-badge.component';
import { ErrorStateComponent } from '../../shared/ui/error-state/error-state.component';
import { ReportButtonComponent } from '../../shared/ui/report-button/report-button.component';
import { SourceIconsComponent } from '../../shared/ui/source-icons/source-icons.component';
import { ToastComponent } from '../../shared/ui/toast/toast.component';
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
    ReportButtonComponent,
    SourceIconsComponent,
    ToastComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="detail" [class.with-apply-bar]="!!open()?.applyUrl">
      @if (open(); as b) {
        <header class="space-y-3">
          <app-closing-date-badge [closingDate]="b.closingDate" [status]="b.status" />
          <div class="flex items-start gap-2">
            <h1 class="min-w-0 flex-1 text-2xl font-bold [overflow-wrap:anywhere]">{{ b.name }}</h1>
            <app-report-button class="-mt-2 -mr-2" [bursaryId]="b.id" />
          </div>
          @if (b.funderName) {
            <p class="text-muted">{{ b.funderName }}</p>
          }
          <app-source-icons [sources]="b.sources" />
        </header>

        <section class="section" aria-labelledby="who-heading">
          <h2 id="who-heading" class="text-xl font-bold">Who can apply</h2>
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

        <section class="section" aria-labelledby="fields-heading">
          <h2 id="fields-heading" class="text-xl font-bold">
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

        <section class="section" aria-labelledby="covered-heading">
          <h2 id="covered-heading" class="text-xl font-bold">What’s covered</h2>
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
          <section class="section" aria-labelledby="workback-heading">
            <h2 id="workback-heading" class="text-xl font-bold">Work-back obligation</h2>
            <p>
              {{
                b.workBackDetails ??
                  'A work-back obligation applies. Check the funder’s site for the details.'
              }}
            </p>
          </section>
        }

        <section class="section" aria-labelledby="about-heading">
          <h2 id="about-heading" class="text-xl font-bold">About this bursary</h2>
          <p>{{ b.summary }}</p>
        </section>

        <p class="text-sm text-muted">Last updated {{ lastUpdated(b) }}</p>
        <div class="ad-slot" data-slot="detail" hidden></div>

        @if (b.applyUrl) {
          <div class="apply-bar">
            <a
              class="apply-button"
              [href]="b.applyUrl"
              target="_blank"
              rel="noopener nofollow"
              >{{ applyLabel(b) }}</a
            >
          </div>
        }
      } @else if (closed(); as c) {
        <app-closed-bursary [summary]="c" />
      } @else if (page().kind === 'notFound') {
        <app-not-found-page />
      } @else {
        <app-error-state message="We couldn't load this bursary." (retry)="retry()" />
      }
      <app-toast />
    </div>
  `,
  styles: `
    .detail {
      max-width: 720px;
      padding: 24px 16px;
    }
    .section {
      display: grid;
      gap: 8px;
      border-top: 1px solid var(--line);
      padding-top: 24px;
    }
    .apply-button {
      display: flex;
      align-items: center;
      justify-content: center;
      min-height: 48px;
      border-radius: var(--radius-control);
      background: var(--jacaranda);
      color: #fff;
      font-size: 20px;
      font-weight: 600;
      text-align: center;
      overflow-wrap: anywhere;
    }
    .apply-button:hover {
      text-decoration: underline;
      text-underline-offset: 4px;
    }
    @media (min-width: 768px) {
      .detail {
        padding: 24px;
      }
      .apply-bar {
        margin-top: 32px;
      }
    }
    /* Below 768 px the apply button is a sticky bottom bar; the page reserves matching space below it. */
    @media (max-width: 767px) {
      .detail.with-apply-bar {
        padding-bottom: calc(6rem + env(safe-area-inset-bottom));
      }
      .apply-bar {
        position: fixed;
        inset: auto 0 0 0;
        z-index: 10;
        padding: 12px 16px max(12px, env(safe-area-inset-bottom));
        background: var(--paper);
        border-top: 1px solid var(--line);
      }
    }
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

  /** "Apply on Sasol’s site", or the generic label when the funder is unknown. */
  protected applyLabel(b: BursaryDetailResponse): string {
    return b.funderName ? `Apply on ${b.funderName}’s site` : 'Apply on the funder’s site';
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
