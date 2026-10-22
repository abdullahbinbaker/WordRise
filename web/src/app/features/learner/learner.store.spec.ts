import { TestBed } from '@angular/core/testing';
import { DEMO_LESSON, DemoLearnerData } from './demo-learner-data';
import { LearnerStore, answerStep } from './learner.store';
import { LearnerData, ReviewCard } from './learner.models';

describe('LearnerStore', () => {
  let store: LearnerStore;

  beforeEach(async () => {
    TestBed.configureTestingModule({ providers: [LearnerStore] });
    store = TestBed.inject(LearnerStore);
    await store.init(new DemoLearnerData(), 'home');
  });

  it('loads today and the goal on start', () => {
    expect(store.today()?.nextLesson?.title).toBe(DEMO_LESSON.title);
    expect(store.goal()).toBe(10);
    expect(store.allowedGoals()).toEqual([5, 10, 15]);
  });

  it('treats the goal screen as the onboarding start', async () => {
    await store.init(new DemoLearnerData(), 'goal');
    expect(store.onboarded()).toBe(false);
  });

  it('starts a lesson with the due reviews, then moves to the new words', async () => {
    await store.startLesson();
    expect(store.screen()).toBe('review');

    for (let i = 0; i < store.reviewCards().length; i++) {
      store.revealCard();
      await store.rateCard('good');
    }

    expect(store.screen()).toBe('newword');
    expect(store.reviewedCount()).toBe(4);
    expect(store.lessonProgress()).toBe(20);
  });

  it('skips the review step when nothing is due', async () => {
    const noReviews = new DemoLearnerData();
    noReviews.dueReviews = () => Promise.resolve<ReviewCard[]>([]);
    await store.init(noReviews, 'home');

    await store.startLesson();

    expect(store.screen()).toBe('newword');
  });

  it('saves the lesson at the end and shows the result', async () => {
    await store.startLesson();
    await store.finishLesson();

    expect(store.screen()).toBe('done');
    expect(store.completion()).toEqual({ alreadyCompleted: false, wordsAdded: 3, streak: 6 });
  });

  it('a review-only session ends on the done screen without a lesson', async () => {
    await store.startReview();
    for (let i = 0; i < 4; i++) {
      await store.rateCard('easy');
    }

    expect(store.screen()).toBe('done');
    expect(store.lesson()).toBeNull();
    expect(store.reviewedCount()).toBe(4);
  });

  it('shows the error and stays on the card when saving a rating fails', async () => {
    const failing = new DemoLearnerData();
    failing.rate = () => Promise.reject(new Error('المراجعة متوقفة حالياً.'));
    await store.init(failing as LearnerData, 'home');
    await store.startLesson();

    await store.rateCard('hard');

    expect(store.error()).toBe('المراجعة متوقفة حالياً.');
    expect(store.cardIndex()).toBe(0);
  });

  it('walks the quiz button through choose, check, retry and next', () => {
    expect(answerStep(null, false, false)).toBe('choose');
    expect(answerStep(0, false, true)).toBe('check');
    expect(answerStep(0, true, false)).toBe('retry');
    expect(answerStep(0, true, true)).toBe('next');
  });
});
