import { TestBed } from '@angular/core/testing';
import { provideRouter, Router, UrlTree } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { Component } from '@angular/core';
import { accountDeletedGuard } from './account-deleted-guard';
import { rememberAccountDeleted } from './account-deleted-flag';

@Component({ template: 'deleted' })
class DeletedPage {}

@Component({ template: 'home' })
class HomePage {}

describe('accountDeletedGuard', () => {
  beforeEach(() => {
    sessionStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        provideRouter([
          { path: '', component: HomePage },
          { path: 'account-deleted', component: DeletedPage, canActivate: [accountDeletedGuard] },
        ]),
      ],
    });
  });

  it('should show the page right after the deletion', async () => {
    rememberAccountDeleted(sessionStorage);

    const harness = await RouterTestingHarness.create('/account-deleted');

    expect(TestBed.inject(Router).url).toBe('/account-deleted');
    expect(harness.routeNativeElement?.textContent).toContain('deleted');
  });

  it('should send a direct visit home', async () => {
    await RouterTestingHarness.create('/account-deleted');

    expect(TestBed.inject(Router).url).toBe('/');
  });

  it('should show the page only once', async () => {
    rememberAccountDeleted(sessionStorage);
    const harness = await RouterTestingHarness.create('/account-deleted');
    await harness.navigateByUrl('/');

    await harness.navigateByUrl('/account-deleted');

    expect(TestBed.inject(Router).url).toBe('/');
  });

  it('should send home when storage is blocked', () => {
    vi.spyOn(Storage.prototype, 'getItem').mockImplementation(() => {
      throw new Error('blocked');
    });

    const result = TestBed.runInInjectionContext(() =>
      accountDeletedGuard(undefined as never, undefined as never),
    );

    expect(result instanceof UrlTree && result.toString()).toBe('/');
    vi.restoreAllMocks();
  });
});
