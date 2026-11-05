import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { expectNoAxeViolations } from '../../../shared/testing/axe';
import { AccountDeleted } from './account-deleted';

describe('AccountDeleted', () => {
  let fixture: ComponentFixture<AccountDeleted>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AccountDeleted],
      providers: [provideRouter([])],
    }).compileComponents();

    fixture = TestBed.createComponent(AccountDeleted);
    await fixture.whenStable();
  });

  it('should say the account is deleted', () => {
    const heading = (fixture.nativeElement as HTMLElement).querySelector('h1');
    expect(heading?.textContent).toContain('Your account has been deleted');
  });

  it('should lead back to the home page', () => {
    const link = (fixture.nativeElement as HTMLElement).querySelector('a');
    expect(link?.getAttribute('href')).toBe('/');
  });

  it('should have no AXE violations', async () => {
    await expectNoAxeViolations(fixture);
  }, 20_000);
});
