import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { type VariantProps, cva } from 'class-variance-authority';
import { cn } from '../../utils/cn';

const iconButtonVariants = cva(
  'inline-flex items-center justify-center cursor-pointer transition-all focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-ring',
  {
    variants: {
      intent: {
        default:
          'rounded-[10px] bg-transparent text-muted-foreground hover:bg-input hover:text-foreground',
        ghost:
          'rounded-[9px] bg-transparent text-muted-foreground/70 hover:bg-muted hover:text-foreground',
        surface:
          'rounded-[1.25rem] bg-card text-foreground border-[0.5px] border-line hover:rounded-xl',
        // Mirrors cv-button's `soft-destructive`: colours live in the variant so callers never layer competing
        // `text-*` classes on `default`'s text-muted-foreground. The hover fill is an opaque token pair; an alpha
        // fill composites to plain gray on dark cards.
        destructive:
          'rounded-full bg-transparent text-destructive-text hover:bg-destructive-soft hover:text-destructive-text',
      },
      size: {
        sm: 'h-8 w-8',
        md: 'h-9 w-9',
        lg: 'h-10 w-10',
      },
    },
    defaultVariants: {
      intent: 'default',
      size: 'md',
    },
  }
);

export type IconButtonVariants = VariantProps<typeof iconButtonVariants>;

@Component({
  selector: '[cv-icon-button]',
  templateUrl: './cv-icon-button.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    '[class]': 'classes()',
  },
})
export class CvIconButton {
  readonly intent = input<IconButtonVariants['intent']>('default');
  readonly size = input<IconButtonVariants['size']>('md');
  readonly class = input<string>('');

  protected readonly classes = computed(() =>
    cn(iconButtonVariants({ intent: this.intent(), size: this.size() }), this.class())
  );
}
