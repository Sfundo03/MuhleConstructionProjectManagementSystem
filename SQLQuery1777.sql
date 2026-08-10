-- 1. Make sure the Client role actually exists
IF NOT EXISTS (SELECT 1 FROM AspNetRoles WHERE Name = 'Client')
    INSERT INTO AspNetRoles (Id, Name) VALUES (NEWID(), 'Client');

-- 2. Add this specific user to it
INSERT INTO AspNetUserRoles (UserId, RoleId)
SELECT u.Id, r.Id
FROM AspNetUsers u, AspNetRoles r
WHERE u.Email = 'sfundozuma114@icloud.com'
  AND r.Name = 'Client'
  AND NOT EXISTS (
      SELECT 1 FROM AspNetUserRoles ur
      WHERE ur.UserId = u.Id AND ur.RoleId = r.Id
  );