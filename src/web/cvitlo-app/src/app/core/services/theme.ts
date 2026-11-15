import { DOCUMENT } from '@angular/common';
import { effect, inject, Injectable, signal } from '@angular/core';

type Theme = 'dark' | 'light';

// The `cvitlo-theme` cookie is host-only so the Keycloak login pages on the same host read the same choice.
// Cookies are not port-scoped, so in dev localhost:4200 and Keycloak on localhost:8080 share it too.
@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly document = inject(DOCUMENT);
  private readonly key = 'cvitlo-theme';
  private readonly oneYearInSeconds = 31536000;
  private readonly _isDark = signal(this.getInitial());
  readonly isDark = this._isDark.asReadonly();

  constructor() {
    effect(() => {
      this.document.documentElement.classList.toggle('dark', this._isDark());
    });
  }

  toggle(): void {
    const next = !this._isDark();
    this._isDark.set(next);
    this.writeCookie(next ? 'dark' : 'light');
  }

  private getInitial(): boolean {
    const stored = this.readCookie() ?? this.migrateFromStorage();
    if (stored) return stored === 'dark';
    try {
      return this.document.defaultView?.matchMedia('(prefers-color-scheme: dark)').matches ?? false;
    } catch {
      return false;
    }
  }

  private readCookie(): Theme | null {
    try {
      const match = this.document.cookie
        .split(';')
        .map((part) => part.trim())
        .find((part) => part.startsWith(`${this.key}=`));
      return this.asTheme(match?.slice(this.key.length + 1));
    } catch {
      return null;
    }
  }

  // Migrates the old localStorage choice once; the Keycloak login pages cannot read localStorage.
  private migrateFromStorage(): Theme | null {
    try {
      const stored = this.asTheme(localStorage.getItem(this.key));
      if (stored) {
        this.writeCookie(stored);
        localStorage.removeItem(this.key);
      }
      return stored;
    } catch {
      return null;
    }
  }

  private writeCookie(value: Theme): void {
    try {
      let cookie = `${this.key}=${value}; Path=/; SameSite=Lax; Max-Age=${this.oneYearInSeconds}`;
      if (this.document.location.protocol === 'https:') cookie += '; Secure';
      this.document.cookie = cookie;
    } catch {
      // Private mode can block cookies; the theme then just is not remembered.
    }
  }

  private asTheme(value: string | null | undefined): Theme | null {
    return value === 'dark' || value === 'light' ? value : null;
  }
}
