import { Routes } from '@angular/router';
import { bursaryDetailResolver } from './features/bursary/bursary-detail.resolver';
import { adminGuard, unsavedChangesGuard } from './features/admin/admin.guards';
import { signedInGuard } from './features/tracker/tracker.guard';

export const routes: Routes = [
  { path: '', title: 'GetABursary', loadComponent: () => import('./features/home/home.page').then((m) => m.HomePage) },
  { path: 'search', title: 'Search bursaries', loadComponent: () => import('./features/search/search.page').then((m) => m.SearchPage) },
  { path: 'bursaries/field/:fieldSlug', loadComponent: () => import('./features/seo/field-landing.page').then((m) => m.FieldLandingPage) },
  {
    path: 'bursaries/:slug',
    runGuardsAndResolvers: 'always',
    resolve: { bursary: bursaryDetailResolver },
    loadComponent: () => import('./features/bursary/bursary-detail.page').then((m) => m.BursaryDetailPage),
  },
  {
    path: 'my-bursaries',
    title: 'My bursaries',
    canActivate: [signedInGuard],
    loadComponent: () => import('./features/tracker/tracker.page').then((m) => m.TrackerPage),
  },
  {
    path: 'admin',
    canMatch: [adminGuard],
    loadComponent: () => import('./features/admin/admin.page').then((m) => m.AdminPage),
    children: [
      {
        path: '',
        title: 'Review queue',
        loadComponent: () => import('./features/admin/review-queue.page').then((m) => m.ReviewQueuePage),
      },
      {
        path: 'bursaries',
        title: 'Admin bursaries',
        loadComponent: () => import('./features/admin/bursaries.page').then((m) => m.AdminBursariesPage),
      },
      {
        path: 'bursaries/:id',
        title: 'Edit bursary',
        canDeactivate: [unsavedChangesGuard],
        loadComponent: () => import('./features/admin/bursary-editor.page').then((m) => m.BursaryEditorPage),
      },
      {
        path: 'sources',
        title: 'Sources',
        loadComponent: () => import('./features/admin/sources/sources.page').then((m) => m.SourcesPage),
      },
    ],
  },
  { path: 'account/confirm', title: 'Confirm your email', loadComponent: () => import('./features/account/confirm.page').then((m) => m.ConfirmPage) },
  { path: 'account/forgot', title: 'Reset your password', loadComponent: () => import('./features/account/forgot.page').then((m) => m.ForgotPage) },
  { path: 'account/reset', title: 'Choose a new password', loadComponent: () => import('./features/account/reset.page').then((m) => m.ResetPage) },
  { path: 'about', title: 'About', loadComponent: () => import('./features/static/about.page').then((m) => m.AboutPage) },
  { path: 'privacy', title: 'Privacy', loadComponent: () => import('./features/static/privacy.page').then((m) => m.PrivacyPage) },
  { path: '**', title: 'Page not found', loadComponent: () => import('./features/static/not-found.page').then((m) => m.NotFoundPage) },
];
