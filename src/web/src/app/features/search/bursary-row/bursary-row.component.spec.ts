import { Dialog } from '@angular/cdk/dialog';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { BursarySummaryResponse } from '../../../core/api/models';
import { closedBursary, closingSoonBursary, openBursary } from '../../../../testing/fixtures';
import { TODAY } from '../../../shared/format/today';
import { BursaryRowComponent } from './bursary-row.component';

describe('BursaryRowComponent', () => {
  afterEach(() => {
    vi.unstubAllGlobals();
    TestBed.inject(Dialog).closeAll();
  });

  function render(bursary: BursarySummaryResponse, index = 0) {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: TODAY, useValue: '2026-11-02' },
      ],
    });
    const fixture = TestBed.createComponent(BursaryRowComponent);
    fixture.componentRef.setInput('bursary', bursary);
    fixture.componentRef.setInput('index', index);
    fixture.detectChanges();
    return { fixture, el: fixture.nativeElement as HTMLElement };
  }

  it('openRow_rendersDateBlockTitleLinkAndDetails', () => {
    const { el } = render(openBursary);
    const link = el.querySelector<HTMLAnchorElement>('h2 a')!;
    expect(link.getAttribute('href')).toBe(`/bursaries/${openBursary.slug}`);
    expect(el.querySelector('.date-column .date-block')).not.toBeNull();
    expect(el.querySelector('.funder')!.textContent).toBe('Anglo American');
    expect(el.querySelector('.eligibility')!.textContent).toBe('Undergraduate, min 65% average');
    const sources = Array.from(el.querySelectorAll('.found-on a'));
    expect(sources.map((a) => a.textContent?.trim())).toEqual(openBursary.sources.map((s) => s.name));
    expect(el.querySelector('article')!.hasAttribute('aria-disabled')).toBe(false);
  });

  it('closingSoon_showsHowManyDaysLeft', () => {
    const { el } = render(closingSoonBursary);
    expect(el.textContent).toContain('closes in 8 days');
  });

  it('titleLink_isStretchedOverTheRow_andFlagSitsAboveIt', () => {
    const { el } = render(openBursary);
    expect(el.querySelector('h2 a')!.classList.contains('stretched-link')).toBe(true);
    expect(el.querySelector('article')!.classList.contains('relative')).toBe(true);
    expect(el.querySelector('.report-flag')!.classList.contains('z-10')).toBe(true);
  });

  it('flag_click_opensReportDialogAndDoesNotNavigate', () => {
    const { el } = render(openBursary);
    const click = new MouseEvent('click', { bubbles: true, cancelable: true });
    el.querySelector('.report-flag')!.dispatchEvent(click);
    TestBed.tick();
    expect(click.defaultPrevented).toBe(true);
    expect(el.querySelector('.report-flag')!.closest('a')).toBeNull();
    expect(document.querySelector('[role="dialog"]')!.textContent).toContain('Report a problem');
  });

  it('closedRow_hasNoLink_isAriaDisabled_showsClosedAndSources', () => {
    const { el } = render(closedBursary);
    expect(el.querySelector('a[href^="/bursaries/"]')).toBeNull();
    expect(el.querySelector('article')!.getAttribute('aria-disabled')).toBe('true');
    expect(el.textContent).toContain('Closed');
    expect(el.querySelectorAll('app-source-links a').length).toBe(1);
  });

  it('saveButton_shownOnOpenRow_notOnClosedRow', () => {
    expect(render(openBursary).el.querySelector('app-save-button button')).not.toBeNull();
    TestBed.resetTestingModule();
    expect(render(closedBursary).el.querySelector('app-save-button')).toBeNull();
  });

  it('firstRows_animate_butNotRowFourOnwards', () => {
    expect(render(closingSoonBursary, 0).el.querySelector('article')!.classList.contains('swipe-in')).toBe(true);
    TestBed.resetTestingModule();
    expect(render(closingSoonBursary, 3).el.querySelector('article')!.classList.contains('swipe-in')).toBe(false);
  });

  it('reducedMotion_rendersWithoutAnimationClass', () => {
    vi.stubGlobal('matchMedia', () => ({ matches: true }));
    expect(render(closingSoonBursary, 0).el.querySelector('article')!.classList.contains('swipe-in')).toBe(false);
  });

  it('longTitle_clampsToThreeLines_andFunderToTwoWithFullTextInTitle', () => {
    const name = 'Very long bursary name '.repeat(30);
    const funderName = 'A very long funder name '.repeat(10);
    const { el } = render({ ...openBursary, name, funderName });
    expect(el.querySelector('h2')!.classList.contains('line-clamp-3')).toBe(true);
    expect(el.querySelector('h2')!.classList.contains('min-w-0')).toBe(true);
    expect(el.querySelector('.funder')!.classList.contains('line-clamp-2')).toBe(true);
    expect(el.querySelector('.funder')!.getAttribute('title')).toBe(funderName);
  });
});
