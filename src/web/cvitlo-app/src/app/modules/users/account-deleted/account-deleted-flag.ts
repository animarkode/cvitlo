/**
 * A one-time pass to the account-deleted page. The account page sets it right before leaving; the page's guard
 * spends it. A refresh, a bookmark or a link someone sent cannot show "your account was deleted" on their own.
 */
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
