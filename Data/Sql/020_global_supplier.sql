ALTER TABLE `global_settings`
  ADD COLUMN `supplier` varchar(150) NOT NULL DEFAULT 'Supplier' AFTER `app_name`;
