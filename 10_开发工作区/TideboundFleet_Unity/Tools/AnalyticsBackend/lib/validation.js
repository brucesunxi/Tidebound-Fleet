export class ApiError extends Error {
  constructor(status, code) { super(code); this.status = status; this.code = code; }
}
const fail = () => { throw new ApiError(400, 'invalid_request'); };
export function object(value, allowed, required = allowed) {
  if (!value || typeof value !== 'object' || Array.isArray(value)) fail();
  if (Object.keys(value).some(key => !allowed.includes(key)) || required.some(key => !(key in value))) fail();
}
export function uuid(value) {
  if (typeof value !== 'string' || !/^(?:[a-f0-9]{32}|[a-f0-9]{8}(?:-[a-f0-9]{4}){3}-[a-f0-9]{12})$/i.test(value)) fail();
  const hex = value.replaceAll('-', '').toLowerCase();
  return `${hex.slice(0,8)}-${hex.slice(8,12)}-${hex.slice(12,16)}-${hex.slice(16,20)}-${hex.slice(20)}`;
}
export function registration(body) {
  object(body, ['installationId','secret','consent','ageEligible','isTest']);
  if (body.consent !== true || body.ageEligible !== true) throw new ApiError(403, 'analytics_not_permitted');
  if (typeof body.isTest !== 'boolean' || !/^[a-f0-9]{64}$/.test(body.secret)) fail();
  return { id: uuid(body.installationId), secret: body.secret, isTest: body.isTest };
}
export function credential(header) {
  if (typeof header !== 'string') throw new ApiError(401, 'unauthorized');
  const match = header.match(/^Bearer ([a-f0-9-]{32,36})\.([a-f0-9]{64})$/i);
  if (!match) throw new ApiError(401, 'unauthorized');
  try { return { id: uuid(match[1]), secret: match[2] }; }
  catch { throw new ApiError(401, 'unauthorized'); }
}
export function events(body, now = Date.now()) {
  object(body, ['sessionId','appVersion','platform','events']);
  const sessionId = uuid(body.sessionId);
  if (typeof body.appVersion !== 'string' || !/^[a-zA-Z0-9._+-]{1,32}$/.test(body.appVersion) ||
      !['Android','iOS','Editor'].includes(body.platform) || !Array.isArray(body.events) || body.events.length < 1 || body.events.length > 32) fail();
  const allowed = ['session_start','level_start','level_resume','level_complete','level_restart','level_deadlock','tool_use'];
  const seen = new Set();
  const items = body.events.map(event => {
    object(event, ['eventId','name','occurredAt','level','attemptId','tool','durationMs'], ['eventId','name','occurredAt']);
    const id = uuid(event.eventId);
    if (seen.has(id) || !allowed.includes(event.name)) fail();
    seen.add(id);
    const at = typeof event.occurredAt === 'string' && /^\d{4}-\d{2}-\d{2}T.+Z$/.test(event.occurredAt) ? Date.parse(event.occurredAt) : NaN;
    if (!Number.isFinite(at) || at < now - 7 * 86400000 || at > now + 300000) fail();
    const isLevel = event.name !== 'session_start';
    if (isLevel && (!Number.isInteger(event.level) || event.level < 1 || event.level > 10000)) fail();
    if (!isLevel && (event.level !== undefined && event.level !== 0 || event.attemptId || event.tool || event.durationMs)) fail();
    const attemptId = isLevel ? uuid(event.attemptId) : null;
    if (event.name === 'tool_use' ? !['Rescue','Shuffle','Reverse'].includes(event.tool) : Boolean(event.tool)) fail();
    if (event.durationMs !== undefined && (!Number.isInteger(event.durationMs) || event.durationMs < 0 || event.durationMs > 604800000)) fail();
    return { id, name: event.name, occurredAt: new Date(at).toISOString(), level: isLevel ? event.level : null,
      attemptId, tool: event.tool || null, durationMs: event.durationMs ?? null };
  });
  return { sessionId, appVersion: body.appVersion, platform: body.platform, items };
}
export function range(query, now = new Date()) {
  const end = new Date(Date.UTC(now.getUTCFullYear(), now.getUTCMonth(), now.getUTCDate() + 1));
  const to = query.get('to') || end.toISOString().slice(0,10);
  const from = query.get('from') || new Date(end - 30 * 86400000).toISOString().slice(0,10);
  for (const value of [from,to]) if (!/^\d{4}-\d{2}-\d{2}$/.test(value) || !Number.isFinite(Date.parse(value)) || new Date(value).toISOString().slice(0,10) !== value) fail();
  if (Date.parse(to) <= Date.parse(from) || Date.parse(to) > +end || Date.parse(from) < +end - 90 * 86400000) fail();
  if (query.has('test') && !['true','false'].includes(query.get('test'))) fail();
  return { from, to, isTest: query.get('test') === 'true' };
}
