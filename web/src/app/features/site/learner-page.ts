import { Component } from '@angular/core';
import { Learner } from '../learner/learner';

/** Full-screen learner app for phones (`/app`). */
@Component({
  selector: 'app-learner-page',
  imports: [Learner],
  template: `
    <div class="frame">
      <app-learner start="goal" tabs="bottom" />
    </div>
  `,
  styles: `
    :host {
      display: flex;
      justify-content: center;
      min-height: 100dvh;
      background: #E9E6E0;
    }

    .frame {
      width: 100%;
      max-width: 480px;
      height: 100dvh;
      background: var(--bg);
      display: flex;
      flex-direction: column;
    }
  `,
})
export class LearnerPage {}
