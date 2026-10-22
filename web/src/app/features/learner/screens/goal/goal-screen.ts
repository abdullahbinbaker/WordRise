import { Component, computed, inject } from '@angular/core';
import { ArabicDigitsPipe, toArabicDigits } from '../../../../core/arabic-digits.pipe';
import { LearnerStore } from '../../learner.store';

const GOAL_LABELS: Record<number, { title: string; sub: string }> = {
  5: { title: 'بداية هادئة', sub: 'حوالي ٥ دقائق يومياً' },
  10: { title: 'متوازن', sub: 'حوالي ١٠ دقائق يومياً' },
  15: { title: 'طموح', sub: 'حوالي ١٥ دقيقة يومياً' },
};

@Component({
  selector: 'app-goal-screen',
  imports: [ArabicDigitsPipe],
  templateUrl: './goal-screen.html',
  styleUrl: './goal-screen.css',
  host: { class: 'screen' },
})
export class GoalScreen {
  protected readonly store = inject(LearnerStore);

  protected readonly options = computed(() =>
    this.store.allowedGoals().map((count) => ({
      count,
      ...(GOAL_LABELS[count] ?? { title: `${toArabicDigits(count)} كلمات`, sub: '' }),
    })),
  );

  protected readonly tip = computed(() => {
    const goal = this.store.goal();
    return `${toArabicDigits(goal)} كلمات يومياً = ${toArabicDigits(goal * 30)} كلمة في الشهر`;
  });
}
