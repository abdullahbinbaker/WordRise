import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AdminApi, describeError } from './admin.api';
import { StaffMember } from './admin.models';

const ROLE_LABELS: Record<string, string> = { Admin: 'مدير', Reviewer: 'مراجع' };

/** Admins decide who may review lessons. Only people who already registered can be named. */
@Component({
  selector: 'app-staff-page',
  imports: [FormsModule],
  styleUrl: './admin-page.css',
  template: `
    <h1>الفريق</h1>
    <p class="lead">المراجع يرى الدروس وينشرها. اطلب منه أن يسجّل حساباً أولاً، ثم أضف بريده هنا. صلاحية المدير تُمنح من إعدادات السيرفر فقط.</p>

    <section class="card">
      <h2>إضافة مراجع</h2>
      <form class="row" (ngSubmit)="add()">
        <div class="field">
          <label for="s-email">بريده الإلكتروني</label>
          <input id="s-email" class="ltr" type="email" name="email" [(ngModel)]="email" autocomplete="off" />
        </div>
        <button type="submit" class="btn-primary" [disabled]="busy() || !email.trim()">أضفه مراجعاً</button>
      </form>
      @if (error()) {
        <p class="error" role="alert" style="margin-top: 12px">{{ error() }}</p>
      }
      @if (message()) {
        <p class="ok" role="status" style="margin-top: 12px">{{ message() }}</p>
      }
    </section>

    <section class="card">
      <table>
        <thead><tr><th>الحساب</th><th>الدور</th><th>التحقق بخطوتين</th><th></th></tr></thead>
        <tbody>
          @for (member of staff(); track member.id) {
            <tr>
              <td class="en" dir="ltr">{{ member.email }}</td>
              <td>{{ roleLabels(member) }}</td>
              <td>{{ member.twoFactorEnabled ? 'مفعّل' : 'غير مفعّل' }}</td>
              <td>
                @if (member.roles.includes('Reviewer') && !member.roles.includes('Admin')) {
                  <button type="button" class="btn-quiet" [disabled]="busy()" (click)="remove(member)">إزالة</button>
                }
              </td>
            </tr>
          }
        </tbody>
      </table>
      <p class="muted">التغيير يسري عند تسجيل دخول المراجع في المرة التالية.</p>
    </section>
  `,
})
export class StaffPage implements OnInit {
  private readonly api = inject(AdminApi);

  protected readonly staff = signal<StaffMember[]>([]);
  protected readonly busy = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly message = signal<string | null>(null);
  protected email = '';

  protected roleLabels = (member: StaffMember) => member.roles.map((role) => ROLE_LABELS[role] ?? role).join('، ');

  ngOnInit(): Promise<void> {
    return this.load();
  }

  protected async add(): Promise<void> {
    const email = this.email.trim();
    await this.act(async () => {
      await this.api.addReviewer(email);
      this.email = '';
      this.message.set(`أُضيف ${email} مراجعاً.`);
    });
  }

  protected remove(member: StaffMember): Promise<void> {
    return this.act(async () => {
      await this.api.removeReviewer(member.id);
      this.message.set(`أُزيل ${member.email} من المراجعين.`);
    });
  }

  private async act(action: () => Promise<void>): Promise<void> {
    this.busy.set(true);
    this.error.set(null);
    this.message.set(null);
    try {
      await action();
      await this.load();
    } catch (error) {
      this.error.set(describeError(error, 'تعذّر تنفيذ الطلب.'));
    } finally {
      this.busy.set(false);
    }
  }

  private async load(): Promise<void> {
    try {
      this.staff.set(await this.api.staff());
    } catch (error) {
      this.error.set(describeError(error, 'تعذّر تحميل الفريق.'));
    }
  }
}
