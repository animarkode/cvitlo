import { inject, Injectable } from '@angular/core';
import { Dialog } from '@angular/cdk/dialog';
import { map, Observable } from 'rxjs';
import {
  ConfirmDialogData,
  CvConfirmDialog,
} from '../../../shared/components/cv-confirm-dialog/cv-confirm-dialog';

/** Asks the person to confirm a destructive account change; emits `true` only on an explicit confirm. */
@Injectable({ providedIn: 'root' })
export class AccountConfirm {
  private readonly dialog = inject(Dialog);

  ask(data: ConfirmDialogData): Observable<boolean> {
    return this.dialog
      .open<boolean>(CvConfirmDialog, {
        data,
        panelClass: 'cv-dialog-panel',
        backdropClass: 'cv-dialog-backdrop',
      })
      .closed.pipe(map(confirmed => confirmed === true));
  }
}
