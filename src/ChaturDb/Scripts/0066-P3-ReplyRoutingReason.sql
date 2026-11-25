-- [REQ-UI-111] A reply keeps why Automatic routing picked the model that wrote it, so the reason shown beside the
-- model's name is still there after the app is closed and opened again. NULL when the owner picked the model, when
-- routing was off, and on every reply recorded before this script (the router's reason was only written to the
-- RoutingChoice log, never to the reply).
ALTER TABLE SessionEvent ADD COLUMN RoutingReason TEXT NULL;
