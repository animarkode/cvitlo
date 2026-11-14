import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'cv-hero-blobs',
  imports: [],
  templateUrl: './cv-hero-blobs.html',
  styleUrl: './cv-hero-blobs.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'absolute inset-0 w-full h-full pointer-events-none' },
})
export class CvHeroBlobs {}
