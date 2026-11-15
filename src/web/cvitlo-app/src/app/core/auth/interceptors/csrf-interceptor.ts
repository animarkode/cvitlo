import { HttpErrorResponse, HttpInterceptorFn, HttpStatusCode } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { AuthStore } from '../auth-store';
import { API_ROUTES } from '../../routes/api-routes';
import { environment } from '../../../../environments/environment';

// A cross-site page cannot add this header.
const CSRF_HEADER = 'X-CSRF';

// The session may end elsewhere (another tab, Keycloak's account console, expiry), so a 401 re-checks it.
export const csrfInterceptor: HttpInterceptorFn = (req, next) => {
  if (!req.url.startsWith(environment.api)) {
    return next(req);
  }

  const authStore = inject(AuthStore);

  return next(req.clone({ setHeaders: { [CSRF_HEADER]: '1' } })).pipe(
    catchError((error: unknown) => {
      if (
        error instanceof HttpErrorResponse &&
        error.status === HttpStatusCode.Unauthorized &&
        req.url !== API_ROUTES.auth.user
      ) {
        authStore.reload();
      }

      return throwError(() => error);
    }),
  );
};
