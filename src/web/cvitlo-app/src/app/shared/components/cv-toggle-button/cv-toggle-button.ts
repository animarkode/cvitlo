import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

@Component({
  selector: 'cv-toggle-button',
  templateUrl: './cv-toggle-button.html',
  styleUrl: './cv-toggle-button.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CvToggleButton {
  isOpen = input(false);
  clicked = output<void>();

  protected readonly stateClasses = computed(() =>
    this.isOpen()
      ? 'rounded-[20px] bg-primary text-primary-foreground'
      : 'rounded-[20px] bg-foreground text-card dark:bg-surface dark:text-foreground',
  );

  onButtonClick() {
    this.clicked.emit();
  }
}
