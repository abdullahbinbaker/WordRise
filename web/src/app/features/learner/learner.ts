import { Component, OnInit, computed, inject, input } from '@angular/core';
import { LearnerStore, Screen } from './learner.store';
import { ChatScreen } from './screens/chat/chat-screen';
import { DoneScreen } from './screens/done/done-screen';
import { GoalScreen } from './screens/goal/goal-screen';
import { GrammarScreen } from './screens/grammar/grammar-screen';
import { HomeScreen } from './screens/home/home-screen';
import { ListenScreen } from './screens/listen/listen-screen';
import { NewWordScreen } from './screens/new-word/new-word-screen';
import { ReviewScreen } from './screens/review/review-screen';
import { WordsScreen } from './screens/words/words-screen';

/** The learner app: used full screen on phones and embedded in the website. */
@Component({
  selector: 'app-learner',
  imports: [
    GoalScreen,
    HomeScreen,
    ReviewScreen,
    NewWordScreen,
    ChatScreen,
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

  readonly start = input<Screen>('goal');
  readonly tabs = input<'top' | 'bottom'>('bottom');

  protected readonly tabItems: readonly { screen: Screen; label: string }[] = [
    { screen: 'home', label: 'الرئيسية' },
    { screen: 'words', label: 'كلماتي' },
    { screen: 'goal', label: 'هدفي' },
  ];

  protected readonly showTabs = computed(() => {
    const screen = this.store.screen();
    return screen === 'home' || screen === 'words' || (screen === 'goal' && this.store.onboarded());
  });

  ngOnInit(): void {
    this.store.start(this.start());
  }
}
