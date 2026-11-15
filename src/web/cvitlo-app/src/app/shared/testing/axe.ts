import { ComponentFixture } from '@angular/core/testing';
import axe from 'axe-core';
import { expect } from 'vitest';

// Attached to `document.body` so axe evaluates it in document context. color-contrast is off because
// jsdom cannot compute colour; contrast is checked in contrast.spec.ts.
export async function expectNoAxeViolations(
  fixture: ComponentFixture<unknown>,
): Promise<void> {
  const root = fixture.nativeElement as HTMLElement;
  document.body.appendChild(root);
  try {
    const results = await axe.run(root, {
      runOnly: { type: 'tag', values: ['wcag2a', 'wcag2aa'] },
      rules: { 'color-contrast': { enabled: false } },
    });
    const summary = results.violations
      .map(v => `${v.id}: ${v.help} (${v.nodes.length} node(s))`)
      .join('\n');
    expect(results.violations, `AXE violations:\n${summary}`).toEqual([]);
  } finally {
    root.remove();
  }
}
