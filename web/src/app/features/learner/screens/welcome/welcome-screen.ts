import { Component, inject } from '@angular/core';
import { LearnerStore } from '../../learner.store';

/** Asked once, right after the daily goal: do you know the letters, and where do you want to start? */
@Component({
  selector: 'app-welcome-screen',
  templateUrl: './welcome-screen.html',
  styleUrl: './welcome-screen.css',
  host: { class: 'screen' },
})
export class WelcomeScreen {
  protected readonly store = inject(LearnerStore);
}
