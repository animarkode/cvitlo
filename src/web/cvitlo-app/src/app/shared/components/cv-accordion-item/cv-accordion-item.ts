import {
  booleanAttribute,
  ChangeDetectionStrategy,
  Component,
  effect,
  ElementRef,
  inject,
  input,
  model,
  OnDestroy,
  OnInit,
  viewChild,
} from '@angular/core';
import { LucideAngularModule } from 'lucide-angular';
import { CvAccordion } from '../cv-accordion/cv-accordion';

@Component({
  selector: 'cv-accordion-item',
  imports: [LucideAngularModule],
  templateUrl: './cv-accordion-item.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    class: 'block shrink-0',
    '[class.open]': 'open()',
  },
})
export class CvAccordionItem implements OnInit, OnDestroy {
  readonly label = input.required<string>();
  readonly icon = input<string>();
  readonly count = input<number>();
  readonly open = model(false);
  /** Render the chevron's hover background as a circle instead of a rounded square. */
  readonly roundChevron = input(false, { transform: booleanAttribute });

  private readonly accordion = inject(CvAccordion, { optional: true });
  private readonly body = viewChild.required<ElementRef<HTMLElement>>('body');

  protected readonly triggerId = `cv-ai-t-${Math.random().toString(36).slice(2, 9)}`;
  protected readonly bodyId = `cv-ai-b-${Math.random().toString(36).slice(2, 9)}`;

  constructor() {
    // Single source of truth for single-open enforcement: any way of opening this item asks the
    // accordion to close its siblings. It writes only OTHER items, so it never re-triggers itself.
    effect(() => {
      if (this.open()) {
        this.accordion?.itemOpened(this);
      }
    });
  }

  ngOnInit(): void {
    this.accordion?.register(this);
  }

  ngOnDestroy(): void {
    this.accordion?.unregister(this);
  }

  protected toggle(): void {
    this.body().nativeElement.style.removeProperty('transition');
    this.open.set(!this.open());
  }

  // Collapses synchronously on drag grab: CDK measures the boundary on the next pointermove and zoneless
  // change detection may not have flushed by then, so the closed state is written straight to the DOM.
  collapseForDrag(): void {
    const el = this.body().nativeElement;
    el.style.transition = 'none';
    el.style.gridTemplateRows = '0fr';
    this.open.set(false);
  }
}
