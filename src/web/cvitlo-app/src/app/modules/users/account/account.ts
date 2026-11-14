import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { map } from 'rxjs';
import {
  KeyRound,
  Link,
  LogOut,
  LUCIDE_ICONS,
  LucideAngularModule,
  LucideIconProvider,
  Monitor,
  User,
} from 'lucide-angular';
import { AuthClient } from '../../../core/auth/auth-client';
import { AuthStore } from '../../../core/auth/auth-store';
import { ACCOUNT_SECTIONS, APP_ROUTES } from '../../../core/routes/app-routes';
import { AccountClient } from './account-client';
import { CvButton } from '../../../shared/components/cv-button/cv-button';
import { CvInitials } from '../../../shared/components/cv-initials/cv-initials';

const SUCCESS_MESSAGES: Record<string, string> = {
  password: 'Password updated',
  passkey: 'Passkey added',
  totp: 'Authenticator app set up',
  email: 'E-mail updated',
  'link-google': 'Google account linked',
  'link-github': 'GitHub account linked',
  reauthenticate: 'You signed in again. You can delete your account now.',
};

interface AccountSectionLink {
  path: string;
  label: string;
  icon: string;
}

const SECTION_LINKS: readonly AccountSectionLink[] = [
  { path: ACCOUNT_SECTIONS.personalInfo, label: 'Personal info', icon: 'user' },
  { path: ACCOUNT_SECTIONS.signingIn, label: 'Signing in', icon: 'key-round' },
  { path: ACCOUNT_SECTIONS.devices, label: 'Device activity', icon: 'monitor' },
  { path: ACCOUNT_SECTIONS.linkedAccounts, label: 'Linked accounts', icon: 'link' },
];

/**
 * The person's account settings: who they are, a side menu of sections, and the open section. Keycloak is only
 * visited for the steps that need its own pages (password, passkeys, 2FA, e-mail, linking, signing in again before deletion), and returns
 * to the section the step belongs to with `?action=…&status=…`.
 */
@Component({
  selector: 'app-account',
  imports: [RouterLink, RouterLinkActive, RouterOutlet, LucideAngularModule, CvButton, CvInitials],
  providers: [
    { provide: LUCIDE_ICONS, multi: true, useValue: new LucideIconProvider({ User, KeyRound, Monitor, Link, LogOut }) },
  ],
  templateUrl: './account.html',
  styleUrl: './account.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Account {
  private readonly authClient = inject(AuthClient);
  private readonly router = inject(Router);

  protected readonly user = inject(AuthStore).user;
  protected readonly sections = SECTION_LINKS;
  protected readonly devicesPath = ACCOUNT_SECTIONS.devices;

  private readonly devices = inject(AccountClient).devicesResource();
  protected readonly deviceCount = computed(() => this.devices.value()?.length ?? null);

  protected readonly fullName = computed(() => {
    const user = this.user();
    return [user?.firstName, user?.lastName].filter(Boolean).join(' ') || user?.email || '';
  });

  private readonly query = toSignal(inject(ActivatedRoute).queryParamMap.pipe(map(p => ({ action: p.get('action'), status: p.get('status') }))), {
    initialValue: { action: null, status: null },
  });

  protected readonly statusMessage = computed(() => {
    const { action, status } = this.query();
    if (!action || !status) return '';
    if (status === 'success') return SUCCESS_MESSAGES[action] ?? 'Done';
    if (status === 'cancelled') return 'Cancelled';
    return 'Something went wrong';
  });

  protected readonly failed = computed(() => {
    const { action, status } = this.query();
    return !!action && !!status && status !== 'success' && status !== 'cancelled';
  });

  protected signOut(): void {
    this.authClient.signOut().subscribe({
      complete: () => this.router.navigateByUrl(APP_ROUTES.home),
      error: () => this.router.navigateByUrl(APP_ROUTES.home),
    });
  }
}
