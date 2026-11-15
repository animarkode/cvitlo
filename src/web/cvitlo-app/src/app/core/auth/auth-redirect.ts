import { DOCUMENT, inject, Injectable } from '@angular/core';
import { API_ROUTES } from '../routes/api-routes';

// Full-page navigations, never fetches: Keycloak's pages take the credentials, and the browser must
// carry that flow's redirects and cookies itself.
@Injectable({
  providedIn: 'root',
})
export class AuthRedirect {
  private readonly document = inject(DOCUMENT);

  signIn(returnUrl = '/'): void {
    this.document.location.assign(API_ROUTES.auth.login(returnUrl));
  }

  signUp(): void {
    this.document.location.assign(API_ROUTES.auth.register);
  }
}
