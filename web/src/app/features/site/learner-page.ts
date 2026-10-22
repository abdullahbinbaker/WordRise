import { Component, computed, inject, input } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { Learner } from '../learner/learner';
import { Screen } from '../learner/learner.store';

/** The signed-in learner's app (`/app`), full screen on phones. */
@Component({
  selector: 'app-learner-page',
  imports: [Learner],
  template: `
    <div class="frame">
      <app-learner
        mode="live"
        tabs="bottom"
        [start]="startScreen()"
        [learnerName]="auth.displayName()"
        [logout]="logout"
      />
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
export class LearnerPage {
  protected readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  /** `?start=goal` right after registering: a new learner picks their daily goal first. */
  readonly start = input<string>();

  protected readonly startScreen = computed((): Screen => (this.start() === 'goal' ? 'goal' : 'home'));

  protected readonly logout = (): void => {
    void this.auth.logout().then(() => this.router.navigateByUrl('/'));
  };
}
