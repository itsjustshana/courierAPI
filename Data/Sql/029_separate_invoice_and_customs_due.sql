-- Invoice amount is freight only. Amount due adds customs when unpaid.
UPDATE `user_package_assignments` AS assignment
INNER JOIN `UserPackages` AS package
    ON package.`package_id` = assignment.`package_id`
SET assignment.`invoice_cost` = GREATEST(
        ROUND(assignment.`invoice_cost` - COALESCE(package.`customs_charges`, 0), 2),
        0
    ),
    assignment.`updated_at` = UTC_TIMESTAMP();

UPDATE `UserPackages` AS package
INNER JOIN `user_package_assignments` AS assignment
    ON assignment.`package_id` = package.`package_id`
SET package.`invoice_amount` = assignment.`invoice_cost`,
    package.`amount_due` = CASE
        WHEN package.`paid_date` IS NULL THEN ROUND(
            assignment.`invoice_cost` + COALESCE(package.`customs_charges`, 0), 2)
        ELSE 0
    END;
