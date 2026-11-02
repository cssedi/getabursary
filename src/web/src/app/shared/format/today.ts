import { InjectionToken, PLATFORM_ID, TransferState, inject, makeStateKey } from '@angular/core';
import { isPlatformServer } from '@angular/common';

const TODAY_KEY = makeStateKey<string>('today');

/** Today's date in South Africa as `yyyy-MM-dd`. */
export function todayInSouthAfrica(now: Date = new Date()): string {
  return new Intl.DateTimeFormat('en-CA', { timeZone: 'Africa/Johannesburg' }).format(now);
}

/**
 * "Today" as `yyyy-MM-dd`. Computed on the server and handed to the browser through TransferState,
 * so "closes in N days" renders identically on both sides (no hydration mismatch).
 */
export const TODAY = new InjectionToken<string>('TODAY', {
  providedIn: 'root',
  factory: () => {
    const state = inject(TransferState);
    if (state.hasKey(TODAY_KEY)) return state.get(TODAY_KEY, '');
    const today = todayInSouthAfrica();
    if (isPlatformServer(inject(PLATFORM_ID))) state.set(TODAY_KEY, today);
    return today;
  },
});
