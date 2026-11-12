import { afterNextRender, ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { LucideAngularModule, LucideIconProvider, LUCIDE_ICONS, Moon, Sun } from 'lucide-angular';
import { CvIconButton } from '../cv-icon-button/cv-icon-button';
import { ThemeService } from '../../../core/services/theme';

@Component({
  selector: 'cv-theme-toggle',
  imports: [LucideAngularModule, CvIconButton],
  providers: [
    {
      provide: LUCIDE_ICONS,
      multi: true,
      useValue: new LucideIconProvider({ Moon, Sun }),
    },
  ],
  templateUrl: './cv-theme-toggle.html',
  styleUrl: './cv-theme-toggle.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CvThemeToggle {
  protected readonly themeService = inject(ThemeService);
  protected readonly animationsReady = signal(false);

  constructor() {
    afterNextRender(() => this.animationsReady.set(true));
  }
}
