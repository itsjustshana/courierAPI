ALTER TABLE `user_package_assignments`
  ADD COLUMN `per_lb_cost` decimal(10,2) NOT NULL DEFAULT 0.00 AFTER `updated_at`,
  ADD COLUMN `per_lb_markup` decimal(10,2) NOT NULL DEFAULT 0.00 AFTER `per_lb_cost`,
  ADD COLUMN `invoice_cost` decimal(12,2) NOT NULL DEFAULT 0.00 AFTER `per_lb_markup`;

UPDATE `user_package_assignments` AS assignment
INNER JOIN `clients` AS client ON client.`id` = assignment.`client_id`
INNER JOIN `UserPackages` AS package ON package.`package_id` = assignment.`package_id`
SET assignment.`per_lb_cost` = client.`per_lb_cost`,
    assignment.`per_lb_markup` = client.`per_lb_markup`,
    assignment.`invoice_cost` = ROUND(
      COALESCE(package.`weight`, 0) * (client.`per_lb_cost` + client.`per_lb_markup`)
      + COALESCE(package.`customs_charges`, 0), 2);
