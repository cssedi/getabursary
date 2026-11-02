import { TestBed } from '@angular/core/testing';
import { ToastComponent } from './toast.component';
import { ToastService } from './toast.service';

describe('ToastComponent', () => {
  it('show_message_rendersInsidePoliteLiveRegion', () => {
    const fixture = TestBed.createComponent(ToastComponent);
    TestBed.inject(ToastService).show('Report sent');
    fixture.detectChanges();
    const region = (fixture.nativeElement as HTMLElement).querySelector('[aria-live="polite"]')!;
    expect(region.textContent).toContain('Report sent');
  });
});
