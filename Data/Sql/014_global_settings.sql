CREATE TABLE `global_settings` (
  `id` int NOT NULL AUTO_INCREMENT,
  `app_name` varchar(100) NOT NULL,
  `logo_url` text NULL,
  `created_at` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`)
);

INSERT INTO `global_settings` (`app_name`, `logo_url`)
VALUES ('MekMiCourier', NULL);
