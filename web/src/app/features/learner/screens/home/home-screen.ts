import { Component, computed, inject } from '@angular/core';
import { ArabicDigitsPipe, toArabicDigits } from '../../../../core/arabic-digits.pipe';
import { LearnerStore } from '../../learner.store';

type HomeAction = 'lesson' | 'review' | 'words';

@Component({
  selector: 'app-home-screen',
  imports: [ArabicDigitsPipe],
  templateUrl: './home-screen.html',
  styleUrl: './home-screen.css',
  host: { class: 'screen' },
})
export class HomeScreen {
  protected readonly store = inject(LearnerStore);
  protected readonly today = computed(() => this.store.today()!);

  protected readonly greeting = computed(() => {
    const hour = new Date().getHours();
    return hour < 12 ? 'صباح الخير' : 'مساء الخير';
  });

  /** The lesson steps; the review step only when there is something to review. */
  protected readonly steps = computed(() => {
    const today = this.today();
    const next = today.nextLesson;
    if (!next) return [];
    if (next.track === 'foundations') {
      return [`${toArabicDigits(next.newWordsCount)} عناصر نتعرّف عليها`, 'تمارين قصيرة للتثبيت'];
    }
    return [
      ...(today.reviewsEnabled && today.dueReviewCount > 0 ? [`مراجعة ${toArabicDigits(today.dueReviewCount)} كلمات`] : []),
      `${toArabicDigits(next.newWordsCount)} كلمات جديدة`,
      `محادثة: ${next.title}`,
      `قاعدة: ${next.grammarPoint}`,
      'تمرين استماع',
    ];
  });

  /** A message above the lesson card when today is not a regular lesson day. */
  protected readonly notice = computed(() => {
    const today = this.today();
    if (today.newLessonsPaused) {
      return `اليوم نثبّت ما تعلّمته: لديك ${toArabicDigits(today.dueReviewCount)} كلمة للمراجعة. تعود الدروس الجديدة عندما تصبح ${toArabicDigits(today.reviewBacklogLimit ?? 0)} أو أقل.`;
    }
    if (!today.nextLesson && !today.completedLessonToday) {
      return 'أنهيت كل الدروس المتاحة! دروس جديدة قريباً.';
    }
    return null;
  });

  protected readonly action = computed((): { kind: HomeAction; label: string } => {
    const today = this.today();
    const canReview = today.reviewsEnabled && today.dueReviewCount > 0;
    if (today.completedLessonToday) return { kind: 'words', label: 'تصفّح كلماتي' };
    if (today.newLessonsPaused) return { kind: 'review', label: 'ابدأ المراجعة' };
    if (today.nextLesson) return { kind: 'lesson', label: 'ابدأ درس اليوم' };
    if (canReview) return { kind: 'review', label: 'راجع كلماتك' };
    return { kind: 'words', label: 'تصفّح كلماتي' };
  });

  protected run(kind: HomeAction): void {
    if (kind === 'lesson') {
      void this.store.startLesson();
    } else if (kind === 'review') {
      void this.store.startReview();
    } else {
      this.store.go('words');
    }
  }
}
