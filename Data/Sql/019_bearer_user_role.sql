ALTER TABLE `users` DROP CHECK `CK_users_role`;

ALTER TABLE `users`
ADD CONSTRAINT `CK_users_role` CHECK (`role` IN
  ('SuperAdmin','Bearer','TenantOwner','Dispatcher','Driver','Customer'));
