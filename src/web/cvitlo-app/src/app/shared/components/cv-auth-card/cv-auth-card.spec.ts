import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CvAuthCard } from './cv-auth-card';

describe('CvAuthCard', () => {
  let component: CvAuthCard;
  let fixture: ComponentFixture<CvAuthCard>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CvAuthCard],
    }).compileComponents();

    fixture = TestBed.createComponent(CvAuthCard);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('has no card fill of its own', () => {
    const classes = (fixture.nativeElement as HTMLElement).className;

    expect(classes).not.toContain('bg-card');
  });

  it('fills its fields with the card colour so they stand out from the page', () => {
    const classes = (fixture.nativeElement as HTMLElement).className;

    expect(classes).toContain('[--color-input:var(--color-card)]');
  });
});
