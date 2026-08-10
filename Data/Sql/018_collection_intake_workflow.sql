ALTER TABLE `supplier_collections`
  ADD COLUMN `supplier_name` varchar(150) NOT NULL DEFAULT 'Supplier' AFTER `collection_number`,
  ADD COLUMN `bearer_user_id` int NULL AFTER `supplier_name`,
  ADD COLUMN `status` varchar(30) NOT NULL DEFAULT 'Open' AFTER `bearer_user_id`,
  ADD COLUMN `completed_at` datetime NULL AFTER `status`,
  ADD KEY `IX_supplier_collections_bearer` (`bearer_user_id`),
  ADD KEY `IX_supplier_collections_status` (`status`),
  ADD CONSTRAINT `FK_supplier_collections_bearer` FOREIGN KEY (`bearer_user_id`) REFERENCES `users` (`id`);
