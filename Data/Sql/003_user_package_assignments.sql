CREATE TABLE IF NOT EXISTS `user_package_assignments` (
  `package_id` int NOT NULL,
  `client_id` int NOT NULL,
  `user_id` int NOT NULL,
  `assigned_by_user_id` int NOT NULL,
  `assigned_at` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `updated_at` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  PRIMARY KEY (`package_id`),
  KEY `IX_package_assignments_client` (`client_id`),
  KEY `IX_package_assignments_user` (`user_id`),
  KEY `IX_package_assignments_assigned_by` (`assigned_by_user_id`),
  CONSTRAINT `FK_package_assignments_package`
    FOREIGN KEY (`package_id`) REFERENCES `UserPackages` (`package_id`) ON DELETE CASCADE,
  CONSTRAINT `FK_package_assignments_client`
    FOREIGN KEY (`client_id`) REFERENCES `clients` (`id`) ON DELETE RESTRICT,
  CONSTRAINT `FK_package_assignments_user`
    FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE RESTRICT,
  CONSTRAINT `FK_package_assignments_assigned_by`
    FOREIGN KEY (`assigned_by_user_id`) REFERENCES `users` (`id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
