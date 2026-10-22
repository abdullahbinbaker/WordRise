import { Component, OnInit, computed, effect, inject, input } from '@angular/core';
import { ApiLearnerData } from './api-learner-data';
import { DemoLearnerData } from './demo-learner-data';
import { LearnerStore, Screen } from './learner.store';
import { DoneScreen } from './screens/done/done-screen';
import { DialogueScreen } from './screens/dialogue/dialogue-screen';
import { GoalScreen } from './screens/goal/goal-screen';
import { GrammarScreen } from './screens/grammar/grammar-screen';
import { HomeScreen } from './screens/home/home-screen';
import { ListenScreen } from './screens/listen/listen-screen';
import { NewWordScreen } from './screens/new-word/new-word-screen';
import { ReviewScreen } from './screens/review/review-screen';
import { WordsScreen } from './screens/words/words-screen';

/**
 * The learner app: full screen on phones (live data) and embedded in the landing page
 * (demo data, nothing saved).
 */
@Component({
  selector: 'app-learner',
  imports: [
    GoalScreen,
    HomeScreen,
    ReviewScreen,
    NewWordScreen,
    DialogueScreen,
    GrammarScreen,
    ListenScreen,
    DoneScreen,
    WordsScreen,
  ],
  providers: [LearnerStore],
  templateUrl: './learner.html',
  styleUrl: './learner.css',
})
export class Learner implements OnInit {
  protected readonly store = inject(LearnerStore);
  private readonly api = inject(ApiLearnerData);

  readonly mode = input<'live' | 'demo'>('demo');
  readonly start = input<Screen>('home');
  readonly tabs = input<'top' | 'bottom'>('bottom');
  readonly learnerName = input('');
  readonly logout = input<(() => void) | null>(null);

  protected readonly tabItems: readonly { screen: Screen; label: string }[] = [
    { screen: 'home', label: 'الرئيسية' },
    { screen: 'words', label: 'كلماتي' },
    { screen: 'goal', label: 'هدفي' },
  ];

  protected readonly showTabs = computed(() => {
    const screen = this.store.screen();
    return screen === 'home' || screen === 'words' || (screen === 'goal' && this.store.onboarded());
  });

  /** Lesson screens need the lesson loaded; until then the shell shows progress. */
  protected readonly ready = computed(() => {
    const screen = this.store.screen();
    if (screen === 'home') return !!this.store.today();
    if (['newword', 'dialogue', 'grammar', 'listen'].includes(screen)) return !!this.store.lesson();
    return true;
  });

  constructor() {
    effect(() => this.store.learnerName.set(this.learnerName()));
    effect(() => this.store.logoutHandler.set(this.logout()));
  }

  ngOnInit(): void {
    void this.store.init(this.mode() === 'live' ? this.api : new DemoLearnerData(), this.start());
  }
}
