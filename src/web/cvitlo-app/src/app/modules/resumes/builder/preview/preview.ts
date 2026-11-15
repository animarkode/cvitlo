import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  computed,
  effect,
  ElementRef,
  inject,
  input,
  OnDestroy,
  signal,
  viewChild,
} from '@angular/core';
import type * as PdfJs from 'pdfjs-dist';
import { LucideAngularModule } from 'lucide-angular';
import { ResumesStore } from '../../resumes-store';
import { PDFJS_LOADER } from './pdfjs-loader';

@Component({
  selector: 'app-preview',
  imports: [LucideAngularModule],
  templateUrl: './preview.html',
  styleUrl: './preview.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'block h-full min-h-0' },
})
export class Preview implements OnDestroy {
  protected readonly store = inject(ResumesStore);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly host: ElementRef<HTMLElement> = inject(ElementRef);
  private readonly pdfjsLoader = inject(PDFJS_LOADER);
  private readonly canvasA = viewChild<ElementRef<HTMLCanvasElement>>('canvasA');
  private readonly canvasB = viewChild<ElementRef<HTMLCanvasElement>>('canvasB');
  private readonly frame = viewChild<ElementRef<HTMLDivElement>>('frame');
  private readonly paper = viewChild<ElementRef<HTMLDivElement>>('paper');

  // False when the preview is the only pane (mobile, #425): the min-width lock would exceed a 375px viewport.
  readonly lockWidth = input(true);

  readonly currentPage = signal(1);
  readonly totalPages = signal(0);
  readonly scrollable = signal(true);

  // The canvas on screen; renders target the other one and flip this only once they resolve.
  readonly frontBuffer = signal<'a' | 'b'>('a');

  // Empty unless the skeleton shows; a persistent live region (see preview.html) is what screen readers announce.
  protected readonly previewAnnouncement = computed(() =>
    this.store.previewStatus() === 'generating'
      && (!this.store.preview() || !this.store.hasRenderedPreview())
      ? 'Generating preview'
      : '',
  );

  private pdfDoc: PdfJs.PDFDocumentProxy | null = null;
  private pdfDocRevision: number | null = null;
  private pdfDocGeneration: number | null = null;
  private pdfDocToken = 0;
  private pdfjsLib: typeof PdfJs | null = null;
  private renderToken = 0;
  private documentToken = 0;
  private pendingSync: {
    readonly revision: number;
    readonly generation: number;
    readonly documentToken: number;
  } | null = null;
  private lockedWidth = 0;

  // Tracked so a newer render cancels an older one instead of both drawing into the same 2D context.
  private inFlightRenderTask: PdfJs.RenderTask | null = null;

  private async loadPdfjs(): Promise<typeof PdfJs> {
    if (this.pdfjsLib) return this.pdfjsLib;
    this.pdfjsLib = await this.pdfjsLoader();
    return this.pdfjsLib;
  }

  constructor() {
    effect(() => {
      const blob = this.store.preview();
      if (blob) {
        void this.renderPdfSafely(
          blob,
          this.store.previewRevision() ?? 0,
          this.store.previewGeneration() ?? 0,
        );
      }
    });

    effect((onCleanup) => {
      const paperEl = this.paper()?.nativeElement;
      if (!paperEl) return;
      const ro = new ResizeObserver(() => {
        if (this.pdfDoc) void this.renderCurrentPageSafely();
      });
      ro.observe(paperEl);
      onCleanup(() => ro.disconnect());
    });
  }

  prev(): void {
    this.currentPage.update(p => Math.max(1, p - 1));
    this.paper()?.nativeElement.scrollTo({ top: 0 });
    void this.renderCurrentPageSafely();
  }

  next(): void {
    this.currentPage.update(p => Math.min(this.totalPages(), p + 1));
    this.paper()?.nativeElement.scrollTo({ top: 0 });
    void this.renderCurrentPageSafely();
  }

