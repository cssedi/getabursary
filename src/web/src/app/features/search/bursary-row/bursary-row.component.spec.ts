import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { BursarySummaryResponse } from '../../../core/api/models';
import { closedBursary, closingSoonBursary, openBursary } from '../../../../testing/fixtures';
import { TODAY } from '../../../shared/format/today';
import { BursaryRowComponent } from './bursary-row.component';

describe('BursaryRowComponent', () => {
  afterEach(() => vi.unstubAllGlobals());

  function render(bursary: BursarySummaryResponse, index = 0) {
    TestBed.configureTestingModule({
      providers: [provideRouter([]), { provide: TODAY, useValue: '2026-11-02' }],
    });
    const fixture = TestBed.createComponent(BursaryRowComponent);
    fixture.componentRef.setInput('bursary', bursary);
    fixture.componentRef.setInput('index', index);
    fixture.detectChanges();
    return { fixture, el: fixture.nativeElement as HTMLElement };
  }

  it('openRow_rendersDateBlockTitleLinkAndDetails', () => {
    const { el } = render(openBursary);
    const link = el.querySelector<HTMLAnchorElement>('h3 a')!;
    expect(link.getAttribute('href')).toBe(`/bursaries/${openBursary.slug}`);
    expect(el.querySelector('.date-column .date-block')).not.toBeNull();
    expect(el.querySelector('.funder')!.textContent).toBe('Anglo American');
    expect(el.querySelector('.eligibility')!.textContent).toBe('Undergraduate, min 65% average');
    expect(el.querySelector('.coverage')!.textContent).toBe('Covers tuition, books and stipend');
    expect(el.querySelector('.sources-count')!.textContent).toBe('found on 2 sites');
    expect(el.querySelector('article')!.hasAttribute('aria-disabled')).toBe(false);
  });

  it('coverage_moreThanThree_showsPlusNMore', () => {
    const { el } = render(closingSoonBursary);
    expect(el.querySelector('.coverage')!.textContent).toBe('Covers tuition, accommodation and meals +1 more');
    expect(el.textContent).toContain('closes in 8 days');
  });

  it('titleLink_isStretchedOverTheRow_andFlagSitsAboveIt', () => {
    const { el } = render(openBursary);
    expect(el.querySelector('h3 a')!.classList.contains('stretched-link')).toBe(true);
    expect(el.querySelector('article')!.classList.contains('relative')).toBe(true);
    expect(el.querySelector('.report-flag')!.classList.contains('z-10')).toBe(true);
  });

  it('flag_click_emitsReportAndDoesNotNavigate', () => {
    const { fixture, el } = render(openBursary);
    const reported = vi.fn();
    fixture.componentInstance.report.subscribe(reported);
    const click = new MouseEvent('click', { bubbles: true, cancelable: true });
    el.querySelector('.report-flag')!.dispatchEvent(click);
    expect(reported).toHaveBeenCalledTimes(1);
    expect(el.querySelector('.report-flag')!.closest('a')).toBeNull();
  });

  it('closedRow_hasNoLink_isAriaDisabled_showsClosedAndSources', () => {
    const { el } = render(closedBursary);
    expect(el.querySelector('a[href^="/bursaries/"]')).toBeNull();
    expect(el.querySelector('article')!.getAttribute('aria-disabled')).toBe('true');
    expect(el.textContent).toContain('Closed');
    expect(el.querySelectorAll('app-source-icons a').length).toBe(1);
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

  it('longTitle_clampsToThreeLines_andFunderTruncates', () => {
    const name = 'Very long bursary name '.repeat(30);
    const { el } = render({ ...openBursary, name, funderName: 'A very long funder name '.repeat(10) });
    expect(el.querySelector('h3')!.classList.contains('line-clamp-3')).toBe(true);
    expect(el.querySelector('h3')!.classList.contains('min-w-0')).toBe(true);
    expect(el.querySelector('.funder')!.classList.contains('truncate')).toBe(true);
  });
});
