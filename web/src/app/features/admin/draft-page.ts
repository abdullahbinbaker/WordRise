import { Component, OnInit, computed, inject, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AdminApi, describeError } from './admin.api';
import { CRITERION_LABELS, DraftDetail, STATUS_LABELS } from './admin.models';

/** One lesson: what the AI wrote, what the reviewer found, and the person's decision. */
@Component({
  selector: 'app-draft-page',
  imports: [FormsModule, RouterLink],
  styleUrl: './admin-page.css',
  template: `
    <p><a routerLink="/admin">← كل الدروس</a></p>

    @if (draft(); as d) {
      <h1>{{ d.content?.title || d.brief.scenario }}</h1>
      <p class="lead">
        <span class="badge" [class]="'badge ' + d.status">{{ statusLabel() }}</span>
        الموقف: {{ d.brief.scenario }} · القاعدة: <span class="en" dir="ltr">{{ d.brief.grammarStep }}. {{ d.brief.grammarPoint }}</span>
        ({{ d.brief.level }}) · {{ d.brief.wordCount }} كلمات
      </p>

      @if (message()) {
        <p class="ok" role="status">{{ message() }}</p>
      }
      @if (error()) {
        <p class="error" role="alert">{{ error() }}</p>
      }
      @if (d.error) {
        <p class="error" role="alert">سبب التعطّل: {{ d.error }}</p>
      }

      @if (d.status === 'aiApproved' || d.status === 'needsAttention') {
        <section class="card" aria-labelledby="decide">
          <h2 id="decide">قرارك</h2>
          @if (d.status === 'needsAttention') {
            <p class="muted">لم يرضَ المراجع عن الدرس بعد المحاولات المسموحة. اقرأ ملاحظاته أدناه، ثم انشره كما هو أو أعده بملاحظة.</p>
          }
          <div class="row">
            <button type="button" class="btn-primary" [disabled]="busy()" (click)="approve()">انشر للمتعلّمين</button>
          </div>
          <h3>أو أعده إلى الكاتب</h3>
          <textarea [(ngModel)]="note" aria-label="ما الذي يجب تغييره؟" placeholder="اكتب ما يجب تغييره، والكاتب يعدّل الدرس بناءً عليه."></textarea>
          <div class="row" style="margin-top: 10px">
            <button type="button" class="btn-warn" [disabled]="busy() || !note.trim()" (click)="reject()">أعده مع الملاحظة</button>
          </div>
        </section>
      }

      @if (d.status === 'failed') {
        <section class="card">
          <p>تعطّلت المحاولة قبل أن تكتمل (مثلاً انقطاع الخدمة أو غياب مفتاح Claude). لا شيء ضاع.</p>
          <button type="button" class="btn-primary" [disabled]="busy()" (click)="requeue()">أعد المحاولة</button>
        </section>
      }

      @if (d.status === 'queued' || d.status === 'aiApproved' || d.status === 'needsAttention' || d.status === 'failed') {
        <section class="card">
          @if (confirmingDiscard()) {
            <p>سيُحفظ الدرس في تبويب «ملغى» ولن يصل إلى المتعلّمين، ويمكنك استرجاعه لاحقاً. هل تريد إلغاءه؟</p>
            <div class="row">
              <button type="button" class="btn-warn" [disabled]="busy()" (click)="discard()">نعم، ألغِ الدرس</button>
              <button type="button" class="btn-quiet" [disabled]="busy()" (click)="confirmingDiscard.set(false)">تراجع</button>
            </div>
          } @else {
            <button type="button" class="btn-quiet" [disabled]="busy()" (click)="confirmingDiscard.set(true)">ألغِ هذا الدرس</button>
          }
        </section>
      }

      @if (d.status === 'discarded') {
        <section class="card">
          <p>هذا الدرس مُلغى: محفوظ للسجلّ ولا يصل إلى المتعلّمين.</p>
          <button type="button" class="btn-primary" [disabled]="busy()" (click)="restore()">استرجع الدرس</button>
        </section>
      }

      @if (d.status === 'published') {
        <p class="ok">منشور للمتعلّمين كالدرس رقم {{ d.publishedLessonNumber }}.</p>
      }

      @if (d.feedback.length > 0) {
        <section class="card">
          <h2>ملاحظاتك السابقة</h2>
          <ul class="plain">
            @for (item of d.feedback; track item.atUtc) {
              <li>{{ item.text }}</li>
            }
          </ul>
        </section>
      }

      @if (d.content; as c) {
        <section class="card">
          <h2>الكلمات</h2>
          <table>
            <thead><tr><th>English</th><th>العربية</th><th>النطق</th><th>مثال</th></tr></thead>
            <tbody>
              @for (word of c.words; track word.target) {
                <tr>
                  <td class="en" dir="ltr"><strong>{{ word.target }}</strong></td>
                  <td>{{ word.native }}</td>
                  <td>{{ word.pronunciation }}</td>
                  <td><span class="en" dir="ltr">{{ word.example }}</span><br /><span class="muted">{{ word.exampleNative }}</span></td>
                </tr>
              }
            </tbody>
          </table>
        </section>

        <section class="card">
          <h2>الحوار</h2>
          @for (line of c.dialogue; track $index) {
            <p>
              <strong>{{ line.speaker }}{{ line.isLearner ? ' (المتعلّم)' : '' }}:</strong>
              <span class="en" dir="ltr"> {{ line.target }}</span><br />
              <span class="muted">{{ line.native }}</span>
            </p>
          }
        </section>

        <section class="card">
          <h2>القاعدة: <span class="en" dir="ltr">{{ c.grammar.title }}</span> — {{ c.grammar.meaning }}</h2>
          <p>{{ c.grammar.explanation }}</p>
          <p class="muted">{{ c.grammar.pattern }}</p>
          <ul class="plain">
            @for (example of c.grammar.examples; track example.target) {
              <li><span class="en" dir="ltr">{{ example.target }}</span> — {{ example.native }}</li>
            }
          </ul>
          @if (c.grammar.note) {
            <p class="muted">{{ c.grammar.note }}</p>
          }
        </section>

        <section class="card">
          <h2>التمارين</h2>
          <h3>{{ c.chatPrompt }}</h3>
          <ul class="plain">
            @for (option of c.chatOptions; track option.text) {
              <li class="en" dir="ltr">{{ option.text }} {{ option.isCorrect ? '✔' : '' }}</li>
            }
          </ul>
          <h3>استماع: <span class="en" dir="ltr">{{ c.listeningSentence }}</span></h3>
          <ul class="plain">
            @for (option of c.listeningOptions; track option.text) {
              <li class="en" dir="ltr">{{ option.text }} {{ option.isCorrect ? '✔' : '' }}</li>
            }
          </ul>
        </section>
      }

      @if (d.revisions.length > 0) {
        <section class="card">
          <h2>حكم المراجع</h2>
          @for (revision of d.revisions; track revision.number) {
            <h3>
              المحاولة {{ revision.number }} —
              {{ revision.passed ? 'اجتازت' : 'لم تجتز' }}
              ({{ revision.source === 'ai' ? 'مراجع الذكاء الاصطناعي' : 'فحص الشكل' }})
            </h3>
            <p>{{ revision.summary }}</p>
            @if (revision.criteria.length > 0) {
              <ul class="plain">
                @for (criterion of revision.criteria; track criterion.criterion) {
                  <li>{{ criterionLabel(criterion.criterion) }}: {{ criterion.passed ? '✔' : '✘' }} {{ criterion.comment }}</li>
                }
              </ul>
            }
            @for (issue of revision.issues; track $index) {
              <p class="muted">
                {{ issue.severity === 'blocking' ? 'مشكلة' : 'اقتراح' }} ({{ issue.where }}): {{ issue.problem }}
                @if (issue.suggestion) {
                  — {{ issue.suggestion }}
                }
              </p>
            }
          }
        </section>
      }
    } @else if (loadError()) {
      <p class="error" role="alert">{{ loadError() }}</p>
    } @else {
      <p class="muted">جارٍ التحميل…</p>
    }
  `,
})
export class DraftPage implements OnInit {
  private readonly api = inject(AdminApi);

