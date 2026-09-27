import {Routes} from '@angular/router';
import {customerGuard,adminGuard} from './core/auth.service';
export const routes:Routes=[
 {path:'',loadComponent:()=>import('./pages/catalog.component').then(m=>m.CatalogComponent)},
 {path:'configure/:id',loadComponent:()=>import('./pages/configure.component').then(m=>m.ConfigureComponent)},
 {path:'login',loadComponent:()=>import('./pages/auth.component').then(m=>m.AuthComponent),data:{mode:'login'}},
 {path:'register',loadComponent:()=>import('./pages/auth.component').then(m=>m.AuthComponent),data:{mode:'register'}},
 {path:'confirm-email',loadComponent:()=>import('./pages/auth.component').then(m=>m.AuthComponent),data:{mode:'confirm'}},
 {path:'forgot-password',loadComponent:()=>import('./pages/auth.component').then(m=>m.AuthComponent),data:{mode:'forgot'}},
 {path:'reset-password',loadComponent:()=>import('./pages/auth.component').then(m=>m.AuthComponent),data:{mode:'reset'}},
 {path:'checkout',canActivate:[customerGuard],loadComponent:()=>import('./pages/checkout.component').then(m=>m.CheckoutComponent)},
 {path:'workspace',canActivate:[customerGuard],loadComponent:()=>import('./pages/workspace.component').then(m=>m.WorkspaceComponent)},
 {path:'orders/:id',canActivate:[customerGuard],loadComponent:()=>import('./pages/order.component').then(m=>m.OrderComponent)},
 {path:'admin',canActivate:[adminGuard],loadComponent:()=>import('./pages/admin.component').then(m=>m.AdminComponent)},
 {path:'**',redirectTo:''}
];

