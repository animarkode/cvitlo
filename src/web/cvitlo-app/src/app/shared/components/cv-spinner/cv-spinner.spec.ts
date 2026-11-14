import { ComponentFixture, TestBed } from '@angular/core/testing';
import { CvSpinner } from './cv-spinner';

describe('CvSpinner', () => {
  let component: CvSpinner;
  let fixture: ComponentFixture<CvSpinner>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CvSpinner],
    }).compileComponents();

    fixture = TestBed.createComponent(CvSpinner);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should default size to "medium"', () => {
    expect(component.size()).toBe('medium');
  });

  it('should accept "small" size input', () => {
    fixture.componentRef.setInput('size', 'small');
    expect(component.size()).toBe('small');
  });

  it('should accept "large" size input', () => {
    fixture.componentRef.setInput('size', 'large');
    expect(component.size()).toBe('large');
  });
});