  /** From the route, `/admin/drafts/:id`. */
  readonly id = input.required<string>();

  protected readonly draft = signal<DraftDetail | null>(null);
  protected readonly busy = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly message = signal<string | null>(null);
  protected readonly loadError = signal<string | null>(null);
  protected note = '';
  protected readonly confirmingDiscard = signal(false);

  protected readonly statusLabel = computed(() => {
    const draft = this.draft();
    return draft ? STATUS_LABELS[draft.status] : '';
  });

  protected criterionLabel = (key: string) => CRITERION_LABELS[key] ?? key;

  ngOnInit(): Promise<void> {
    return this.load();
  }

  protected approve(): Promise<void> {
    return this.act(() => this.api.approve(this.id()), 'نُشر الدرس للمتعلّمين.');
  }

  protected async reject(): Promise<void> {
    const note = this.note;
    await this.act(() => this.api.reject(this.id(), note), 'أُعيد الدرس إلى الكاتب مع ملاحظتك.');
    this.note = '';
  }

  protected async discard(): Promise<void> {
    await this.act(() => this.api.discard(this.id()), 'أُلغي الدرس. تجده في تبويب «ملغى».');
    this.confirmingDiscard.set(false);
  }

  protected restore(): Promise<void> {
    return this.act(() => this.api.restore(this.id()), 'استُرجع الدرس.');
  }

  protected requeue(): Promise<void> {
    return this.act(() => this.api.requeue(this.id()), 'أُعيد الدرس إلى الطابور.');
  }

  private async act(action: () => Promise<unknown>, success: string): Promise<void> {
    this.busy.set(true);
    this.error.set(null);
    this.message.set(null);
    try {
      await action();
      this.message.set(success);
      await this.load();
    } catch (error) {
      this.error.set(describeError(error, 'تعذّر تنفيذ الطلب.'));
    } finally {
      this.busy.set(false);
    }
  }

  private async load(): Promise<void> {
    try {
      this.draft.set(await this.api.draft(this.id()));
    } catch (error) {
      this.loadError.set(describeError(error, 'تعذّر تحميل الدرس.'));
    }
  }
}
