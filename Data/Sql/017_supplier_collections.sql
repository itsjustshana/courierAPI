CREATE TABLE `supplier_collections` (
  `id` int NOT NULL AUTO_INCREMENT,
  `collection_number` varchar(50) NOT NULL,
  `created_by_user_id` int NOT NULL,
  `notes` varchar(500) NULL,
  `created_at` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `UX_supplier_collections_number` (`collection_number`),
  KEY `IX_supplier_collections_created_by` (`created_by_user_id`),
  CONSTRAINT `FK_supplier_collections_users` FOREIGN KEY (`created_by_user_id`) REFERENCES `users` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE `supplier_collection_items` (
  `collection_id` int NOT NULL,
  `package_id` int NOT NULL,
  `added_at` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`collection_id`, `package_id`),
  UNIQUE KEY `UX_supplier_collection_items_package` (`package_id`),
  CONSTRAINT `FK_supplier_collection_items_collection` FOREIGN KEY (`collection_id`) REFERENCES `supplier_collections` (`id`) ON DELETE CASCADE,
  CONSTRAINT `FK_supplier_collection_items_package` FOREIGN KEY (`package_id`) REFERENCES `UserPackages` (`package_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
