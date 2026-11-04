import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { fields, institutions } from '../../../../testing/fixtures';
import { routes } from '../../../app.routes';

describe('FilterPanelComponent (on /search)', () => {
  let harness: RouterTestingHarness;
  let http: HttpTestingController;
  let router: Router;
  let el: HTMLElement;

  async function open(url: string) {
    TestBed.configureTestingModule({
      providers: [provideRouter(routes), provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
    router = TestBed.inject(Router);
    harness = await RouterTestingHarness.create();
    await harness.navigateByUrl(url);
    harness.detectChanges();
    http.expectOne('/api/v1/taxonomy/institutions').flush(institutions);
    http.expectOne('/api/v1/taxonomy/fields').flush(fields);
    await harness.fixture.whenStable();
    harness.detectChanges();
    el = harness.routeNativeElement as HTMLElement;
  }

  afterEach(() => vi.useRealTimers());

  it('showClosed_default_isUnchecked', async () => {
    await open('/search');
    const showClosed = Array.from(el.querySelectorAll('label')).find((l) => l.textContent?.includes('Show closed'));
    expect(showClosed?.querySelector<HTMLInputElement>('input')?.checked).toBe(false);
    expect(el.textContent).toContain('Filters (0)');
  });

  it('showClosed_ticked_writesUrlAndReplacesHistory', async () => {
    await open('/search?page=3');
    const label = Array.from(el.querySelectorAll('label')).find((l) => l.textContent?.includes('Show closed'));
    label!.querySelector<HTMLInputElement>('input')!.click();
    await harness.fixture.whenStable();
    expect(router.url).toBe('/search?includeClosed=true');
  });

  it('average_typing_isDebouncedBy300ms', async () => {
    await open('/search');
    vi.useFakeTimers();
    const input = el.querySelector<HTMLInputElement>('#filter-average')!;
    input.value = '7';
    input.dispatchEvent(new Event('input'));
    vi.advanceTimersByTime(200);
    input.value = '75';
    input.dispatchEvent(new Event('input'));
    vi.advanceTimersByTime(200);
    expect(router.url).toBe('/search');
    vi.advanceTimersByTime(150);
    vi.useRealTimers();
    await harness.fixture.whenStable();
    expect(router.url).toBe('/search?average=75');
  });

  it('fieldOfStudy_checkbox_addsFieldIdToUrl', async () => {
    await open('/search');
    const labels = Array.from(el.querySelectorAll('details label'));
    const chem = labels.find((l) => l.textContent?.includes('Chemical Engineering'))!;
    chem.querySelector<HTMLInputElement>('input')!.click();
    await harness.fixture.whenStable();
    expect(router.url).toBe(`/search?fieldIds=${fields[0].children[0].id}`);
  });

  it('fieldGroups_openOnlyWithSelection_andClearEmptiesThatGroup', async () => {
    await open(`/search?fieldIds=${fields[0].children[0].id}`);
    const groups = Array.from(el.querySelectorAll<HTMLDetailsElement>('details.field-group'));
    expect(groups.map((g) => g.open)).toEqual(fields.map((f, i) => i === 0));
    el.querySelector<HTMLButtonElement>('.clear-group')!.click();
    await harness.fixture.whenStable();
    expect(router.url).toBe('/search');
  });

  it('fieldQuery_filtersGroupsAndChildren_andShowsEmptyMessage', async () => {
    await open('/search');
    const box = el.querySelector<HTMLInputElement>('#filter-field-query')!;
    box.value = 'chemical';
    box.dispatchEvent(new Event('input'));
    harness.detectChanges();
    const groups = el.querySelectorAll<HTMLDetailsElement>('details.field-group');
    expect(groups.length).toBe(1);
    expect(groups[0].open).toBe(true);
    box.value = 'zzz';
    box.dispatchEvent(new Event('input'));
    harness.detectChanges();
    expect(el.querySelectorAll('details.field-group').length).toBe(0);
    expect(el.textContent).toContain('No fields match');
  });

  it('clearFilters_emptiesUrl', async () => {
    await open('/search?studyLevel=Honours&average=70&includeClosed=true&sort=Newest');
    http.match(() => true);
    const clear = Array.from(el.querySelectorAll('button')).find((b) => b.textContent?.includes('Clear filters'))!;
    clear.click();
    await harness.fixture.whenStable();
    expect(router.url).toBe('/search');
  });

  it('controls_allNativeAndLabelled_forKeyboardUse', async () => {
    await open('/search');
    const controls = el.querySelectorAll<HTMLElement>('select, input, button, summary');
    expect(controls.length).toBeGreaterThan(8);
    for (const c of Array.from(controls)) {
      expect(c.tabIndex).toBeGreaterThanOrEqual(0);
      if (c.matches('select, input')) {
        const labelled = c.closest('label') || (c.id && el.querySelector(`label[for="${c.id}"]`));
        expect(labelled, c.outerHTML).toBeTruthy();
      }
    }
  });
});
