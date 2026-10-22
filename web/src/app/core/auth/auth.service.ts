import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

export interface Me {
  id: string;
  email: string;
  displayName: string | null;
  roles: string[];
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

/** The website signs in with an HttpOnly cookie; the API sets and reads it. */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);

  /** Undefined until the first check, null when signed out. */
  readonly user = signal<Me | null | undefined>(undefined);
  readonly isSignedIn = computed(() => !!this.user());

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

  async login(email: string, password: string): Promise<void> {
    try {
      await firstValueFrom(this.http.post('/api/v1/auth/login?useCookies=true', { email, password }));
    } catch (error) {
      throw new Error(
        error instanceof HttpErrorResponse && error.status === 401
          ? 'البريد أو كلمة المرور غير صحيحة.'
          : 'تعذّر تسجيل الدخول. حاول مرة أخرى.',
      );
    }
    await this.loadUser();
  }

  async register(displayName: string, email: string, password: string): Promise<void> {
    try {
      await firstValueFrom(this.http.post('/api/v1/auth/register', { email, password }));
    } catch (error) {
      throw new Error(this.registerError(error));
    }
    await this.login(email, password);
    if (displayName.trim()) {
      await firstValueFrom(this.http.put('/api/v1/me', { displayName: displayName.trim() }));
      await this.loadUser();
    }
  }

  async logout(): Promise<void> {
    try {
      await firstValueFrom(this.http.post('/api/v1/auth/logout', null));
    } finally {
      this.user.set(null);
    }
  }

  private registerError(error: unknown): string {
    const codes = error instanceof HttpErrorResponse ? Object.keys((error.error as ValidationProblem)?.errors ?? {}) : [];
    const known = codes.map((code) => REGISTER_ERRORS[code]).filter(Boolean);
    return known[0] ?? 'تعذّر إنشاء الحساب. تحقّق من البيانات وحاول مرة أخرى.';
  }
}
