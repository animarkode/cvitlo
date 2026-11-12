import { ChangeDetectionStrategy, Component, DestroyRef, effect, inject } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ResumesStore } from '../../resumes-store';
import { CvField } from '../../../../shared/components/cv-field/cv-field';
import { CvDatePicker } from '../../../../shared/components/cv-date-picker/cv-date-picker';

@Component({
  selector: 'app-person',
  imports: [ReactiveFormsModule, CvField, CvDatePicker],
  templateUrl: './person.html',
  styleUrl: './person.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Person {
  private readonly fb = inject(NonNullableFormBuilder);
  private readonly destroyRef = inject(DestroyRef);
  protected readonly store = inject(ResumesStore);

  protected readonly form = this.fb.group({
    jobTitle: [''],
    firstname: [''],
    lastname: [''],
    email: [''],
    phone: [''],
    country: [''],
    city: [''],
    street: [''],
    postCode: [''],
    nationality: [''],
    dateOfBirth: [''],
    drivingLicense: [''],
  });

  constructor() {
    effect(() => {
      const person = this.store.person();
      if (person) {
        this.form.patchValue(person, { emitEvent: false });
      }
    });

    this.form.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.store.updatePerson(this.form.getRawValue()));
  }
}
