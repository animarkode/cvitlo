import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { cn } from '../../../utils/cn';

@Component({
  selector: 'cv-card-content',
  templateUrl: './cv-card-content.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    '[class]': 'classes()',
  },
})
export class CvCardContent {
  readonly class = input<string>('');

  protected readonly classes = computed(() =>
    cn('p-6 pt-0', this.class())
  );
}
