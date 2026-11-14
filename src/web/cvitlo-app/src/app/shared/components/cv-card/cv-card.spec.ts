import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CvCard } from './cv-card';

describe('CvCard', () => {
  let component: CvCard;
  let fixture: ComponentFixture<CvCard>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CvCard],
    }).compileComponents();

    fixture = TestBed.createComponent(CvCard);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
