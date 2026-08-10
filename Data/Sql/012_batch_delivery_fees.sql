ALTER TABLE `clients`
  ADD COLUMN `default_delivery_fee` decimal(10,2) NOT NULL DEFAULT 0.00 AFTER `batch_handling_mode`;

ALTER TABLE `package_batches`
  ADD COLUMN `delivery_fee` decimal(10,2) NOT NULL DEFAULT 0.00 AFTER `fulfillment_method`,
  ADD COLUMN `delivery_area` varchar(100) NULL AFTER `delivery_fee`,
  ADD COLUMN `delivery_address` varchar(255) NULL AFTER `delivery_area`,
  ADD COLUMN `delivery_fee_source` varchar(20) NOT NULL DEFAULT 'ClientDefault' AFTER `delivery_address`,
  ADD COLUMN `delivery_fee_override_reason` varchar(255) NULL AFTER `delivery_fee_source`;
