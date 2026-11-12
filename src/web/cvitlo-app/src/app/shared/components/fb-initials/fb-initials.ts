import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

@Component({
  selector: 'fb-initials',
  templateUrl: './fb-initials.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FbInitials {
  // Nullable: a Keycloak account (e.g. a GitHub sign-in without a public name) may have no name.
  firstName = input.required<string | null>();
  lastName = input.required<string | null>();
  /** Off where the avatar is decoration only (not a button), so it does not hint it can be clicked. */
  hoverable = input(true);
  /** md (40px) for toolbars; lg (48px) beside a two-line name block. */
  size = input<'md' | 'lg'>('md');
  // Radius is half the side, so the tile is a circle at rest.
  tileClasses = computed(() => (this.size() === 'lg' ? 'size-12 rounded-3xl' : 'size-10 rounded-[20px]'));
  textClasses = computed(() => (this.size() === 'lg' ? 'text-xl' : 'text-lg'));
  firstChar = computed(() => (this.firstName() ?? '').charAt(0).toUpperCase());
  lastChar = computed(() => (this.lastName() ?? '').charAt(0).toUpperCase());
}
