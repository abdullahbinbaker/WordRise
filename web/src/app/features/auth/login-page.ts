import { Component, inject, input, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService, EmailNotConfirmedError, TwoFactorRequiredError } from '../../core/auth/auth.service';

@Component({
  selector: 'app-login-page',
  imports: [ReactiveFormsModule, RouterLink],
  template: `
    <main class="panel" dir="rtl">
      <a class="brand" routerLink="/" aria-label="Wordrise، الصفحة الرئيسية">
        <span class="brand__mark en" aria-hidden="true">W</span>
        <span class="brand__name en" dir="ltr">Wordrise</span>
      </a>

      <div class="intro">
        <h1>أهلاً بعودتك</h1>
        <p>سجّل دخولك لتكمل درس اليوم.</p>
      </div>

      <form [formGroup]="form" (ngSubmit)="submit()" novalidate>
        <div class="field">
          <label for="login-email">البريد الإلكتروني</label>
          <input id="login-email" class="ltr" type="email" autocomplete="email" formControlName="email" />
        </div>
        <div class="field">
          <label for="login-password">كلمة المرور</label>
          <input id="login-password" class="ltr" type="password" autocomplete="current-password" formControlName="password" />
        </div>

        @if (needsCode()) {
          <div class="field">
            <label for="login-code">رمز المصادقة</label>
            <input id="login-code" class="ltr" inputmode="numeric" autocomplete="one-time-code" maxlength="8" formControlName="code" />
            <span class="field__hint">الرمز المكوّن من 6 أرقام في تطبيق المصادقة (أو رمز الاسترداد).</span>
          </div>
        }

        @if (error()) {
          <p class="error" role="alert">{{ error() }}</p>
        }
        @if (unconfirmed()) {
          @if (resent()) {
            <p class="note" role="status">أرسلنا الرابط من جديد. افتح بريدك.</p>
          } @else {
            <button type="button" class="link-button" [disabled]="busy()" (click)="resend()">أرسل رابط التفعيل مرة أخرى</button>
          }
        }

        <button type="submit" class="btn-primary" [disabled]="busy()">
          {{ busy() ? 'جارٍ الدخول…' : 'تسجيل الدخول' }}
        </button>
      </form>

      <p class="switch"><a routerLink="/forgot-password">نسيت كلمة المرور؟</a></p>
      <p class="switch">ليس لديك حساب؟ <a routerLink="/register">أنشئ حساباً مجاناً</a></p>
    </main>
  `,
  styleUrl: './auth-page.css',
})
export class LoginPage {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  /** Where to go after signing in (set by the auth guard). */
  readonly returnUrl = input<string>('');

  protected readonly busy = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly needsCode = signal(false);
  protected readonly unconfirmed = signal(false);
  protected readonly resent = signal(false);

  protected readonly form = new FormGroup({
    email: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.email] }),
    password: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    code: new FormControl('', { nonNullable: true }),
  });

  protected async resend(): Promise<void> {
    this.busy.set(true);
    try {
      await this.auth.resendConfirmation(this.form.getRawValue().email);
      this.resent.set(true);
    } catch (error) {
      this.error.set((error as Error).message);
    } finally {
      this.busy.set(false);
    }
  }

  protected async submit(): Promise<void> {
    if (this.form.invalid) {
      this.error.set('أدخل البريد الإلكتروني وكلمة المرور.');
      return;
    }

    this.busy.set(true);
    this.error.set(null);
    this.unconfirmed.set(false);
    try {
      const { email, password, code } = this.form.getRawValue();
      await this.auth.login(email, password, code.trim());
      await this.router.navigateByUrl(this.returnUrl() || (this.auth.isStaff() ? '/admin' : '/app'));
    } catch (error) {
      this.needsCode.set(this.needsCode() || error instanceof TwoFactorRequiredError);
      this.unconfirmed.set(error instanceof EmailNotConfirmedError);
      this.resent.set(false);
      this.error.set((error as Error).message);
    } finally {
      this.busy.set(false);
    }
  }
}
