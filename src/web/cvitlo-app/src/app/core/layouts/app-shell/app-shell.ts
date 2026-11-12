import { ChangeDetectionStrategy, Component, DestroyRef, ElementRef, inject, OnInit, signal, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterOutlet } from '@angular/router';
import { filter } from 'rxjs';
import { AppNavbar } from '../app-navbar/app-navbar';

/** How far the content scrolls before the navbar turns to glass. */
const SCROLLED_THRESHOLD_PX = 10;

@Component({
  selector: 'app-shell',
  imports: [RouterOutlet, AppNavbar],
  templateUrl: './app-shell.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'block h-full' },
})
export class AppShell implements OnInit {
  protected readonly scrolled = signal(false);

  private readonly content = viewChild.required<ElementRef<HTMLElement>>('content');
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  ngOnInit(): void {
    this.scrollToTopOnNavigation();
  }

  protected onContentScroll(): void {
    this.scrolled.set(this.content().nativeElement.scrollTop > SCROLLED_THRESHOLD_PX);
  }

  // The router restores scroll on the window only; this container keeps its offset across pages otherwise.
  private scrollToTopOnNavigation(): void {
    this.router.events
      .pipe(
        filter((event) => event instanceof NavigationEnd),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe(() => this.content().nativeElement.scrollTo({ top: 0 }));
  }
}
