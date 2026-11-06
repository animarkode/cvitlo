import { DOCUMENT, inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { APP_ROUTES } from '../../../core/routes/app-routes';
import { takeAccountDeleted } from './account-deleted-flag';

/** Shows the account-deleted page once, right after the deletion; any other visit goes home. */
export const accountDeletedGuard: CanActivateFn = () => {
  const storage = inject(DOCUMENT).defaultView?.sessionStorage;
  return takeAccountDeleted(storage) || inject(Router).parseUrl(APP_ROUTES.home);
};
