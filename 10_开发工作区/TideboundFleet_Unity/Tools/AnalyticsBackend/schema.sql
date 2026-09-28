CREATE SCHEMA IF NOT EXISTS tidebound_analytics;
-- statement-break
CREATE TABLE IF NOT EXISTS tidebound_analytics.installations (
  id uuid PRIMARY KEY,
  token_hash text NOT NULL,
  is_test boolean NOT NULL,
  created_at timestamptz NOT NULL DEFAULT now(),
  first_seen_at timestamptz,
  last_seen_at timestamptz NOT NULL DEFAULT now()
);
-- statement-break
CREATE TABLE IF NOT EXISTS tidebound_analytics.events (
  id uuid PRIMARY KEY,
  installation_id uuid NOT NULL REFERENCES tidebound_analytics.installations(id) ON DELETE CASCADE,
  session_id uuid NOT NULL,
  name text NOT NULL CHECK (name IN ('session_start','level_start','level_resume','level_complete','level_restart','level_deadlock','tool_use')),
  occurred_at timestamptz NOT NULL,
  received_at timestamptz NOT NULL DEFAULT now(),
  app_version varchar(32) NOT NULL,
  platform varchar(16) NOT NULL,
  level integer CHECK (level BETWEEN 1 AND 10000),
  attempt_id uuid,
  tool varchar(16),
  duration_ms integer CHECK (duration_ms BETWEEN 0 AND 604800000)
);
-- statement-break
CREATE INDEX IF NOT EXISTS events_time_install ON tidebound_analytics.events(occurred_at, installation_id);
-- statement-break
CREATE UNIQUE INDEX IF NOT EXISTS events_attempt_once ON tidebound_analytics.events(installation_id, attempt_id, name)
  WHERE name IN ('level_start','level_complete','level_restart');
-- statement-break
CREATE TABLE IF NOT EXISTS tidebound_analytics.rate_limits (
  bucket text PRIMARY KEY,
  hits integer NOT NULL,
  expires_at timestamptz NOT NULL
);