  private async renderPdf(
    blob: Blob,
    revision: number,
    generation: number,
    documentToken: number,
  ): Promise<void> {
    const pdfjs = await this.loadPdfjs();
    if (documentToken !== this.documentToken) return;
    const buffer = await blob.arrayBuffer();
    if (documentToken !== this.documentToken) return;
    // A regeneration keeps the page being read; only the first document defaults to page 1.
    const isFirstDocument = this.pdfDoc === null;
    const document = await pdfjs.getDocument({ data: buffer }).promise;
    if (documentToken !== this.documentToken) {
      await document.loadingTask.destroy();
      return;
    }
    this.pdfDoc = document;
    this.pdfDocRevision = revision;
    this.pdfDocGeneration = generation;
    this.pdfDocToken = documentToken;
    this.totalPages.set(this.pdfDoc.numPages);
    if (isFirstDocument) {
      this.currentPage.set(1);
    } else {
      // The resume may have gotten shorter.
      this.currentPage.update(page => Math.min(page, this.pdfDoc!.numPages));
    }
    this.cdr.markForCheck();
    await this.renderCurrentPageSafely();
  }

  // Fire-and-forget wrapper so a render failure is not an unhandled rejection that leaves a stale frame frozen;
  // a failure for the pending document is reported to the store.
  private async renderCurrentPageSafely(): Promise<void> {
    const revision = this.pdfDocRevision;
    const generation = this.pdfDocGeneration;
    const documentToken = this.pdfDocToken;
    try {
      await this.renderCurrentPage();
    } catch (error) {
      console.error('Failed to render preview page', error);
      if (
        revision !== null
        && generation !== null
        && this.pendingSync?.revision === revision
        && this.pendingSync.generation === generation
        && this.pendingSync.documentToken === documentToken
      ) {
        this.pendingSync = null;
        this.store.reportPreviewRenderFailure(revision, generation, error);
      }
    }
  }

  // Fire-and-forget wrapper: renderPdf can reject (chunk-load failure, corrupt PDF) before any page is drawn,
  // which renderCurrentPageSafely cannot catch.
  private async renderPdfSafely(blob: Blob, revision: number, generation: number): Promise<void> {
    const documentToken = ++this.documentToken;
    this.pendingSync = { revision, generation, documentToken };
    try {
      await this.renderPdf(blob, revision, generation, documentToken);
    } catch (error) {
      console.error('Failed to load preview PDF', error);
      if (
        this.pendingSync?.revision === revision
        && this.pendingSync.generation === generation
        && this.pendingSync.documentToken === documentToken
      ) {
        this.pendingSync = null;
        this.store.reportPreviewRenderFailure(revision, generation, error);
      }
    }
  }

  private backCanvas(): ElementRef<HTMLCanvasElement> | undefined {
    return this.frontBuffer() === 'a' ? this.canvasB() : this.canvasA();
  }

  private frontCanvas(): ElementRef<HTMLCanvasElement> | undefined {
    return this.frontBuffer() === 'a' ? this.canvasA() : this.canvasB();
  }

  // The rejection is consumed here; renderCurrentPage tells cancellation from a genuine failure.
  private async cancelInFlightRender(): Promise<void> {
    const task = this.inFlightRenderTask;
    if (!task) return;
    task.cancel();
    try {
      await task.promise;
    } catch {
      // Expected: cancel() rejects the task's promise; we only need it settled.
    }
  }

