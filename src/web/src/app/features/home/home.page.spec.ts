import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { faculties, fields, institutions, qualifications } from '../../../testing/fixtures';
import { HomePage } from './home.page';

async function setup() {
  TestBed.configureTestingModule({ providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()] });
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
  http.expectOne('/api/v1/taxonomy/institutions').flush(institutions);
  http.expectOne('/api/v1/taxonomy/fields').flush(fields);
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
    http.expectOne(`/api/v1/taxonomy/institutions/${institutions[0].id}/faculties`).flush(faculties);
    await flush();
    http.expectOne(`/api/v1/taxonomy/faculties/${faculties[0].id}/qualifications`).flush(qualifications.slice(0, 2));
    http.expectOne(`/api/v1/taxonomy/faculties/${faculties[1].id}/qualifications`).flush(qualifications.slice(2));
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
  return { el, $, navigate, choose, chooseUniversity, submit, type };
}

describe('HomePage', () => {
  it('degreeSelect_beforeUniversity_isDisabledWithPlaceholder', async () => {
    const { $ } = await setup();
    const degree = $<HTMLSelectElement>('#home-degree');
    expect(degree.disabled).toBe(true);
    expect(degree.options[0].textContent).toBe('Choose a university first…');
  });

  it('degreeSelect_afterUniversity_isEnabledAndGroupedByFaculty', async () => {
    const { $, chooseUniversity } = await setup();
    await chooseUniversity();
    const degree = $<HTMLSelectElement>('#home-degree');
    expect(degree.disabled).toBe(false);
    const groups = Array.from(degree.querySelectorAll('optgroup')).map((g) => [
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
    const labels = ['home-degree', 'home-university', 'home-study-level', 'home-average'].map(
      (id) => el.querySelector(`label[for="${id}"]`)?.textContent,
    );
    expect(labels).toEqual(['Degree', 'University', 'Study level', 'Your average']);
    const average = $<HTMLInputElement>('#home-average');
    expect(average.type).toBe('text');
    expect(average.getAttribute('inputmode')).toBe('numeric');
    expect(average.getAttribute('autocomplete')).toBe('off');
    expect(average.name).toBe('average');
    expect(average.placeholder).toBe('e.g. 72…');
  });

  it('browseByField_rendersLinksToFieldPages', async () => {
    const { el } = await setup();
    const hrefs = Array.from(el.querySelectorAll('a')).map((a) => a.getAttribute('href'));
    expect(hrefs).toEqual(['/bursaries/field/engineering', '/bursaries/field/physical-sciences']);
  });

  it('submit_withoutUniversity_showsErrorAndFocusesIt', async () => {
    const { el, $, navigate, submit } = await setup();
    document.body.append(el); // focus() only works on attached elements
    await submit();
    expect(navigate).not.toHaveBeenCalled();
    expect($('#home-university-error').textContent).toContain('Choose your university');
    expect(document.activeElement).toBe($('#home-university'));
    el.remove();
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
    await choose('#home-degree', qualifications[0].id);
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
