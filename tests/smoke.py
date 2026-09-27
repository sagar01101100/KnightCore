"""End-to-end API smoke tests against an isolated local SQLite development database.
Usage: DOTNET=/path/to/dotnet python3 tests/smoke.py
Only synthetic data and local email delivery are used.
"""
import http.cookiejar,json,os,pathlib,secrets,subprocess,tempfile,time,urllib.request,urllib.error,urllib.parse,concurrent.futures
ROOT=pathlib.Path(__file__).resolve().parents[1]
BASE='http://127.0.0.1:5087/api/v1/'
class Client:
 def __init__(self):
  self.jar=http.cookiejar.CookieJar();self.http=urllib.request.build_opener(urllib.request.ProxyHandler({}),urllib.request.HTTPCookieProcessor(self.jar));self.token=''
 def call(self,method,path,data=None,expected=200,headers=None):
  h={'Content-Type':'application/json','X-XSRF-TOKEN':self.token};h.update(headers or {})
  req=urllib.request.Request(BASE+path,data=json.dumps(data).encode() if data is not None else None,headers=h,method=method)
  try:r=self.http.open(req,timeout=20)
  except urllib.error.HTTPError as e:r=e
  body=r.read();assert r.status==expected,(method,path,r.status,body[:1000])
  return json.loads(body) if r.headers.get('Content-Type','').startswith('application/json') else body
 def csrf(self):self.token=self.call('GET','auth/csrf')['token']
 def login(self,email,password):
  self.csrf();self.call('POST','auth/login',{'email':email,'password':password});self.csrf()
 def register(self):
  email=secrets.token_hex(5)+'@example.invalid';pw='Kc9!'+secrets.token_urlsafe(20);self.csrf()
  r=self.call('POST','auth/register',{'name':'Test Customer','email':email,'password':pw})
  q=urllib.parse.parse_qs(urllib.parse.urlparse(r['developmentConfirmationUrl']).query)
  self.call('POST','auth/confirm-email',{'userId':q['userId'][0],'token':q['token'][0]});self.login(email,pw)
