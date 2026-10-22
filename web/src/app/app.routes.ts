import { Routes } from '@angular/router';
import { LandingPage } from './features/site/landing-page';
import { LearnerPage } from './features/site/learner-page';

export const routes: Routes = [
  { path: '', component: LandingPage, title: 'Wordrise | تعلّم الإنجليزية بخطوات صغيرة كل يوم' },
  { path: 'app', component: LearnerPage, title: 'Wordrise | درس اليوم' },
  { path: '**', redirectTo: '' },
];
