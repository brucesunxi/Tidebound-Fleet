import { neon } from '@neondatabase/serverless';
import { createHash } from 'node:crypto';
import { ApiError } from './validation.js';
export const hash = value => createHash('sha256').update(value).digest('hex');
export function database(env = process.env) {
  if (!env.DATABASE_URL) throw new ApiError(503, 'service_not_configured');
  const sql = neon(env.DATABASE_URL);
  async function rate(bucket, limit, seconds) {
    const rows = await sql`INSERT INTO tidebound_analytics.rate_limits(bucket,hits,expires_at)
      VALUES (${bucket},1,now() + ${seconds} * interval '1 second')
      ON CONFLICT(bucket) DO UPDATE SET hits = tidebound_analytics.rate_limits.hits + 1
      RETURNING hits`;
    if (rows[0].hits > limit) throw new ApiError(429, 'rate_limited');
  }
  return {
    rate,
    async register({ id, secret, isTest }) {
      const tokenHash = hash(secret);
      const rows = await sql`INSERT INTO tidebound_analytics.installations(id,token_hash,is_test)
        VALUES (${id},${tokenHash},${isTest}) ON CONFLICT(id) DO UPDATE SET id=EXCLUDED.id
        WHERE tidebound_analytics.installations.token_hash=EXCLUDED.token_hash
        AND tidebound_analytics.installations.is_test=EXCLUDED.is_test RETURNING id`;
      if (!rows.length) throw new ApiError(409, 'registration_conflict');
    },
    async authenticate({ id, secret }) {
      const rows = await sql`SELECT id FROM tidebound_analytics.installations WHERE id=${id} AND token_hash=${hash(secret)}`;
      if (!rows.length) throw new ApiError(401, 'unauthorized');
    },
    async insert(id, batch) {
      const firstAt = batch.items.map(e => e.occurredAt).sort()[0];
      const queries = batch.items.map(e => sql`INSERT INTO tidebound_analytics.events
        (id,installation_id,session_id,name,occurred_at,app_version,platform,level,attempt_id,tool,duration_ms)
        VALUES (${e.id},${id},${batch.sessionId},${e.name},${e.occurredAt},${batch.appVersion},${batch.platform},${e.level},${e.attemptId},${e.tool},${e.durationMs})
        ON CONFLICT DO NOTHING RETURNING id`);
      queries.push(sql`UPDATE tidebound_analytics.installations SET
        first_seen_at=LEAST(COALESCE(first_seen_at,${firstAt}::timestamptz),${firstAt}::timestamptz),last_seen_at=now() WHERE id=${id}`);
      const results = await sql.transaction(queries);
      return results.slice(0,-1).reduce((sum, rows) => sum + rows.length, 0);
    },
    async summary({ from, to, isTest }) {
      const results = await sql.transaction([
        sql`SELECT (e.occurred_at AT TIME ZONE 'UTC')::date AS day,count(DISTINCT e.installation_id)::int AS active_installations,
          count(*)::int AS events FROM tidebound_analytics.events e JOIN tidebound_analytics.installations i ON i.id=e.installation_id
          WHERE e.occurred_at>=${from}::date AND e.occurred_at<${to}::date AND i.is_test=${isTest} GROUP BY day ORDER BY day`,
        sql`SELECT e.level,count(*) FILTER(WHERE name='level_start')::int AS starts,
          count(*) FILTER(WHERE name='level_complete')::int AS completions,
          count(*) FILTER(WHERE name='level_restart')::int AS restarts,
          count(*) FILTER(WHERE name='level_deadlock')::int AS deadlocks,
          round(avg(duration_ms) FILTER(WHERE name='level_complete'))::int AS mean_complete_active_ms
          FROM tidebound_analytics.events e JOIN tidebound_analytics.installations i ON i.id=e.installation_id
          WHERE e.occurred_at>=${from}::date AND e.occurred_at<${to}::date AND i.is_test=${isTest} AND e.level IS NOT NULL
          GROUP BY e.level ORDER BY e.level`,
        sql`SELECT tool,count(*)::int AS uses FROM tidebound_analytics.events e JOIN tidebound_analytics.installations i ON i.id=e.installation_id
          WHERE e.occurred_at>=${from}::date AND e.occurred_at<${to}::date AND i.is_test=${isTest} AND name='tool_use' GROUP BY tool ORDER BY tool`,
        sql`WITH cohorts AS (SELECT id,(first_seen_at AT TIME ZONE 'UTC')::date AS day FROM tidebound_analytics.installations
          WHERE first_seen_at>=${from}::date AND first_seen_at<${to}::date AND is_test=${isTest}),
          activity AS (SELECT DISTINCT installation_id,(occurred_at AT TIME ZONE 'UTC')::date AS day FROM tidebound_analytics.events)
          SELECT c.day AS cohort_day,d.offset_day,count(*)::int AS cohort_size,
            count(a.installation_id)::int AS returning_installations,
            round(count(a.installation_id)::numeric / count(*),4) AS retention
          FROM cohorts c CROSS JOIN (VALUES(1),(7),(30)) d(offset_day)
          LEFT JOIN activity a ON a.installation_id=c.id AND a.day=c.day+d.offset_day
          WHERE c.day+d.offset_day < (now() AT TIME ZONE 'UTC')::date
          GROUP BY c.day,d.offset_day ORDER BY c.day,d.offset_day`
      ]);
      return { from, to, timezone: 'UTC', isTest, daily: results[0], levels: results[1], tools: results[2], retention: results[3] };
    },
    async remove(id) { await sql`DELETE FROM tidebound_analytics.installations WHERE id=${id}`; },
    async cleanup() {
      await sql.transaction([
        // Daily cleanup starts at 89 days to leave headroom within the 90-day policy limit.
        sql`DELETE FROM tidebound_analytics.events WHERE occurred_at < now()-interval '89 days'`,
        sql`DELETE FROM tidebound_analytics.installations WHERE last_seen_at < now()-interval '90 days'`,
        sql`DELETE FROM tidebound_analytics.rate_limits WHERE expires_at < now()`
      ]);
    }
  };
}
