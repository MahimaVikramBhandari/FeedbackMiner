import { Routes } from '@angular/router';
import { DashboardComponent } from './dashboard/dashboard.component';
import { FeedbackPageComponent } from './feedback/feedback-page.component';
import { ReportsPageComponent } from './reports/reports-page.component';
import { ThemesPageComponent } from './themes/themes-page.component';

export const routes: Routes = [
  { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
  { path: 'dashboard', component: DashboardComponent },
  { path: 'feedback', component: FeedbackPageComponent },
  { path: 'themes', component: ThemesPageComponent },
  { path: 'reports', component: ReportsPageComponent },
  { path: '**', redirectTo: 'dashboard' }
];