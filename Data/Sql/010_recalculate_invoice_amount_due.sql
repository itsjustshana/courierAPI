UPDATE `user_package_assignments` AS assignment
INNER JOIN `clients` AS client ON client.`id` = assignment.`client_id`
INNER JOIN `UserPackages` AS package ON package.`package_id` = assignment.`package_id`
SET assignment.`invoice_cost` = ROUND(
      COALESCE(package.`weight`, 0) * (client.`per_lb_cost` + client.`per_lb_markup`)
      + COALESCE(package.`customs_charges`, 0)
      + COALESCE(package.`additional_markup`, 0), 2),
    package.`invoice_amount` = ROUND(
      COALESCE(package.`weight`, 0) * (client.`per_lb_cost` + client.`per_lb_markup`)
      + COALESCE(package.`customs_charges`, 0)
      + COALESCE(package.`additional_markup`, 0), 2),
    package.`amount_due` = CASE
      WHEN package.`paid_date` IS NULL THEN ROUND(
        COALESCE(package.`weight`, 0) * (client.`per_lb_cost` + client.`per_lb_markup`)
        + COALESCE(package.`customs_charges`, 0)
        + COALESCE(package.`additional_markup`, 0), 2)
      ELSE package.`amount_due`
    END;
