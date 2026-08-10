UPDATE `users`
SET `role` = 'SuperAdmin'
WHERE `role` = 'PlatformAdmin';

ALTER TABLE `users` DROP CHECK `CK_users_role`;

ALTER TABLE `users`
ADD CONSTRAINT `CK_users_role` CHECK (`role` IN
  ('SuperAdmin','TenantOwner','Dispatcher','Driver','Customer'));
