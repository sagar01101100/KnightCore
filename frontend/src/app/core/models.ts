export type Money=string|number;
export interface User {id:string;name:string;email:string;roles:string[]}
export interface Segment {id:string;name:string;icon:string;sortOrder:number}
export interface Feature {id:string;code:string;name:string;description:string;priceMode:string;unitPrice:Money;maxQuantity:number;requires:string[]}
export interface Package {id:string;segmentId:string;name:string;summary:string;accent:string;versionId:string;version:number;basePrice:Money;included:string[];delivery:string;features:Feature[]}
export interface Catalog {segments:Segment[];packages:Package[];evaluation:boolean;termsVersion:string}
export interface Selection {packageVersionId:string;items:{featureId:string;quantity:number}[]}
export interface Draft extends Selection {packageId:string;custom:boolean}
export interface Line {code:string;name:string;description:string;quantity:number;unitPrice:Money;amount:Money}
export interface Price {packageVersionId:string;packageName:string;included:string[];lines:Line[];subtotal:Money;delivery:string}
export interface Billing {businessName:string;address:string;city:string;state:string;postalCode:string;taxId:string|null}
export interface Snapshot {packageName:string;packageVersionId:string;included:string[];lines:Line[];billing:Billing;seller:{name:string;address:string;email:string;taxId:string|null};subtotal:Money;taxRate:Money;taxAmount:Money;total:Money;currency:string;delivery:string;termsVersion:string;scope:string;evaluation:boolean}
export interface Quote {id:string;version:number;status:string;expiresUtc:string;createdUtc:string;snapshot:Snapshot}
export interface Order {id:string;number:string;status:string;createdUtc:string;snapshot:Snapshot;email?:string;name?:string}
export interface OrderDetail extends Order {concurrencyToken:string;deliveryUrl:string|null;history:{status:string;note:string;createdUtc:string}[];invoice:{id:string;number:string;pdfStatus:string;issuedUtc:string;dueUtc:string};allowedStatuses:string[]}
export interface RequestItem {id:string;packageName:string;requirements:string;status:string;quoteId:string|null;createdUtc:string;email?:string;customerName?:string;price?:Price}
export interface Page<T>{items:T[];total:number;page:number;pageSize:number}
export const money=(v:Money|undefined)=>new Intl.NumberFormat('en-IN',{style:'currency',currency:'INR',maximumFractionDigits:2}).format(Number(v??0));
export const date=(v:string)=>new Date(v).toLocaleDateString('en-IN',{day:'numeric',month:'short',year:'numeric'});
export const statusLabel=(s:string)=>s.replace(/([a-z])([A-Z])/g,'$1 $2');
export const icons:Record<string,string>={restaurants:'⌘',salons:'✂',property:'▥',clinics:'✚',coaching:'◈',manufacturing:'⚙'};

