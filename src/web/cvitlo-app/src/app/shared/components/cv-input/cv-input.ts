import {
  ChangeDetectionStrategy,
  Component,
  computed,
  DestroyRef,
  inject,
  input,
  OnInit,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ControlValueAccessor, NgControl } from '@angular/forms';
import { Eye, EyeOff, Lock, LockKeyhole, LUCIDE_ICONS, LucideAngularModule, LucideIconProvider, Mail, User } from 'lucide-angular';

@Component({
  selector: 'cv-input',
  templateUrl: './cv-input.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [LucideAngularModule],
  providers: [{ provide: LUCIDE_ICONS, multi: true, useValue: new LucideIconProvider({ Eye, EyeOff, Lock, LockKeyhole, Mail, User }) }],
})
export class CvInput implements ControlValueAccessor, OnInit {
  readonly label = input('');
  readonly type = input('text');
  readonly placeholder = input('');
  readonly autocomplete = input('off');
  readonly showToggle = input(false);
  readonly icon = input<'user' | 'email' | 'password' | 'lock' | null>(null);
  /** `stacked`: label above the field. `inline`: label in a fixed column to the left, as in a settings row. */
  readonly layout = input<'stacked' | 'inline'>('stacked');

  readonly focused = signal(false);
  readonly showPassword = signal(false);
  protected readonly value = signal('');

  private readonly destroyRef = inject(DestroyRef);
  private readonly ngControl = inject(NgControl, { self: true, optional: true });

  private readonly controlTick = signal(0);

  readonly hasError = computed(() => {
    this.controlTick();
    const ctrl = this.ngControl?.control;
    return !!(ctrl?.invalid && ctrl.touched);
  });

  readonly errorMessage = computed(() => {
    this.controlTick();
    const ctrl = this.ngControl?.control;
    if (!ctrl?.touched) return null;
    if (ctrl.hasError('required')) return `${this.label() || 'This field'} is required.`;
    if (ctrl.hasError('minlength')) {
      const req = (ctrl.getError('minlength') as { requiredLength: number }).requiredLength;
      return `Must be at least ${req} characters.`;
    }
    if (ctrl.hasError('email')) return 'Must be a valid email.';
    if (ctrl.hasError('mismatch')) return 'Passwords do not match.';
    return null;
  });

  readonly inputType = computed(() =>
    this.type() === 'password' && this.showPassword() ? 'text' : this.type(),
  );

  protected readonly labelClasses = computed(() => {
    const tone = this.hasError() ? 'text-destructive-text' : 'text-muted-foreground';
    const size = this.layout() === 'inline' ? 'basis-28 shrink-0 text-sm' : 'text-[0.8rem]';
    return `${tone} ${size}`;
  });

  protected readonly rootClasses = computed(() =>
    this.layout() === 'inline' ? 'flex flex-wrap items-center gap-x-4 gap-y-1.5' : 'flex flex-col gap-1.5',
  );

  // The outline is the field's boundary, so it keeps 3:1 against the card (WCAG 1.4.11); focus thickens it.
  protected readonly wrapClasses = computed(() => {
    const size = this.layout() === 'inline' ? 'grow shrink basis-60 max-w-90' : '';
    return `${this.outlineClass()} ${size}`;
  });

  private readonly outlineClass = computed(() => {
    if (this.hasError()) return 'shadow-[inset_0_0_0_1px_var(--color-destructive-text)]';
    if (this.focused()) return 'shadow-[inset_0_0_0_2px_var(--color-provider-border)]';
    return 'shadow-[inset_0_0_0_1px_var(--color-provider-border)]';
  });

  protected readonly iconClasses = computed(() => {
    if (this.hasError()) return 'text-destructive-text';
    if (this.focused()) return 'text-muted-foreground';
    return 'text-muted-foreground/70';
  });

  protected readonly lucideIconName = computed<string | null>(() => {
    const ic = this.icon();
    if (!ic) return null;
    const map: Record<string, string> = { user: 'user', email: 'mail', password: 'lock-keyhole', lock: 'lock' };
    return map[ic] ?? null;
  });

  readonly inputId = `cv-input-${Math.random().toString(36).slice(2, 9)}`;
  readonly errorId = `${this.inputId}-error`;

  private onChange: (v: string) => void = () => {};
  private onTouched: () => void = () => {};

  constructor() {
    if (this.ngControl) {
      this.ngControl.valueAccessor = this;
    }
  }

  ngOnInit(): void {
    this.trackControlEvents();
  }

  private trackControlEvents(): void {
    const ctrl = this.ngControl?.control;
    if (!ctrl) return;
    ctrl.events
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.controlTick.update(n => n + 1));
  }

  writeValue(val: string): void {
    this.value.set(val ?? '');
  }

  registerOnChange(fn: (v: string) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  onInput(event: Event): void {
    const val = (event.target as HTMLInputElement).value;
    this.value.set(val);
    this.onChange(val);
  }

  onBlur(): void {
    this.focused.set(false);
    this.onTouched();
  }

  onFocus(): void {
    this.focused.set(true);
  }

  togglePasswordVisibility(): void {
    this.showPassword.update(v => !v);
  }
}
