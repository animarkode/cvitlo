import { InjectionToken } from '@angular/core';
import type * as PdfJs from 'pdfjs-dist';

export type PdfJsLoader = () => Promise<typeof PdfJs>;

// The worker goes through the bundler's file loader so it is emitted as a hashed asset from the same pdfjs-dist
// version as the library.
async function loadPdfJsWithWorker(): Promise<typeof PdfJs> {
  const [lib, worker] = await Promise.all([
    import('pdfjs-dist'),
    import('pdfjs-dist/build/pdf.worker.min.mjs', { with: { loader: 'file' } }),
  ]);
  lib.GlobalWorkerOptions.workerSrc = new URL(
    worker.default,
    document.baseURI,
  ).toString();
  return lib;
}

// Behind DI so preview tests never execute PDF.js browser globals.
export const PDFJS_LOADER = new InjectionToken<PdfJsLoader>('PDF.js loader', {
  providedIn: 'root',
  factory: () => loadPdfJsWithWorker,
});
