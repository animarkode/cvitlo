import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { of } from 'rxjs';

import { Account } from './account';
import { AuthClient } from '../../../core/auth/auth-client';
import { AuthStore } from '../../../core/auth/auth-store';
import { API_ROUTES } from '../../../core/routes/api-routes';
import { ACCOUNT_SECTIONS } from '../../../core/routes/app-routes';
import { expectNoAxeViolations } from '../../../shared/testing/axe';

@Component({ selector: 'app-section-stub', template: '<h1>Section</h1>' })
class SectionStub {}

const user = { id: '1', email: 'ada@example.com', firstName: 'Ada', lastName: 'Lovelace', name: 'Ada Lovelace', roles: [] };

describe('Account', () => {
  let harness: RouterTestingHarness;
  const signOut = vi.fn(() => of(undefined));

  const el = () => harness.routeNativeElement as HTMLElement;
  const status = () => el().querySelector('[data-test="status"]')!;
  const links = () => Array.from(el().querySelectorAll<HTMLAnchorElement>('[data-test="section-link"]'));

  async function open(url: string): Promise<void> {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([
          {
            path: 'account',
            component: Account,
            children: Object.values(ACCOUNT_SECTIONS).map(path => ({ path, component: SectionStub })),
          },
          { path: '', component: SectionStub },
        ]),
        { provide: AuthStore, useValue: { user: () => user } },
        { provide: AuthClient, useValue: { signOut } },
      ],
    });
    harness = await RouterTestingHarness.create(url);
    TestBed.inject(HttpTestingController).expectOne(API_ROUTES.auth.account.devices).flush([{ id: 'a' }, { id: 'b' }, { id: 'c' }]);
    await harness.fixture.whenStable();
    harness.detectChanges();
  }

  it('should show who is signed in above the menu', async () => {
    await open('/account/personal-info');

    expect(el().querySelector('[data-test="user-name"]')!.textContent).toContain('Ada Lovelace');
    expect(el().textContent).toContain('Your personal account');
  });

  it('should link every section and mark the open one as the current page', async () => {
    await open('/account/devices');

    expect(links().map(a => a.getAttribute('href'))).toEqual([
      '/account/personal-info',
      '/account/signing-in',
      '/account/devices',
      '/account/linked-accounts',
    ]);
    expect(links().map(a => a.getAttribute('aria-current'))).toEqual([null, null, 'page', null]);
    expect(el().querySelector('app-section-stub')).not.toBeNull();
  });

  it('should count the signed-in devices next to Device activity', async () => {
    await open('/account/personal-info');

    expect(el().querySelector('[data-test="device-count"]')!.textContent?.trim()).toBe('3');
  });

  it('should announce nothing without a returned action', async () => {
    await open('/account/personal-info');

    expect(status().getAttribute('role')).toBe('status');
    expect(status().textContent?.trim()).toBe('');
  });

  it.each([
    ['/account/signing-in?action=password&status=success', 'Password updated'],
    ['/account/signing-in?action=passkey&status=cancelled', 'Cancelled'],
    ['/account/signing-in?action=totp&status=error', 'Something went wrong'],
  ])('should show the outcome of %s', async (url, message) => {
    await open(url);

    expect(status().textContent).toContain(message);
  });

  it('should sign out and go home', async () => {
    await open('/account/personal-info');

    el().querySelector<HTMLElement>('[data-test="sign-out"]')!.click();
    await harness.fixture.whenStable();

    expect(signOut).toHaveBeenCalled();
    expect(TestBed.inject(Router).url).toBe('/');
  });

  it('should have no AXE violations', async () => {
    await open('/account/personal-info');

    await expectNoAxeViolations(harness.fixture);
  }, 20000);
});
