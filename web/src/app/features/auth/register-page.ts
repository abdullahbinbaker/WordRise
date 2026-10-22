import { Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'app-register-page',
  imports: [ReactiveFormsModule, RouterLink],
  template: `
    <main class="panel" dir="rtl">
      <a class="brand" routerLink="/" aria-label="Wordrise، الصفحة الرئيسية">
        <span class="brand__mark en" aria-hidden="true">W</span>
        <span class="brand__name en" dir="ltr">Wordrise</span>
      </a>

      <div class="intro">
        <h1>ابدأ تعلّم الإنجليزية</h1>
        <p>مجاني بالكامل. دقيقة واحدة وتبدأ أول درس.</p>
      </div>

      <form [formGroup]="form" (ngSubmit)="submit()" novalidate>
        <div class="field">
          <label for="register-name">اسمك</label>
          <input id="register-name" type="text" autocomplete="name" formControlName="displayName" />
          <span class="field__hint">نناديك به في التطبيق، مثلاً: أبو خالد</span>
        </div>
        <div class="field">
          <label for="register-email">البريد الإلكتروني</label>
          <input id="register-email" class="ltr" type="email" autocomplete="email" formControlName="email" />
        </div>
        <div class="field">
          <label for="register-password">كلمة المرور</label>
          <input id="register-password" class="ltr" type="password" autocomplete="new-password" formControlName="password" />
          <span class="field__hint">8 أحرف على الأقل، وفيها رقم واحد على الأقل</span>
        </div>

        @if (error()) {
          <p class="error" role="alert">{{ error() }}</p>
        }

        <button type="submit" class="btn-primary" [disabled]="busy()">
          {{ busy() ? 'جارٍ إنشاء الحساب…' : 'إنشاء الحساب' }}
        </button>
      </form>

      <p class="switch">لديك حساب؟ <a routerLink="/login">سجّل الدخول</a></p>
    </main>
  `,
  styleUrl: './auth-page.css',
})
export class RegisterPage {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly busy = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly form = new FormGroup({
    displayName: new FormControl('', { nonNullable: true, validators: [Validators.maxLength(40)] }),
    email: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.email] }),
    password: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.minLength(8), Validators.pattern(/\d/)],
    }),
  });

  protected async submit(): Promise<void> {
    const controls = this.form.controls;
    if (controls.email.invalid) {
      this.error.set('أدخل بريداً إلكترونياً صحيحاً.');
      return;
    }
    if (controls.password.invalid) {
      this.error.set('كلمة المرور يجب أن تكون 8 أحرف على الأقل، وفيها رقم واحد على الأقل.');
      return;
    }
    if (controls.displayName.invalid) {
      this.error.set('الاسم يجب ألا يزيد على 40 حرفاً.');
      return;
    }

    this.busy.set(true);
    this.error.set(null);
    try {
      const { displayName, email, password } = this.form.getRawValue();
      await this.auth.register(displayName, email, password);
      // A new learner starts by choosing their daily word commitment.
      await this.router.navigate(['/app'], { queryParams: { start: 'goal' } });
    } catch (error) {
      this.error.set((error as Error).message);
    } finally {
      this.busy.set(false);
    }
  }
}
