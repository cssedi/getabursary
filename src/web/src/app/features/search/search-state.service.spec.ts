import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { routes } from '../../app.routes';
import { SearchStateService } from './search-state.service';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';

const INST = '0196a5f0-0000-7000-8000-000000000001';
const FAC = '0196a5f0-0000-7000-8000-000000000011';
const QUAL = '0196a5f0-0000-7000-8000-000000000021';
const FIELD = '0196a5f0-0000-7000-8000-000000000031';

describe('SearchStateService', () => {
  let state: SearchStateService;
  let router: Router;
  let harness: RouterTestingHarness;

  async function open(url: string) {
    await harness.navigateByUrl(url);
    await harness.fixture.whenStable();
  }

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [provideRouter(routes), provideHttpClient(), provideHttpClientTesting()],
    });
    harness = await RouterTestingHarness.create();
    state = TestBed.inject(SearchStateService);
    router = TestBed.inject(Router);
  });

  it('criteria_urlWithAllParams_roundTrips', async () => {
    const url =
      `/search?institutionId=${INST}&facultyId=${FAC}&qualificationId=${QUAL}&fieldIds=${FIELD}` +
      '&studyLevel=Honours&average=65&closingFrom=2026-12-01&closingTo=2027-01-31&includeClosed=true' +
      '&sort=Newest&page=3';
    await open(url);

    const c = state.criteria();
    expect(c).toEqual({
      institutionId: INST,
      facultyId: FAC,
      qualificationId: QUAL,
      fieldIds: [FIELD],
      studyLevel: 'Honours',
      average: 65,
      closingFrom: '2026-12-01',
      closingTo: '2027-01-31',
      includeClosed: true,
      sort: 'Newest',
      page: 3,
    });

    await state.update({ page: 3 });
    const parsed = router.parseUrl(router.url);
    expect(parsed.queryParams['qualificationId']).toBe(QUAL);
    expect(parsed.queryParams['fieldIds']).toBe(FIELD);
    expect(parsed.queryParams['page']).toBe('3');
  });

  it('criteria_invalidValues_areDropped', async () => {
    await open(
      `/search?average=150&sort=Bogus&studyLevel=Wizard&institutionId=nope&closingFrom=2026-13-45&page=-2&fieldIds=x,${FIELD}`,
    );
    const c = state.criteria();
    expect(c.average).toBeUndefined();
    expect(c.sort).toBeUndefined();
    expect(c.studyLevel).toBeUndefined();
    expect(c.institutionId).toBeUndefined();
    expect(c.closingFrom).toBeUndefined();
    expect(c.page).toBe(1);
    expect(c.fieldIds).toEqual([FIELD]);
  });

  it('criteria_includeClosedMissing_defaultsToFalse', async () => {
    await open('/search');
    expect(state.criteria().includeClosed).toBe(false);
    expect('includeClosed' in state.apiParams()).toBe(false);
  });

  it('apiParams_withFacultyId_omitsFacultyId', async () => {
    await open(`/search?facultyId=${FAC}&qualificationId=${QUAL}&includeClosed=true`);
    expect(state.apiParams()).toEqual({ qualificationId: QUAL, includeClosed: true });
  });

  it('update_filterChange_resetsPageAndReplacesUrl', async () => {
    await open('/search?page=4');
    const navigate = vi.spyOn(router, 'navigate');

    await state.update({ studyLevel: 'Masters' });

    expect(navigate).toHaveBeenCalledWith(
      ['/search'],
      expect.objectContaining({ replaceUrl: true, queryParams: { studyLevel: 'Masters' } }),
    );
    expect(state.criteria().page).toBe(1);
    expect(state.criteria().studyLevel).toBe('Masters');
  });

  it('update_onlyPage_keepsPage', async () => {
    await open('/search?studyLevel=Masters');
    await state.update({ page: 2 });
    expect(state.criteria().page).toBe(2);
    expect(state.criteria().studyLevel).toBe('Masters');
  });

  it('clear_removesEveryParamFromUrl', async () => {
    await open(`/search?qualificationId=${QUAL}&average=70&includeClosed=true&sort=Newest&page=2`);
    await state.clear();
    expect(router.url).toBe('/search');
    expect(state.criteria().includeClosed).toBe(false);
  });
});
