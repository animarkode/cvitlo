import { Routes } from '@angular/router';
import { ACCOUNT_SECTIONS } from '../../core/routes/app-routes';

export const usersRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./account/account').then(m => m.Account),
    children: [
      { path: '', pathMatch: 'full', redirectTo: ACCOUNT_SECTIONS.personalInfo },
      {
        path: ACCOUNT_SECTIONS.personalInfo,
        loadComponent: () => import('./account/personal-info/personal-info').then(m => m.PersonalInfo),
      },
      {
        path: ACCOUNT_SECTIONS.signingIn,
        loadComponent: () => import('./account/signing-in/signing-in').then(m => m.SigningIn),
      },
      {
        path: ACCOUNT_SECTIONS.devices,
        loadComponent: () => import('./account/device-activity/device-activity').then(m => m.DeviceActivity),
      },
      {
        path: ACCOUNT_SECTIONS.linkedAccounts,
        loadComponent: () => import('./account/linked-accounts/linked-accounts').then(m => m.LinkedAccounts),
      },
    ],
  },
];
