import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { faculties, institutions, qualifications } from '../../../testing/fixtures';
import { HomePage } from './home.page';

describe('HomePage', () => {
  it('search_submit_pushesSearchUrlWithQueryParams', async () => {
    TestBed.configureTestingModule({ providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()] });
    const http = TestBed.inject(HttpTestingController);
    const router = TestBed.inject(Router);
    const navigate = vi.spyOn(router, 'navigate').mockResolvedValue(true);

    const fixture = TestBed.createComponent(HomePage);
    const el = fixture.nativeElement as HTMLElement;
    fixture.detectChanges();
    const flush = async () => {
      fixture.detectChanges();
      await fixture.whenStable();
      fixture.detectChanges();
    };
    const choose = async (selector: string, value: string) => {
      const s = el.querySelector<HTMLSelectElement>(selector)!;
      s.value = value;
      s.dispatchEvent(new Event('change'));
      await flush();
    };

    expect(el.querySelector('h1')?.textContent).toBe('Find bursaries for your degree');
    http.expectOne('/api/v1/taxonomy/institutions').flush(institutions);
    await flush();
    await choose('select[id$="-institution"]', institutions[0].id);
    http.expectOne(`/api/v1/taxonomy/institutions/${institutions[0].id}/faculties`).flush(faculties);
    await flush();
    await choose('select[id$="-faculty"]', faculties[0].id);
    http.expectOne(`/api/v1/taxonomy/faculties/${faculties[0].id}/qualifications`).flush(qualifications);
    await flush();
    await choose('select[id$="-qualification"]', qualifications[0].id);
    await choose('#home-study-level', 'Undergraduate');
    const average = el.querySelector<HTMLInputElement>('#home-average')!;
    average.value = '72';
    average.dispatchEvent(new Event('input'));

    el.querySelector<HTMLButtonElement>('button[type="submit"]')!.click();

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
