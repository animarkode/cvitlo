import {
  afterNextRender,
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  input,
  signal,
} from '@angular/core';
import { LucideAngularModule } from 'lucide-angular';
import { ResumesStore } from '../../resumes-store';
import { Localization } from '../localization/localization';

export type ActionsOrientation = 'vertical' | 'horizontal';

@Component({
  selector: 'app-builder-actions',
  imports: [LucideAngularModule, Localization],
  templateUrl: './builder-actions.html',
  styleUrl: './builder-actions.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    '[class]': 'hostClasses()',
    role: 'group',
    'aria-label': 'Resume actions',
  },
})
export class BuilderActions {
  protected readonly store = inject(ResumesStore);

  readonly orientation = input<ActionsOrientation>('vertical');

  // The dropdown opens away from the island: left beside the preview, up from the bottom bar.
  protected readonly menuPlacement = computed(() =>
    this.orientation() === 'vertical' ? ('left' as const) : ('top' as const),
  );

  protected readonly hostClasses = computed(() =>
    this.orientation() === 'vertical'
      ? 'self-center flex flex-col items-center gap-1.5 shrink-0 bg-card border-[0.5px] border-line rounded-full p-1.5'
      : 'flex flex-row items-center gap-1 shrink-0',
  );

  // Gates the save animation until after first render, so a resume that mounts already
  // 'saved' does not play the success animation. Mirrors cv-theme-toggle's guard.
  protected readonly animationsReady = signal(false);

  // Empty for 'idle' and 'saving' so only the terminal state is announced, once per transition.
  protected readonly saveAnnouncement = computed(() => {
    switch (this.store.saveStatus()) {
      case 'saved':
        return 'Saved';
      case 'error':
        return this.store.syncErrorKind() === 'preview' ? 'Sync failed' : 'Save failed';
      default:
        return '';
    }
  });

  protected readonly errorLabel = computed(() =>
    this.store.syncErrorKind() === 'preview' ? 'Sync failed' : 'Save failed',
  );

  constructor() {
    afterNextRender(() => this.animationsReady.set(true));
  }
}
