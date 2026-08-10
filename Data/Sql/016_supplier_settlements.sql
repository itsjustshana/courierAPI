ALTER TABLE `UserPackages`
  ADD COLUMN `supplier_amount` decimal(12,2) NULL AFTER `additional_markup`,
  ADD COLUMN `supplier_paid_date` date NULL AFTER `supplier_amount`,
  ADD COLUMN `supplier_payment_reference` varchar(100) NULL AFTER `supplier_paid_date`;
