import { FormControl } from '@angular/forms';
import { MatDatepicker } from '@angular/material/datepicker';
import { format, setMonth, setYear } from 'date-fns';

export type DatePrecision = 'day' | 'month' | 'year';

export function setMonthAndYear(
  formControl: FormControl,
  normalized: Date,
  datepicker: MatDatepicker<Date>,
): void {
  const current = formControl.value instanceof Date ? formControl.value : new Date();
  const updated = setYear(setMonth(current, normalized.getMonth()), normalized.getFullYear());
  formControl.setValue(updated);
  datepicker.close();
}

export function setYearOnly(
  formControl: FormControl,
  normalized: Date,
  datepicker: MatDatepicker<Date>,
): void {
  const current = formControl.value instanceof Date ? formControl.value : new Date();
  const updated = setYear(current, normalized.getFullYear());
  formControl.setValue(updated);
  datepicker.close();
}

export function displayFormatFor(precision: DatePrecision): string {
  if (precision === 'year') {
    return 'yyyy';
  }
  if (precision === 'month') {
    return 'MMM, yyyy';
  }
  return 'd MMMM, yyyy';
}

// Parts the precision hides are pinned (day -> 01, month -> 01-01) so the value round-trips to a DateOnly.
export function isoFor(date: Date, precision: DatePrecision): string {
  if (precision === 'year') {
    return format(date, 'yyyy') + '-01-01';
  }
  if (precision === 'month') {
    return format(date, 'yyyy-MM') + '-01';
  }
  return format(date, 'yyyy-MM-dd');
}

// Fresh per picker: the picker rewrites `dateInput` parse/display from displayFormatFor as precision changes.
export function createDatePickerFormats() {
  return {
    parse: { dateInput: 'd MMMM, yyyy' },
    display: {
      dateInput: 'd MMMM, yyyy',
      monthYearLabel: 'MMM yyyy',
      dateA11yLabel: 'd MMMM yyyy',
      monthYearA11yLabel: 'MMMM yyyy',
    },
  };
}

export const BIRTH_DATE_FORMATS = {
  parse: { dateInput: 'd MMMM, yyyy' },
  display: {
    dateInput: 'd MMMM, yyyy',
    monthYearLabel: 'MMM yyyy',
    dateA11yLabel: 'd MMMM yyyy',
    monthYearA11yLabel: 'MMMM yyyy',
  },
};

export const RANGE_MONTH_DATE_FORMATS = {
  parse: { dateInput: 'MMM, yyyy' },
  display: {
    dateInput: 'MMM, yyyy',
    monthYearLabel: 'MMM yyyy',
    dateA11yLabel: 'MMM yyyy',
    monthYearA11yLabel: 'MMMM yyyy',
  },
};
