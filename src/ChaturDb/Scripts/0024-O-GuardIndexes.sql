-- Cluster O — additive only, never touching 0001 or 0002 (REQ-NFR-003): a composite index
-- supporting RoleRightsGuard's lookup of a role's right for one action (Guards/RoleRightsGuard.cs,
-- REQ-NFR-005). IXRoleRightRoleId already covers RoleId alone; this covers the guard's actual
-- WHERE RoleId = @RoleId AND Action = @Action.

CREATE INDEX IXRoleRightRoleIdAction ON RoleRight (RoleId, Action);
