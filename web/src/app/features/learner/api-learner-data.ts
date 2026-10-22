import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, firstValueFrom } from 'rxjs';
import {
  CompleteResult,
  DailyGoal,
  LearnerData,
  Lesson,
  MyWord,
  Rating,
  ReviewCard,
  Today,
  WordFilter,
} from './learner.models';

const BASE = '/api/v1/learning';

/** API error codes, in the learner's language. */
const ERROR_MESSAGES: Record<string, string> = {
  'learning.review_backlog': 'راجع كلماتك أولاً: الدروس الجديدة متوقفة حتى تقلّ المراجعات المتراكمة.',
  'learning.reviews_disabled': 'المراجعة متوقفة حالياً.',
  'learning.daily_goal_invalid': 'اختر 5 أو 10 أو 15 كلمة.',
  'learning.lesson_not_found': 'هذا الدرس غير موجود.',
  'learning.word_not_found': 'هذه الكلمة ليست في مراجعاتك.',
};

/** The signed-in learner's real data, through the Learning API. */
@Injectable({ providedIn: 'root' })
export class ApiLearnerData extends LearnerData {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  today = () => this.call(this.http.get<Today>(`${BASE}/today`));
  goal = () => this.call(this.http.get<DailyGoal>(`${BASE}/goal`));
  setGoal = (wordsPerDay: number) => this.call(this.http.put<void>(`${BASE}/goal`, { wordsPerDay }));
  lesson = (lessonId: string) => this.call(this.http.get<Lesson>(`${BASE}/lessons/${lessonId}`));
  dueReviews = () => this.call(this.http.get<ReviewCard[]>(`${BASE}/reviews/due`));
  rate = (wordId: string, rating: Rating) => this.call(this.http.post<void>(`${BASE}/reviews/${wordId}`, { rating }));
  completeLesson = (lessonId: string) =>
    this.call(this.http.post<CompleteResult>(`${BASE}/lessons/${lessonId}/complete`, null));
  words = (filter: WordFilter) => this.call(this.http.get<MyWord[]>(`${BASE}/words`, { params: { filter } }));

  private async call<T>(request: Observable<T>): Promise<T> {
    try {
      return await firstValueFrom(request);
    } catch (error) {
      if (error instanceof HttpErrorResponse && error.status === 401) {
        await this.router.navigate(['/login'], { queryParams: { returnUrl: '/app' } });
      }
      throw new Error(this.message(error));
    }
  }

  private message(error: unknown): string {
    if (!(error instanceof HttpErrorResponse)) {
      return 'حدث خطأ غير متوقع. حاول مرة أخرى.';
    }
    if (error.status === 0) {
      return 'تعذّر الاتصال بالخادم. تحقّق من الإنترنت وحاول مرة أخرى.';
    }
    if (error.status === 401) {
      return 'انتهت جلستك. سجّل الدخول مرة أخرى.';
    }

    // Business rules answer { code }, validation answers { errors: { code: [...] } }.
    const body = error.error as { code?: string; errors?: Record<string, string[]> } | null;
    const code = body?.code ?? Object.keys(body?.errors ?? {})[0];
    return (code && ERROR_MESSAGES[code]) || 'حدث خطأ. حاول مرة أخرى.';
  }
}
