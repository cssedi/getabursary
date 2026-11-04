import { ApplicationRef, signal } from '@angular/core';
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
        { provide: AuthService, useValue: { user, isAdmin: () => user()?.roles.includes('Admin') ?? false, signOut, openSignIn } },
      ],
    });
    const fixture = TestBed.createComponent(HeaderComponent);
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  beforeEach(() => user.set(null));

  afterEach(() => document.querySelectorAll('.cdk-overlay-container').forEach((o) => (o.innerHTML = '')));

  it('signedOut_showsSignInButton_andNoAccountMenu', () => {
    const el = render();
    const button = el.querySelector<HTMLButtonElement>('nav button')!;
    expect(button.textContent).toContain('Sign in');
    button.click();
    expect(openSignIn).toHaveBeenCalled();
    expect(el.querySelector('details')).toBeNull();
  });

  it('student_showsMyBursariesAndAccountMenuWithoutAdmin', () => {
    user.set({ id: 'u', email: 'thandi@example.co.za', roles: ['Student'] });
    const el = render();
    const nav = Array.from(el.querySelectorAll('nav > a')).map((a) => a.textContent!.trim());
    expect(nav).toEqual(['Search', 'My bursaries']);
    expect(el.querySelector('summary')!.textContent).toContain('thandi@example.co.za');
    expect(el.querySelectorAll('details a')).toHaveLength(0);
    el.querySelector<HTMLButtonElement>('details button')!.click();
    expect(signOut).toHaveBeenCalled();
  });

  it('admin_showsAdminLinkInAccountMenu', () => {
    user.set({ id: 'u', email: 'a@example.co.za', roles: ['Student', 'Admin'] });
    const el = render();
    const links = Array.from(el.querySelectorAll('details a')).map((a) => a.textContent!.trim());
    expect(links).toEqual(['Admin']);
  });

  it('menuSheet_opensWithMenuButton_andEscapeClosesIt', async () => {
    const el = render();
    const app = TestBed.inject(ApplicationRef);
    el.querySelector<HTMLButtonElement>('button[aria-haspopup="dialog"]')!.click();
    await vi.waitFor(() => expect(document.querySelector('[role="dialog"]')).not.toBeNull());
    await app.whenStable();
    const dialog = document.querySelector('[role="dialog"]');
    expect(dialog).not.toBeNull();
    expect(dialog!.textContent).toContain('Search');
    document
      .querySelector('app-mobile-nav')!
      .dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', code: 'Escape', keyCode: 27, bubbles: true }));
    await app.whenStable();
    expect(document.querySelector('[role="dialog"]')).toBeNull();
  });
});
