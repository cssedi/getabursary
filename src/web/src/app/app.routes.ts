import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', title: 'GetABursary', loadComponent: () => import('./features/home/home.page').then((m) => m.HomePage) },
  { path: 'search', title: 'Search bursaries', loadComponent: () => import('./features/search/search.page').then((m) => m.SearchPage) },
  { path: 'bursaries/field/:fieldSlug', loadComponent: () => import('./features/seo/field-landing.page').then((m) => m.FieldLandingPage) },
  { path: 'bursaries/:slug', loadComponent: () => import('./features/bursary/bursary-detail.page').then((m) => m.BursaryDetailPage) },
  { path: 'about', title: 'About', loadComponent: () => import('./features/static/about.page').then((m) => m.AboutPage) },
  { path: 'privacy', title: 'Privacy', loadComponent: () => import('./features/static/privacy.page').then((m) => m.PrivacyPage) },
  { path: '**', title: 'Page not found', loadComponent: () => import('./features/static/not-found.page').then((m) => m.NotFoundPage) },
];
