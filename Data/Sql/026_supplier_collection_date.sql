ALTER TABLE `supplier_collections`
    ADD COLUMN `collection_date` DATE NULL AFTER `notes`;
UPDATE `supplier_collections` SET `collection_date` = DATE(`created_at`) WHERE `collection_date` IS NULL;
ALTER TABLE `supplier_collections` MODIFY COLUMN `collection_date` DATE NOT NULL;
