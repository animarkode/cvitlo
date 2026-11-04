import { helpText, humanize } from './account-labels';

describe('account labels', () => {
  it.each([
    ['password-display-name', 'Password'],
    ['otp-display-name', 'Authenticator app'],
    ['webauthn-passwordless-display-name', 'Passkey'],
    ['webauthn-display-name', 'Security key'],
    ['${firstName}', 'First name'],
    ['phoneNumber', 'Phone number'],
    ['recovery-codes-display-name', 'Recovery codes'],
  ])('should name %s as %s', (key, expected) => {
    expect(humanize(key)).toBe(expected);
  });

  it.each([
    ['password-help-text', 'Sign in with your e-mail and password.'],
    ['otp-help-text', 'Enter a code from an authenticator app when you sign in.'],
    ['webauthn-passwordless-help-text', 'Sign in with your fingerprint, face or screen lock.'],
  ])('should turn %s into a sentence', (key, expected) => {
    expect(helpText(key)).toBe(expected);
  });

  it('should drop a help key it does not know rather than show it raw', () => {
    expect(helpText('recovery-codes-help-text')).toBeNull();
  });

  it('should keep help that is already a sentence', () => {
    expect(helpText('Sign in with a passkey.')).toBe('Sign in with a passkey.');
  });

  it('should show no help when there is none', () => {
    expect(helpText(null)).toBeNull();
    expect(helpText('  ')).toBeNull();
  });
});