with tempfile.TemporaryDirectory(prefix='knightcore-test-') as tmp:
 env=os.environ.copy();pw='Kc9!'+secrets.token_urlsafe(20)
 env.update({'ASPNETCORE_ENVIRONMENT':'Development','ASPNETCORE_URLS':'http://127.0.0.1:5087','Database__Provider':'Sqlite','ConnectionStrings__Default':'Data Source='+tmp+'/test.db','DataDirectory':tmp,'Bootstrap__AdminEmail':'admin@example.invalid','Bootstrap__AdminPassword':pw,'PublicOrigin':'http://localhost:4200'})
 log=open(tmp+'/server.log','w');p=subprocess.Popen([os.environ.get('DOTNET','dotnet'),'run','--no-build','--project',str(ROOT/'backend/KnightCore.Api')],env=env,stdout=log,stderr=log)
 try:
  guest=Client()
  for _ in range(80):
   try:catalog=guest.call('GET','catalog');break
   except (urllib.error.URLError,ConnectionError):time.sleep(.25)
  else:raise RuntimeError('API did not start')
  assert len(catalog['packages'])==6
  (ROOT/'frontend/public/demo-catalog.json').write_text(json.dumps(catalog,indent=2))
  guest.csrf();guest.call('GET','orders',expected=401)
  Client().call('POST','estimates',{},expected=400)
  package=next(x for x in catalog['packages'] if x['id']=='restaurants')
  selection={'packageVersionId':package['versionId'],'items':[{'featureId':package['features'][0]['id'],'quantity':1}]}
  price=guest.call('POST','estimates',selection);assert float(price['subtotal'])==float(package['basePrice'])+float(package['features'][0]['unitPrice'])
  guest.call('POST','estimates',dict(selection,items=selection['items']*2),expected=422)
  print('PASS public catalog, authoritative pricing, duplicate validation, login required',flush=True)
  c=Client();c.register();other=Client();other.register();admin=Client();admin.login('admin@example.invalid',pw)
  billing={'businessName':'Test Restaurant','address':'12 Example Street','city':'Kolkata','state':'West Bengal','postalCode':'700001','taxId':None}
  q=c.call('POST','quotes',{'selection':selection,'billing':billing});body={'quoteId':q['id'],'quoteVersion':q['version'],'acceptedTermsVersion':q['snapshot']['termsVersion']};headers={'Idempotency-Key':secrets.token_hex(12)}
  order=c.call('POST','orders',body,201,headers);replay=c.call('POST','orders',body,201,headers);assert order['orderId']==replay['orderId']
  c.call('POST','orders',dict(body,quoteVersion=99),409,headers)
  other.call('GET','orders/'+order['orderId'],expected=404);other.call('GET','invoices/'+order['invoiceId']+'/download',expected=404)
  print('PASS registration, confirmation, login, order, idempotent retry, cross-account isolation',flush=True)
  for _ in range(50):
   detail=c.call('GET','orders/'+order['orderId'])
   if detail['invoice']['pdfStatus']=='Ready':break
   time.sleep(.4)
  assert detail['invoice']['pdfStatus']=='Ready',detail
  pdf=c.call('GET','invoices/'+order['invoiceId']+'/download');assert pdf.startswith(b'%PDF')
  (ROOT/'docs/sample-invoice.pdf').write_bytes(pdf)
  print('PASS background PDF generation and authorized download',flush=True)
  detail=admin.call('GET','orders/'+order['orderId']);status={'status':'InProgress','concurrencyToken':detail['concurrencyToken'],'note':'Development started','deliveryUrl':None}
  admin.call('PATCH','admin/orders/'+order['orderId']+'/status',status);admin.call('PATCH','admin/orders/'+order['orderId']+'/status',status,412)
  request=c.call('POST','custom-requests',{'selection':selection,'billing':billing,'requirements':'Build a custom kitchen display workflow.'})
  offer=admin.call('POST','admin/custom-requests/'+request['id']+'/offer',{'scope':'Custom kitchen display with three stations.','customAmount':5000,'delivery':'4 weeks','validDays':7})
  assert float(offer['snapshot']['total'])==float(price['subtotal'])+5000
  c.call('POST','orders',{'quoteId':offer['id'],'quoteVersion':offer['version'],'acceptedTermsVersion':offer['snapshot']['termsVersion']},201,{'Idempotency-Key':secrets.token_hex(12)})
  print('PASS admin status transitions, stale update protection, custom quote and acceptance',flush=True)
  fixed=c.call('POST','quotes',{'selection':selection,'billing':billing})
  edit={'version':package['version'],'name':package['name'],'summary':package['summary'],'basePrice':float(package['basePrice'])+1000,'delivery':package['delivery'],'included':package['included'],'features':[{k:f[k] for k in ['code','name','description','unitPrice','priceMode','maxQuantity','requires']} for f in package['features']]}
  admin.call('POST','admin/packages/'+package['id']+'/publish',edit)
  guest.call('POST','estimates',selection,expected=409)
  fixedbody={'quoteId':fixed['id'],'quoteVersion':fixed['version'],'acceptedTermsVersion':fixed['snapshot']['termsVersion']}
  # Separate HTTP clients share the authenticated cookies and token to simulate a double click.
  def place_once(_):
   parallel=Client();parallel.jar=c.jar;parallel.http=urllib.request.build_opener(urllib.request.ProxyHandler({}),urllib.request.HTTPCookieProcessor(c.jar));parallel.token=c.token
   return parallel.call('POST','orders',fixedbody,201,{'Idempotency-Key':'parallel-fixed-quote'})
  with concurrent.futures.ThreadPoolExecutor(max_workers=2) as pool:results=list(pool.map(place_once,range(2)))
  assert results[0]['orderId']==results[1]['orderId']
  locked=c.call('GET','orders/'+results[0]['orderId']);assert locked['snapshot']['total']==fixed['snapshot']['total']
  print('PASS CSRF, catalog versioning, immutable quoted prices, concurrent duplicate submission',flush=True)
  print('ALL SMOKE TESTS PASSED',flush=True)
 except Exception:
  log.flush();print(pathlib.Path(tmp+'/server.log').read_text()[-5000:]);raise
 finally:
  p.terminate()
  try:p.wait(timeout=8)
  except subprocess.TimeoutExpired:p.kill()
