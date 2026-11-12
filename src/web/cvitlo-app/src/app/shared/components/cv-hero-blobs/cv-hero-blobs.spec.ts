import { TestBed } from '@angular/core/testing';
import { CvHeroBlobs } from './cv-hero-blobs';

describe('CvHeroBlobs', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CvHeroBlobs],
    }).compileComponents();
  });

  it('should create', () => {
    const fixture = TestBed.createComponent(CvHeroBlobs);
    expect(fixture.componentInstance).toBeTruthy();
  });
});
