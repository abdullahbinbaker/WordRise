import { Routes } from '@angular/router';
import { authGuard, guestGuard } from './core/auth/auth.guard';
import { LoginPage } from './features/auth/login-page';
import { RegisterPage } from './features/auth/register-page';
import { LandingPage } from './features/site/landing-page';
import { LearnerPage } from './features/site/learner-page';

export const routes: Routes = [
  { path: '', component: LandingPage, title: 'Wordrise | تعلّم الإنجليزية بخطوات صغيرة كل يوم' },
  { path: 'login', component: LoginPage, canActivate: [guestGuard], title: 'Wordrise | تسجيل الدخول' },
  { path: 'register', component: RegisterPage, canActivate: [guestGuard], title: 'Wordrise | إنشاء حساب' },
  { path: 'admin', loadChildren: () => import('./features/admin/admin.routes').then((m) => m.adminRoutes) },
  { path: 'app', component: LearnerPage, canActivate: [authGuard], title: 'Wordrise | درس اليوم' },
  { path: '**', redirectTo: '' },
];
