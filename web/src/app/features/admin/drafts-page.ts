import { DatePipe } from '@angular/common';
import { Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AdminApi, describeError } from './admin.api';
import { DraftCount, DraftListItem, DraftStatus, GrammarPoint, STATUS_LABELS } from './admin.models';

const FILTERS: { value: DraftStatus | undefined; label: string }[] = [
  { value: undefined, label: 'الكل' },
  { value: 'aiApproved', label: STATUS_LABELS.aiApproved },
  { value: 'needsAttention', label: STATUS_LABELS.needsAttention },
  { value: 'queued', label: STATUS_LABELS.queued },
  { value: 'inProgress', label: STATUS_LABELS.inProgress },
  { value: 'failed', label: STATUS_LABELS.failed },
  { value: 'published', label: STATUS_LABELS.published },
  { value: 'discarded', label: STATUS_LABELS.discarded },
];

/** The review queue: lessons the AI wrote, waiting for a person, plus a form to ask for more. */
@Component({
  selector: 'app-drafts-page',
  imports: [ReactiveFormsModule, RouterLink, DatePipe],
  styleUrl: './admin-page.css',
  template: `
    <h1>مراجعة الدروس</h1>
    <p class="lead">اطلب درساً جديداً، ثم راجع ما كتبه الذكاء الاصطناعي قبل أن يصل إلى المتعلّمين.</p>

    <section class="card" aria-labelledby="new-title">
      <h2 id="new-title">درس جديد</h2>
      <form class="row" [formGroup]="form" (ngSubmit)="create()" novalidate>
        <div class="field">
          <label for="d-scenario">الموقف</label>
          <input id="d-scenario" formControlName="scenario" placeholder="مثلاً: في البقالة" maxlength="60" />
        </div>
        <div class="field">
          <label for="d-step">قاعدة الدرس</label>
          <select id="d-step" formControlName="grammarStep">
            @for (point of curriculum(); track point.number) {
              <option [ngValue]="point.number">{{ point.number }}. {{ point.title }} ({{ point.level }})</option>
            }
          </select>
        </div>
        <div class="field" style="max-width: 120px">
          <label for="d-words">عدد الكلمات</label>
          <input id="d-words" type="number" min="3" max="10" formControlName="wordCount" />
        </div>
        <button type="submit" class="btn-primary" [disabled]="busy()">اطلب الدرس</button>
      </form>
      @if (formError()) {
        <p class="error" role="alert" style="margin-top: 12px">{{ formError() }}</p>
      }
    </section>

    <div class="tabs" role="group" aria-label="تصفية حسب الحالة">
      @for (filter of filters; track filter.label) {
        <button type="button" [class.on]="filter.value === status()" (click)="setFilter(filter.value)">
          {{ filter.label }}
          @if (countOf(filter.value); as n) {
            <span class="muted">({{ n }})</span>
          }
        </button>
      }
    </div>

    @if (loadError()) {
      <p class="error" role="alert">{{ loadError() }}</p>
    }

    <section class="card">
      @if (drafts().length === 0) {
        <p class="muted">لا توجد دروس هنا بعد.</p>
      } @else {
        <table>
          <thead>
            <tr>
              <th>الموقف</th>
              <th>القاعدة</th>
              <th>الحالة</th>
              <th>المحاولات</th>
              <th>آخر تحديث</th>
            </tr>
          </thead>
          <tbody>
            @for (draft of drafts(); track draft.id) {
              <tr>
                <td>
                  <a [routerLink]="['/admin/drafts', draft.id]">{{ draft.scenario }}</a>
                </td>
                <td class="en" dir="ltr">{{ draft.grammarStep }}. {{ draft.grammarPoint }}</td>
                <td>
                  <span class="badge" [class]="'badge ' + draft.status">{{ label(draft.status) }}</span>
                  @if (draft.publishedLessonNumber) {
                    <span class="muted"> الدرس {{ draft.publishedLessonNumber }}</span>
                  }
                </td>
                <td>{{ draft.revisionCount }}</td>
                <td class="muted">{{ draft.updatedAtUtc | date: 'short' }}</td>
              </tr>
            }
          </tbody>
        </table>
      }
    </section>
  `,
})
export class DraftsPage implements OnInit {
  private readonly api = inject(AdminApi);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly filters = FILTERS;
  protected readonly drafts = signal<DraftListItem[]>([]);
  protected readonly curriculum = signal<GrammarPoint[]>([]);
  protected readonly status = signal<DraftStatus | undefined>(undefined);
  protected readonly busy = signal(false);
  protected readonly formError = signal<string | null>(null);
  protected readonly loadError = signal<string | null>(null);

  protected readonly form = new FormGroup({
    scenario: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    grammarStep: new FormControl(1, { nonNullable: true }),
    wordCount: new FormControl(5, { nonNullable: true }),
  });

  protected readonly counts = signal<DraftCount[]>([]);

  protected label = (status: DraftStatus) => STATUS_LABELS[status];

  /** The number on a tab; "all" counts everything still alive (not the discarded). */
  protected countOf(status: DraftStatus | undefined): number {
    const counts = this.counts();
    return status
      ? (counts.find((c) => c.status === status)?.count ?? 0)
      : counts.filter((c) => c.status !== 'discarded').reduce((sum, c) => sum + c.count, 0);
  }

  async ngOnInit(): Promise<void> {
    // New drafts move on their own while the worker runs, so the list refreshes itself.
    const timer = setInterval(() => void this.refresh(), 8000);
    this.destroyRef.onDestroy(() => clearInterval(timer));

    try {
      this.curriculum.set(await this.api.curriculum());
    } catch (error) {
      this.loadError.set(describeError(error, 'تعذّر تحميل قواعد المنهج.'));
    }
    await this.refresh();
  }

  protected async setFilter(status: DraftStatus | undefined): Promise<void> {
    this.status.set(status);
    await this.refresh();
  }

  protected async create(): Promise<void> {
    const { scenario, grammarStep, wordCount } = this.form.getRawValue();
    if (scenario.trim().length < 2) {
      this.formError.set('اكتب الموقف (حرفان على الأقل).');
      return;
    }

    this.busy.set(true);
    this.formError.set(null);
    try {
      await this.api.createDraft(scenario.trim(), grammarStep, wordCount);
      this.form.controls.scenario.reset('');
      await this.refresh();
    } catch (error) {
      this.formError.set(describeError(error, 'تعذّر إنشاء الطلب.'));
    } finally {
      this.busy.set(false);
    }
  }

  private async refresh(): Promise<void> {
    try {
      const [drafts, counts] = await Promise.all([this.api.drafts(this.status()), this.api.draftCounts()]);
      this.drafts.set(drafts);
      this.counts.set(counts);
      this.loadError.set(null);
    } catch (error) {
      this.loadError.set(describeError(error, 'تعذّر تحميل الدروس.'));
    }
  }
}
