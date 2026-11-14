const KNOWN_LABELS: Record<string, string> = {
  firstname: 'First name',
  lastname: 'Last name',
  email: 'Email',
  username: 'Username',
  password: 'Password',
  otp: 'Authenticator app',
  totp: 'Authenticator app',
  webauthn: 'Security key',
  'webauthn-passwordless': 'Passkey',
};

/** Help for each credential type, keyed by the type Keycloak names in its `{type}-help-text` message key. */
const KNOWN_HELP: Record<string, string> = {
  password: 'Sign in with your e-mail and password.',
  otp: 'Enter a code from an authenticator app when you sign in.',
  totp: 'Enter a code from an authenticator app when you sign in.',
  webauthn: 'Use a security key as a second step when you sign in.',
  'webauthn-passwordless': 'Sign in with your fingerprint, face or screen lock.',
};

// Keycloak's Account API returns message keys, not text: the console theme holds the messages.
const DISPLAY_NAME_SUFFIX = /-display-name$/;
const HELP_TEXT_SUFFIX = /-help-text$/;

function bareKey(key: string | null | undefined): string {
  return (key ?? '').trim().replace(/^\$\{(.*)\}$/, '$1').trim();
}

/**
 * Turns a Keycloak label — a message key like `${firstName}` or `password-display-name`, or an identifier like
 * `webauthn-passwordless` — into text a person can read.
 */
export function humanize(key: string | null | undefined): string {
  const bare = bareKey(key).replace(DISPLAY_NAME_SUFFIX, '');
  if (!bare) return '';
  const known = KNOWN_LABELS[bare.toLowerCase()];
  if (known) return known;
  const words = bare
    .replace(/([a-z0-9])([A-Z])/g, '$1 $2')
    .replace(/[-_.]+/g, ' ')
    .trim()
    .toLowerCase();
  return words.charAt(0).toUpperCase() + words.slice(1);
}

/**
 * Turns a credential type's help into a sentence: a `{type}-help-text` key becomes the known help for that type,
 * a key with no known help is dropped rather than shown raw, and text that is already a sentence is kept.
 */
export function helpText(key: string | null | undefined): string | null {
  const bare = bareKey(key);
  if (!bare) return null;
  if (!HELP_TEXT_SUFFIX.test(bare)) return bare;
  return KNOWN_HELP[bare.replace(HELP_TEXT_SUFFIX, '').toLowerCase()] ?? null;
}
