import {Component,inject,signal} from '@angular/core';
import {FormBuilder,ReactiveFormsModule,Validators} from '@angular/forms';
import {ActivatedRoute,Router,RouterLink} from '@angular/router';
import {ApiService} from '../core/api.service';
import {AuthService} from '../core/auth.service';
import {DraftService} from '../core/draft.service';
@Component({standalone:true,imports:[ReactiveFormsModule,RouterLink],template:`
<div class="auth-page"><aside class="auth-story"><span class="large-knight" aria-hidden="true">♞</span><div><div class="eyebrow">YOUR NEXT MOVE</div><h1>Good software.<br>Great possibilities.</h1><p>One place for your configurations, quotes, projects, and invoices.</p></div><span class="auth-story-foot">KNIGHTCORE / BUILT AROUND YOU</span></aside><section class="auth-form-wrap"><a routerLink="/" class="back-link">← Back to collection</a><div class="auth-form-inner"><span class="eyebrow">KNIGHTCORE ACCOUNT</span><h1>{{title()}}</h1><p class="form-intro">{{subtitle()}}</p>@if(error()){<div class="alert error" role="alert">{{error()}}</div>}@if(message()){<div class="alert success" role="status">{{message()}}</div>}
@if(devLink()){<div class="development-link"><strong>Local development mailbox</strong><p>Email is saved locally in this environment.</p><a class="btn btn-secondary" [href]="devLink()">{{mode==='register'?'Confirm email':'Open reset link'}} →</a></div>}
@if(mode==='confirm'){<a routerLink="/login" [queryParams]="{returnTo:returnTo}" class="btn btn-primary full-width">Continue to sign in →</a>}@else{
<form [formGroup]="form" (ngSubmit)="submit()" novalidate>
 @if(mode==='register'){<label>Full name<input formControlName="name" autocomplete="name" placeholder="Your name"></label>}
 @if(mode==='login'||mode==='register'||mode==='forgot'){<label>Email address<input formControlName="email" type="email" autocomplete="email" placeholder="you@business.com"></label>}
 @if(mode==='login'||mode==='register'||mode==='reset'){<label>Password<input formControlName="password" type="password" [autocomplete]="mode==='login'?'current-password':'new-password'" placeholder="Enter your password"></label>@if(mode!=='login'){<small class="field-hint">At least 10 characters, with uppercase, lowercase, and a number.</small>}}
 @if(mode==='login'){<div class="form-between"><span>Secure access to your workspace</span><a routerLink="/forgot-password">Forgot password?</a></div>}
 @if(form.invalid&&submitted()){<p class="field-error">Please enter valid details in every field.</p>}
 <button class="btn btn-primary full-width" [disabled]="busy()" type="submit">{{busy()?'Please wait…':buttonLabel()}} <span aria-hidden="true">→</span></button>
</form>
@if(mode==='login'){<p class="form-switch">New to KnightCore? <a routerLink="/register" [queryParams]="{returnTo:returnTo}">Create an account</a></p>}@else{<p class="form-switch">Already have an account? <a routerLink="/login" [queryParams]="{returnTo:returnTo}">Sign in</a></p>}
}
</div></section></div>`})
export class AuthComponent{
 private fb=inject(FormBuilder);private route=inject(ActivatedRoute);private router=inject(Router);private api=inject(ApiService);private auth=inject(AuthService);private draft=inject(DraftService);
 readonly mode=this.route.snapshot.data['mode'] as string;readonly returnTo=this.safeReturn(this.route.snapshot.queryParamMap.get('returnTo'));
 busy=signal(false);submitted=signal(false);error=signal('');message=signal('');devLink=signal('');
 form=this.fb.nonNullable.group({name:[''],email:[''],password:['']});
 constructor(){
  if(['login','register','forgot'].includes(this.mode))this.form.controls.email.setValidators([Validators.required,Validators.email]);
  if(['login','register','reset'].includes(this.mode))this.form.controls.password.setValidators([Validators.required,Validators.minLength(this.mode==='login'?1:10),Validators.maxLength(128)]);
  if(this.mode==='register')this.form.controls.name.setValidators([Validators.required,Validators.minLength(2)]);
  if(this.mode==='confirm')void this.confirm();
 }
 safeReturn(v:string|null){return v?.startsWith('/')&&!v.startsWith('//')?v:(this.draft.value()?'/checkout':'/workspace')}
 title(){return({login:'Welcome back.',register:'Make your next move.',forgot:'Forgot your password?',reset:'Set a new password.',confirm:'Confirm your email.'} as Record<string,string>)[this.mode]}
 subtitle(){return this.mode==='register'?'Create your account to start a project.':this.mode==='login'?'Sign in to pick up where you left off.':this.mode==='confirm'?'Verifying your account details.':'We’ll help you get back to your workspace.'}
 buttonLabel(){return({login:'Sign in',register:'Create account',forgot:'Send reset link',reset:'Update password'} as Record<string,string>)[this.mode]}
 async confirm(){this.busy.set(true);try{const r=await this.api.post<{message:string}>('auth/confirm-email',{userId:this.route.snapshot.queryParamMap.get('userId'),token:this.route.snapshot.queryParamMap.get('token')});this.message.set(r.message)}catch(e){this.error.set((e as Error).message)}finally{this.busy.set(false)}}
 async submit(){
  this.submitted.set(true);if(this.form.invalid||this.busy())return;this.busy.set(true);this.error.set('');this.message.set('');const v=this.form.getRawValue();
  try{
   if(this.mode==='login'){await this.auth.login(v.email,v.password);await this.router.navigateByUrl(this.returnTo)}
   else if(this.mode==='register'){const r=await this.api.post<{message:string;developmentConfirmationUrl:string|null}>('auth/register',v);this.message.set(r.message);this.devLink.set(this.localLink(r.developmentConfirmationUrl));}
   else if(this.mode==='forgot'){const r=await this.api.post<{message:string;developmentResetUrl:string|null}>('auth/forgot-password',{email:v.email});this.message.set(r.message);this.devLink.set(this.localLink(r.developmentResetUrl));}
   else{const r=await this.api.post<{message:string}>('auth/reset-password',{userId:this.route.snapshot.queryParamMap.get('userId'),token:this.route.snapshot.queryParamMap.get('token'),password:v.password});this.message.set(r.message)}
  }catch(e){this.error.set((e as Error).message)}finally{this.busy.set(false)}
 }
 localLink(v:string|null){if(!v)return '';const u=new URL(v);return u.pathname+u.search}
}

