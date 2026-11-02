import { DialogRef } from '@angular/cdk/dialog';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { ApiError } from '../../core/api/api-error';
import { AuthService } from '../../core/auth/auth.service';
import { SignInDialogComponent } from './sign-in-dialog.component';

describe('SignInDialogComponent', () => {
  let fixture: ComponentFixture<SignInDialogComponent>;
  let el: HTMLElement;
  const signIn = vi.fn();
  const signUp = vi.fn();
  const close = vi.fn();

  beforeEach(() => {
    signIn.mockReset();
    signUp.mockReset();
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        { provide: DialogRef, useValue: { close } },
        { provide: AuthService, useValue: { signIn, signUp } },
      ],
    });
    fixture = TestBed.createComponent(SignInDialogComponent);
    fixture.detectChanges();
    el = fixture.nativeElement as HTMLElement;
  });

  function fill(email: string, password: string) {
    for (const [id, value] of [['#auth-email', email], ['#auth-password', password]]) {
      const input = el.querySelector<HTMLInputElement>(id)!;
      input.value = value;
      input.dispatchEvent(new Event('input'));
    }
    fixture.detectChanges();
  }

  async function submit() {
    el.querySelector('form')!.dispatchEvent(new Event('submit', { cancelable: true }));
    await fixture.whenStable();
    fixture.detectChanges();
  }

  function tab(label: string) {
    Array.from(el.querySelectorAll<HTMLElement>('[role=tab]')).find((t) => t.textContent!.trim() === label)!.click();
    fixture.detectChanges();
  }

  it('hasGoogleLinkAndForgotLink', () => {
    expect(el.querySelector<HTMLAnchorElement>('a[href^="/api/v1/auth/external/google?returnUrl="]')).not.toBeNull();
    expect(el.querySelector('a[href="/account/forgot"]')!.textContent).toContain('Forgot password?');
  });

  it('submitEmpty_showsInlineErrorsAndCallsNothing', async () => {
    await submit();
    expect(el.querySelector('#auth-email-error')!.textContent).toContain('Enter an email address');
    expect(el.querySelector('#auth-password-error')!.textContent).toContain('Enter your password.');
    expect(signIn).not.toHaveBeenCalled();
  });

  it('signIn_success_closesDialog', async () => {
    signIn.mockResolvedValue(undefined);
    fill('thandi@example.co.za', 'password1');
    await submit();
    expect(signIn).toHaveBeenCalledWith('thandi@example.co.za', 'password1');
    expect(close).toHaveBeenCalled();
  });

  it('signIn_401_showsWrongPasswordMessage', async () => {
    signIn.mockRejectedValue(new ApiError(401, 'Unknown', 'Unauthorized', [], { detail: 'Failed' }));
    fill('thandi@example.co.za', 'nope-nope');
    await submit();
    expect(el.querySelector('[role=alert]')!.textContent).toContain('Email or password is wrong');
  });

  it('signUp_shortPassword_blockedBeforeRequest', async () => {
    tab('Create account');
    fill('thandi@example.co.za', 'short');
    await submit();
    expect(el.querySelector('#auth-password-error')!.textContent).toContain('at least 8 characters');
    expect(signUp).not.toHaveBeenCalled();
    expect(el.querySelector('#auth-password')!.getAttribute('autocomplete')).toBe('new-password');
  });

  it('signUp_apiPasswordError_isShownBesidePasswordField', async () => {
    signUp.mockRejectedValue(
      new ApiError(400, 'Unknown', 'One or more validation errors occurred.', [], {
        errors: { PasswordRequiresDigit: ['Passwords must have at least one digit (0-9).'] },
      }),
    );
    tab('Create account');
    fill('thandi@example.co.za', 'passwordonly');
    await submit();
    expect(el.querySelector('#auth-password-error')!.textContent).toContain('at least one digit');
    expect(el.querySelector('#auth-password')!.getAttribute('aria-invalid')).toBe('true');
  });

  it('signUp_success_asksToCheckEmail', async () => {
    signUp.mockResolvedValue(undefined);
    tab('Create account');
    fill('thandi@example.co.za', 'password1');
    await submit();
    expect(el.textContent).toContain('Check your email to confirm your account.');
  });
});
