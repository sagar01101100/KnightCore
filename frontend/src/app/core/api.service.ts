import {Injectable,inject} from '@angular/core';
import {HttpClient,HttpErrorResponse} from '@angular/common/http';
import {firstValueFrom} from 'rxjs';

@Injectable({providedIn:'root'})
export class ApiService {
  readonly designPreview=new URLSearchParams(location.search).get('preview')==='design';
  private readonly http=inject(HttpClient);
  private csrf='';
  private ready:Promise<void>|undefined;
  refreshCsrf():Promise<void>{
    this.ready=firstValueFrom(this.http.get<{token:string}>('/api/v1/auth/csrf')).then(r=>{this.csrf=r.token});
    return this.ready;
  }
  async request<T>(method:string,path:string,body?:unknown,extraHeaders:Record<string,string>={}):Promise<T>{
    try{
      if(this.designPreview){
        if(method==='GET'&&path==='catalog')return await firstValueFrom(this.http.get<T>('/demo-catalog.json'));
        if(method==='GET'&&path==='auth/session')return {user:null} as T;
        throw new Error('This read-only design preview has no backend connection. Run the supplied application to use this feature.');
      }
      await (this.ready??this.refreshCsrf());
      return await firstValueFrom(this.http.request<T>(method,'/api/v1/'+path,{body,headers:{'X-XSRF-TOKEN':this.csrf,...extraHeaders}}));
    }catch(e){
      if(!this.csrf)this.ready=undefined;
      if(e instanceof HttpErrorResponse){
        const message=e.error?.message??(e.status===401?'Please sign in to continue.':e.status===403?'You do not have access to this action.':e.status===0?'Unable to connect. Please try again.':'The request could not be completed. Please try again.');
        throw new Error(message);
      }
      throw e;
    }
  }
  get<T>(p:string){return this.request<T>('GET',p)}
  post<T>(p:string,b:unknown,headers:Record<string,string>={}){return this.request<T>('POST',p,b,headers)}
  patch<T>(p:string,b:unknown){return this.request<T>('PATCH',p,b)}
  async download(id:string,name:string):Promise<void>{
    const blob=await firstValueFrom(this.http.get('/api/v1/invoices/'+id+'/download',{responseType:'blob'}));
    const url=URL.createObjectURL(blob);const a=document.createElement('a');a.href=url;a.download=name+'.pdf';a.click();setTimeout(()=>URL.revokeObjectURL(url),1000);
  }
}
