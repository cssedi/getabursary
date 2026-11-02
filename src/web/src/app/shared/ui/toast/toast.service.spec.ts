import { TestBed } from '@angular/core/testing';
import { TOAST_MS, ToastService } from './toast.service';

describe('ToastService', () => {
  beforeEach(() => vi.useFakeTimers());
  afterEach(() => vi.useRealTimers());

  it('show_message_clearsAfterFourSeconds', () => {
    const toast = TestBed.inject(ToastService);
    toast.show('Report sent');
    vi.advanceTimersByTime(TOAST_MS - 1);
    expect(toast.message()).toBe('Report sent');
    vi.advanceTimersByTime(1);
    expect(toast.message()).toBeNull();
  });

  it('show_secondMessageBeforeFirstClears_restartsTheTimer', () => {
    const toast = TestBed.inject(ToastService);
    toast.show('First');
    vi.advanceTimersByTime(3000);
    toast.show('Second');
    vi.advanceTimersByTime(3000);
    expect(toast.message()).toBe('Second');
    vi.advanceTimersByTime(1000);
    expect(toast.message()).toBeNull();
  });
});
