import test from 'node:test';
import assert from 'node:assert/strict';
import { randomUUID } from 'node:crypto';
import { events, registration, range } from '../lib/validation.js';
import { handler } from '../lib/handler.js';
const now = Date.parse('2026-09-28T12:00:00Z');
const id = randomUUID(), sessionId = randomUUID(), secret = 'a'.repeat(64);
const makeBatch = () => ({ sessionId, appVersion: '1.0.0', platform: 'Android', events: [
  { eventId: randomUUID(), name: 'level_start', occurredAt: new Date(now).toISOString(), level: 1, attemptId: randomUUID() }
] });
async function call(operation, { method = 'POST', body, authorization, query = '', db = {}, env = {} } = {}) {
  let data, status; const headers = {};
  const res = { setHeader: (k,v) => headers[k] = v, set statusCode(v) { status=v; }, end: v => data=JSON.parse(v) };
  await handler(operation, { env, createDatabase: () => db, now: () => now })({ method, body,
    url: `/api/${operation}${query}`, headers: { 'content-type': 'application/json', authorization }, socket: { remoteAddress: '127.0.0.1' } },res);
  return { status, data, headers };
}
test('registration rejects unknown age, absent consent and arbitrary personal fields', () => {
  const valid = { installationId:id,secret,consent:true,ageEligible:true,isTest:true };
  assert.equal(registration(valid).id,id);
  for (const bad of [{consent:false},{ageEligible:false},{email:'private@example.invalid'}]) assert.throws(() => registration({...valid,...bad}));
});
test('event schema validates entire batch before insertion, bounds time and rejects purchase tokens', () => {
  assert.equal(events(makeBatch(),now).items.length,1);
  for (const bad of [{level:0},{tool:'Rescue'},{purchaseToken:'private'},{occurredAt:'2025-01-01T00:00:00Z'},{name:'unknown'}]) {
    const batch=makeBatch(); Object.assign(batch.events[0],bad); assert.throws(() => events(batch,now));
  }
  const batch=makeBatch();batch.events.push(batch.events[0]);assert.throws(()=>events(batch,now));
});
test('valid tool and Unity N-format attempt IDs normalize correctly', () => {
  const batch=makeBatch();batch.events[0].name='tool_use';batch.events[0].tool='Reverse';batch.events[0].attemptId='abcdef0123456789abcdef0123456789';
  assert.equal(events(batch,now).items[0].attemptId,'abcdef01-2345-6789-abcd-ef0123456789');
});
test('statistics require authentication before opening any database connection', async () => {
  const db={summary(){throw new Error('must not reach database');}};
  assert.equal((await call('summary',{method:'GET',db,env:{ANALYTICS_ADMIN_TOKEN:secret}})).status,401);
  assert.equal((await call('summary',{method:'GET',db})).status,503);
});
test('malformed ingestion never authenticates or writes',async()=>{
  let calls=0;const db={authenticate(){calls++;},insert(){calls++;}};
  const response=await call('events',{body:{},authorization:`Bearer ${id}.${secret}`,db});
  assert.equal(response.status,400);assert.equal(calls,0);
});
test('ingestion passes validated batch, authenticates and acknowledges duplicate-safe writes',async()=>{
  const order=[];const db={async authenticate(){order.push('auth');},async rate(){order.push('rate');},async insert(_,batch){order.push('insert');assert.equal(batch.items.length,1);return 0;}};
  const response=await call('events',{body:makeBatch(),authorization:`Bearer ${id}.${secret}`,db});
  assert.equal(response.status,200);assert.deepEqual(response.data,{accepted:1,inserted:0});assert.deepEqual(order,['auth','rate','insert']);
});
test('driver errors never leak connection details',async()=>{
  const response=await call('events',{body:makeBatch(),authorization:`Bearer ${id}.${secret}`,db:{authenticate(){throw new Error('secret connection details');}}});
  assert.equal(response.status,503);assert.deepEqual(response.data,{error:'temporarily_unavailable'});
});
test('deletion requires the installation credential and affects that installation only',async()=>{
  let removed;const db={async authenticate(auth){assert.equal(auth.id,id);},async remove(value){removed=value;}};
  const response=await call('installation',{method:'DELETE',authorization:`Bearer ${id}.${secret}`,db});
  assert.equal(response.status,200);assert.equal(removed,id);
  assert.equal((await call('installation',{method:'DELETE',db})).status,401);
});
test('cleanup requires cron secret and unsupported methods cannot change data',async()=>{
  let cleaned=false;const db={async cleanup(){cleaned=true;}};
  assert.equal((await call('cleanup',{method:'GET',db,env:{CRON_SECRET:secret}})).status,401);
  assert.equal(cleaned,false);
  assert.equal((await call('cleanup',{method:'GET',db,env:{CRON_SECRET:secret},authorization:`Bearer ${secret}`})).status,200);
  assert.equal(cleaned,true);
  assert.equal((await call('events',{method:'GET'})).status,405);
});
test('range distinguishes test stream, caps retention window, and rejects invalid calendar dates',()=>{
  assert.equal(range(new URLSearchParams(),new Date(now)).isTest,false);
  assert.equal(range(new URLSearchParams('test=true'),new Date(now)).isTest,true);
  for(const query of ['from=2026-02-30','from=2025-01-01','to=2028-01-01','test=1'])assert.throws(()=>range(new URLSearchParams(query),new Date(now)));
});
