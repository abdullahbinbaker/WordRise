import { Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'app-forgot-password-page',
  imports: [ReactiveFormsModule, RouterLink],
  template: `
    <main class="panel" dir="rtl">
      <a class="brand" routerLink="/" aria-label="Wordrise، الصفحة الرئيسية">
        <span class="brand__mark en" aria-hidden="true">W</span>
        <span class="brand__name en" dir="ltr">Wordrise</span>
      </a>

      <div class="intro">
        <h1>نسيت كلمة المرور؟</h1>
        <p>اكتب بريدك الإلكتروني وسنرسل إليك رابطاً لاختيار كلمة مرور جديدة.</p>
      </div>

      @if (sent()) {
        <p class="note" role="status">
          إن كان هذا البريد مسجّلاً لدينا فقد أرسلنا إليه الرابط. افتح بريدك واضغط عليه (وانظر في الرسائل غير المرغوبة أيضاً).
        </p>
      } @else {
        <form [formGroup]="form" (ngSubmit)="submit()" novalidate>
          <div class="field">
            <label for="forgot-email">البريد الإلكتروني</label>
            <input id="forgot-email" class="ltr" type="email" autocomplete="email" formControlName="email" />
          </div>

          @if (error()) {
            <p class="error" role="alert">{{ error() }}</p>
          }

          <button type="submit" class="btn-primary" [disabled]="busy()">
            {{ busy() ? 'جارٍ الإرسال…' : 'أرسل الرابط' }}
          </button>
        </form>
      }

      <p class="switch"><a routerLink="/login">العودة لتسجيل الدخول</a></p>
    </main>
  `,
  styleUrl: './auth-page.css',
})
export class ForgotPasswordPage {
  private readonly auth = inject(AuthService);

  protected readonly busy = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly sent = signal(false);

  protected readonly form = new FormGroup({
    email: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.email] }),
  });

  protected async submit(): Promise<void> {
    if (this.form.invalid) {
      this.error.set('أدخل بريداً إلكترونياً صحيحاً.');
      return;
    }

    this.busy.set(true);
    this.error.set(null);
    try {
      await this.auth.forgotPassword(this.form.getRawValue().email);
      this.sent.set(true);
    } catch (error) {
      this.error.set((error as Error).message);
    } finally {
      this.busy.set(false);
    }
  }
}
