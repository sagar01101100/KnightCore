import {Component,inject,signal,OnDestroy} from '@angular/core';
import {ActivatedRoute,Router,RouterLink} from '@angular/router';
import {ApiService} from '../core/api.service';
import {DraftService} from '../core/draft.service';
import {Catalog,Package,Price,money,icons} from '../core/models';
@Component({standalone:true,imports:[RouterLink],template:`
<div class="page config-page">
 <a routerLink="/" class="back-link">← Back to collection</a>
 @if(error()){<div class="alert error" role="alert">{{error()}}</div>}
 @if(pkg();as p){
 <div class="page-heading"><div><div class="eyebrow">MAKE IT YOURS</div><h1>{{p.name}}</h1><p>{{p.summary}}</p></div><span class="step-label">01 / Configure</span></div>
 <div class="config-layout"><div class="config-content">
  <section class="panel"><div class="section-heading"><span class="step-circle">01</span><div><h2>Your foundation</h2><p>Everything below is included in the base package.</p></div><span class="pill">Included</span></div><div class="included-grid">@for(f of p.included;track f){<div class="included-item"><span class="check-disc">✓</span>{{f}}</div>}</div></section>
  <section class="panel"><div class="section-heading"><span class="step-circle">02</span><div><h2>Add what matters</h2><p>Choose the capabilities that fit your business.</p></div></div><div class="feature-list">@for(f of p.features;track f.id){<div class="feature-option" [class.chosen]="!!selected()[f.id]"><label class="feature-choice"><input type="checkbox" [checked]="!!selected()[f.id]" (change)="toggle(f.id,$any($event.target).checked)"><span class="feature-copy"><strong>{{f.name}}</strong><span>{{f.description}}</span>@if(f.requires.length){<small>Requires: {{requireNames(f.requires)}}</small>}</span><span class="feature-price">+{{money(f.unitPrice)}}@if(f.priceMode==='PerUnit'){<small>per location</small>}</span></label>@if(selected()[f.id]&&f.priceMode==='PerUnit'){<label class="quantity-label">Additional locations <input type="number" min="1" [max]="f.maxQuantity" [value]="selected()[f.id]" (change)="quantity(f.id,$any($event.target).value)"></label>}</div>}</div></section>
  <section class="panel custom-option"><label class="feature-choice"><input type="checkbox" [checked]="custom()" (change)="custom.set($any($event.target).checked);save()"><span class="feature-copy"><strong>Have something else in mind?</strong><span>Request custom features. We’ll review the scope and send a quote before you order.</span></span><span class="pill">Custom</span></label></section>
 </div><aside class="summary-panel"><div class="summary-top"><span class="eyebrow">YOUR CONFIGURATION</span><span class="summary-mark" aria-hidden="true">♞</span></div><h2>{{p.name}}</h2><div class="summary-lines"><div><span>Base package</span><strong>{{money(p.basePrice)}}</strong></div>@for(line of price()?.lines?.slice(1)||[];track line.code){<div><span>{{line.name}}@if(line.quantity>1){ × {{line.quantity}}}</span><strong>{{money(line.amount)}}</strong></div>}</div><div class="summary-total"><span>{{custom()?'Known subtotal':'One-time subtotal'}}</span><strong>{{money(price()?.subtotal??p.basePrice)}}</strong><small>Applicable tax confirmed at checkout.</small></div>@if(custom()){<div class="summary-note">Custom work will be priced after reviewing your requirements.</div>}<div class="delivery-note"><span aria-hidden="true">◷</span><span>Estimated delivery <strong>{{p.delivery}}</strong></span></div><button class="btn btn-primary full-width" (click)="continue()" [disabled]="loading()||!price()">{{loading()?'Updating…':custom()?'Request a custom quote':'Continue to review'}} <span aria-hidden="true">→</span></button><p class="summary-footnote">You’ll review the complete scope and price before placing your order.</p></aside></div>
 <div class="mobile-checkout"><div><small>{{custom()?'Known subtotal':'Subtotal'}}</small><strong>{{money(price()?.subtotal??p.basePrice)}}</strong></div><button class="btn btn-primary" (click)="continue()" [disabled]="loading()||!price()">Continue →</button></div>
 }@else if(!error()){<div class="loading-state"><span class="spinner"></span>Loading your configuration…</div>}
</div>`})
export class ConfigureComponent implements OnDestroy{
 private api=inject(ApiService);private route=inject(ActivatedRoute);private router=inject(Router);private draft=inject(DraftService);
 pkg=signal<Package|null>(null);price=signal<Price|null>(null);selected=signal<Record<string,number>>({});custom=signal(false);error=signal('');loading=signal(false);money=money;icons=icons;private timer:ReturnType<typeof setTimeout>|undefined;private revision=0;
 constructor(){void this.load()}
 async load(){try{const data=await this.api.get<Catalog>('catalog');const p=data.packages.find(x=>x.id===this.route.snapshot.paramMap.get('id'));if(!p)throw new Error('This package is unavailable.');this.pkg.set(p);const d=this.draft.value();if(d?.packageVersionId===p.versionId){this.selected.set(Object.fromEntries(d.items.map(x=>[x.featureId,x.quantity])));this.custom.set(d.custom)}await this.calculate()}catch(e){this.error.set((e as Error).message)}}
 selection(){return{packageVersionId:this.pkg()!.versionId,items:Object.entries(this.selected()).map(([featureId,quantity])=>({featureId,quantity}))}}
 save(){const p=this.pkg();if(p)this.draft.save({...this.selection(),packageId:p.id,custom:this.custom()})}
 toggle(id:string,on:boolean){const n={...this.selected()};if(on)n[id]=1;else delete n[id];this.selected.set(n);this.changed()}
 quantity(id:string,q:string){this.selected.set({...this.selected(),[id]:Number(q)});this.changed()}
 changed(){this.save();this.loading.set(true);this.price.set(null);clearTimeout(this.timer);this.timer=setTimeout(()=>void this.calculate(),180)}
 async calculate(){const v=++this.revision;this.loading.set(true);this.error.set('');try{const r=await this.api.post<Price>('estimates',this.selection());if(v===this.revision)this.price.set(r)}catch(e){if(v===this.revision){this.price.set(null);this.error.set((e as Error).message)}}finally{if(v===this.revision)this.loading.set(false)}}
 requireNames(codes:string[]){return codes.map(c=>this.pkg()?.features.find(f=>f.code===c)?.name??c).join(', ')}
 continue(){if(!this.price()||this.loading())return;this.save();void this.router.navigate(['/checkout'])}
 ngOnDestroy(){clearTimeout(this.timer);this.revision++}
}

