import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CvCardTitle } from './cv-card-title';

describe('CvCardTitle', () => {
  let component: CvCardTitle;
  let fixture: ComponentFixture<CvCardTitle>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CvCardTitle],
    }).compileComponents();

    fixture = TestBed.createComponent(CvCardTitle);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
