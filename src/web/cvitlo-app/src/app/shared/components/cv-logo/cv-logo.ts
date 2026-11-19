import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
  selector: 'cv-logo',
  templateUrl: './cv-logo.html',
  styleUrl: './cv-logo.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { '[style.font-size.px]': 'size()' },
})
export class CvLogo {
  /** Font size of the wordmark in px. */
  size = input(28);
}
