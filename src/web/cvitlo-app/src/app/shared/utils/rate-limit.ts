import { HttpErrorResponse } from '@angular/common/http';
import { MonoTypeOperatorFunction, throwError, timer } from 'rxjs';
import { retry } from 'rxjs/operators';

const FALLBACK_RETRY_AFTER_MS = 2000;

// A large Retry-After must not stall the UI.
const MAX_RETRY_AFTER_MS = 10000;

const MAX_RATE_LIMIT_RETRIES = 2;

// Jitter desynchronises tabs throttled together, which would otherwise retry in lockstep and hit the next 429 together.
const MAX_RETRY_JITTER_MS = 500;

// The 429 application/problem+json built by `RateLimitRejection.Problem` on the API.
export function isRateLimited(error: unknown): error is HttpErrorResponse {
  return error instanceof HttpErrorResponse && error.status === 429;
}

// Retry-After is whole seconds; clamped so a hostile or misconfigured value cannot freeze the caller.
export function retryAfterMs(error: HttpErrorResponse): number {
  const header = error.headers.get('Retry-After');
  const seconds = Number(header);

  if (header === null || !Number.isFinite(seconds) || seconds <= 0) {
    return FALLBACK_RETRY_AFTER_MS;
  }

  return Math.min(seconds * 1000, MAX_RETRY_AFTER_MS);
}

// A 429 is rejected before the request runs, so retrying is safe even for non-idempotent creates.
export function retryOnRateLimit<T>(): MonoTypeOperatorFunction<T> {
  return retry<T>({
    count: MAX_RATE_LIMIT_RETRIES,
    delay: (error: unknown) =>
      isRateLimited(error)
        ? timer(retryAfterMs(error) + Math.random() * MAX_RETRY_JITTER_MS)
        : throwError(() => error),
  });
}