  private async renderCurrentPage(): Promise<void> {
    const paperEl = this.paper();
    const frameEl = this.frame();
    const backCanvasEl = this.backCanvas();
    if (!this.pdfDoc || !paperEl || !frameEl || !backCanvasEl) return;

    const pdfDoc = this.pdfDoc;
    const documentRevision = this.pdfDocRevision;
    const documentGeneration = this.pdfDocGeneration;
    const pdfDocToken = this.pdfDocToken;
    const token = ++this.renderToken;

    // A prior render may still draw into this back canvas; cancel() only requests a stop, so wait for it to settle
    // before touching the canvas.
    await this.cancelInFlightRender();
    // Teardown or a newer render may have landed during the await.
    if (token !== this.renderToken || this.pdfDoc !== pdfDoc) return;

    const page = await pdfDoc.getPage(this.currentPage());
    if (token !== this.renderToken || this.pdfDoc !== pdfDoc) return;

    const natural = page.getViewport({ scale: 1 });

    // Lock the card to the page's aspect ratio once its flex share gets narrower than the width at which the page
    // fills its height, so the page never shrinks or crops.
    this.lockCardWidth(paperEl.nativeElement, natural.width / natural.height);

    // The scrollbar is hidden, so no width is reserved for it.
    const width = paperEl.nativeElement.clientWidth;
    if (width <= 0) return;

    const fit = width / natural.width;
    const dpr = window.devicePixelRatio || 1;
    const viewport = page.getViewport({ scale: fit * dpr });

    // Draw into the back buffer only: setting canvas.width clears it and render() is async, so doing that to the
    // visible canvas is the flicker (#456 §2).
    const canvas = backCanvasEl.nativeElement;
    canvas.width = viewport.width;
    canvas.height = viewport.height;
    canvas.style.width = `${natural.width * fit}px`;
    canvas.style.height = `${natural.height * fit}px`;
    // Inline opacity 0 overrides the CSS resting value of 1 (preview.css); the promotion below fades in from it.
    // Done on every render so each page turn crossfades, not only the first paint.
    canvas.style.opacity = '0';

    const renderTask = page.render({ canvas, viewport });
    this.inFlightRenderTask = renderTask;
    try {
      await renderTask.promise;
    } catch (error) {
      // A newer render's cancelInFlightRender() rejects with RenderingCancelledException, which is benign;
      // anything else is a genuine failure and must propagate.
      if (this.pdfjsLib && error instanceof this.pdfjsLib.RenderingCancelledException) {
        return;
      }
      throw error;
    } finally {
      if (this.inFlightRenderTask === renderTask) this.inFlightRenderTask = null;
    }
    // Torn down (ngOnDestroy bumps the token) or superseded: do not swap buffers or resize. This also covers
    // teardown landing between render().promise resolving and here.
    if (token !== this.renderToken) return;

    // The wrapper owns #paper's scroll extent; restore scrollTop because resizing it can make the browser clamp it.
    // Canvases keep explicit CSS sizes so the resize never squashes the frame still on screen.
    const scrollTop = paperEl.nativeElement.scrollTop;
    frameEl.nativeElement.style.width = `${natural.width * fit}px`;
    frameEl.nativeElement.style.height = `${natural.height * fit}px`;
    paperEl.nativeElement.scrollTop = scrollTop;

    // Snap the demoted canvas to the new size: after a shrinking resize its stale larger geometry would stick
    // out past #frame, which does not clip. Unconditional so no size check has to track lockCardWidth's rounding.
    const demotedCanvasEl = this.frontCanvas()?.nativeElement;
    if (demotedCanvasEl) {
      demotedCanvasEl.style.width = `${natural.width * fit}px`;
      demotedCanvasEl.style.height = `${natural.height * fit}px`;
    }

    // Flip buffers only now that both frames are painted. Clearing the inline opacity fades the incoming canvas
    // 0 -> 1 above the outgoing one, which stays opaque so --color-card never bleeds through (#456 §2).
    canvas.style.removeProperty('opacity');
    this.frontBuffer.update(buffer => (buffer === 'a' ? 'b' : 'a'));
    this.cdr.markForCheck();
    if (
      documentRevision !== null
      && documentGeneration !== null
      && this.pendingSync?.revision === documentRevision
      && this.pendingSync.generation === documentGeneration
      && this.pendingSync.documentToken === pdfDocToken
    ) {
      this.pendingSync = null;
      this.store.confirmPreviewRendered(documentRevision, documentGeneration);
    }
  }

  // Pins the card's min-width to the width at which the page fills the height; below that the editor absorbs the space.
  private lockCardWidth(paperEl: HTMLDivElement, aspect: number): void {
    if (!this.lockWidth()) {
      // Sole pane: a min-width here would overflow the viewport.
      this.host.nativeElement.style.removeProperty('min-width');
      this.lockedWidth = 0;
      this.scrollable.set(true);
      return;
    }
    const height = paperEl.clientHeight;
    if (height <= 0) return;
    const minWidth = Math.round(height * aspect);
    if (minWidth !== this.lockedWidth) {
      this.lockedWidth = minWidth;
      this.host.nativeElement.style.minWidth = `${minWidth}px`;
    }
    // Static once the flex row clamps the card to min-width: the page then fills it exactly, so sub-pixel
    // rounding must not make it scrollable.
    this.scrollable.set(this.host.nativeElement.clientWidth > minWidth + 0.5);
  }

  ngOnDestroy(): void {
    // Bump the tokens first so a render already past its await bails out instead of touching a torn-down document.
    this.renderToken++;
    this.documentToken++;
    this.pendingSync = null;
    this.inFlightRenderTask?.cancel();
    void this.pdfDoc?.loadingTask?.destroy();
    this.pdfDoc = null;
    this.pdfDocRevision = null;
    this.pdfDocGeneration = null;
    this.pdfDocToken = 0;
  }
}
