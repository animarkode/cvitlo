import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CvCardHeader } from './cv-card-header';

describe('CvCardHeader', () => {
  let component: CvCardHeader;
  let fixture: ComponentFixture<CvCardHeader>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CvCardHeader],
    }).compileComponents();

    fixture = TestBed.createComponent(CvCardHeader);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
