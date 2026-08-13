INSERT INTO `package_statuses` (`name`, `display_order`, `is_active`)
VALUES ('Delivered, Awaiting Payment', 6, 1)
ON DUPLICATE KEY UPDATE
    `display_order` = 6,
    `is_active` = 1,
    `updated_at` = CURRENT_TIMESTAMP(6);

DELETE FROM `package_statuses`
WHERE `name` = 'Delivered, Awaiting Pickup';

UPDATE `UserPackages`
SET `status` = 'Delivered, Awaiting Payment'
WHERE `status` = 'Delivered, Awaiting Pickup';
