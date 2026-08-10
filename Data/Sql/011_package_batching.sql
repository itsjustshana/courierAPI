ALTER TABLE `clients`
  ADD COLUMN `batch_handling_mode` varchar(20) NOT NULL DEFAULT 'None' AFTER `per_lb_markup`;

CREATE TABLE `package_batches` (
  `id` int NOT NULL AUTO_INCREMENT,
  `client_id` int NOT NULL,
  `batch_number` varchar(50) NOT NULL,
  `fulfillment_method` varchar(20) NOT NULL,
  `status` varchar(30) NOT NULL DEFAULT 'Draft',
  `scheduled_date` date DEFAULT NULL,
  `completed_date` date DEFAULT NULL,
  `created_by_user_id` int NOT NULL,
  `notes` varchar(500) DEFAULT NULL,
  `created_at` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `updated_at` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  PRIMARY KEY (`id`),
  UNIQUE KEY `UX_package_batches_number` (`batch_number`),
  KEY `IX_package_batches_client_status` (`client_id`, `status`),
  KEY `IX_package_batches_created_by` (`created_by_user_id`),
  CONSTRAINT `FK_package_batches_client` FOREIGN KEY (`client_id`) REFERENCES `clients` (`id`) ON DELETE RESTRICT,
  CONSTRAINT `FK_package_batches_created_by` FOREIGN KEY (`created_by_user_id`) REFERENCES `users` (`id`) ON DELETE RESTRICT,
  CONSTRAINT `CK_package_batches_method` CHECK (`fulfillment_method` IN ('Delivery','Pickup')),
  CONSTRAINT `CK_package_batches_status` CHECK (`status` IN ('Draft','Ready','Scheduled','OutForDelivery','ReadyForPickup','Collected','Delivered','Cancelled'))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `package_batch_items` (
  `batch_id` int NOT NULL,
  `package_id` int NOT NULL,
  `added_at` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  PRIMARY KEY (`batch_id`, `package_id`),
  UNIQUE KEY `UX_package_batch_items_package` (`package_id`),
  CONSTRAINT `FK_package_batch_items_batch` FOREIGN KEY (`batch_id`) REFERENCES `package_batches` (`id`) ON DELETE CASCADE,
  CONSTRAINT `FK_package_batch_items_package` FOREIGN KEY (`package_id`) REFERENCES `UserPackages` (`package_id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
