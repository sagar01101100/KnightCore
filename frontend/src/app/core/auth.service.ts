import {Injectable,inject,signal} from '@angular/core';
import {CanActivateFn,Router} from '@angular/router';
import {ApiService} from './api.service';
import {User} from './models';
@Injectable({providedIn:'root'})
export class AuthService{
 readonly api=inject(ApiService);
 readonly user=signal<User|null>(null);
 readonly ready=this.api.get<{user:User|null}>('auth/session').then(r=>this.user.set(r.user)).catch(()=>{});
 async login(email:string,password:string){const r=await this.api.post<{user:User}>('auth/login',{email,password});this.user.set(r.user);await this.api.refreshCsrf();}
 async logout(){await this.api.post('auth/logout',{});this.user.set(null);await this.api.refreshCsrf();}
 get isAdmin(){return this.user()?.roles.includes('Admin')??false}
}
export const customerGuard:CanActivateFn=async(_,state)=>{
 const auth=inject(AuthService);const router=inject(Router);await auth.ready;
 return auth.user()?true:router.createUrlTree(['/login'],{queryParams:{returnTo:state.url}});
};
export const adminGuard:CanActivateFn=async()=>{
 const auth=inject(AuthService);const router=inject(Router);await auth.ready;
 return auth.isAdmin?true:router.createUrlTree(['/']);
};

