import { Component, computed, inject } from '@angular/core';
import { ArabicDigitsPipe, toArabicDigits } from '../../../../core/arabic-digits.pipe';
import { LearnerStore } from '../../learner.store';
import { GOAL_OPTIONS } from '../../lesson.data';

@Component({
  selector: 'app-goal-screen',
  imports: [ArabicDigitsPipe],
  templateUrl: './goal-screen.html',
  styleUrl: './goal-screen.css',
  host: { class: 'screen' },
})
export class GoalScreen {
  protected readonly store = inject(LearnerStore);
  protected readonly options = GOAL_OPTIONS;

  protected readonly tip = computed(() => {
    const goal = this.store.goal();
    return `${toArabicDigits(goal)} كلمات يومياً = ${toArabicDigits(goal * 30)} كلمة في الشهر`;
  });
}
