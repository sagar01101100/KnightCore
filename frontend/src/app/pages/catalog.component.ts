import {Component,inject,signal,computed} from '@angular/core';
import {RouterLink} from '@angular/router';
import {ApiService} from '../core/api.service';
import {Catalog,money,icons} from '../core/models';
@Component({standalone:true,imports:[RouterLink],template:`
<div class="page catalog-page">
 <section class="catalog-intro">
  <div><div class="eyebrow"><span class="tiny-square"></span> THE KNIGHTCORE COLLECTION</div><h1>Your business.<br><span class="muted-heading">Your kind of software.</span></h1><p>Start with a proven foundation. Choose the features you need.<br class="desktop-only"> Make it yours, with a clear price from the start.</p></div>
  <div class="intro-note"><span class="note-number">01 — 03</span><p>Choose your industry.<br>Tailor your features.<br><strong>Bring your project to life.</strong></p><span class="note-arrow" aria-hidden="true">↙</span></div>
 </section>
 @if(error()){<div class="alert error" role="alert">{{error()}} <button class="link-button" (click)="load()">Try again</button></div>}
 @if(data();as catalog){
  <div class="catalog-toolbar"><div><h2>Explore by industry <span class="count">{{catalog.segments.length}}</span></h2><p>A starting point for every kind of ambition.</p></div><span class="catalog-caption">One-time development pricing</span></div>
  <div class="filter-row" aria-label="Filter by industry"><button [class.selected]="selected()==='all'" [attr.aria-pressed]="selected()==='all'" (click)="selected.set('all')">All industries</button>@for(segment of catalog.segments;track segment.id){<button [class.selected]="selected()===segment.id" [attr.aria-pressed]="selected()===segment.id" (click)="selected.set(segment.id)">{{segment.name}}</button>}</div>
  <section class="package-grid" aria-label="Software packages">
   @for(p of filtered();track p.id){
    <article class="package-card" [class.featured]="p.id==='restaurants'" [attr.data-accent]="p.accent">
     <div class="card-top"><span class="industry-icon" aria-hidden="true">{{icons[p.segmentId]||'◈'}}</span><span class="card-category">{{segmentName(p.segmentId)}}</span><span class="card-corner" aria-hidden="true">↗</span></div>
     <h3>{{p.name}}</h3><p class="package-description">{{p.summary}}</p>
     <div class="included-preview">@for(f of p.included.slice(0,3);track f){<span><span class="check" aria-hidden="true">✓</span>{{f}}</span>}</div>
     <div class="card-bottom"><div><span class="price-label">STARTING AT</span><strong class="card-price">{{money(p.basePrice)}}</strong></div><a [routerLink]="['/configure',p.id]" class="configure-link" [attr.aria-label]="'Configure '+p.name">Configure <span aria-hidden="true">→</span></a></div>
    </article>
   }
  </section>
  @if(catalog.evaluation){<p class="evaluation-note"><span aria-hidden="true">ⓘ</span> Sample catalog. Prices and delivery estimates are illustrative.</p>}
 }@else if(!error()){<div class="loading-state" role="status"><span class="spinner"></span>Loading the collection…</div>}
 <section class="catalog-bottom"><span class="bottom-icon" aria-hidden="true">◇</span><div><h3>A good foundation. Room to make it yours.</h3><p>Every package includes its core features. Add what matters, or request something custom when you configure.</p></div></section>
</div>
`})
export class CatalogComponent{
 private api=inject(ApiService);data=signal<Catalog|null>(null);error=signal('');selected=signal('all');money=money;icons=icons;
 filtered=computed(()=>this.data()?.packages.filter(p=>this.selected()==='all'||p.segmentId===this.selected())??[]);
 constructor(){void this.load()}
 async load(){this.error.set('');try{const catalog=await this.api.get<Catalog>('catalog');catalog.packages.sort((a,b)=>catalog.segments.findIndex(s=>s.id===a.segmentId)-catalog.segments.findIndex(s=>s.id===b.segmentId));this.data.set(catalog)}catch(e){this.error.set((e as Error).message)}}
 segmentName(id:string){return this.data()?.segments.find(s=>s.id===id)?.name??id}
}
