-- Cluster L (Appearance), REQ-UI-037: the light/dark switch is now a ThemeMode (Light / Dark / FollowSystem).
--   Copy the old boolean Appearance.IsDark setting into the new Appearance.ThemeMode setting, then remove the old key.
--   A database that has no old setting keeps the new default (Dark).

INSERT INTO Setting (Key, Value)
SELECT 'Appearance.ThemeMode', CASE WHEN s.Value = '0' THEN 'Light' ELSE 'Dark' END
FROM Setting s
WHERE s.Key = 'Appearance.IsDark'
  AND NOT EXISTS (SELECT 1 FROM Setting WHERE Key = 'Appearance.ThemeMode');

DELETE FROM Setting WHERE Key = 'Appearance.IsDark';
