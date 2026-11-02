import { PLATFORM_ID, TransferState, makeStateKey } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { formatDate, formatNumber, formatRand } from './format';
import { TODAY } from './today';

describe('format helpers', () => {
  it('formatDate_short_usesEnZa', () => {
    expect(formatDate('2027-03-31').replace(/\./g, '')).toBe('31 Mar 2027');
  });

  it('formatDate_long_usesFullMonth', () => {
    expect(formatDate('2027-03-31', 'long')).toBe('31 March 2027');
  });

  it('formatDate_dayAndMonth_split', () => {
    expect(formatDate('2027-01-05', 'day')).toBe('5');
    expect(formatDate('2027-01-05', 'month').replace(/\./g, '')).toBe('Jan');
  });

  it('formatNumber_enZa_usesSpaceGrouping', () => {
    expect(formatNumber(50000).replace(/\s/g, ' ')).toBe('50 000');
  });

  it('formatRand_zar_usesRandSymbol', () => {
    const text = formatRand(50000).replace(/\s/g, ' ');
    expect(text).toContain('R');
    expect(text).toContain('50 000');
  });
});

describe('TODAY', () => {
  it('browser_withTransferState_usesServerValue', () => {
    TestBed.configureTestingModule({ providers: [{ provide: PLATFORM_ID, useValue: 'browser' }] });
    TestBed.inject(TransferState).set(makeStateKey<string>('today'), '2026-11-02');
    expect(TestBed.inject(TODAY)).toBe('2026-11-02');
  });

  it('server_setsTransferState', () => {
    TestBed.configureTestingModule({ providers: [{ provide: PLATFORM_ID, useValue: 'server' }] });
    const today = TestBed.inject(TODAY);
    expect(today).toMatch(/^\d{4}-\d{2}-\d{2}$/);
    expect(TestBed.inject(TransferState).get(makeStateKey<string>('today'), '')).toBe(today);
  });
});
