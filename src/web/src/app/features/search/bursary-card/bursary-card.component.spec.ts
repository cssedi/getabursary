import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { BursarySummaryResponse } from '../../../core/api/models';
import { closedBursary, closingSoonBursary, openBursary } from '../../../../testing/fixtures';
import { NOW } from '../../../shared/ui/closing-date-badge/closing-date-badge.component';
import { BursaryCardComponent } from './bursary-card.component';

describe('BursaryCardComponent', () => {
  function render(bursary: BursarySummaryResponse): HTMLElement {
    TestBed.configureTestingModule({
      providers: [provideRouter([]), { provide: NOW, useValue: () => new Date('2026-11-02T10:00:00Z') }],
    });
    const fixture = TestBed.createComponent(BursaryCardComponent);
    fixture.componentRef.setInput('bursary', bursary);
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  it('openCard_linksToDetailPage', () => {
    const el = render(openBursary);
    const link = el.querySelector<HTMLAnchorElement>('a[href^="/bursaries/"]')!;
    expect(link.getAttribute('href')).toBe(`/bursaries/${openBursary.slug}`);
    expect(el.querySelector('article')!.hasAttribute('aria-disabled')).toBe(false);
    expect(el.textContent).toContain('Anglo American');
    expect(el.textContent).toContain('65%');
    expect(el.textContent).toContain('Undergraduate');
  });

  it('coverage_moreThanThree_showsPlusN', () => {
    const el = render(closingSoonBursary);
    expect(el.querySelector('.coverage')!.textContent).toBe('Tuition, Accommodation, Meals +1');
    expect(el.textContent).toContain('Closes in 8 days');
  });

  it('coverage_threeOrFewer_hasNoPlus', () => {
    expect(render(openBursary).querySelector('.coverage')!.textContent).toBe('Tuition, Books, Stipend');
  });

  it('closedCard_hasNoLink_isAriaDisabled_showsClosed', () => {
    const el = render(closedBursary);
    expect(el.querySelector('a[href^="/bursaries/"]')).toBeNull();
    expect(el.querySelector('article')!.getAttribute('aria-disabled')).toBe('true');
    expect(el.textContent).toContain('Closed');
    expect(el.querySelectorAll('app-source-icons a').length).toBe(1);
  });
});
