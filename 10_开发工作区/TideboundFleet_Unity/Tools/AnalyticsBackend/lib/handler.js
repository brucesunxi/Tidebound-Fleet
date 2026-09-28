import { timingSafeEqual, createHmac } from 'node:crypto';
import { database } from './database.js';
import { ApiError, registration, credential, events, range } from './validation.js';
export function authorize(header, secret) {
  if (!secret || secret.length < 32) throw new ApiError(503, 'service_not_configured');
  const actual = Buffer.from(typeof header === 'string' ? header : '');
  const expected = Buffer.from(`Bearer ${secret}`);
  if (actual.length !== expected.length || !timingSafeEqual(actual, expected)) throw new ApiError(401, 'unauthorized');
}
export function handler(operation, { env = process.env, createDatabase = database, now = () => Date.now() } = {}) {
  return async (req, res) => {
    res.setHeader('Cache-Control', 'no-store');
    res.setHeader('Content-Type', 'application/json; charset=utf-8');
    res.setHeader('X-Content-Type-Options', 'nosniff');
    const send = (status, body) => { res.statusCode = status; res.end(JSON.stringify(body)); };
    try {
      const method = ['health','summary','cleanup'].includes(operation) ? 'GET' : operation === 'installation' ? 'DELETE' : 'POST';
      if (req.method !== method) { res.setHeader('Allow', method); throw new ApiError(405, 'method_not_allowed'); }
      if (operation === 'health') return send(200, { service: 'tidebound-analytics', version: 1 });
      const query = new URL(req.url, 'https://localhost').searchParams;
      if (operation === 'summary') authorize(req.headers.authorization, env.ANALYTICS_ADMIN_TOKEN);
      if (operation === 'cleanup') authorize(req.headers.authorization, env.CRON_SECRET);
      if (operation === 'summary') return send(200, await createDatabase(env).summary(range(query, new Date(now()))));
      if (operation === 'cleanup') { await createDatabase(env).cleanup(); return send(200, { cleaned: true }); }
      let body = req.body;
      if (method === 'POST') {
        if (!/^application\/json(?:;|$)/i.test(req.headers['content-type'] || '')) throw new ApiError(415, 'json_required');
        if (typeof body === 'string' || Buffer.isBuffer(body)) {
          if (Buffer.byteLength(body) > 49152) throw new ApiError(413, 'payload_too_large');
          try { body = JSON.parse(body); } catch { throw new ApiError(400, 'invalid_request'); }
        }
        if (Buffer.byteLength(JSON.stringify(body) || '') > 49152) throw new ApiError(413, 'payload_too_large');
      }
      if (operation === 'register') {
        const registrationData = registration(body);
        if (!env.ANALYTICS_RATE_SALT || env.ANALYTICS_RATE_SALT.length < 32) throw new ApiError(503, 'service_not_configured');
        const address = req.headers['x-vercel-forwarded-for']?.split(',')[0]?.trim() || req.socket?.remoteAddress;
        if (!address) throw new ApiError(503, 'service_not_configured');
        const bucket = createHmac('sha256', env.ANALYTICS_RATE_SALT).update(`${Math.floor(now()/86400000)}:${address}`).digest('hex');
        const db = createDatabase(env);
        await db.rate(`register:${bucket}`, 100, 86400);
        await db.register(registrationData);
        return send(200, { registered: true });
      }
      const auth = credential(req.headers.authorization);
      const batch = operation === 'events' ? events(body, now()) : null;
      const db = createDatabase(env);
      await db.authenticate(auth);
      if (operation === 'installation') { await db.remove(auth.id); return send(200, { deleted: true }); }
      if (operation !== 'events') throw new ApiError(404, 'not_found');
      await db.rate(`events:${auth.id}:${Math.floor(now()/3600000)}`, 240, 3600);
      const inserted = await db.insert(auth.id, batch);
      return send(200, { accepted: batch.items.length, inserted });
    } catch (error) {
      if (error instanceof ApiError) {
        if (error.status === 429) res.setHeader('Retry-After', '3600');
        return send(error.status, { error: error.code });
      }
      // Do not include driver errors, request bodies, headers, SQL, or identifiers in logs/responses.
      return send(503, { error: 'temporarily_unavailable' });
    }
  };
}
