import { DestroyRef, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormGroup } from '@angular/forms';

// Order is significant: it is the order the parts are joined in.
export type TitleParts = readonly [string | null | undefined, string | null | undefined];

export type TitleFormatter = (parts: TitleParts) => string;

export const firstNonEmptyTitlePart: TitleFormatter = parts =>
  parts.map(part => part?.trim() ?? '').find(part => part.length > 0) ?? '';

// Live display state layered on the store: titles follow every keystroke while
// the save stays debounced. Never written back into ResumesStore.
export class EntryTitles {
  private readonly liveParts = signal<ReadonlyMap<string, TitleParts>>(new Map());
  private readonly focusedIds = signal<ReadonlySet<string>>(new Set());
  private readonly formatters = new Map<string, TitleFormatter>();

  // Plain map on purpose: `resolve` runs from a template expression during change
  // detection and writes here; a signal write there trips the "signal write during
  // render" guard. `resolve` also records store-fallback titles for the focus latch.
  private readonly lastNonEmpty = new Map<string, string>();

  constructor(private readonly destroyRef: DestroyRef) {}

  // Runs from the template (`getForm`), so it only seeds the plain `lastNonEmpty`
  // map; writing the `liveParts` signal here would trip the render guard. Until the
  // first value change `resolve` uses the store fallback the form was built from.
  register(
    id: string,
    form: FormGroup,
    fieldNames: readonly [string, string],
    formatter: TitleFormatter = joinTitleParts,
  ): void {
    const project = (): TitleParts => [form.get(fieldNames[0])?.value, form.get(fieldNames[1])?.value];
    this.formatters.set(id, formatter);

    const initialTitle = formatter(project());
    if (initialTitle) {
      this.lastNonEmpty.set(id, initialTitle);
    }

    form.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      this.setLiveParts(id, project());
    });
  }

  private setLiveParts(id: string, parts: TitleParts): void {
    const title = (this.formatters.get(id) ?? joinTitleParts)(parts);
    if (title) {
      this.lastNonEmpty.set(id, title);
    }
    this.liveParts.update(map => {
      const next = new Map(map);
      next.set(id, parts);
      return next;
    });
  }

  // While `id` is focused an empty title holds the last non-empty one, so editing
  // does not flicker to "Untitled" and back.
  resolve(
    id: string,
    fallback: TitleParts,
    formatter: TitleFormatter = this.formatters.get(id) ?? joinTitleParts,
  ): string {
    const parts = this.liveParts().get(id) ?? fallback;
    const title = formatter(parts);
    if (title) {
      this.lastNonEmpty.set(id, title);
      return title;
    }
    if (this.focusedIds().has(id)) {
      return this.lastNonEmpty.get(id) ?? 'Untitled';
    }
    return 'Untitled';
  }

  onFocusIn(id: string): void {
    this.focusedIds.update(ids => {
      const next = new Set(ids);
      next.add(id);
      return next;
    });
  }

  // Tabbing between the two title fields fires focusout then focusin with a CD pass
  // possibly between; focus moving inside the same wrapper is not a real blur.
  onFocusOut(id: string, event: FocusEvent): void {
    const wrapper = event.currentTarget as HTMLElement | null;
    const related = event.relatedTarget as Node | null;
    if (wrapper && related && wrapper.contains(related)) {
      return;
    }
    this.focusedIds.update(ids => {
      const next = new Set(ids);
      next.delete(id);
      return next;
    });

    // The latch dies with the edit; keeping it would resurrect a cleared title
    // when the user refocuses the empty fields. A non-empty title is re-recorded
    // by the next `resolve`.
    this.lastNonEmpty.delete(id);
  }
}

const joinTitleParts: TitleFormatter = parts =>
  parts
    .map(part => part?.trim() ?? '')
    .filter(part => part.length > 0)
    .join(' at ');
