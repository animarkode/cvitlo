import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import { CvButton } from '../cv-button/cv-button';

export interface ConfirmDialogData {
  title: string;
  message: string;
  confirmLabel?: string;
}

@Component({
  selector: 'cv-confirm-dialog',
  imports: [CvButton],
  templateUrl: './cv-confirm-dialog.html',
  styleUrl: './cv-confirm-dialog.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CvConfirmDialog {
  protected readonly data = inject<ConfirmDialogData>(DIALOG_DATA);
  private readonly dialogRef = inject(DialogRef<boolean>);

  confirm(): void {
    this.dialogRef.close(true);
  }

  cancel(): void {
    this.dialogRef.close(false);
  }
}
