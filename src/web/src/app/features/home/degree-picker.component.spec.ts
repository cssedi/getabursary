import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { faculties, institutions, qualifications } from '../../../testing/fixtures';
import { DegreePickerComponent, DegreeSelection } from './degree-picker.component';

@Component({
  imports: [DegreePickerComponent],
  template: `<app-degree-picker
    [institutionId]="inst"
    [facultyId]="fac"
    (selectionChange)="events.push($event)"
  />`,
})
class HostComponent {
  inst: string | undefined;
  fac: string | undefined;
  events: DegreeSelection[] = [];
}

describe('DegreePickerComponent', () => {
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    http = TestBed.inject(HttpTestingController);
  });

  async function setup(inst?: string, fac?: string) {
    const fixture = TestBed.createComponent(HostComponent);
    fixture.componentInstance.inst = inst;
    fixture.componentInstance.fac = fac;
    fixture.detectChanges();
    await fixture.whenStable();
    const el = fixture.nativeElement as HTMLElement;
    const select = (name: string) => el.querySelector<HTMLSelectElement>(`select[id$="-${name}"]`)!;
    const flush = async () => {
      fixture.detectChanges();
      await fixture.whenStable();
      fixture.detectChanges();
    };
    const choose = async (name: string, value: string) => {
      const s = select(name);
      s.value = value;
      s.dispatchEvent(new Event('change'));
      await flush();
    };
    return { fixture, select, choose, flush };
  }

  it('institutions_whileLoading_showsLoadingAndDisabled', async () => {
    const { select, flush } = await setup();
    expect(select('institution').disabled).toBe(true);
    expect(select('institution').textContent).toContain('Loading…');

    http.expectOne('/api/v1/taxonomy/institutions').flush(institutions);
    await flush();
    expect(select('institution').disabled).toBe(false);
    expect(select('institution').options.length).toBe(institutions.length + 1);
  });

  it('labels_eachSelect_hasLabel', async () => {
    const { fixture } = await setup();
    http.expectOne('/api/v1/taxonomy/institutions').flush(institutions);
    const labels = Array.from((fixture.nativeElement as HTMLElement).querySelectorAll('label')).map((l) => l.textContent);
    expect(labels).toEqual(['University', 'Faculty', 'Qualification']);
  });

  it('selection_cascadesAndResetsLowerLevels', async () => {
    const { fixture, select, choose, flush } = await setup();
    http.expectOne('/api/v1/taxonomy/institutions').flush(institutions);
    await flush();
    expect(select('faculty').disabled).toBe(true);

    await choose('institution', institutions[0].id);
    expect(select('faculty').disabled).toBe(true); // loading
    http.expectOne(`/api/v1/taxonomy/institutions/${institutions[0].id}/faculties`).flush(faculties);
    await flush();
    expect(select('faculty').disabled).toBe(false);

    await choose('faculty', faculties[0].id);
    http.expectOne(`/api/v1/taxonomy/faculties/${faculties[0].id}/qualifications`).flush(qualifications);
    await flush();
    await choose('qualification', qualifications[0].id);
    expect(fixture.componentInstance.events.at(-1)).toEqual({
      institutionId: institutions[0].id,
      facultyId: faculties[0].id,
      qualificationId: qualifications[0].id,
    });

    // Changing the faculty resets the qualification.
    await choose('faculty', faculties[1].id);
    expect(fixture.componentInstance.events.at(-1)).toEqual({
      institutionId: institutions[0].id,
      facultyId: faculties[1].id,
      qualificationId: undefined,
    });
    http.expectOne(`/api/v1/taxonomy/faculties/${faculties[1].id}/qualifications`).flush([]);
    await flush();

    // Changing the institution resets faculty and qualification.
    await choose('institution', institutions[1].id);
    expect(fixture.componentInstance.events.at(-1)).toEqual({
      institutionId: institutions[1].id,
      facultyId: undefined,
      qualificationId: undefined,
    });
    expect(select('qualification').disabled).toBe(true);
    http.expectOne(`/api/v1/taxonomy/institutions/${institutions[1].id}/faculties`).flush(faculties);
  });

  it('inputs_prefilled_loadDependentListsAndSelect', async () => {
    const { select, flush } = await setup(institutions[0].id, faculties[0].id);
    http.expectOne('/api/v1/taxonomy/institutions').flush(institutions);
    http.expectOne(`/api/v1/taxonomy/institutions/${institutions[0].id}/faculties`).flush(faculties);
    http.expectOne(`/api/v1/taxonomy/faculties/${faculties[0].id}/qualifications`).flush(qualifications);
    await flush();
    expect(select('institution').value).toBe(institutions[0].id);
    expect(select('faculty').value).toBe(faculties[0].id);
    expect(select('qualification').options.length).toBe(qualifications.length + 1);
  });
});
