import { Dialog } from '@angular/cdk/dialog';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { MeResponse } from '../core/api/models';
import { AuthService } from '../core/auth/auth.service';
import { HeaderComponent } from './header.component';

describe('HeaderComponent', () => {
  const user = signal<MeResponse | null>(null);
  const signOut = vi.fn();
  const openSignIn = vi.fn();

  function render() {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        { provide: Dialog, useValue: {} },
        { provide: AuthService, useValue: { user, isAdmin: () => user()?.roles.includes('Admin') ?? false, signOut, openSignIn } },
      ],
    });
    const fixture = TestBed.createComponent(HeaderComponent);
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  beforeEach(() => user.set(null));

  it('signedOut_showsSignInButtonOnly', () => {
    const el = render();
    const button = el.querySelector('button')!;
    expect(button.textContent).toContain('Sign in');
    button.click();
    expect(openSignIn).toHaveBeenCalled();
    expect(el.querySelector('details')).toBeNull();
  });

  it('student_showsMenuWithoutAdminLink', () => {
    user.set({ id: 'u', email: 'thandi@example.co.za', roles: ['Student'] });
    const el = render();
    expect(el.querySelector('summary')!.textContent).toContain('thandi@example.co.za');
    const links = Array.from(el.querySelectorAll('details a')).map((a) => a.textContent!.trim());
    expect(links).toEqual(['My bursaries']);
    el.querySelector<HTMLButtonElement>('details button')!.click();
    expect(signOut).toHaveBeenCalled();
  });

  it('admin_showsAdminLink', () => {
    user.set({ id: 'u', email: 'a@example.co.za', roles: ['Student', 'Admin'] });
    const el = render();
    const links = Array.from(el.querySelectorAll('details a')).map((a) => a.textContent!.trim());
    expect(links).toEqual(['My bursaries', 'Admin']);
  });
});
