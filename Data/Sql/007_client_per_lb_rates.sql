ALTER TABLE `clients`
  ADD COLUMN `per_lb_cost` decimal(10,2) NOT NULL DEFAULT 0.00 AFTER `logo_url`,
  ADD COLUMN `per_lb_markup` decimal(10,2) NOT NULL DEFAULT 0.00 AFTER `per_lb_cost`;
