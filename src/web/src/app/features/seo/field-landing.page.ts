import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Observable, catchError, map, of, startWith, switchMap } from 'rxjs';
import { BursaryApi } from '../../core/api/bursary-api';
import { BursarySummaryResponse, FieldOfStudyResponse, PagedResponse } from '../../core/api/models';
import { TaxonomyApi } from '../../core/api/taxonomy-api';
import { ResultListComponent } from '../search/result-list/result-list.component';
import { NotFoundPage } from '../static/not-found.page';
import { SeoService } from './seo.service';

type LandingView =
  | { status: 'loading' }
  | { status: 'error' }
  | { status: 'notFound' }
  | { status: 'ok'; field: FieldOfStudyResponse; result: PagedResponse<BursarySummaryResponse> | null; resultError: boolean };

/** Finds a field by slug among first-order fields and their children. */
export function findFieldBySlug(fields: FieldOfStudyResponse[], slug: string): FieldOfStudyResponse | undefined {
  for (const f of fields) {
    if (f.slug === slug) return f;
    const child = findFieldBySlug(f.children, slug);
    if (child) return child;
  }
  return undefined;
}

@Component({
  selector: 'app-field-landing-page',
  imports: [ResultListComponent, RouterLink, NotFoundPage],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @let v = view();
    @if (v.status === 'notFound') {
      <app-not-found-page />
      <p class="mx-auto max-w-3xl p-4">That field of study isn’t listed. Check the address or search for it.</p>
    } @else {
      <div class="mx-auto grid max-w-3xl gap-4 p-4">
        @if (v.status === 'ok') {
          <h1 class="text-2xl font-bold">{{ v.field.name }} bursaries in South Africa</h1>
          <p>
            Open {{ v.field.name }} bursaries for South African students, with the soonest closing dates first. Add your
            degree and average to see only the ones you qualify for.
          </p>
          <a class="font-medium underline" routerLink="/search" [queryParams]="{ fieldIds: v.field.id }">
            Refine by your degree
          </a>
        }
        <app-result-list
          [result]="result()"
          [loading]="v.status === 'loading'"
          [error]="hasError()"
          [showPagination]="false"
          (retry)="retryCount.update((n) => n + 1)"
          (clearFilters)="retryCount.update((n) => n + 1)"
        />
      </div>
    }
  `,
})
export class FieldLandingPage {
  private readonly route = inject(ActivatedRoute);
  private readonly taxonomy = inject(TaxonomyApi);
  private readonly bursaries = inject(BursaryApi);
  private readonly seo = inject(SeoService);

  protected readonly retryCount = signal(0);
  private readonly slug = toSignal(this.route.paramMap.pipe(map((p) => p.get('fieldSlug') ?? '')), {
    initialValue: this.route.snapshot.paramMap.get('fieldSlug') ?? '',
  });

  protected readonly view = toSignal(
    toObservable(computed(() => ({ slug: this.slug(), retry: this.retryCount() }))).pipe(
      switchMap(({ slug }) => this.load(slug)),
    ),
    { initialValue: { status: 'loading' } as LandingView },
  );

  protected readonly result = computed(() => {
    const v = this.view();
    return v.status === 'ok' ? v.result : null;
  });

  protected readonly hasError = computed(() => {
    const v = this.view();
    return v.status === 'error' || (v.status === 'ok' && v.resultError);
  });

  private load(slug: string): Observable<LandingView> {
    return this.taxonomy.fields().pipe(
      switchMap((fields): Observable<LandingView> => {
        const field = findFieldBySlug(fields, slug);
        if (!field) return of({ status: 'notFound' });
        this.setSeo(field);
        return this.bursaries.search({ fieldIds: [field.id], sort: 'ClosingSoonest' }).pipe(
          map((result): LandingView => ({ status: 'ok', field, result, resultError: false })),
          catchError(() => of<LandingView>({ status: 'ok', field, result: null, resultError: true })),
        );
      }),
      catchError(() => of<LandingView>({ status: 'error' })),
      startWith<LandingView>({ status: 'loading' }),
    );
  }

  private setSeo(field: FieldOfStudyResponse): void {
    const year = new Date().getFullYear();
    this.seo.set({
      title: `${field.name} bursaries ${year}`,
      description: `Open ${field.name} bursaries in South Africa for ${year}, with closing dates, requirements and how to apply.`,
      canonicalPath: `/bursaries/field/${field.slug}`,
    });
  }
}
