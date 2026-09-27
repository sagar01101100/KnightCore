import {Component,inject,signal} from '@angular/core';
import {RouterLink} from '@angular/router';
import {ApiService} from '../core/api.service';
import {AuthService} from '../core/auth.service';
import {Order,Page,RequestItem,money,date,statusLabel} from '../core/models';
@Component({standalone:true,imports:[RouterLink],template:`
<div class="page"><div class="page-heading"><div><div class="eyebrow">YOUR WORKSPACE</div><h1>Good to see you, {{firstName()}}.</h1><p>Your projects, quotes, and next steps.</p></div><a class="btn btn-primary" routerLink="/">Start a new project ↗</a></div>@if(error()){<div class="alert error">{{error()}}</div>}
<div class="workspace-tabs"><button [class.active]="tab()==='orders'" (click)="tab.set('orders')">Projects <span>{{orders()?.total??0}}</span></button><button [class.active]="tab()==='requests'" (click)="tab.set('requests')">Custom requests <span>{{requests().length}}</span></button></div>
@if(tab()==='orders'){@if(orders()?.items?.length){<div class="order-grid">@for(o of orders()!.items;track o.id){<article class="order-card"><div class="row-between"><span class="eyebrow">{{o.number}}</span><span class="status-badge" [attr.data-status]="o.status">{{statusLabel(o.status)}}</span></div><h2>{{o.snapshot.packageName}}</h2><p>{{o.snapshot.lines.length-1}} additional capabilities · {{date(o.createdUtc)}}</p><div class="order-card-bottom"><strong>{{money(o.snapshot.total)}}</strong><a [routerLink]="['/orders',o.id]">View project →</a></div></article>}</div><div class="pagination">@if((orders()?.page??1)>1){<button class="btn btn-secondary" (click)="loadOrders((orders()?.page??1)-1)">Previous</button>}@if((orders()?.page??1)*20<(orders()?.total??0)){<button class="btn btn-secondary" (click)="loadOrders((orders()?.page??1)+1)">Next</button>}</div>}@else{<section class="empty-panel"><span class="empty-icon" aria-hidden="true">◇</span><h2>Your next project starts here.</h2><p>Choose a software package and tailor it to your business.</p><a class="btn btn-primary" routerLink="/">Explore the collection →</a></section>}}
@else{@if(requests().length){<div class="request-list">@for(r of requests();track r.id){<article class="panel"><div class="row-between"><span class="eyebrow">{{date(r.createdUtc)}}</span><span class="status-badge">{{statusLabel(r.status)}}</span></div><h2>{{r.packageName}}</h2><p class="preserve-lines">{{r.requirements}}</p>@if(r.quoteId){<a class="btn btn-primary" routerLink="/checkout" [queryParams]="{quote:r.quoteId}">Review your quote →</a>}@else{<p class="field-hint">Your requirements are awaiting review.</p>}</article>}</div>}@else{<section class="empty-panel"><h2>No custom requests yet.</h2><p>Need something specific? Select custom features when configuring your software.</p><a class="btn btn-secondary" routerLink="/">Find your starting point →</a></section>}}
</div>`})
export class WorkspaceComponent{
 private api=inject(ApiService);readonly auth=inject(AuthService);orders=signal<Page<Order>|null>(null);requests=signal<RequestItem[]>([]);error=signal('');tab=signal('orders');money=money;date=date;statusLabel=statusLabel;
 constructor(){void this.loadOrders(1);void this.loadRequests()}
 firstName(){return this.auth.user()?.name.split(' ')[0]??'there'}
 async loadOrders(page:number){try{this.orders.set(await this.api.get<Page<Order>>('orders?page='+page))}catch(e){this.error.set((e as Error).message)}}
 async loadRequests(){try{this.requests.set(await this.api.get<RequestItem[]>('custom-requests'))}catch(e){this.error.set((e as Error).message)}}
}

