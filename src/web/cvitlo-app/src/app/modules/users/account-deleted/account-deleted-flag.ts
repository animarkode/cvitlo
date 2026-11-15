// A one-time pass: the account page sets it right before leaving and the page's guard spends it, so a
// refresh, bookmark or shared link cannot show "your account was deleted".
const KEY = 'cvitlo.accountDeleted';

export function rememberAccountDeleted(storage: Storage | undefined): void {
  try {
    storage?.setItem(KEY, 'true');
  } catch {
    // Storage blocked: the person lands on the home page instead of the confirmation.
  }
}

export function takeAccountDeleted(storage: Storage | undefined): boolean {
  try {
    const present = storage?.getItem(KEY) === 'true';
    storage?.removeItem(KEY);
    return present;
  } catch {
    return false;
  }
}
