import { ChangeDetectionStrategy, Component } from '@angular/core';
import { Eraser, LUCIDE_ICONS, LucideAngularModule, LucideIconProvider } from 'lucide-angular';

@Component({
  selector: 'app-account-deleted',
  imports: [LucideAngularModule],
  providers: [
    {
      provide: LUCIDE_ICONS,
      multi: true,
      useValue: new LucideIconProvider({ Eraser }),
    },
  ],
  templateUrl: './account-deleted.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    class: 'flex min-h-[calc(100vh-var(--navbar-height))] w-full items-center justify-center px-6 dotted-background',
  },
})
export class AccountDeleted {}
