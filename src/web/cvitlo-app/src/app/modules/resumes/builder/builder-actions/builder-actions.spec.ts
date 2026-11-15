import { ComponentFixture, TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import {
  LUCIDE_ICONS,
  LucideIconProvider,
  Check,
  CircleAlert,
  CircleCheck,
  Download,
  Globe,
} from 'lucide-angular';
import { BuilderActions } from './builder-actions';
import { ResumesStore, SaveStatus, SyncErrorKind } from '../../resumes-store';
import { mockResumesStore } from '../../../../shared/testing/mock-resumes-store';
import { expectNoAxeViolations } from '../../../../shared/testing/axe';

const SAVE_STATUSES: readonly SaveStatus[] = ['idle', 'saving', 'saved', 'error'];

describe('BuilderActions', () => {
  let component: BuilderActions;
  let fixture: ComponentFixture<BuilderActions>;

  const preview = signal<Blob | null>(null);
  const saveStatus = signal<SaveStatus>('idle');
  const syncErrorKind = signal<SyncErrorKind>(null);
  const isDownloading = signal(false);
  const canDownload = signal(true);
  const downloadPdf = vi.fn();

  beforeEach(async () => {
    vi.clearAllMocks();
    preview.set(null);
    saveStatus.set('idle');
    syncErrorKind.set(null);
    isDownloading.set(false);
    canDownload.set(true);

    await TestBed.configureTestingModule({
      imports: [BuilderActions],
      providers: [
        {
          provide: ResumesStore,
          useValue: {
            ...mockResumesStore(),
            preview,
            saveStatus,
            syncErrorKind,
            isDownloading,
            canDownload,
            downloadPdf,
          },
        },
        {
          provide: LUCIDE_ICONS,
          multi: true,
          useValue: new LucideIconProvider({ Check, CircleAlert, CircleCheck, Download, Globe }),
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(BuilderActions);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  function downloadButton(): HTMLButtonElement {
    return fixture.nativeElement.querySelector('[data-testid="download-pdf"]') as HTMLButtonElement;
  }

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  describe('download', () => {
    // The store renders a preview on demand, so the button is enabled without a cached one.
    it('is enabled with no cached preview', () => {
      fixture.detectChanges();
      expect(downloadButton()).toBeTruthy();
      expect(downloadButton().disabled).toBe(false);
    });

    it('calls store.downloadPdf when clicked', () => {
      fixture.detectChanges();
      downloadButton().click();
      expect(downloadPdf).toHaveBeenCalled();
    });

    it('disables and marks itself busy while a download is in flight', () => {
      fixture.detectChanges();
      expect(downloadButton().querySelector('lucide-icon')).toBeTruthy();
      expect(downloadButton().querySelector('.animate-spin')).toBeFalsy();

      isDownloading.set(true);
      fixture.detectChanges();
      expect(downloadButton().disabled).toBe(true);
      expect(downloadButton().getAttribute('aria-busy')).toBe('true');
      // The icon is swapped for the spinner, not layered with it.
      expect(downloadButton().querySelector('.animate-spin')).toBeTruthy();
      expect(downloadButton().querySelector('lucide-icon')).toBeFalsy();
    });

    it('disables download while the current resume revision is still saving', () => {
      canDownload.set(false);
      fixture.detectChanges();

      expect(downloadButton().disabled).toBe(true);
    });
  });

  describe('orientation', () => {
    it('stacks vertically as a card island by default', () => {
      fixture.detectChanges();
      const host = fixture.nativeElement as HTMLElement;
      expect(host.classList.contains('flex-col')).toBe(true);
      expect(host.classList.contains('bg-card')).toBe(true);
    });

    it('lays out in a row with no island chrome when horizontal', () => {
      fixture.componentRef.setInput('orientation', 'horizontal');
      fixture.detectChanges();
      const host = fixture.nativeElement as HTMLElement;
      expect(host.classList.contains('flex-row')).toBe(true);
      // The mobile bottom bar draws the island itself; a nested card would double the chrome.
      expect(host.classList.contains('bg-card')).toBe(false);
    });

    it('opens the language menu upward when horizontal', () => {
      fixture.componentRef.setInput('orientation', 'horizontal');
      fixture.detectChanges();
      const menu = fixture.nativeElement.querySelector('.lang-menu') as HTMLElement;
      expect(menu.classList.contains('lang-menu--top')).toBe(true);
      expect(menu.classList.contains('lang-menu--left')).toBe(false);
    });

    it('keeps an accessible group name in both orientations', () => {
      fixture.detectChanges();
      expect((fixture.nativeElement as HTMLElement).getAttribute('aria-label')).toBe('Resume actions');
      expect((fixture.nativeElement as HTMLElement).getAttribute('role')).toBe('group');
    });
  });

  describe('save status', () => {
    function visualTestIds(): string[] {
      return SAVE_STATUSES.filter(status =>
        fixture.nativeElement.querySelector(`[data-testid="save-status-${status}"]`),
      );
    }

    function liveRegionText(): string {
      const region = fixture.nativeElement.querySelector('[role="status"]') as HTMLElement;
      return region.textContent?.trim() ?? '';
    }

    // The visual is aria-hidden (semantics live in the live region), so assert on the
    // decorative branch and that it is the only visual rendered.
    it('shows the saving indicator while saving', () => {
      saveStatus.set('saving');
      fixture.detectChanges();
      expect(fixture.nativeElement.querySelector('[data-testid="save-status-saving"]')).toBeTruthy();
      expect(visualTestIds()).toEqual(['saving']);
    });

    it('shows the saved indicator when saved', () => {
      saveStatus.set('saved');
      fixture.detectChanges();
      expect(fixture.nativeElement.querySelector('[data-testid="save-status-saved"]')).toBeTruthy();
      expect(visualTestIds()).toEqual(['saved']);
      expect(liveRegionText()).toBe('Saved');
    });

    it('shows the error indicator when a save fails', () => {
      saveStatus.set('error');
      fixture.detectChanges();
      expect(fixture.nativeElement.querySelector('[data-testid="save-status-error"]')).toBeTruthy();
      expect(visualTestIds()).toEqual(['error']);
      expect(liveRegionText()).toBe('Save failed');
    });

    it('distinguishes a preview synchronization failure from a save failure', () => {
      syncErrorKind.set('preview');
      saveStatus.set('error');
      fixture.detectChanges();

      const error = fixture.nativeElement.querySelector(
        '[data-testid="save-status-error"]',
      ) as HTMLElement;
      expect(error.title).toBe('Sync failed');
      expect(liveRegionText()).toBe('Sync failed');
    });

    // `idle` means no server-backed resume yet, so it stays visually neutral.
    it('renders no green tick and announces nothing before a resume has loaded', () => {
      saveStatus.set('idle');
      fixture.detectChanges();
      const host = fixture.nativeElement as HTMLElement;
      expect(host.querySelector('lucide-icon[name="check"]')).toBeFalsy();
      expect(host.querySelector('[class*="bg-success"]')).toBeFalsy();
      expect(liveRegionText()).toBe('');
    });

    // Guards against per-branch sizing: the container keeps one footprint across states.
    it('keeps the same footprint whether idle or saving so the island does not shift', () => {
      saveStatus.set('idle');
      fixture.detectChanges();
      const wrapper = fixture.nativeElement.querySelector('span[aria-hidden="true"]') as HTMLElement;
      const idleClasses = wrapper.className;

      saveStatus.set('saving');
      fixture.detectChanges();
      expect(wrapper.className).toBe(idleClasses);
    });

    it.each(SAVE_STATUSES)('renders exactly one save-status visual for "%s"', status => {
      saveStatus.set(status);
      fixture.detectChanges();
      expect(visualTestIds()).toEqual([status]);
    });

    // The live region is what announces the outcome, since the visuals are aria-hidden.
    it.each([
      ['idle', ''],
      ['saving', ''],
      ['saved', 'Saved'],
      ['error', 'Save failed'],
    ] as const)('has exactly one role="status" region for "%s" with text "%s"', (status, text) => {
      saveStatus.set(status);
      fixture.detectChanges();
      expect(fixture.nativeElement.querySelectorAll('[role="status"]').length).toBe(1);
      expect(liveRegionText()).toBe(text);
    });

    it.each(SAVE_STATUSES)(
      'should have no AXE violations while save status is "%s"',
      async status => {
        saveStatus.set(status);
        fixture.detectChanges();
        await expectNoAxeViolations(fixture);
      },
      20000,
    );
  });
});
