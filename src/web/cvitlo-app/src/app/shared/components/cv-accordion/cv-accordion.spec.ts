import { Component } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { vi } from 'vitest';
import { CvAccordion } from './cv-accordion';
import { CvAccordionItem } from '../cv-accordion-item/cv-accordion-item';

@Component({
  template: `
    <cv-accordion>
      <cv-accordion-item label="Item A" />
      <cv-accordion-item label="Item B" />
    </cv-accordion>
  `,
  imports: [CvAccordion, CvAccordionItem],
})
class HostSingleOpen {}

@Component({
  template: `
    <cv-accordion [allowMultiple]="true">
      <cv-accordion-item label="Item A" />
      <cv-accordion-item label="Item B" />
    </cv-accordion>
  `,
  imports: [CvAccordion, CvAccordionItem],
})
class HostMultiOpen {}

describe('CvAccordion', () => {
  beforeEach(async () => {
    vi.clearAllMocks();
    await TestBed.configureTestingModule({
      imports: [HostSingleOpen, HostMultiOpen],
    }).compileComponents();
  });

  function items(f: ComponentFixture<unknown>): CvAccordionItem[] {
    return f.debugElement
      .queryAll(By.directive(CvAccordionItem))
      .map(de => de.componentInstance as CvAccordionItem);
  }

  function trigger(f: ComponentFixture<unknown>, index: number): HTMLButtonElement {
    return f.debugElement
      .queryAll(By.directive(CvAccordionItem))[index]
      .query(By.css('button[type="button"]')).nativeElement;
  }

  describe('allowMultiple = false (default)', () => {
    let fixture: ComponentFixture<HostSingleOpen>;

    beforeEach(async () => {
      fixture = TestBed.createComponent(HostSingleOpen);
      fixture.detectChanges();
      await fixture.whenStable();
    });

    it('should create', () => {
      expect(fixture.debugElement.query(By.directive(CvAccordion))).toBeTruthy();
    });

    it('should render two accordion items', () => {
      expect(items(fixture).length).toBe(2);
    });

    it('should close item A when item B is opened', () => {
      const [itemA, itemB] = items(fixture);

      trigger(fixture, 0).click();
      fixture.detectChanges();
      expect(itemA.open()).toBe(true);

      trigger(fixture, 1).click();
      fixture.detectChanges();
      expect(itemB.open()).toBe(true);
      expect(itemA.open()).toBe(false);
    });

    it('should allow closing the open item without opening another', () => {
      const [itemA] = items(fixture);
      trigger(fixture, 0).click();
      fixture.detectChanges();
      trigger(fixture, 0).click();
      fixture.detectChanges();
      expect(itemA.open()).toBe(false);
    });

    it('should close item A when item B is opened externally (not via click)', () => {
      const [itemA, itemB] = items(fixture);

      itemA.open.set(true);
      fixture.detectChanges();
      expect(itemA.open()).toBe(true);

      itemB.open.set(true);
      fixture.detectChanges();
      expect(itemB.open()).toBe(true);
      expect(itemA.open()).toBe(false);
    });
  });

  describe('allowMultiple = true', () => {
    let fixture: ComponentFixture<HostMultiOpen>;

    beforeEach(async () => {
      fixture = TestBed.createComponent(HostMultiOpen);
      fixture.detectChanges();
      await fixture.whenStable();
    });

    it('should keep item A open when item B is opened', () => {
      const [itemA, itemB] = items(fixture);

      trigger(fixture, 0).click();
      fixture.detectChanges();
      trigger(fixture, 1).click();
      fixture.detectChanges();

      expect(itemA.open()).toBe(true);
      expect(itemB.open()).toBe(true);
    });
  });

  describe('item registration', () => {
    let fixture: ComponentFixture<HostSingleOpen>;

    beforeEach(async () => {
      fixture = TestBed.createComponent(HostSingleOpen);
      fixture.detectChanges();
      await fixture.whenStable();
    });

    it('should unregister item on destroy so accordion does not close a destroyed item', () => {
      trigger(fixture, 0).click();
      fixture.detectChanges();

      const [itemA] = items(fixture);
      itemA.ngOnDestroy();

      trigger(fixture, 1).click();
      fixture.detectChanges();

      expect(items(fixture)[1].open()).toBe(true);
    });
  });
});
