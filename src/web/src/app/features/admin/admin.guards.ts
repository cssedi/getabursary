import { inject } from '@angular/core';
import { CanDeactivateFn, CanMatchFn } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';

/** Admins only. Not matching lets the 404 route render, so the admin area isn't revealed. */
export const adminGuard: CanMatchFn = async () => {
  const auth = inject(AuthService);
  await auth.load();
  return auth.isAdmin();
};

export interface HasUnsavedChanges {
  hasUnsavedChanges(): boolean;
}

export const unsavedChangesGuard: CanDeactivateFn<HasUnsavedChanges> = (page) =>
  !page.hasUnsavedChanges() || confirm('You have unsaved changes. Leave without saving?');
