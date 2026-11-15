import { DOCUMENT, inject, Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse, httpResource } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_ROUTES } from '../../../core/routes/api-routes';
import { APP_ROUTES } from '../../../core/routes/app-routes';
import { rememberAccountDeleted } from '../account-deleted/account-deleted-flag';
import {
  AccountAction,
  AccountDevice,
  AccountProfile,
  CredentialGroup,
  LinkedAccount,
  UpdateProfileRequest,
} from './account-contracts';

// Reads are resources (create them in an injection context); actions that need Keycloak's own pages
// are full-page navigations, never fetches, as sign-in is.
@Injectable({ providedIn: 'root' })
export class AccountClient {
  private readonly http = inject(HttpClient);
  private readonly document = inject(DOCUMENT);

  profileResource() {
    return httpResource<AccountProfile>(() => API_ROUTES.auth.account.profile);
  }

  credentialsResource() {
    return httpResource<CredentialGroup[]>(() => API_ROUTES.auth.account.credentials);
  }

  devicesResource() {
    return httpResource<AccountDevice[]>(() => API_ROUTES.auth.account.devices);
  }

  linkedAccountsResource() {
    return httpResource<LinkedAccount[]>(() => API_ROUTES.auth.account.linkedAccounts);
  }

  updateProfile(request: UpdateProfileRequest): Observable<AccountProfile> {
    return this.http.post<AccountProfile>(API_ROUTES.auth.account.profile, request);
  }

  removeCredential(id: string): Observable<void> {
    return this.http.delete<void>(API_ROUTES.auth.account.credential(id));
  }

  unlinkAccount(alias: string): Observable<void> {
    return this.http.delete<void>(API_ROUTES.auth.account.linkedAccount(alias));
  }

  signOutSession(id: string): Observable<void> {
    return this.http.delete<void>(API_ROUTES.auth.account.session(id));
  }

  signOutOtherSessions(): Observable<void> {
    return this.http.delete<void>(API_ROUTES.auth.account.otherSessions);
  }

  deleteAccount(): Observable<void> {
    return this.http.delete<void>(API_ROUTES.auth.account.self);
  }

  // A full navigation, not a router one: the in-memory signed-in state must reset.
  leaveDeletedAccount(): void {
    rememberAccountDeleted(this.document.defaultView?.sessionStorage);
    this.document.location.assign(APP_ROUTES.accountDeleted);
  }

  startAction(action: AccountAction): void {
    this.document.location.assign(API_ROUTES.auth.account.action(action));
  }
}

export function problemCode(error: unknown): string | null {
  if (error instanceof HttpErrorResponse) {
    const code = (error.error as { code?: unknown } | null)?.code;
    if (typeof code === 'string') return code;
  }
  return null;
}

export function problemDetail(error: unknown, fallback: string): string {
  if (error instanceof HttpErrorResponse) {
    const detail = (error.error as { detail?: unknown } | null)?.detail;
    if (typeof detail === 'string' && detail.length > 0) return detail;
  }
  return fallback;
}
