import { ComponentFixture, TestBed } from '@angular/core/testing';
import { CvInitials } from './cv-initials';
import { expectNoAxeViolations } from '../../testing/axe';

describe('CvInitials', () => {
  let component: CvInitials;
  let fixture: ComponentFixture<CvInitials>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CvInitials],
    }).compileComponents();

    fixture = TestBed.createComponent(CvInitials);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('firstName', 'John');
    fixture.componentRef.setInput('lastName', 'Doe');
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should reshape on hover by default', () => {
    const tile = (fixture.nativeElement as HTMLElement).querySelector('div')!;
    expect(tile.classList).toContain('hover:rounded-xl');
  });

  it('should not reshape on hover when hoverable is false', () => {
    fixture.componentRef.setInput('hoverable', false);
    fixture.detectChanges();
    const tile = (fixture.nativeElement as HTMLElement).querySelector('div')!;
    expect(tile.classList).not.toContain('hover:rounded-xl');
  });

  it('should be 40px by default and 48px at size lg', () => {
    const tile = (fixture.nativeElement as HTMLElement).querySelector('div')!;
    expect(tile.classList).toContain('size-10');
    fixture.componentRef.setInput('size', 'lg');
    fixture.detectChanges();
    expect(tile.classList).toContain('size-12');
  });

  it('should compute firstChar as uppercase first letter of firstName', () => {
    expect(component.firstChar()).toBe('J');
  });

  it('should compute lastChar as uppercase first letter of lastName', () => {
    expect(component.lastChar()).toBe('D');
  });

  it('should uppercase firstChar when firstName starts with lowercase', () => {
    fixture.componentRef.setInput('firstName', 'alice');
    expect(component.firstChar()).toBe('A');
  });

  it('should uppercase lastChar when lastName starts with lowercase', () => {
    fixture.componentRef.setInput('lastName', 'smith');
    expect(component.lastChar()).toBe('S');
  });

  it('should show nothing for a name the account does not have', () => {
    fixture.componentRef.setInput('firstName', null);
    fixture.componentRef.setInput('lastName', null);

    expect(component.firstChar()).toBe('');
    expect(component.lastChar()).toBe('');
  });

  it('should update firstChar when firstName input changes', () => {
    fixture.componentRef.setInput('firstName', 'Maria');
    expect(component.firstChar()).toBe('M');
  });

  // jsdom can't evaluate colour, so this asserts structure/ARIA only; it also
  // exercises the .dark code path. Real contrast is verified in the browser.
  it('should have no AXE violations (incl. dark mode)', async () => {
    document.documentElement.classList.add('dark');
    try {
      await expectNoAxeViolations(fixture);
    } finally {
      document.documentElement.classList.remove('dark');
    }
  }, 20000);
});
