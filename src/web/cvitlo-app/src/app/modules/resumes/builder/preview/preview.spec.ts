import { ComponentFixture, TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { LUCIDE_ICONS, LucideIconProvider, ChevronLeft, ChevronRight } from 'lucide-angular';
import { Preview } from './preview';
import { PDFJS_LOADER } from './pdfjs-loader';
import { PreviewStatus, ResumesStore } from '../../resumes-store';
import { mockResumesStore } from '../../../../shared/testing/mock-resumes-store';
import { expectNoAxeViolations } from '../../../../shared/testing/axe';

class RenderingCancelledException extends Error {
  constructor(message: string) {
    super(message);
    this.name = 'RenderingCancelledException';
  }
}

// Models the real pdfjs RenderTask contract closely enough to exercise
// cancellation. The injected module exposes this same exception class, so
// production's `instanceof` check remains meaningful without a module mock.
function createRenderTask(inner: Promise<void>) {
  let settled = false;
  let rejectFromCancel: ((reason: unknown) => void) | undefined;
  const promise = new Promise<void>((resolve, reject) => {
    rejectFromCancel = reason => {
      if (settled) return;
      settled = true;
      reject(reason);
    };
    inner.then(
      value => {
        if (!settled) {
          settled = true;
          resolve(value);
        }
      },
      reason => {
        if (!settled) {
          settled = true;
          reject(reason);
        }
      },
    );
  });
  return {
    promise,
    cancel: vi.fn(() => {
      rejectFromCancel?.(new RenderingCancelledException('Rendering cancelled'));
    }),
  };
}

function createMockDocument() {
  let destroyed = false;
  return {
    numPages: 3,
    getPage: vi.fn(() =>
      Promise.resolve({
        getViewport: vi.fn(() => ({ width: 595, height: 842 })),
        render: vi.fn(() => {
          if (destroyed) {
            throw new Error('Cannot render: the PDF document has been destroyed.');
          }
          return createRenderTask(Promise.resolve());
        }),
      }),
    ),
    loadingTask: {
      destroy: vi.fn(() => {
        destroyed = true;
        return Promise.resolve();
      }),
    },
  };
}

type MockDocument = ReturnType<typeof createMockDocument>;

const getDocumentMock = vi.fn<() => { promise: Promise<MockDocument> }>(
  () => ({ promise: Promise.resolve(createMockDocument()) }),
);
const pdfjsMock = {
  GlobalWorkerOptions: { workerSrc: '' },
  RenderingCancelledException,
  getDocument: getDocumentMock,
};

async function lastMockedDoc() {
  const calls = getDocumentMock.mock.results;
  return calls[calls.length - 1].value.promise;
}

class ResizeObserverStub {
  observe(): void {}
  unobserve(): void {}
  disconnect(): void {}
}

describe('Preview', () => {
  let component: Preview;
  let fixture: ComponentFixture<Preview>;

  const preview = signal<Blob | null>(null);
  const previewRevision = signal<number | null>(0);
  const previewGeneration = signal<number | null>(0);
  const previewStatus = signal<PreviewStatus>('unavailable');
  const hasRenderedPreview = signal(true);
  const confirmPreviewRendered = vi.fn();
  const reportPreviewRenderFailure = vi.fn();

  beforeEach(async () => {
    vi.clearAllMocks();
    getDocumentMock.mockReset();
    getDocumentMock.mockImplementation(() => ({ promise: Promise.resolve(createMockDocument()) }));
    vi.stubGlobal('ResizeObserver', ResizeObserverStub);
    preview.set(null);
    previewRevision.set(0);
    previewGeneration.set(0);
    previewStatus.set('unavailable');
    hasRenderedPreview.set(true);

    await TestBed.configureTestingModule({
      imports: [Preview],
      providers: [
        {
          provide: ResumesStore,
          useValue: {
            ...mockResumesStore(),
            preview,
            previewRevision,
            previewGeneration,
            previewStatus,
            hasRenderedPreview,
            confirmPreviewRendered,
            reportPreviewRenderFailure,
          },
        },
        { provide: PDFJS_LOADER, useValue: () => Promise.resolve(pdfjsMock) },
        {
          provide: LUCIDE_ICONS,
          multi: true,
          useValue: new LucideIconProvider({ ChevronLeft, ChevronRight }),
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(Preview);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('renders page navigation on the shared glass surface', () => {
    preview.set(new Blob(['pdf-bytes'], { type: 'application/pdf' }));
    fixture.detectChanges();

    const nav = fixture.nativeElement.querySelector('[aria-label="Page navigation"]');
    expect(nav.classList.contains('glass-surface')).toBe(true);
  });

  it('uses a visible light-theme hover fill and foreground arrows in dark mode', () => {
    preview.set(new Blob(['pdf-bytes'], { type: 'application/pdf' }));
    fixture.detectChanges();

    const buttons = (fixture.nativeElement as HTMLElement).querySelectorAll<HTMLButtonElement>(
      '[aria-label="Page navigation"] button',
    );
    expect(buttons).toHaveLength(2);

    for (const button of buttons) {
      expect(button.classList.contains('hover:enabled:bg-foreground/10')).toBe(true);
      expect(button.classList.contains('dark:hover:enabled:bg-muted')).toBe(true);
      expect(button.classList.contains('dark:text-foreground')).toBe(true);
      expect(button.classList.contains('disabled:opacity-35')).toBe(true);
    }
  });

  it('has no AXE violations', async () => {
    preview.set(new Blob(['pdf-bytes'], { type: 'application/pdf' }));
    fixture.detectChanges();
    await expectNoAxeViolations(fixture);
  }, 20_000);

  describe('first-load skeleton (#456 §1)', () => {
    it('renders the skeleton and not the "no preview" text while generating with no blob yet', () => {
      previewStatus.set('generating');
      fixture.detectChanges();

      expect(fixture.nativeElement.querySelector('.cv-preview-skeleton')).toBeTruthy();
      expect(fixture.nativeElement.textContent).not.toContain('No preview available');
    });

    it('renders the "no preview" text and not the skeleton when unavailable', () => {
      previewStatus.set('unavailable');
      fixture.detectChanges();

      expect(fixture.nativeElement.querySelector('.cv-preview-skeleton')).toBeFalsy();
      expect(fixture.nativeElement.textContent).toContain('No preview available');
    });

    it('keeps rendering the canvas, not the skeleton, once a blob has arrived (anti-flicker)', () => {
      previewStatus.set('generating');
      preview.set(new Blob(['pdf-bytes'], { type: 'application/pdf' }));
      fixture.detectChanges();

      expect(fixture.nativeElement.querySelector('canvas')).toBeTruthy();
      expect(fixture.nativeElement.querySelector('.cv-preview-skeleton')).toBeFalsy();
    });

    it('fills the visible preview area without constraining the skeleton to A4 (#456)', () => {
      previewStatus.set('generating');
      fixture.detectChanges();

      const skeleton = fixture.nativeElement.querySelector('.cv-preview-skeleton') as HTMLElement;

      expect(skeleton.classList.contains('absolute')).toBe(true);
      expect(skeleton.classList.contains('inset-0')).toBe(true);
      expect(skeleton.classList.contains('w-full')).toBe(true);
      expect(skeleton.classList.contains('h-full')).toBe(true);
      expect(skeleton.className).not.toContain('aspect-');
      expect(fixture.nativeElement.querySelector('.cv-preview-skeleton-stage')).toBeFalsy();
    });

    it('animates individual skeleton lines without a page-wide sweep layer', () => {
      previewStatus.set('generating');
      fixture.detectChanges();

      const skeleton = fixture.nativeElement.querySelector('.cv-preview-skeleton') as HTMLElement;
      const lines = skeleton.querySelectorAll('.cv-preview-skeleton-line');

      expect(lines.length).toBeGreaterThan(1);
      expect(skeleton.querySelector('.cv-preview-skeleton-sweep')).toBeFalsy();
    });

    it('has no AXE violations while the skeleton is showing', async () => {
      previewStatus.set('generating');
      fixture.detectChanges();
      await expectNoAxeViolations(fixture);
    }, 20_000);

    it('keeps the live region in the DOM before it gains its announcement text', () => {
      // A live region is only announced when its text changes while it is already in the DOM, so the same node must
      // gain the text.
      fixture.detectChanges();
      const liveRegion = fixture.nativeElement.querySelector(
        '[role="status"][aria-live="polite"]',
      ) as HTMLElement;
      expect(liveRegion).toBeTruthy();
      expect(liveRegion.textContent).toBe('');

      previewStatus.set('generating');
      fixture.detectChanges();

      const liveRegionAfter = fixture.nativeElement.querySelector(
        '[role="status"][aria-live="polite"]',
      ) as HTMLElement;
      expect(liveRegionAfter).toBe(liveRegion);
      expect(liveRegionAfter.textContent).toBe('Generating preview');
    });
  });

  describe('page navigation', () => {
    it('clamps current page within [1, totalPages]', () => {
      component.totalPages.set(3);
      component.currentPage.set(1);

      component.prev();
      expect(component.currentPage()).toBe(1);

      component.next();
      expect(component.currentPage()).toBe(2);

      component.next();
      component.next();
      expect(component.currentPage()).toBe(3);
    });
  });

  describe('standalone pane (#425)', () => {
    // The render chain is not tracked by the zoneless scheduler and jsdom's clientHeight is 0, so stub on the
    // prototype before rendering; otherwise the height guard silently no-ops.
    let originalClientHeight: PropertyDescriptor | undefined;

    beforeEach(() => {
      originalClientHeight = Object.getOwnPropertyDescriptor(HTMLElement.prototype, 'clientHeight');
      Object.defineProperty(HTMLElement.prototype, 'clientHeight', {
        configurable: true,
        get: () => 600,
      });
    });

    afterEach(() => {
      if (originalClientHeight) {
        Object.defineProperty(HTMLElement.prototype, 'clientHeight', originalClientHeight);
      } else {
        Reflect.deleteProperty(HTMLElement.prototype, 'clientHeight');
      }
    });

    it('defaults to locking the card width', () => {
      expect(fixture.componentInstance.lockWidth()).toBe(true);
    });

    it('pins a min-width to fit the card height when locked (default)', async () => {
      const host = fixture.nativeElement as HTMLElement;

      preview.set(new Blob(['pdf-bytes'], { type: 'application/pdf' }));
      fixture.detectChanges();

      // 600px at the mock's A4 aspect (595 / 842): Math.round(600 * (595 / 842)) = 424, too wide for a 375px
      // viewport. Poll: the render chain settles on its own timeline.
      await vi.waitFor(() => expect(host.style.minWidth).toBe('424px'));
      expect(fixture.componentInstance.scrollable()).toBe(false);
    });

    it('never pins a min-width when it is the only pane', async () => {
      const host = fixture.nativeElement as HTMLElement;

      preview.set(new Blob(['pdf-bytes'], { type: 'application/pdf' }));
      fixture.detectChanges();

      // Lock first: both outcomes leave minWidth '' if lockCardWidth never runs, so only a real 424px -> ''
      // transition depends on the guard.
      await vi.waitFor(() => expect(host.style.minWidth).toBe('424px'));

      fixture.componentRef.setInput('lockWidth', false);
      // A distinct Blob reference is needed: the store.preview() effect keys off signal identity.
      preview.set(new Blob(['pdf-bytes-2'], { type: 'application/pdf' }));
      fixture.detectChanges();

      // Same 600px card: without the guard this would force horizontal overflow on a 375px screen.
      await vi.waitFor(() => expect(host.style.minWidth).toBe(''));
      expect(fixture.componentInstance.scrollable()).toBe(true);
    });
  });

  describe('double buffering, page and scroll preservation (#456 §2)', () => {
    // jsdom reports 0 for both client dimensions and renderCurrentPage bails on width <= 0, so stub both up front.
    let originalClientHeight: PropertyDescriptor | undefined;
    let originalClientWidth: PropertyDescriptor | undefined;

    beforeEach(() => {
      originalClientHeight = Object.getOwnPropertyDescriptor(HTMLElement.prototype, 'clientHeight');
      originalClientWidth = Object.getOwnPropertyDescriptor(HTMLElement.prototype, 'clientWidth');
      Object.defineProperty(HTMLElement.prototype, 'clientHeight', { configurable: true, get: () => 600 });
      Object.defineProperty(HTMLElement.prototype, 'clientWidth', { configurable: true, get: () => 800 });
      // jsdom lacks Element.scrollTo, which prev()/next() call on #paper.
      if (typeof HTMLElement.prototype.scrollTo !== 'function') {
        (HTMLElement.prototype as unknown as { scrollTo: () => void }).scrollTo = () => {};
      }
    });

    afterEach(() => {
      if (originalClientHeight) {
        Object.defineProperty(HTMLElement.prototype, 'clientHeight', originalClientHeight);
      } else {
        Reflect.deleteProperty(HTMLElement.prototype, 'clientHeight');
      }
      if (originalClientWidth) {
        Object.defineProperty(HTMLElement.prototype, 'clientWidth', originalClientWidth);
      } else {
        Reflect.deleteProperty(HTMLElement.prototype, 'clientWidth');
      }
    });

    it('renders into the back buffer without touching the front buffer mid-render', async () => {
      preview.set(new Blob(['pdf-bytes-1'], { type: 'application/pdf' }));
      fixture.detectChanges();
      await vi.waitFor(() => expect(component.totalPages()).toBe(3));

      const canvases = () =>
        Array.from(fixture.nativeElement.querySelectorAll('canvas')) as HTMLCanvasElement[];
      const frontBefore = canvases().find(c => c.classList.contains('cv-preview-buffer-front'))!;
      const backBefore = canvases().find(c => !c.classList.contains('cv-preview-buffer-front'))!;

      const widthSetSpy = vi.spyOn(frontBefore, 'width', 'set');
      const heightSetSpy = vi.spyOn(frontBefore, 'height', 'set');

      const doc = await lastMockedDoc();
      let resolveRender!: () => void;
      doc.getPage.mockImplementationOnce(() =>
        Promise.resolve({
          getViewport: vi.fn(() => ({ width: 595, height: 842 })),
          render: vi.fn(() =>
            createRenderTask(
              new Promise<void>(resolve => {
                resolveRender = resolve;
              }),
            ),
          ),
        }),
      );

      component.next();

      // The back buffer is resized synchronously, before page.render() settles.
      await vi.waitFor(() => expect(backBefore.width).toBe(595));
      expect(backBefore.height).toBe(842);

      // The front canvas must not be resized or cleared mid-render.
      expect(widthSetSpy).not.toHaveBeenCalled();
      expect(heightSetSpy).not.toHaveBeenCalled();
      expect(frontBefore.classList.contains('cv-preview-buffer-front')).toBe(true);

      resolveRender();
      await vi.waitFor(() =>
        expect(backBefore.classList.contains('cv-preview-buffer-front')).toBe(true),
      );
    });

    it('keeps the first-load skeleton over the canvas until the revision is painted', async () => {
      hasRenderedPreview.set(false);
      previewStatus.set('generating');
      previewRevision.set(7);
      previewGeneration.set(3);
      preview.set(new Blob(['pdf-bytes'], { type: 'application/pdf' }));
      fixture.detectChanges();

      expect(fixture.nativeElement.querySelector('.cv-preview-skeleton')).toBeTruthy();
      await vi.waitFor(() => expect(confirmPreviewRendered).toHaveBeenCalledWith(7, 3));

      hasRenderedPreview.set(true);
      fixture.detectChanges();
      expect(fixture.nativeElement.querySelector('.cv-preview-skeleton')).toBeFalsy();
    });

    it('does not confirm a manual refresh when the previous document repaints while it is loading', async () => {
      previewRevision.set(1);
      previewGeneration.set(1);
      preview.set(new Blob(['pdf-bytes-1'], { type: 'application/pdf' }));
      fixture.detectChanges();
      await vi.waitFor(() => expect(confirmPreviewRendered).toHaveBeenCalledWith(1, 1));
      confirmPreviewRendered.mockClear();

      let resolveDocument!: (document: Awaited<ReturnType<typeof lastMockedDoc>>) => void;
      getDocumentMock.mockImplementationOnce(
        () => ({
          promise: new Promise(resolve => {
            resolveDocument = resolve;
          }),
        }),
      );

      // A manual refresh replaces the PDF without changing the resume revision, so document identity must
      // disambiguate.
      previewRevision.set(1);
      previewGeneration.set(2);
      preview.set(new Blob(['pdf-bytes-2'], { type: 'application/pdf' }));
      fixture.detectChanges();
      await vi.waitFor(() => expect(getDocumentMock.mock.calls.length).toBe(2));

      // A resize or page turn can repaint the old document while the replacement parses; equal revisions do not prove
      // the refreshed one is visible.
      await (component as unknown as { renderCurrentPage(): Promise<void> }).renderCurrentPage();
      expect(confirmPreviewRendered).not.toHaveBeenCalled();

      resolveDocument({
        numPages: 3,
        getPage: vi.fn(() =>
          Promise.resolve({
            getViewport: vi.fn(() => ({ width: 595, height: 842 })),
            render: vi.fn(() => createRenderTask(Promise.resolve())),
          }),
        ),
        destroy: vi.fn(() => Promise.resolve()),
      } as unknown as Awaited<ReturnType<typeof lastMockedDoc>>);

      await vi.waitFor(() => expect(confirmPreviewRendered).toHaveBeenCalledWith(1, 2));
    });

    it('keeps the outgoing canvas fully opaque throughout the crossfade (branch review: no concurrent fade-out)', async () => {
      // Fading both canvases at once dips composite alpha and bleeds --color-card through (#456 §2). Assert on inline
      // styles/classes; jsdom cannot sample pixels.
      preview.set(new Blob(['pdf-bytes-1'], { type: 'application/pdf' }));
      fixture.detectChanges();
      await vi.waitFor(() => expect(component.totalPages()).toBe(3));

      const canvases = () =>
        Array.from(fixture.nativeElement.querySelectorAll('canvas')) as HTMLCanvasElement[];
      const frontBefore = canvases().find(c => c.classList.contains('cv-preview-buffer-front'))!;
      const backBefore = canvases().find(c => !c.classList.contains('cv-preview-buffer-front'))!;

      const doc = await lastMockedDoc();
      let resolveRender!: () => void;
      doc.getPage.mockImplementationOnce(() =>
        Promise.resolve({
          getViewport: vi.fn(() => ({ width: 595, height: 842 })),
          render: vi.fn(() =>
            createRenderTask(
              new Promise<void>(resolve => {
                resolveRender = resolve;
              }),
            ),
          ),
        }),
      );

      component.next();
      await vi.waitFor(() => expect(backBefore.width).toBe(595));

      // Mid-render: the front canvas has no inline opacity override; the back canvas is reset to 0 for its fade-in.
      expect(frontBefore.style.opacity).toBe('');
      expect(backBefore.style.opacity).toBe('0');

      resolveRender();
      await vi.waitFor(() =>
        expect(backBefore.classList.contains('cv-preview-buffer-front')).toBe(true),
      );

      // After promotion: the promoted canvas has its inline override cleared; the demoted one was never touched.
      expect(backBefore.style.opacity).toBe('');
      expect(frontBefore.style.opacity).toBe('');
    });

    it('opens the very first document on page 1', async () => {
      preview.set(new Blob(['pdf-bytes'], { type: 'application/pdf' }));
      fixture.detectChanges();

      await vi.waitFor(() => expect(component.totalPages()).toBe(3));
      expect(component.currentPage()).toBe(1);
    });

    it('keeps the current page when a replacement document arrives', async () => {
      preview.set(new Blob(['pdf-bytes-1'], { type: 'application/pdf' }));
      fixture.detectChanges();
      await vi.waitFor(() => expect(component.totalPages()).toBe(3));

      component.next();
      await vi.waitFor(() => expect(component.currentPage()).toBe(2));

      preview.set(new Blob(['pdf-bytes-2'], { type: 'application/pdf' }));
      fixture.detectChanges();

      await vi.waitFor(() => expect(getDocumentMock.mock.calls.length).toBe(2));
      expect(component.currentPage()).toBe(2);
    });

    it('clamps the current page when a replacement document is shorter', async () => {
      preview.set(new Blob(['pdf-bytes-1'], { type: 'application/pdf' }));
      fixture.detectChanges();
      await vi.waitFor(() => expect(component.totalPages()).toBe(3));

      component.next();
      component.next();
      await vi.waitFor(() => expect(component.currentPage()).toBe(3));

      // Two pages, so clamping to page 2 differs from resetting to page 1.
      getDocumentMock.mockImplementationOnce(
        () =>
          ({
            promise: Promise.resolve({
              numPages: 2,
              getPage: vi.fn(() =>
                Promise.resolve({
                  getViewport: vi.fn(() => ({ width: 595, height: 842 })),
                  render: vi.fn(() => createRenderTask(Promise.resolve())),
                }),
              ),
              destroy: vi.fn(() => Promise.resolve()),
            }),
          }) as unknown as { promise: Promise<MockDocument> },
      );

      preview.set(new Blob(['pdf-bytes-2'], { type: 'application/pdf' }));
      fixture.detectChanges();

      await vi.waitFor(() => expect(component.totalPages()).toBe(2));
      expect(component.currentPage()).toBe(2);
    });

    it('restores the scroll offset after a replacement resizes the wrapper', async () => {
      preview.set(new Blob(['pdf-bytes-1'], { type: 'application/pdf' }));
      fixture.detectChanges();
      await vi.waitFor(() => expect(component.totalPages()).toBe(3));

      const paperEl = (
        component as unknown as { paper: () => { nativeElement: HTMLDivElement } | undefined }
      ).paper()!.nativeElement;
      paperEl.scrollTop = 120;
      // jsdom does not clamp scrollTop, so assert the setter restored 120 instead of observing the browser reset.
      const scrollTopSetSpy = vi.spyOn(paperEl, 'scrollTop', 'set');

      preview.set(new Blob(['pdf-bytes-2'], { type: 'application/pdf' }));
      fixture.detectChanges();

      await vi.waitFor(() => expect(getDocumentMock.mock.calls.length).toBe(2));
      expect(scrollTopSetSpy).toHaveBeenCalledWith(120);
      expect(paperEl.scrollTop).toBe(120);
    });

    it('shrinks the demoted canvas geometry after a resize to a smaller frame, so it cannot contribute a stale bigger frame (branch review Finding 1)', async () => {
      // A resize down to a smaller frame must snap the demoted canvas to the new size, or its stale larger geometry
      // sticks out past #frame. clientWidth is stubbed as a mutable value so it can shrink between renders.
      let widthValue = 800;
      Object.defineProperty(HTMLElement.prototype, 'clientWidth', {
        configurable: true,
        get: () => widthValue,
      });

      preview.set(new Blob(['pdf-bytes-1'], { type: 'application/pdf' }));
      fixture.detectChanges();
      await vi.waitFor(() => expect(component.totalPages()).toBe(3));

      const canvases = () =>
        Array.from(fixture.nativeElement.querySelectorAll('canvas')) as HTMLCanvasElement[];
      const largeFrameCanvas = canvases().find(c => c.classList.contains('cv-preview-buffer-front'))!;
      // natural.width (595) * fit (800 / 595) == 800 — the page fills the 800px card.
      expect(parseFloat(largeFrameCanvas.style.width)).toBeCloseTo(800, 0);

      // Shrink the frame and re-render, as the ResizeObserver would.
      widthValue = 200;
      const renderCurrentPage = (component as unknown as { renderCurrentPage(): Promise<void> })
        .renderCurrentPage.bind(component);
      await renderCurrentPage();
      fixture.detectChanges();

      // The render promotes the other canvas, so largeFrameCanvas is now demoted; it must be snapped to the new 200px
      // size, or it would stick out past #frame and inflate #paper's scroll extent.
      expect(largeFrameCanvas.classList.contains('cv-preview-buffer-front')).toBe(false);
      expect(parseFloat(largeFrameCanvas.style.width)).toBeCloseTo(200, 0);

      const frameEl = (
        component as unknown as { frame: () => { nativeElement: HTMLDivElement } | undefined }
      ).frame()!.nativeElement;
      expect(parseFloat(frameEl.style.width)).toBeCloseTo(200, 0);
    });
  });

  describe('concurrent render safety (#456 fix round — cancel superseded renders)', () => {
    let originalClientHeight: PropertyDescriptor | undefined;
    let originalClientWidth: PropertyDescriptor | undefined;

    beforeEach(() => {
      originalClientHeight = Object.getOwnPropertyDescriptor(HTMLElement.prototype, 'clientHeight');
      originalClientWidth = Object.getOwnPropertyDescriptor(HTMLElement.prototype, 'clientWidth');
      Object.defineProperty(HTMLElement.prototype, 'clientHeight', { configurable: true, get: () => 600 });
      Object.defineProperty(HTMLElement.prototype, 'clientWidth', { configurable: true, get: () => 800 });
    });

    afterEach(() => {
      if (originalClientHeight) {
        Object.defineProperty(HTMLElement.prototype, 'clientHeight', originalClientHeight);
      } else {
        Reflect.deleteProperty(HTMLElement.prototype, 'clientHeight');
      }
      if (originalClientWidth) {
        Object.defineProperty(HTMLElement.prototype, 'clientWidth', originalClientWidth);
      } else {
        Reflect.deleteProperty(HTMLElement.prototype, 'clientWidth');
      }
    });

    it('cancels a superseded render instead of racing it on the shared back canvas, and promotes only the newer frame', async () => {
      preview.set(new Blob(['pdf-bytes-1'], { type: 'application/pdf' }));
      fixture.detectChanges();
      await vi.waitFor(() => expect(component.totalPages()).toBe(3));

      const doc = await lastMockedDoc();

      // Mirrors real pdfjs: a second render() on a canvas still mid-render throws synchronously. Overlapping renders
      // share this lock, reproducing #456 (backCanvas() is the same element for both).
      const busyCanvases = new Set<HTMLCanvasElement>();
      function lockedRenderTask(canvas: HTMLCanvasElement, inner: Promise<void>) {
        if (busyCanvases.has(canvas)) {
          throw new Error('Cannot use the same canvas during multiple render() operations');
        }
        busyCanvases.add(canvas);
        const task = createRenderTask(inner);
        task.promise.finally(() => busyCanvases.delete(canvas)).catch(() => {});
        return task;
      }

      let taskA: ReturnType<typeof createRenderTask> | undefined;
      let resolveB!: () => void;

      doc.getPage
        .mockImplementationOnce(() =>
          Promise.resolve({
            // Distinct from B's viewport so the final dimensions can only come from the later render.
            getViewport: vi.fn(() => ({ width: 111, height: 111 })),
            render: vi.fn(({ canvas }: { canvas: HTMLCanvasElement }) => {
              // Never resolves; settles only via cancellation below.
              taskA = lockedRenderTask(canvas, new Promise<void>(() => {}));
              return taskA;
            }),
          }),
        )
        .mockImplementationOnce(() =>
          Promise.resolve({
            getViewport: vi.fn(() => ({ width: 222, height: 222 })),
            render: vi.fn(({ canvas }: { canvas: HTMLCanvasElement }) =>
              lockedRenderTask(
                canvas,
                new Promise<void>(resolve => {
                  resolveB = resolve;
                }),
              ),
            ),
          }),
        );

      const renderCurrentPage = (component as unknown as { renderCurrentPage(): Promise<void> })
        .renderCurrentPage.bind(component);

      const call1 = renderCurrentPage();
      await vi.waitFor(() => expect(taskA).toBeDefined());
      const call2 = renderCurrentPage();
      await vi.waitFor(() => expect(resolveB).toBeDefined());

      resolveB();

      // call1's RenderingCancelledException must be swallowed and call2 must complete normally.
      await expect(call2).resolves.toBeUndefined();
      await expect(call1).resolves.toBeUndefined();

      expect(taskA!.cancel).toHaveBeenCalledTimes(1);

      // The flip reaches the DOM only on change detection (class binding).
      fixture.detectChanges();
      const frontCanvas = fixture.nativeElement.querySelector(
        'canvas.cv-preview-buffer-front',
      ) as HTMLCanvasElement;
      expect(frontCanvas.width).toBe(222);
    });

    it('surfaces a genuine render failure instead of swallowing it like a cancellation', async () => {
      preview.set(new Blob(['pdf-bytes-1'], { type: 'application/pdf' }));
      fixture.detectChanges();
      await vi.waitFor(() => expect(component.totalPages()).toBe(3));

      const doc = await lastMockedDoc();
      const busyCanvases = new Set<HTMLCanvasElement>();
      function lockedRenderTask(canvas: HTMLCanvasElement, inner: Promise<void>) {
        if (busyCanvases.has(canvas)) {
          throw new Error('Cannot use the same canvas during multiple render() operations');
        }
        busyCanvases.add(canvas);
        const task = createRenderTask(inner);
        task.promise.finally(() => busyCanvases.delete(canvas)).catch(() => {});
        return task;
      }

      let taskA: ReturnType<typeof createRenderTask> | undefined;
      const failure = new Error('pdfjs internal render error');

      doc.getPage
        .mockImplementationOnce(() =>
          Promise.resolve({
            getViewport: vi.fn(() => ({ width: 111, height: 111 })),
            render: vi.fn(({ canvas }: { canvas: HTMLCanvasElement }) => {
              taskA = lockedRenderTask(canvas, new Promise<void>(() => {}));
              return taskA;
            }),
          }),
        )
        .mockImplementationOnce(() =>
          Promise.resolve({
            getViewport: vi.fn(() => ({ width: 222, height: 222 })),
            render: vi.fn(({ canvas }: { canvas: HTMLCanvasElement }) =>
              lockedRenderTask(canvas, Promise.reject(failure)),
            ),
          }),
        );

      const renderCurrentPage = (component as unknown as { renderCurrentPage(): Promise<void> })
        .renderCurrentPage.bind(component);

      const call1 = renderCurrentPage();
      await vi.waitFor(() => expect(taskA).toBeDefined());
      const call2 = renderCurrentPage();

      // call1's cancellation is still swallowed; only call2's genuine failure surfaces.
      await expect(call1).resolves.toBeUndefined();
      await expect(call2).rejects.toBe(failure);
      expect(taskA!.cancel).toHaveBeenCalledTimes(1);
    });

    it('logs a genuine render failure from a fire-and-forget call site instead of leaving it unhandled (#456 fix round §3)', async () => {
      // The fire-and-forget call sites use renderCurrentPageSafely; assert it logs the rejection instead of leaving
      // an unhandled rejection and a frozen stale frame.
      preview.set(new Blob(['pdf-bytes-1'], { type: 'application/pdf' }));
      fixture.detectChanges();
      await vi.waitFor(() => expect(component.totalPages()).toBe(3));

      const consoleErrorSpy = vi.spyOn(console, 'error').mockImplementation(() => {});
      const doc = await lastMockedDoc();
      const failure = new Error('pdfjs internal render error');
      doc.getPage.mockImplementationOnce(() =>
        Promise.resolve({
          getViewport: vi.fn(() => ({ width: 595, height: 842 })),
          render: vi.fn(() => createRenderTask(Promise.reject(failure))),
        }),
      );

      component.next();

      await vi.waitFor(() =>
        expect(consoleErrorSpy).toHaveBeenCalledWith('Failed to render preview page', failure),
      );

      consoleErrorSpy.mockRestore();
    });

    it('logs a renderPdf failure instead of leaving it an unhandled rejection (branch review Finding 3)', async () => {
      // The constructor effect's renderPdf covers loadPdfjs, the blob read and getDocument(); a corrupt PDF rejecting
      // there must be caught. Pin the exact wrapper message so a regression fails fast instead of timing out.
      const consoleErrorSpy = vi.spyOn(console, 'error').mockImplementation(() => {});
      const failure = new Error('Invalid PDF structure');
      getDocumentMock.mockImplementationOnce(
        () => ({ promise: Promise.reject(failure) }),
      );

      previewRevision.set(9);
      previewGeneration.set(4);
      preview.set(new Blob(['corrupt-pdf-bytes'], { type: 'application/pdf' }));
      fixture.detectChanges();

      await vi.waitFor(() =>
        expect(consoleErrorSpy).toHaveBeenCalledWith('Failed to load preview PDF', failure),
      );
      expect(reportPreviewRenderFailure).toHaveBeenCalledWith(9, 4, failure);

      consoleErrorSpy.mockRestore();
    });

    it('does not flip the buffer or resize the frame when torn down between render() settling and the swap (#456 fix round §2)', async () => {
      preview.set(new Blob(['pdf-bytes-1'], { type: 'application/pdf' }));
      fixture.detectChanges();
      await vi.waitFor(() => expect(component.totalPages()).toBe(3));

      const frontBefore = fixture.nativeElement.querySelector(
        'canvas.cv-preview-buffer-front',
      ) as HTMLCanvasElement;
      const frameEl = (
        component as unknown as { frame: () => { nativeElement: HTMLDivElement } | undefined }
      ).frame()!.nativeElement;
      const widthBefore = frameEl.style.width;
      const heightBefore = frameEl.style.height;

      const doc = await lastMockedDoc();
      let resolveRender!: () => void;
      doc.getPage.mockImplementationOnce(() =>
        Promise.resolve({
          getViewport: vi.fn(() => ({ width: 595, height: 842 })),
          render: vi.fn(() =>
            createRenderTask(
              new Promise<void>(resolve => {
                resolveRender = resolve;
              }),
            ),
          ),
        }),
      );

      const pending = (component as unknown as { renderCurrentPage(): Promise<void> })
        .renderCurrentPage();
      await vi.waitFor(() => expect(resolveRender).toBeDefined());

      // Resolve render().promise and destroy in the same tick: the continuation is a microtask, so ngOnDestroy bumps
      // the token first. This hits the second token check's window, not the getPage one.
      resolveRender();
      expect(() => fixture.destroy()).not.toThrow();

      await expect(pending).resolves.toBeUndefined();

      expect(frameEl.style.width).toBe(widthBefore);
      expect(frameEl.style.height).toBe(heightBefore);
      expect(frontBefore.classList.contains('cv-preview-buffer-front')).toBe(true);
    });
  });

  describe('ngOnDestroy (#425 mobile editor<->preview toggle recreates Preview per tap)', () => {
    // jsdom reports 0 dimensions and renderCurrentPage bails on width <= 0; stub both so execution reaches
    // page.render.
    let originalClientHeight: PropertyDescriptor | undefined;
    let originalClientWidth: PropertyDescriptor | undefined;

    beforeEach(() => {
      originalClientHeight = Object.getOwnPropertyDescriptor(HTMLElement.prototype, 'clientHeight');
      originalClientWidth = Object.getOwnPropertyDescriptor(HTMLElement.prototype, 'clientWidth');
      Object.defineProperty(HTMLElement.prototype, 'clientHeight', { configurable: true, get: () => 600 });
      Object.defineProperty(HTMLElement.prototype, 'clientWidth', { configurable: true, get: () => 800 });
    });

    afterEach(() => {
      if (originalClientHeight) {
        Object.defineProperty(HTMLElement.prototype, 'clientHeight', originalClientHeight);
      } else {
        Reflect.deleteProperty(HTMLElement.prototype, 'clientHeight');
      }
      if (originalClientWidth) {
        Object.defineProperty(HTMLElement.prototype, 'clientWidth', originalClientWidth);
      } else {
        Reflect.deleteProperty(HTMLElement.prototype, 'clientWidth');
      }
    });

    it('destroys the pdf loading task and clears the reference', async () => {
      preview.set(new Blob(['pdf-bytes'], { type: 'application/pdf' }));
      fixture.detectChanges();

      // Wait for renderPdf to assign a pdfDoc before destroying it.
      await vi.waitFor(() => expect(component.totalPages()).toBe(3));
      const doc = await lastMockedDoc();

      fixture.destroy();

      expect(doc.loadingTask.destroy).toHaveBeenCalledTimes(1);
      expect((component as unknown as { pdfDoc: unknown }).pdfDoc).toBeNull();
    });

    it('does not throw or touch the destroyed document when torn down mid-render', async () => {
      preview.set(new Blob(['pdf-bytes'], { type: 'application/pdf' }));
      fixture.detectChanges();
      await vi.waitFor(() => expect(component.totalPages()).toBe(3));
      const doc = await lastMockedDoc();

      // Kick off a second render (a page turn) and destroy before its getPage resumes, like the mobile toggle tearing
      // down mid-render; the renderToken guard must stop page.render.
      const pending = (component as unknown as { renderCurrentPage(): Promise<void> })
        .renderCurrentPage();

      expect(() => fixture.destroy()).not.toThrow();

      await expect(pending).resolves.toBeUndefined();
      expect(doc.loadingTask.destroy).toHaveBeenCalledTimes(1);
    });
  });
});
