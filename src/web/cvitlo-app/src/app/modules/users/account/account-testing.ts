import { vi } from 'vitest';
import { of } from 'rxjs';

export function confirmAnswering(answer: boolean) {
  return { ask: vi.fn(() => of(answer)) };
}
