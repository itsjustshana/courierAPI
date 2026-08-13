CREATE TABLE IF NOT EXISTS `package_statuses` (
  `id` int NOT NULL AUTO_INCREMENT,
  `name` varchar(50) NOT NULL,
  `display_order` int NOT NULL DEFAULT 0,
  `is_active` tinyint(1) NOT NULL DEFAULT 1,
  `created_at` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `updated_at` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  PRIMARY KEY (`id`),
  UNIQUE KEY `UX_package_statuses_name` (`name`),
  KEY `IX_package_statuses_active_order` (`is_active`, `display_order`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

UPDATE `package_statuses` SET `is_active` = 0;

INSERT INTO `package_statuses` (`name`, `display_order`, `is_active`) VALUES
  ('In US warehouse', 1, 1),
  ('In Transit', 2, 1),
  ('Delayed', 3, 1),
  ('In Jamaica', 4, 1),
  ('Out for Delivery', 5, 1),
  ('Delivered, Awaiting Payment', 6, 1),
  ('Delivered', 7, 1)
ON DUPLICATE KEY UPDATE
  `name` = VALUES(`name`),
  `display_order` = VALUES(`display_order`),
  `is_active` = 1,
  `updated_at` = CURRENT_TIMESTAMP(6);
