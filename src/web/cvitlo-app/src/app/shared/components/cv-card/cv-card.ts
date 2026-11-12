import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { cn } from '../../utils/cn';

@Component({
  selector: 'cv-card',
  templateUrl: './cv-card.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    '[class]': 'classes()',
  },
})
export class CvCard {
  readonly class = input<string>('');

  protected readonly classes = computed(() =>
    cn('rounded-2xl bg-card text-card-foreground shadow-sm border-[0.5px] border-line', this.class())
  );
}
