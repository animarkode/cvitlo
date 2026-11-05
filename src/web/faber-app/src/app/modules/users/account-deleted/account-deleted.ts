import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CircleCheck, LUCIDE_ICONS, LucideAngularModule, LucideIconProvider } from 'lucide-angular';
import { APP_ROUTES } from '../../../core/routes/app-routes';
import { FbButton } from '../../../shared/components/fb-button/fb-button';

@Component({
  selector: 'app-account-deleted',
  imports: [RouterLink, LucideAngularModule, FbButton],
  providers: [
    {
      provide: LUCIDE_ICONS,
      multi: true,
      useValue: new LucideIconProvider({ CircleCheck }),
    },
  ],
  templateUrl: './account-deleted.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    class: 'flex min-h-[calc(100vh-var(--navbar-height))] w-full items-center justify-center px-6 dotted-background',
  },
})
export class AccountDeleted {
  protected readonly home = APP_ROUTES.home;
}
