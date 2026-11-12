import {
  ChangeDetectionStrategy,
  Component,
  computed,
  contentChild,
  input,
  output,
  TemplateRef,
} from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import {
  GripVertical,
  LUCIDE_ICONS,
  LucideAngularModule,
  LucideIconProvider,
  Plus,
  Trash2,
} from 'lucide-angular';
import { CdkDrag, CdkDragDrop, CdkDragHandle, CdkDropList } from '@angular/cdk/drag-drop';
import { CvIconButton } from '../../../../shared/components/cv-icon-button/cv-icon-button';
import { CvAccordion } from '../../../../shared/components/cv-accordion/cv-accordion';
import { CvAccordionItem } from '../../../../shared/components/cv-accordion-item/cv-accordion-item';
import { Orderly } from '../../orderly';

@Component({
  selector: 'app-orderable-list',
  imports: [
    NgTemplateOutlet,
    CvIconButton,
    LucideAngularModule,
    CdkDropList,
    CdkDrag,
    CdkDragHandle,
    CvAccordion,
    CvAccordionItem,
  ],
  providers: [
    { provide: LUCIDE_ICONS, multi: true, useValue: new LucideIconProvider({ Plus, GripVertical, Trash2 }) },
  ],
  templateUrl: './orderable-list.html',
  styleUrl: './orderable-list.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class OrderableList<T extends Orderly = Orderly> {
  items = input.required<T[]>();
  title = input.required<string>();
  hideTitle = input(false);
  titleFn = input.required<(item: T) => string>();
  addLabel = input<string>();

  itemTemplate = contentChild.required(TemplateRef);

  protected readonly resolvedAddLabel = computed(() => this.addLabel() ?? `Add ${this.title()}`);

  add = output<void>();
  delete = output<Orderly>();
  itemDrop = output<CdkDragDrop<Orderly[]>>();

  protected onAdd(): void {
    this.add.emit();
  }

  protected onDelete(item: T, event: Event): void {
    event.stopPropagation();
    this.delete.emit(item);
  }

  protected onDrop(event: CdkDragDrop<T[]>): void {
    this.itemDrop.emit(event as unknown as CdkDragDrop<Orderly[]>);
  }
}
