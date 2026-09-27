import {Injectable,signal} from '@angular/core';
import {Draft} from './models';
@Injectable({providedIn:'root'})
export class DraftService {
 readonly value=signal<Draft|null>(this.read());
 private read():Draft|null{
  try{const d=JSON.parse(localStorage.getItem('knightcore.configuration')??'null');return d&&typeof d.packageId==='string'&&typeof d.packageVersionId==='string'&&Array.isArray(d.items)&&d.items.length<=30?d:null}catch{return null}
 }
 save(d:Draft){this.value.set(d);localStorage.setItem('knightcore.configuration',JSON.stringify(d))}
 clear(){this.value.set(null);localStorage.removeItem('knightcore.configuration')}
}

