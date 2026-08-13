ALTER TABLE `clients`
    ADD COLUMN `address1` VARCHAR(255) NULL AFTER `phone`,
    ADD COLUMN `address2` VARCHAR(255) NULL AFTER `address1`,
    ADD COLUMN `city` VARCHAR(100) NULL AFTER `address2`,
    ADD COLUMN `zip` VARCHAR(20) NULL AFTER `city`,
    ADD COLUMN `state` VARCHAR(100) NULL AFTER `zip`;
