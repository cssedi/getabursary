import { ApiError } from '../api/api-error';

export interface AuthErrors {
  email?: string;
  password?: string;
  /** Not tied to one field: shown above the buttons. */
  form?: string;
}

const TOO_MANY = 'Too many attempts. Wait a few minutes and try again.';
const GENERIC = 'That didn’t work. Check your connection and try again.';

/**
 * Turns a failed Identity call into inline messages (06 §8). Registration and reset failures arrive as
 * `errors: { PasswordTooShort: ['…'] }`; a failed login is a 401 whose `detail` says why.
 */
export function authErrors(error: unknown): AuthErrors {
  if (!(error instanceof ApiError)) return { form: GENERIC };
  if (error.status === 429) return { form: TOO_MANY };
  if (error.status === 401) {
    switch (error.extensions['detail']) {
      case 'NotAllowed':
        return { form: 'Confirm your email first. Use the link we sent you, then sign in.' };
      case 'LockedOut':
        return { form: TOO_MANY };
      default:
        return { form: 'Email or password is wrong. Check them and try again.' };
    }
  }

  const found: AuthErrors = {};
  const keyed = error.extensions['errors'];
  if (error.status === 400 && typeof keyed === 'object' && keyed !== null && !Array.isArray(keyed)) {
    for (const [code, messages] of Object.entries(keyed)) {
      const first = Array.isArray(messages) ? messages[0] : undefined;
      if (typeof first !== 'string') continue;
      if (code.startsWith('Password')) found.password ??= first;
      else if (code.includes('Email') || code.includes('UserName')) found.email ??= first;
      else found.form ??= first;
    }
  }
  return found.email || found.password || found.form ? found : { form: GENERIC };
}
