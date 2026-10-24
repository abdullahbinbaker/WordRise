import { isPlatformBrowser } from '@angular/common';
import { Component, OnInit, PLATFORM_ID, inject, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';

/** The page the "confirm your email" message links to (`?userId=...&code=...`). */
@Component({
  selector: 'app-confirm-email-page',
  imports: [RouterLink],
  template: `
    <main class="panel" dir="rtl">
      <a class="brand" routerLink="/" aria-label="Wordrise، الصفحة الرئيسية">
        <span class="brand__mark en" aria-hidden="true">W</span>
        <span class="brand__name en" dir="ltr">Wordrise</span>
      </a>

      @switch (state()) {
        @case ('working') {
          <div class="intro"><h1>جارٍ تفعيل بريدك…</h1></div>
        }
        @case ('done') {
          <div class="intro"><h1>تم تفعيل بريدك</h1></div>
          <p class="note" role="status">شكراً لك. يمكنك الآن تسجيل الدخول.</p>
          <a class="btn-primary" routerLink="/login">تسجيل الدخول</a>
        }
        @case ('failed') {
          <div class="intro"><h1>تعذّر التفعيل</h1></div>
          <p class="error" role="alert">الرابط غير صالح أو انتهت صلاحيته. سجّل الدخول وسنرسل لك رابطاً جديداً.</p>
          <p class="switch"><a routerLink="/login">تسجيل الدخول</a></p>
        }
      }
    </main>
  `,
  styleUrl: './auth-page.css',
})
export class ConfirmEmailPage implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly browser = isPlatformBrowser(inject(PLATFORM_ID));

  readonly userId = input<string>('');
  readonly code = input<string>('');
  readonly changedEmail = input<string>('');

  protected readonly state = signal<'working' | 'done' | 'failed'>('working');

  async ngOnInit(): Promise<void> {
    // The link is single-use: only the browser may spend it, not the server render.
    if (!this.browser) return;

    if (!this.userId() || !this.code()) {
      this.state.set('failed');
      return;
    }

    try {
      await this.auth.confirmEmail(this.userId(), this.code(), this.changedEmail() || undefined);
      this.state.set('done');
    } catch {
      this.state.set('failed');
    }
  }
}
