import {Component,inject,signal} from '@angular/core';
import {Router,RouterLink,RouterLinkActive,RouterOutlet} from '@angular/router';
import {AuthService} from './core/auth.service';
import {ApiService} from './core/api.service';
@Component({selector:'app-root',standalone:true,imports:[RouterOutlet,RouterLink,RouterLinkActive],template:`
<a class="skip-link" href="#main">Skip to content</a>
@if(api.designPreview){<div class="preview-banner">READ-ONLY DESIGN PREVIEW · Run the supplied Angular/.NET app for ordering and invoices.</div>}
<header class="site-header">
 <div class="header-inner">
  <a class="brand" routerLink="/" aria-label="KnightCore home"><span class="brand-mark" aria-hidden="true">♞</span><span>Knight<span class="brand-light">Core</span></span></a>
  <nav class="desktop-nav" aria-label="Main navigation"><a routerLink="/" routerLinkActive="active" [routerLinkActiveOptions]="{exact:true}">Software catalog</a><a routerLink="/workspace" routerLinkActive="active">My workspace</a>@if(auth.isAdmin){<a routerLink="/admin" routerLinkActive="active">Admin</a>}</nav>
  <div class="header-actions">@if(auth.user();as user){<span class="user-initial" [title]="user.name">{{user.name.charAt(0).toUpperCase()}}</span><button class="btn text-btn desktop-only" (click)="logout()">Sign out</button>}@else{<a class="btn btn-small btn-light desktop-only" routerLink="/login">Sign in <span aria-hidden="true">↗</span></a>}<button class="menu-toggle" (click)="menu.set(!menu())" [attr.aria-expanded]="menu()" aria-controls="mobile-navigation" aria-label="Toggle navigation">{{menu()?'✕':'☰'}}</button></div>
 </div>
 @if(menu()){<nav id="mobile-navigation" class="mobile-nav" aria-label="Mobile navigation"><a routerLink="/" (click)="menu.set(false)">Software catalog</a><a routerLink="/workspace" (click)="menu.set(false)">My workspace</a>@if(auth.isAdmin){<a routerLink="/admin" (click)="menu.set(false)">Admin</a>}@if(auth.user()){<button (click)="logout()">Sign out</button>}@else{<a routerLink="/login" (click)="menu.set(false)">Sign in</a>}</nav>}
</header>
<main id="main"><router-outlet /></main>
<footer class="site-footer"><a class="footer-brand" routerLink="/">♞ KnightCore</a><span>Software that fits your business.</span><span class="footer-note">Thoughtfully configured. Clearly priced.</span></footer>
`})
export class AppComponent{
 readonly api=inject(ApiService);
 readonly auth=inject(AuthService);private router=inject(Router);menu=signal(false);
 async logout(){try{await this.auth.logout();this.menu.set(false);await this.router.navigate(['/']);}catch{location.reload()}}
}
