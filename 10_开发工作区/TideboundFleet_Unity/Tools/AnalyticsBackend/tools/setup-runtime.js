// Explicit operator approval is required before running this production permission change.
import { neon } from '@neondatabase/serverless';
import { readFileSync, writeFileSync, chmodSync } from 'node:fs';
import { randomBytes } from 'node:crypto';
import { fileURLToPath } from 'node:url';
const envFile=fileURLToPath(new URL('../../../../../90_本地私密配置_不提交/.env',import.meta.url));
try {
  process.loadEnvFile(envFile);
  const sql=neon(process.env.NEON_DATABASE_URL||process.env.DATABASE_URL);
  if(!process.env.TIDEBOUND_ANALYTICS_DATABASE_URL) {
    const existing=await sql`SELECT 1 FROM pg_roles WHERE rolname='tidebound_analytics_runtime'`;
    if(existing.length)throw new Error('Existing role requires its existing credential; refusing to rotate it.');
    const password=randomBytes(32).toString('hex');
    await sql.transaction([
      sql.query("CREATE ROLE tidebound_analytics_runtime LOGIN PASSWORD '"+password+"'"),
      sql.query('GRANT USAGE ON SCHEMA tidebound_analytics TO tidebound_analytics_runtime'),
      sql.query('GRANT SELECT,INSERT,UPDATE,DELETE ON ALL TABLES IN SCHEMA tidebound_analytics TO tidebound_analytics_runtime')
    ]);
    const url=new URL(process.env.NEON_DATABASE_URL||process.env.DATABASE_URL);
    url.username='tidebound_analytics_runtime';url.password=password;
    process.env.TIDEBOUND_ANALYTICS_DATABASE_URL=url.toString();
  }
  let source=readFileSync(envFile,'utf8');
  for(const name of ['TIDEBOUND_ANALYTICS_DATABASE_URL','ANALYTICS_ADMIN_TOKEN','ANALYTICS_RATE_SALT','CRON_SECRET']) {
    if(!process.env[name])process.env[name]=randomBytes(32).toString('hex');
    if(!new RegExp('^'+name+'=','m').test(source))source+='\n'+name+'='+process.env[name]+'\n';
  }
  writeFileSync(envFile,source,{mode:0o600});chmodSync(envFile,0o600);
  const runtime=neon(process.env.TIDEBOUND_ANALYTICS_DATABASE_URL);
  await runtime`SELECT count(*)::int FROM tidebound_analytics.events`;
  console.log('Scoped runtime role and private local settings ready; read check passed.');
} catch(error) { console.error(JSON.stringify({failed:true,category:error.name,code:error.code}));process.exitCode=1; }
