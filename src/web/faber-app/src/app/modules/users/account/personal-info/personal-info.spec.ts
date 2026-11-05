import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { PersonalInfo } from './personal-info';
import { AuthStore } from '../../../../core/auth/auth-store';
import { of } from 'rxjs';
import { AccountClient } from '../account-client';
import { AccountConfirm } from '../account-confirm';
import { AccountProfile } from '../account-contracts';
import { API_ROUTES } from '../../../../core/routes/api-routes';
import { expectNoAxeViolations } from '../../../../shared/testing/axe';

const profile: AccountProfile = {
  username: 'ada', email: 'ada@example.com', emailVerified: false, firstName: 'Ada', lastName: 'Lovelace',
  fields: [
    { name: 'username', displayName: '${username}', required: true, readOnly: true, minLength: null, maxLength: null, value: 'ada' },
    { name: 'firstName', displayName: '${firstName}', required: true, readOnly: false, minLength: null, maxLength: 5, value: 'Ada' },
    { name: 'lastName', displayName: '${lastName}', required: true, readOnly: false, minLength: null, maxLength: null, value: 'Lovelace' },
    { name: 'phone', displayName: 'phoneNumber', required: false, readOnly: false, minLength: null, maxLength: null, value: '123' },
  ],
};

describe('PersonalInfo', () => {
  let fixture: ComponentFixture<PersonalInfo>;
  let http: HttpTestingController;
  const auth = { reload: vi.fn() };

  const el = () => fixture.nativeElement as HTMLElement;
  const inputs = () => Array.from(el().querySelectorAll<HTMLInputElement>('input'));

  function type(input: HTMLInputElement, value: string): void {
    input.value = value;
    input.dispatchEvent(new Event('input'));
  }

  beforeEach(async () => {
    auth.reload.mockClear();
    await TestBed.configureTestingModule({
      imports: [PersonalInfo],
      providers: [provideHttpClient(), provideHttpClientTesting(), { provide: AuthStore, useValue: auth }],
    }).compileComponents();
    http = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(PersonalInfo);
    fixture.detectChanges();
    http.expectOne(API_ROUTES.auth.account.profile).flush(profile);
    await fixture.whenStable();
    fixture.detectChanges();
  });

  it('should show the e-mail read-only with its verification hint', () => {
    expect(el().querySelector('[data-test="email"]')!.textContent).toContain('ada@example.com');
    expect(el().querySelector('[data-test="email-verified"]')!.textContent).toContain('Not verified');
    expect(inputs().length).toBe(3);
    expect(inputs().map(i => i.value)).toEqual(['Ada', 'Lovelace', '123']);
    expect(el().textContent).toContain('Phone number');
  });

  it('should navigate to the e-mail action', () => {
    const start = vi.spyOn(TestBed.inject(AccountClient), 'startAction').mockImplementation(() => undefined);

    el().querySelector<HTMLElement>('[data-test="change-email"]')!.click();

    expect(start).toHaveBeenCalledWith('email');
  });

  it('should not submit while a rule from the profile is broken', () => {
    type(inputs()[0], 'Augusta Ada');
    fixture.detectChanges();

    el().querySelector<HTMLElement>('[data-test="save"]')!.click();

    http.expectNone(API_ROUTES.auth.account.profile);
  });

  it('should POST the names and the other editable fields', async () => {
    type(inputs()[0], 'Ada2');
    type(inputs()[2], '456');
    el().querySelector<HTMLElement>('[data-test="save"]')!.click();

    const req = http.expectOne(API_ROUTES.auth.account.profile);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ firstName: 'Ada2', lastName: 'Lovelace', attributes: { phone: '456' } });
    req.flush({ ...profile, firstName: 'Ada2' });
    await fixture.whenStable();
    fixture.detectChanges();

    expect(el().querySelector('[data-test="saved"]')!.textContent).toContain('Details saved');
  });

  it('should reload the signed-in user after a successful save', async () => {
    el().querySelector<HTMLElement>('[data-test="save"]')!.click();
    http.expectOne(API_ROUTES.auth.account.profile).flush(profile);
    await fixture.whenStable();

    expect(auth.reload).toHaveBeenCalledTimes(1);
  });

  it('should not reload the signed-in user when the save fails', async () => {
    el().querySelector<HTMLElement>('[data-test="save"]')!.click();
    http.expectOne(API_ROUTES.auth.account.profile).flush({}, { status: 500, statusText: 'Server Error' });
    await fixture.whenStable();

    expect(auth.reload).not.toHaveBeenCalled();
  });

  it('should show server validation errors beside the field', async () => {
    el().querySelector<HTMLElement>('[data-test="save"]')!.click();

    http.expectOne(API_ROUTES.auth.account.profile).flush(
      { errors: { lastName: ['Last name is not allowed.'] } },
      { status: 400, statusText: 'Bad Request' },
    );
    fixture.detectChanges();

    expect(el().querySelector('[data-test="server-error"]')!.textContent).toContain('Last name is not allowed.');
    expect(el().querySelector('[data-test="save-error"]')).not.toBeNull();
  });

  it('should call the save button Update profile', () => {
    expect(el().querySelector('[data-test="save"]')!.textContent?.trim()).toBe('Update profile');
  });

  it('should show a Verified chip once the e-mail is verified', async () => {
    el().querySelector<HTMLElement>('[data-test="save"]')!.click();
    http.expectOne(API_ROUTES.auth.account.profile).flush({ ...profile, emailVerified: true });
    await fixture.whenStable();
    fixture.detectChanges();

    expect(el().querySelector('[data-test="email-verified"]')!.textContent?.trim()).toBe('Verified');
  });

  function confirmDelete(): void {
    vi.spyOn(TestBed.inject(AccountConfirm), 'ask').mockReturnValue(of(true));
    el().querySelector<HTMLElement>('[data-test="delete-account"]')!.click();
  }

  it('should leave for the home page once the account is deleted', () => {
    const leave = vi.spyOn(TestBed.inject(AccountClient), 'leaveDeletedAccount').mockImplementation(() => undefined);
    confirmDelete();

    const req = http.expectOne(API_ROUTES.auth.account.self);
    expect(req.request.method).toBe('DELETE');
    req.flush(null, { status: 204, statusText: 'No Content' });

    expect(leave).toHaveBeenCalledTimes(1);
  });

  it('should sign in again when the API asks for a recent sign-in', () => {
    const start = vi.spyOn(TestBed.inject(AccountClient), 'startAction').mockImplementation(() => undefined);
    confirmDelete();

    http.expectOne(API_ROUTES.auth.account.self).flush(
      { code: 'Account.ReauthenticationRequired' },
      { status: 403, statusText: 'Forbidden' },
    );

    expect(start).toHaveBeenCalledWith('reauthenticate');
  });

  it('should show a failure when Keycloak is unavailable', () => {
    const start = vi.spyOn(TestBed.inject(AccountClient), 'startAction').mockImplementation(() => undefined);
    confirmDelete();

    http.expectOne(API_ROUTES.auth.account.self).flush({ code: 'Account.Unavailable' }, { status: 502, statusText: 'Bad Gateway' });
    fixture.detectChanges();

    expect(start).not.toHaveBeenCalled();
    expect(el().querySelector('[data-test="delete-error"]')!.textContent).toContain('could not be deleted');
    expect(el().querySelector<HTMLButtonElement>('[data-test="delete-account"]')!.disabled).toBe(false);
  });

  it('should ignore a second click while deleting', () => {
    confirmDelete();
    fixture.detectChanges();

    expect(el().querySelector<HTMLButtonElement>('[data-test="delete-account"]')!.disabled).toBe(true);
    el().querySelector<HTMLElement>('[data-test="delete-account"]')!.click();

    http.expectOne(API_ROUTES.auth.account.self);
  });

  it('should stay put when deleting is declined', () => {
    vi.spyOn(TestBed.inject(AccountConfirm), 'ask').mockReturnValue(of(false));

    el().querySelector<HTMLElement>('[data-test="delete-account"]')!.click();

    http.expectNone(API_ROUTES.auth.account.self);
  });

  it('should have no AXE violations', async () => {
    await expectNoAxeViolations(fixture);
  }, 20000);
});
