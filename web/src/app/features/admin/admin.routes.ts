import { Routes } from '@angular/router';
import { AdminShell } from './admin-shell';
import { adminOnlyGuard, staffGuard, twoFactorGuard } from './admin.guards';

export const adminRoutes: Routes = [
  {
    path: '',
    component: AdminShell,
    canActivate: [staffGuard],
    children: [
      {
        path: '',
        pathMatch: 'full',
        canActivate: [twoFactorGuard],
        loadComponent: () => import('./drafts-page').then((m) => m.DraftsPage),
        title: 'Wordrise | مراجعة الدروس',
      },
      {
        path: 'drafts/:id',
        canActivate: [twoFactorGuard],
        loadComponent: () => import('./draft-page').then((m) => m.DraftPage),
        title: 'Wordrise | مراجعة درس',
      },
      {
        path: 'staff',
        canActivate: [twoFactorGuard, adminOnlyGuard],
        loadComponent: () => import('./staff-page').then((m) => m.StaffPage),
        title: 'Wordrise | الفريق',
      },
      {
        path: 'security',
        loadComponent: () => import('./security-page').then((m) => m.SecurityPage),
        title: 'Wordrise | الأمان',
      },
    ],
  },
];
