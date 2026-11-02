import { Dialog } from '@angular/cdk/dialog';
import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { errorInterceptor } from '../../../core/api/error.interceptor';
import { ReportButtonComponent } from './report-button.component';

@Component({
  selector: 'app-report-button-host',
  imports: [ReportButtonComponent],
  template: `<app-report-button [bursaryId]="id" />`,
})
class HostComponent {
  readonly id = '0196a5f0-0000-7000-8000-000000000101';
}

describe('ReportButtonComponent', () => {
  let fixture: ReturnType<typeof TestBed.createComponent<HostComponent>>;
  let button: HTMLButtonElement;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withFetch(), withInterceptors([errorInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    fixture = TestBed.createComponent(HostComponent);
    fixture.detectChanges();
    button = (fixture.nativeElement as HTMLElement).querySelector('button')!;
  });

  afterEach(() => {
    TestBed.inject(Dialog).closeAll();
    TestBed.inject(HttpTestingController).verify();
  });

  it('button_hasAccessibleNameAndIsIconOnly', () => {
    expect(button.getAttribute('aria-label')).toBe('Report a problem with this listing');
    expect(button.textContent!.trim()).toBe('');
  });

  it('click_opensDialog_andStopsPropagation', () => {
    const parentClick = vi.fn();
    (fixture.nativeElement as HTMLElement).addEventListener('click', parentClick);
    button.click();
    TestBed.tick();
    expect(document.querySelector('[role="dialog"]')!.textContent).toContain('Report a problem');
    expect(parentClick).not.toHaveBeenCalled();
  });

  it('escape_closesDialog_andReturnsFocusToFlag', async () => {
    button.focus();
    button.click();
    TestBed.tick();
    const dialog = document.querySelector<HTMLElement>('[role="dialog"]')!;
    // CDK checks keyCode (27), not key, so set both.
    const esc = new KeyboardEvent('keydown', { key: 'Escape', bubbles: true });
    Object.defineProperty(esc, 'keyCode', { value: 27 });
    dialog.dispatchEvent(esc);
    TestBed.tick();
    await fixture.whenStable();
    expect(document.querySelector('[role="dialog"]')).toBeNull();
    expect(document.activeElement).toBe(button);
  });
});
