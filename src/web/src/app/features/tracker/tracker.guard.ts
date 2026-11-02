import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';

/** Signed-in students only; everyone else goes home with the sign-in dialog open. */
export const signedInGuard: CanActivateFn = async () => {
  const auth = inject(AuthService);
  await auth.load();
  if (auth.user()) return true;
  auth.openSignIn();
  return inject(Router).parseUrl('/');
};
