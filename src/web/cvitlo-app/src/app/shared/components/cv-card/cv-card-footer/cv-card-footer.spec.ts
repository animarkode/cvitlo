import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CvCardFooter } from './cv-card-footer';

describe('CvCardFooter', () => {
  let component: CvCardFooter;
  let fixture: ComponentFixture<CvCardFooter>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CvCardFooter],
    }).compileComponents();

    fixture = TestBed.createComponent(CvCardFooter);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
