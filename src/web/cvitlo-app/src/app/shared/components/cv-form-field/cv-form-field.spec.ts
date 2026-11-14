import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CvFormField } from './cv-form-field';

describe('CvFormField', () => {
  let component: CvFormField;
  let fixture: ComponentFixture<CvFormField>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CvFormField],
    }).compileComponents();

    fixture = TestBed.createComponent(CvFormField);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
