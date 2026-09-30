import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    title: 'Volcanic Hazard Monitor',
    loadChildren: () => import('@hazard/feature-dashboard').then((m) => m.HAZARD_ROUTES),
  },
];
