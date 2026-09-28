import { randomUUID, randomBytes } from 'node:crypto';
import { fileURLToPath } from 'node:url';
import { database } from '../lib/database.js';
import { events, range } from '../lib/validation.js';
process.loadEnvFile(fileURLToPath(new URL('../../../../../90_本地私密配置_不提交/.env',import.meta.url)));
const db=database(),id=randomUUID(),secret=randomBytes(32).toString('hex');
try {
  await db.register({id,secret,isTest:true});
  await db.authenticate({id,secret});
  const batch=events({sessionId:randomUUID(),appVersion:'integration',platform:'Editor',events:[
    {eventId:randomUUID(),name:'session_start',occurredAt:new Date().toISOString()},
    {eventId:randomUUID(),name:'tool_use',occurredAt:new Date().toISOString(),level:1,attemptId:randomUUID(),tool:'Reverse'}
  ]});
  const first=await db.insert(id,batch),duplicate=await db.insert(id,batch);
  if(first!==2||duplicate!==0)throw new Error('deduplication_failed');
  const stats=await db.summary(range(new URLSearchParams('test=true')));
  if(!stats.tools.some(row=>row.tool==='Reverse'&&row.uses>=1))throw new Error('summary_failed');
  console.log(JSON.stringify({registration:true,authentication:true,inserted:first,retryInserted:duplicate,summary:true}));
} catch(error) { console.error(JSON.stringify({failed:true,category:error.name,code:error.code}));process.exitCode=1; }
finally { try {await db.remove(id);console.log('Synthetic test installation and events removed.');}catch{console.error('Synthetic data cleanup failed.');process.exitCode=1;} }
