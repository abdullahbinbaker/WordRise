import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';

/** Staff area: signed-out visitors go to sign in, ordinary learners go home. */
export const staffGuard: CanActivateFn = async (_route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const user = await auth.ensureUser();
  if (!user) {
    return router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
  }
  return auth.isStaff() ? true : router.createUrlTree(['/app']);
};

/** Lessons and staff need a session that was opened with the two-factor code. */
export const twoFactorGuard: CanActivateFn = async () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  await auth.ensureUser();
  return auth.user()?.signedInWithTwoFactor ? true : router.createUrlTree(['/admin/security']);
};

export const adminOnlyGuard: CanActivateFn = async () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  await auth.ensureUser();
  return auth.isAdmin() ? true : router.createUrlTree(['/admin']);
};
