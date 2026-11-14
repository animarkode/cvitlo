import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
  selector: 'cv-form-field',
  imports: [],
  templateUrl: './cv-form-field.html',
  styleUrl: './cv-form-field.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CvFormField {
  label = input('');
  hint = input('');
  inputId = input('');
}
