import { ChangeDetectionStrategy, Component, input } from '@angular/core';

type Size = 'small' | 'medium' | 'large';

@Component({
  selector: 'cv-spinner',
  templateUrl: './cv-spinner.html',
  styleUrl: './cv-spinner.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CvSpinner {
  size = input<Size>('medium');
}
