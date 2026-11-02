import { TestBed } from '@angular/core/testing';
import { TODAY } from '../../format/today';
import { ClosingDateBadgeComponent } from './closing-date-badge.component';

describe('ClosingDateBadgeComponent', () => {
  function render(closingDate: string | null, status: 'Open' | 'Closed' = 'Open'): HTMLElement {
    TestBed.configureTestingModule({ providers: [{ provide: TODAY, useValue: '2026-11-02' }] });
    const fixture = TestBed.createComponent(ClosingDateBadgeComponent);
    fixture.componentRef.setInput('closingDate', closingDate);
    fixture.componentRef.setInput('status', status);
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }
  const text = (el: HTMLElement) => el.textContent!.replace(/\s+/g, ' ').trim();

  it('closesToday_zeroDays_highlightsAndSaysClosesToday', () => {
    const el = render('2026-11-02');
    expect(text(el)).toBe('Closes 2 Nov closes today');
    expect(el.querySelector('.swipe')).not.toBeNull();
  });

  it('closesTomorrow_oneDay_showsSingularDay', () => {
    const el = render('2026-11-03');
    expect(text(el)).toContain('closes in 1 day');
    expect(text(el)).not.toContain('1 days');
    expect(el.querySelector('.swipe')).not.toBeNull();
  });

  it('fourteenDays_highlightsWithCountdown', () => {
    const el = render('2026-11-16');
    expect(text(el)).toContain('closes in 14 days');
    expect(el.querySelector('.swipe')).not.toBeNull();
  });

  it('fifteenDays_noHighlightNoCountdown', () => {
    const el = render('2026-11-17');
    expect(text(el)).toBe('Closes 17 Nov');
    expect(el.querySelector('.swipe')).toBeNull();
    expect(el.querySelector('.badge-soon')).toBeNull();
  });

  it('otherYear_showsYear', () => {
    expect(text(render('2027-03-31'))).toBe('Closes 31 Mar 2027');
  });

  it('closedStatus_showsClosed', () => {
    const el = render('2026-09-30', 'Closed');
    expect(text(el)).toBe('Closed');
    expect(el.querySelector('.swipe')).toBeNull();
  });

  it('pastDate_showsClosed', () => {
    expect(text(render('2026-11-01'))).toBe('Closed');
  });
});
