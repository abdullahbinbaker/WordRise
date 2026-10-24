import { Component, inject, input, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';

/** The page the "forgot password" email links to (`?email=...&code=...`). */
@Component({
  selector: 'app-reset-password-page',
  imports: [ReactiveFormsModule, RouterLink],
  template: `
    <main class="panel" dir="rtl">
      <a class="brand" routerLink="/" aria-label="Wordrise، الصفحة الرئيسية">
        <span class="brand__mark en" aria-hidden="true">W</span>
        <span class="brand__name en" dir="ltr">Wordrise</span>
      </a>

      <div class="intro">
        <h1>كلمة مرور جديدة</h1>
        <p>اختر كلمة مرور جديدة لحسابك.</p>
      </div>

      @if (done()) {
        <p class="note" role="status">تم تغيير كلمة المرور. يمكنك الآن تسجيل الدخول بها.</p>
        <a class="btn-primary" routerLink="/login">تسجيل الدخول</a>
      } @else if (!email() || !code()) {
        <p class="error" role="alert">هذا الرابط ناقص. افتحه من الرسالة التي وصلتك، أو اطلب رابطاً جديداً.</p>
        <p class="switch"><a routerLink="/forgot-password">اطلب رابطاً جديداً</a></p>
      } @else {
        <form [formGroup]="form" (ngSubmit)="submit()" novalidate>
          <div class="field">
            <label for="reset-password">كلمة المرور الجديدة</label>
            <input id="reset-password" class="ltr" type="password" autocomplete="new-password" formControlName="password" />
            <span class="field__hint">8 أحرف على الأقل، وفيها رقم واحد على الأقل</span>
          </div>

          @if (error()) {
            <p class="error" role="alert">{{ error() }}</p>
          }

          <button type="submit" class="btn-primary" [disabled]="busy()">
            {{ busy() ? 'جارٍ الحفظ…' : 'احفظ كلمة المرور' }}
          </button>
        </form>
      }
    </main>
  `,
  styleUrl: './auth-page.css',
})
export class ResetPasswordPage {
  private readonly auth = inject(AuthService);

  readonly email = input<string>('');
  readonly code = input<string>('');

  protected readonly busy = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly done = signal(false);

  protected readonly form = new FormGroup({
    password: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.minLength(8), Validators.pattern(/\d/)],
    }),
  });

  protected async submit(): Promise<void> {
    if (this.form.invalid) {
      this.error.set('كلمة المرور يجب أن تكون 8 أحرف على الأقل، وفيها رقم واحد على الأقل.');
      return;
    }

    this.busy.set(true);
    this.error.set(null);
    try {
      await this.auth.resetPassword(this.email(), this.code(), this.form.getRawValue().password);
      this.done.set(true);
    } catch (error) {
      this.error.set((error as Error).message);
    } finally {
      this.busy.set(false);
    }
  }
}
