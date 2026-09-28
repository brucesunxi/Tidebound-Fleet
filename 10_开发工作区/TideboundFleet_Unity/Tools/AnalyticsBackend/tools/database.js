import { neon } from '@neondatabase/serverless';
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { resolve } from 'node:path';
const root = fileURLToPath(new URL('../../../../../', import.meta.url));
try {
  const source = readFileSync(resolve(root, '90_本地私密配置_不提交/.env'), 'utf8');
  for (const line of source.split(/\r?\n/)) {
    const match = line.match(/^\s*([A-Z_]+)\s*=\s*(.*?)\s*$/);
    if (match && !process.env[match[1]]) process.env[match[1]] = match[2].replace(/^(['"])(.*)\1$/, '$2');
  }
  const sql = neon(process.env.NEON_DATABASE_URL || process.env.DATABASE_URL);
  if (process.argv[2] === 'check') {
    const rows = await sql`SELECT count(*)::int AS table_count FROM information_schema.tables WHERE table_schema = 'tidebound_analytics'`;
    console.log(JSON.stringify({ connected: true, analyticsTables: rows[0].table_count }));
  } else if (process.argv[2] === 'migrate') {
    const source = readFileSync(new URL('../schema.sql', import.meta.url), 'utf8');
    await sql.transaction(source.split('-- statement-break').map(s => s.trim()).filter(Boolean).map(s => sql.query(s)));
    console.log('Analytics schema migration completed.');
  } else throw new Error('Unknown command');
} catch (error) { console.error(JSON.stringify({ failed: true, category: error.name, code: error.code, causeCode: error.cause?.code, networkCode: error.sourceError?.cause?.code, reason: ['fetch failed','authentication failed','does not exist','disabled','timeout','connect','fetch is not a function','expected'].filter(x => String(error.message).toLowerCase().includes(x)) })); process.exitCode = 1; }
