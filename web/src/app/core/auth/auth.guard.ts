import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

/** Sends signed-out visitors to the login page, then back to where they were going. */
export const authGuard: CanActivateFn = async (_route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  return (await auth.ensureUser()) ? true : router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
};

/** Signed-in learners skip the login and register pages. */
export const guestGuard: CanActivateFn = async () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  if (!(await auth.ensureUser())) return true;
  // Staff land in the review portal; learners in the app.
  return router.createUrlTree([auth.isStaff() ? '/admin' : '/app']);
};
