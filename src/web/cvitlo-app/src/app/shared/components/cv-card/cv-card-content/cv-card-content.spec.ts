import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CvCardContent } from './cv-card-content';

describe('CvCardContent', () => {
  let component: CvCardContent;
  let fixture: ComponentFixture<CvCardContent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CvCardContent],
    }).compileComponents();

    fixture = TestBed.createComponent(CvCardContent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
