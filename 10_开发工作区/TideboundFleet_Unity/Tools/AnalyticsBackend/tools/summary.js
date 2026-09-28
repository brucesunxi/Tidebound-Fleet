import { fileURLToPath } from 'node:url';
try {
  process.loadEnvFile(fileURLToPath(new URL('../../../../../90_本地私密配置_不提交/.env',import.meta.url)));
  if(!process.env.ANALYTICS_ADMIN_TOKEN)throw new Error('Missing configuration');
  const url=new URL('https://tidebound-fleet.vercel.app/api/summary');
  for(const arg of process.argv.slice(2)) {
    const match=arg.match(/^--(from|to|test)=(.+)$/);
    if(!match)throw new Error('Invalid argument');
    url.searchParams.set(match[1],match[2]);
  }
  const response=await fetch(url,{headers:{Authorization:'Bearer '+process.env.ANALYTICS_ADMIN_TOKEN}});
  if(!response.ok) { console.error('Statistics request failed, HTTP '+response.status);process.exitCode=1; }
  else console.log(JSON.stringify(await response.json(),null,2));
}catch {console.error('Statistics unavailable. Check private configuration and deployment status.');process.exitCode=1;}
