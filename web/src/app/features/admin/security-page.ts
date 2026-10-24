import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { AdminApi, describeError } from './admin.api';
import { buildOtpAuthUri, qrCodeFor } from './two-factor-qr';

/** Two-factor sign-in: staff cannot open the lesson tools without it. */
@Component({
  selector: 'app-security-page',
  imports: [FormsModule],
  styleUrl: './admin-page.css',
  template: `
    <h1>الأمان</h1>
    <p class="lead">حسابات الفريق تحتاج التحقق بخطوتين: رمز من تطبيق المصادقة (Google Authenticator أو Microsoft Authenticator أو غيرهما) في كل مرة تسجّل فيها الدخول.</p>

    @if (error()) {
      <p class="error" role="alert">{{ error() }}</p>
    }

    @if (recoveryCodes(); as codes) {
      <section class="card">
        <h2>رموز الاسترداد</h2>
        <p>احفظها في مكان آمن الآن؛ لن تظهر مرة أخرى. كل رمز يُستعمل مرة واحدة إذا ضاع هاتفك.</p>
        <ul class="plain en" dir="ltr">
          @for (code of codes; track code) {
            <li>{{ code }}</li>
          }
        </ul>
      </section>
    }

    @if (!enabled()) {
      <section class="card">
        <h2>تفعيل التحقق بخطوتين</h2>
        @if (key()) {
          <ol class="plain">
            <li>افتح تطبيق المصادقة على هاتفك واختر «إضافة حساب» ثم «مسح رمز QR».</li>
            <li>وجّه كاميرا التطبيق إلى هذا الرمز:</li>
          </ol>
           @if (qrImage(); as image) {
            <p><img class="qr" [src]="image" width="240" height="240" alt="رمز QR لإضافة الحساب إلى تطبيق المصادقة" /></p>
          }
          <details>
            <summary>لا تستطيع المسح؟ أدخل المفتاح يدوياً</summary>
            <p>في التطبيق اختر «إدخال مفتاح الإعداد» (الحساب: Wordrise، النوع: مبني على الوقت):</p>
            <p><span class="key">{{ formattedKey() }}</span></p>
            <p class="muted">أو إن كنتَ تفتح هذه الصفحة من الهاتف نفسه: <a [href]="otpauthUri()">إضافة إلى تطبيق المصادقة</a></p>
          </details>
          <p class="muted">بعد الإضافة، اكتب الرمز المكوّن من 6 أرقام الظاهر في التطبيق:</p>
          <div class="row">
            <div class="field" style="max-width: 220px">
              <label for="sec-code">الرمز المكوّن من 6 أرقام</label>
              <input id="sec-code" class="ltr" inputmode="numeric" autocomplete="one-time-code" maxlength="8" name="code" [(ngModel)]="code" />
            </div>
            <button type="button" class="btn-primary" [disabled]="busy() || !code.trim()" (click)="enable()">فعّل</button>
          </div>
        } @else {
          <p class="muted">جارٍ التحضير…</p>
        }
      </section>
    } @else if (!signedInWithTwoFactor()) {
      <section class="card">
        <h2>التحقق مفعّل، لكن هذه الجلسة بدون رمز</h2>
        <p>سجّل الخروج ثم ادخل من جديد وأدخل الرمز لتفتح أدوات الدروس.</p>
        <button type="button" class="btn-primary" (click)="signInAgain()">سجّل الدخول بالرمز</button>
      </section>
    } @else {
      <p class="ok">التحقق بخطوتين مفعّل وهذه الجلسة مفتوحة بالرمز.</p>
    }
  `,
})
export class SecurityPage implements OnInit {
  private readonly api = inject(AdminApi);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly key = signal<string | null>(null);
  protected readonly enabled = signal(false);
  protected readonly recoveryCodes = signal<string[] | null>(null);
  protected readonly busy = signal(false);
  protected readonly error = signal<string | null>(null);
  protected code = '';

  protected readonly signedInWithTwoFactor = computed(() => !!this.auth.user()?.signedInWithTwoFactor);
  protected readonly formattedKey = computed(() => (this.key() ?? '').replace(/(.{4})/g, '$1 ').trim());
  protected readonly otpauthUri = computed(() => buildOtpAuthUri(this.auth.user()?.email ?? '', this.key() ?? ''));
  protected readonly qrImage = signal<string | null>(null);

  async ngOnInit(): Promise<void> {
    try {
      const state = await this.api.twoFactor();
      this.enabled.set(state.isTwoFactorEnabled);
      if (!state.isTwoFactorEnabled) {
        this.key.set((await this.api.newTwoFactorKey()).sharedKey);
        this.qrImage.set(await qrCodeFor(this.otpauthUri()));
      }
    } catch (error) {
      this.error.set(describeError(error, 'تعذّر تحميل إعدادات الأمان.'));
    }
  }

  protected async enable(): Promise<void> {
    this.busy.set(true);
    this.error.set(null);
    try {
      const state = await this.api.enableTwoFactor(this.code.trim());
      this.enabled.set(state.isTwoFactorEnabled);
      this.recoveryCodes.set(state.recoveryCodes);
      this.code = '';
    } catch (error) {
      this.error.set(describeError(error, 'الرمز غير صحيح أو انتهت صلاحيته. جرّب الرمز الحالي.'));
    } finally {
      this.busy.set(false);
    }
  }

  protected async signInAgain(): Promise<void> {
    await this.auth.logout();
    await this.router.navigateByUrl('/login?returnUrl=%2Fadmin');
  }
}
