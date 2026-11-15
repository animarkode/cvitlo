import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { DialogRef } from '@angular/cdk/dialog';
import {
  LucideAngularModule,
  LucideIconProvider,
  LUCIDE_ICONS,
  User,
  FileText,
  Briefcase,
  GraduationCap,
  Zap,
  Globe,
  Link2,
  BookOpen,
  Heart,
  ChevronsUpDown,
  ChevronsDownUp,
} from 'lucide-angular';
import { ResumesStore } from '../../resumes-store';

export interface SectionSheetItem {
  readonly id: string;
  readonly label: string;
  readonly icon: string;
}

// Shared by the desktop rail and the mobile sheet so the two lists cannot drift apart.
export const SECTION_SHEET_ITEMS: readonly SectionSheetItem[] = [
  { id: 'person',     label: 'Personal Details',     icon: 'user' },
  { id: 'summary',    label: 'Professional Summary', icon: 'file-text' },
  { id: 'experience', label: 'Experience',            icon: 'briefcase' },
  { id: 'education',  label: 'Education',            icon: 'graduation-cap' },
  { id: 'skill',      label: 'Skills',               icon: 'zap' },
  { id: 'language',   label: 'Languages',            icon: 'globe' },
  { id: 'link',       label: 'Links',                icon: 'link-2' },
  { id: 'course',     label: 'Courses',              icon: 'book-open' },
  { id: 'project',    label: 'Projects',             icon: 'briefcase' },
  { id: 'hobby',      label: 'Hobbies',              icon: 'heart' },
];

// Below `md` the rail is replaced by this sheet. Focus trap, Escape and backdrop come from CDK Dialog.
@Component({
  selector: 'app-section-sheet',
  imports: [LucideAngularModule],
  providers: [
    {
      provide: LUCIDE_ICONS,
      multi: true,
      useValue: new LucideIconProvider({
        User, FileText, Briefcase, GraduationCap, Zap, Globe,
        Link2, BookOpen, Heart, ChevronsUpDown, ChevronsDownUp,
      }),
    },
  ],
  templateUrl: './section-sheet.html',
  styleUrl: './section-sheet.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SectionSheet {
  private readonly store = inject(ResumesStore);
  private readonly dialogRef = inject(DialogRef<void>);

  protected readonly items = SECTION_SHEET_ITEMS;

  protected select(id: string): void {
    this.store.requestSection(id);
    this.dialogRef.close();
  }

  protected expandAll(): void {
    this.store.requestAllSections('expand');
    this.dialogRef.close();
  }

  protected collapseAll(): void {
    this.store.requestAllSections('collapse');
    this.dialogRef.close();
  }

  protected close(): void {
    this.dialogRef.close();
  }
}
