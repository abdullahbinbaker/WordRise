import { Component, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';

/** Frame of the staff area: a header with the sections, and the page below it. */
@Component({
  selector: 'app-admin-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  styleUrl: './admin.css',
  template: `
    <header class="bar" dir="rtl">
      <a class="brand" routerLink="/admin" aria-label="Wordrise، لوحة المراجعة">
        <span class="brand__mark en" aria-hidden="true">W</span>
        <span class="brand__name en" dir="ltr">Wordrise</span>
      </a>
      <nav aria-label="أقسام لوحة المراجعة">
        <a routerLink="/admin" routerLinkActive="on" [routerLinkActiveOptions]="{ exact: true }">الدروس</a>
        @if (auth.isAdmin()) {
          <a routerLink="/admin/staff" routerLinkActive="on">الفريق</a>
        }
        <a routerLink="/admin/security" routerLinkActive="on">الأمان</a>
      </nav>
      <span class="spacer"></span>
      <span class="who en" dir="ltr">{{ auth.user()?.email }}</span>
      <button type="button" class="btn-quiet" (click)="logout()">خروج</button>
    </header>
    <main class="page" dir="rtl">
      <router-outlet />
    </main>
  `,
})
export class AdminShell {
  protected readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected logout(): void {
    void this.auth.logout().then(() => this.router.navigateByUrl('/'));
  }
}
