import { RenderMode, ServerRoute } from '@angular/ssr';

export const serverRoutes: ServerRoute[] = [
  // The public landing page is prerendered for speed and search engines.
  { path: '', renderMode: RenderMode.Prerender },
  // Everything else depends on the signed-in learner, so it renders in the browser.
  { path: '**', renderMode: RenderMode.Client },
];
