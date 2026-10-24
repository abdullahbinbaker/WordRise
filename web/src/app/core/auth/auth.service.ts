import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

export interface Me {
  id: string;
  email: string;
  displayName: string | null;
  roles: string[];
  twoFactorEnabled: boolean;
  signedInWithTwoFactor: boolean;
}

/** Shape of ASP.NET validation problems: field or error code → messages. */
interface ValidationProblem {
  errors?: Record<string, string[]>;
}

/** Identity error codes the register endpoint returns, in the learner's language. */
const REGISTER_ERRORS: Record<string, string> = {
  DuplicateUserName: 'هذا البريد مسجّل مسبقاً. جرّب تسجيل الدخول.',
  DuplicateEmail: 'هذا البريد مسجّل مسبقاً. جرّب تسجيل الدخول.',
  InvalidEmail: 'البريد الإلكتروني غير صحيح.',
  PasswordTooShort: 'كلمة المرور يجب أن تكون 8 أحرف على الأقل.',
  PasswordRequiresDigit: 'كلمة المرور يجب أن تحتوي على رقم واحد على الأقل.',
};

/** Thrown by `login` when the account has two-factor sign-in on and no code was given. */
export class TwoFactorRequiredError extends Error {
  constructor() {
    super('أدخل الرمز من تطبيق المصادقة.');
  }
}

/** Thrown when the account exists but its email address was not confirmed yet. */
export class EmailNotConfirmedError extends Error {
  constructor() {
    super('فعّل بريدك الإلكتروني أولاً: أرسلنا إليك رابط التفعيل.');
  }
}

/** The website signs in with an HttpOnly cookie; the API sets and reads it. */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);

  /** Undefined until the first check, null when signed out. */
  readonly user = signal<Me | null | undefined>(undefined);
  readonly isSignedIn = computed(() => !!this.user());
  readonly isStaff = computed(() => !!this.user()?.roles.some((role) => role === 'Reviewer' || role === 'Admin'));
  readonly isAdmin = computed(() => !!this.user()?.roles.includes('Admin'));

  /** Name to greet the learner with: their display name, else the start of their email. */
  readonly displayName = computed(() => {
    const user = this.user();
    return user ? (user.displayName ?? user.email.split('@')[0]) : '';
  });

  async loadUser(): Promise<Me | null> {
    try {
      const me = await firstValueFrom(this.http.get<Me>('/api/v1/me'));
      this.user.set(me);
    } catch {
      this.user.set(null);
    }
    return this.user() ?? null;
  }

  /** Resolves the signed-in state once; later calls reuse it. */
  async ensureUser(): Promise<Me | null> {
    return this.user() === undefined ? this.loadUser() : (this.user() ?? null);
  }

  async login(email: string, password: string, twoFactorCode?: string): Promise<void> {
    try {
      await firstValueFrom(
        this.http.post('/api/v1/auth/login?useCookies=true', { email, password, twoFactorCode: twoFactorCode || undefined }),
      );
    } catch (error) {
      if (error instanceof HttpErrorResponse && error.status === 401) {
        const detail = (error.error as { detail?: string } | null)?.detail;
        if (detail === 'RequiresTwoFactor') {
          throw new TwoFactorRequiredError();
        }
        if (detail === 'NotAllowed') {
          throw new EmailNotConfirmedError();
        }
        if (detail === 'LockedOut') {
          throw new Error('تم إيقاف الدخول مؤقتاً بسبب محاولات خاطئة كثيرة. انتظر 5 دقائق ثم حاول مرة أخرى، أو اختر «نسيت كلمة المرور».');
        }
        throw new Error(
          twoFactorCode ? 'الرمز غير صحيح أو انتهت صلاحيته.' : 'البريد أو كلمة المرور غير صحيحة.',
        );
      }
      throw new Error('تعذّر تسجيل الدخول. حاول مرة أخرى.');
    }
    await this.loadUser();
    await this.applyRememberedName(email);
  }

  async register(displayName: string, email: string, password: string): Promise<void> {
    try {
      await firstValueFrom(this.http.post('/api/v1/auth/register', { email, password }));
    } catch (error) {
      throw new Error(this.registerError(error));
    }
    // The name is saved once the account can sign in (right now, or after its email is confirmed).
    this.rememberName(email, displayName);
    await this.login(email, password);
  }

  /** Always succeeds from the learner's side: the API does not reveal whether the address has an account. */
  async forgotPassword(email: string): Promise<void> {
    try {
      await firstValueFrom(this.http.post('/api/v1/auth/forgotPassword', { email }));
    } catch {
      throw new Error('تعذّر إرسال الرسالة. حاول مرة أخرى بعد قليل.');
    }
  }

  async resetPassword(email: string, resetCode: string, newPassword: string): Promise<void> {
    try {
      await firstValueFrom(this.http.post('/api/v1/auth/resetPassword', { email, resetCode, newPassword }));
    } catch (error) {
      const codes = error instanceof HttpErrorResponse ? Object.keys((error.error as ValidationProblem)?.errors ?? {}) : [];
      const known = codes.map((code) => REGISTER_ERRORS[code]).filter(Boolean);
      throw new Error(known[0] ?? 'الرابط غير صالح أو انتهت صلاحيته. اطلب رابطاً جديداً.');
    }
  }

  async confirmEmail(userId: string, code: string, changedEmail?: string): Promise<void> {
    const params = new URLSearchParams({ userId, code });
    if (changedEmail) params.set('changedEmail', changedEmail);
    try {
      await firstValueFrom(this.http.get(`/api/v1/auth/confirmEmail?${params}`, { responseType: 'text' }));
    } catch {
      throw new Error('الرابط غير صالح أو انتهت صلاحيته.');
    }
  }

  async resendConfirmation(email: string): Promise<void> {
    try {
      await firstValueFrom(this.http.post('/api/v1/auth/resendConfirmationEmail', { email }));
    } catch {
      throw new Error('تعذّر إرسال الرسالة. حاول مرة أخرى بعد قليل.');
    }
  }

  async logout(): Promise<void> {
    try {
      await firstValueFrom(this.http.post('/api/v1/auth/logout', null));
    } finally {
      this.user.set(null);
    }
  }

  private static readonly NameKey = 'wordrise.pending-name';

  private rememberName(email: string, displayName: string): void {
    try {
      if (displayName.trim()) {
        localStorage.setItem(AuthService.NameKey, JSON.stringify({ email: email.toLowerCase(), name: displayName.trim() }));
      }
    } catch {
      // Without storage the learner is greeted by the start of their email instead.
    }
  }

  private async applyRememberedName(email: string): Promise<void> {
    try {
      const saved = JSON.parse(localStorage.getItem(AuthService.NameKey) ?? 'null') as { email: string; name: string } | null;
      if (!saved || saved.email !== email.toLowerCase()) return;
      localStorage.removeItem(AuthService.NameKey);
      await firstValueFrom(this.http.put('/api/v1/me', { displayName: saved.name }));
      await this.loadUser();
    } catch {
      // The name is a nicety; never fail a sign-in over it.
    }
  }

  private registerError(error: unknown): string {
    const codes = error instanceof HttpErrorResponse ? Object.keys((error.error as ValidationProblem)?.errors ?? {}) : [];
    const known = codes.map((code) => REGISTER_ERRORS[code]).filter(Boolean);
    return known[0] ?? 'تعذّر إنشاء الحساب. تحقّق من البيانات وحاول مرة أخرى.';
  }
}
