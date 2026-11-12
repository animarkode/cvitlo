import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { CvAccordionItem } from '../cv-accordion-item/cv-accordion-item';

@Component({
  selector: 'cv-accordion',
  templateUrl: './cv-accordion.html',
  styleUrl: './cv-accordion.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'flex flex-col' },
})
export class CvAccordion {
  readonly allowMultiple = input(false);

  private readonly _items: CvAccordionItem[] = [];

  register(item: CvAccordionItem): void {
    this._items.push(item);
  }

  unregister(item: CvAccordionItem): void {
    const idx = this._items.indexOf(item);
    if (idx !== -1) this._items.splice(idx, 1);
  }

  itemOpened(item: CvAccordionItem): void {
    if (!this.allowMultiple()) {
      this._items.filter(i => i !== item).forEach(i => i.open.set(false));
    }
  }
}
