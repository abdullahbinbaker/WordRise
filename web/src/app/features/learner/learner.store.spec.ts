import { LearnerStore, answerStep } from './learner.store';
import { REVIEW_CARDS } from './lesson.data';

describe('LearnerStore', () => {
  let store: LearnerStore;

  beforeEach(() => {
    store = new LearnerStore();
  });

  it('treats any start screen other than goal as already onboarded', () => {
    store.start('home');
    expect(store.onboarded()).toBe(true);
    store.start('goal');
    expect(store.onboarded()).toBe(false);
  });

  it('moves from the last review card to the new words', () => {
    store.startLesson();
    for (let i = 0; i < REVIEW_CARDS.length; i++) {
      store.revealCard();
      store.rateCard();
    }
    expect(store.screen()).toBe('newword');
    expect(store.lessonProgress()).toBe(20);
  });

  it('counts half a card once the meaning is revealed', () => {
    store.startLesson();
    store.revealCard();
    expect(store.lessonProgress()).toBe(2.5);
  });

  it('ignores a new pick after the chat answer is checked', () => {
    store.pickChat(1);
    store.checkChat();
    store.pickChat(0);
    expect(store.chatPick()).toBe(1);
    expect(store.chatOk()).toBe(false);
  });

  it('walks the quiz button through choose, check, retry and next', () => {
    expect(answerStep(null, false, false)).toBe('choose');
    expect(answerStep(0, false, true)).toBe('check');
    expect(answerStep(0, true, false)).toBe('retry');
    expect(answerStep(0, true, true)).toBe('next');
  });

  it('marks the lesson done and returns home', () => {
    store.go('done');
    store.finishLesson();
    expect(store.lessonDone()).toBe(true);
    expect(store.screen()).toBe('home');
  });
});
