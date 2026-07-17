import { inject } from '@angular/core';
import { CanActivateFn, Router, UrlTree } from '@angular/router';

export abstract class AuthSession {
  abstract isAuthenticated(): boolean;
  abstract hasRole(role: string): boolean;
}

export const authGuard: CanActivateFn = (route, state): boolean | UrlTree => {
  const session = inject(AuthSession);
  const router = inject(Router);
  const requiredRole = route.data['role'] as string | undefined;

  if (!session.isAuthenticated()) {
    return router.createUrlTree(['/login'], {
      queryParams: { redirectTo: state.url },
    });
  }

  if (requiredRole && !session.hasRole(requiredRole)) {
    return router.createUrlTree(['/forbidden']);
  }

  return true;
};
