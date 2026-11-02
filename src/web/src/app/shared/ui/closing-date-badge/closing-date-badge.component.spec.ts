import { TestBed } from '@angular/core/testing';
import { ClosingDateBadgeComponent, NOW } from './closing-date-badge.component';

describe('ClosingDateBadgeComponent', () => {
  function render(closingDate: string | null, status: 'Open' | 'Closed' = 'Open'): string {
    TestBed.configureTestingModule({
      providers: [{ provide: NOW, useValue: () => new Date('2026-11-02T10:00:00Z') }],
    });
    const fixture = TestBed.createComponent(ClosingDateBadgeComponent);
    fixture.componentRef.setInput('closingDate', closingDate);
    fixture.componentRef.setInput('status', status);
    fixture.detectChanges();
    return (fixture.nativeElement as HTMLElement).textContent!.replace(/\s+/g, ' ').trim();
  }

  it('closesToday_zeroDays_showsClosesToday', () => {
    expect(render('2026-11-02')).toBe('Closes 2 Nov 2026Closes today');
  });

  it('closesTomorrow_oneDay_showsSingularDay', () => {
    const text = render('2026-11-03');
    expect(text).toContain('Closes in 1 day');
    expect(text).not.toContain('1 days');
  });

  it('fourteenDays_showsClosingSoonBadge', () => {
    expect(render('2026-11-16')).toContain('Closes in 14 days');
  });

  it('fifteenDays_showsDateOnly', () => {
    expect(render('2026-11-17')).toBe('Closes 17 Nov 2026');
  });

  it('farFuture_formatsDate', () => {
    expect(render('2027-03-31')).toBe('Closes 31 Mar 2027');
  });

  it('closedStatus_showsClosed', () => {
    expect(render('2026-09-30', 'Closed')).toBe('Closed');
  });

  it('pastDate_showsClosed', () => {
    expect(render('2026-11-01')).toBe('Closed');
  });
});
