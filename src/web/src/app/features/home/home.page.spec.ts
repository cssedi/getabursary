import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { DeferBlockBehavior, DeferBlockState, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { faculties, fields, institutions, qualifications, searchPage } from '../../../testing/fixtures';
import { HomePage } from './home.page';

const degreesByFaculty = [
  { ...faculties[0], qualifications: qualifications.slice(0, 2) },
  { ...faculties[1], qualifications: qualifications.slice(2) },
];

async function setup(closingSoon = searchPage) {
  // jsdom has no IntersectionObserver, so the @defer blocks are rendered by hand below.
  TestBed.configureTestingModule({
    deferBlockBehavior: DeferBlockBehavior.Manual,
    providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
  });
  const http = TestBed.inject(HttpTestingController);
  const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
  const fixture = TestBed.createComponent(HomePage);
  const el = fixture.nativeElement as HTMLElement;
  const flush = async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  };
  await flush();
  for (const block of await fixture.getDeferBlocks()) await block.render(DeferBlockState.Complete);
  http.expectOne('/api/v1/taxonomy/institutions').flush(institutions);
  http.expectOne('/api/v1/taxonomy/fields').flush(fields);
  http.expectOne((r) => r.url === '/api/v1/bursaries/search').flush(closingSoon);
  await flush();
  const $ = <T extends HTMLElement>(selector: string) => el.querySelector<T>(selector)!;
  const choose = async (selector: string, value: string) => {
    const s = $<HTMLSelectElement>(selector);
    s.value = value;
    s.dispatchEvent(new Event('change'));
    await flush();
  };
  const chooseUniversity = async () => {
    await choose('#home-university', institutions[0].id);
    http.expectOne(`/api/v1/taxonomy/institutions/${institutions[0].id}/qualifications`).flush(degreesByFaculty);
    await flush();
  };
  const submit = async () => {
    $<HTMLButtonElement>('button[type="submit"]').click();
    await flush();
  };
  const type = async (value: string) => {
    const input = $<HTMLInputElement>('#home-average');
    input.value = value;
    input.dispatchEvent(new Event('input'));
    await flush();
  };
  return { el, $, http, navigate, choose, chooseUniversity, submit, type };
}

describe('HomePage', () => {
  it('universityPick_makesOneQualificationsRequest', async () => {
    const { choose, http } = await setup();
    await choose('#home-university', institutions[0].id);
    const taxonomyRequests = http.match((r) => r.url.startsWith('/api/v1/taxonomy/institutions/'));
    expect(taxonomyRequests.map((r) => r.request.url)).toEqual([
      `/api/v1/taxonomy/institutions/${institutions[0].id}/qualifications`,
    ]);
    taxonomyRequests[0].flush(degreesByFaculty);
  });

  it('qualificationSelect_beforeUniversity_isDisabledWithReason', async () => {
    const { $ } = await setup();
    const select = $<HTMLSelectElement>('#home-qualification');
    expect(select.disabled).toBe(true);
    expect(select.options[0].textContent).toBe('Pick a university first');
  });

  it('qualificationSelect_whileLoading_saysLoading', async () => {
    const { $, choose, http } = await setup();
    await choose('#home-university', institutions[0].id);
    expect($<HTMLSelectElement>('#home-qualification').options[0].textContent).toBe('Loading qualifications…');
    http.expectOne(`/api/v1/taxonomy/institutions/${institutions[0].id}/qualifications`).flush([]);
  });

  it('qualificationSelect_afterUniversity_isEnabledAndGroupedByFaculty', async () => {
    const { $, chooseUniversity } = await setup();
    await chooseUniversity();
    const select = $<HTMLSelectElement>('#home-qualification');
    expect(select.disabled).toBe(false);
    const groups = Array.from(select.querySelectorAll('optgroup')).map((g) => [
      g.label,
      g.querySelectorAll('option').length,
    ]);
    expect(groups).toEqual([
      [faculties[0].name, 2],
      [faculties[1].name, 1],
    ]);
  });

  it('controls_all_haveLabelsAndAverageAttributes', async () => {
    const { el, $ } = await setup();
    const labels = ['home-university', 'home-qualification', 'home-study-level', 'home-average'].map(
      (id) => el.querySelector(`label[for="${id}"]`)?.textContent,
    );
    expect(labels).toEqual(['University', 'Qualification', 'Year of study', 'Your average']);
    const average = $<HTMLInputElement>('#home-average');
    expect(average.type).toBe('text');
    expect(average.getAttribute('inputmode')).toBe('numeric');
    expect(average.getAttribute('autocomplete')).toBe('off');
    expect(average.name).toBe('average');
    expect(average.placeholder).toBe('For example 72…');
  });

  it('browseByField_rendersLinksToFieldPages', async () => {
    const { el } = await setup();
    const hrefs = Array.from(el.querySelectorAll('a')).map((a) => a.getAttribute('href'));
    expect(hrefs).toContain('/bursaries/field/engineering');
    expect(hrefs).toContain('/bursaries/field/physical-sciences');
  });

  it('submit_withNothingChosen_navigatesToSearchWithNoFilters', async () => {
    const { navigate, submit } = await setup();
    await submit();
    expect(navigate).toHaveBeenCalledWith(['/search'], { queryParams: {} });
  });

  it('universityChange_clearsChosenQualification', async () => {
    const { navigate, choose, chooseUniversity, submit, http } = await setup();
    await chooseUniversity();
    await choose('#home-qualification', qualifications[0].id);
    await choose('#home-university', institutions[1].id);
    http.expectOne(`/api/v1/taxonomy/institutions/${institutions[1].id}/qualifications`).flush([]);
    await submit();
    expect(navigate.mock.calls[0][1]?.queryParams).toEqual({ institutionId: institutions[1].id });
  });

  it('closingSoon_withBursaries_linksEachToItsPage', async () => {
    const { el } = await setup();
    const hrefs = Array.from(el.querySelectorAll('[aria-labelledby="closing-soon-heading"] ul a')).map((a) => a.getAttribute('href'));
    expect(hrefs).toEqual(searchPage.items.map((b) => `/bursaries/${b.slug}`));
  });

  it('closingSoon_empty_showsInvitationToSearch', async () => {
    const { el } = await setup({ ...searchPage, items: [], totalCount: 0 });
    expect(el.textContent).toContain('No bursaries close in the next while. Search by your degree instead.');
  });

  it('submit_withInvalidAverage_showsFixItMessageAndDoesNotNavigate', async () => {
    const { $, navigate, chooseUniversity, submit, type } = await setup();
    await chooseUniversity();
    await type('150');
    await submit();
    expect(navigate).not.toHaveBeenCalled();
    expect($('#home-average-error').textContent).toBe('Enter your average as a number from 0 to 100, like 72.');
  });

  it('submit_withValidInput_pushesSearchUrlWithQueryParams', async () => {
    const { navigate, choose, chooseUniversity, submit, type } = await setup();
    await chooseUniversity();
    await choose('#home-qualification', qualifications[0].id);
    await choose('#home-study-level', 'Undergraduate');
    await type('72');
    await submit();
    expect(navigate).toHaveBeenCalledTimes(1);
    const [commands, extras] = navigate.mock.calls[0];
    expect(commands).toEqual(['/search']);
    expect(extras?.replaceUrl).toBeUndefined(); // a push, not a replace
    expect(extras?.queryParams).toEqual({
      institutionId: institutions[0].id,
      facultyId: faculties[0].id,
      qualificationId: qualifications[0].id,
      studyLevel: 'Undergraduate',
      average: 72,
    });
  });
});
