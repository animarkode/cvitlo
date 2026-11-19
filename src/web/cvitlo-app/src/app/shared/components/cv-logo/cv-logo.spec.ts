import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CvLogo } from './cv-logo';

describe('CvLogo', () => {
  let fixture: ComponentFixture<CvLogo>;
  let host: HTMLElement;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CvLogo],
    }).compileComponents();

    fixture = TestBed.createComponent(CvLogo);
    host = fixture.nativeElement as HTMLElement;
    await fixture.whenStable();
  });

  it('should render the lowercase wordmark', () => {
    expect(host.textContent?.trim()).toBe('cvitlo');
  });

  it('should hide the decorative mark from assistive tech', () => {
    expect(host.querySelector('svg')?.getAttribute('aria-hidden')).toBe('true');
  });

  it('should default the host font size to 28px', () => {
    expect(host.style.fontSize).toBe('28px');
  });

  it('should follow the size input', async () => {
    fixture.componentRef.setInput('size', 20);
    await fixture.whenStable();

    expect(host.style.fontSize).toBe('20px');
  });
});
