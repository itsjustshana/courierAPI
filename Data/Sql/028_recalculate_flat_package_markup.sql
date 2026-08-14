-- Recalculate existing assigned packages using:
-- (weight * per-lb cost) + flat package markup + customs charges.
UPDATE `user_package_assignments` AS assignment
INNER JOIN `UserPackages` AS package
    ON package.`package_id` = assignment.`package_id`
SET assignment.`invoice_cost` = ROUND(
        COALESCE(package.`weight`, 0) * assignment.`per_lb_cost`
        + assignment.`per_lb_markup`
        + COALESCE(package.`customs_charges`, 0),
        2
    ),
    assignment.`updated_at` = UTC_TIMESTAMP();

UPDATE `UserPackages` AS package
INNER JOIN `user_package_assignments` AS assignment
    ON assignment.`package_id` = package.`package_id`
SET package.`invoice_amount` = assignment.`invoice_cost`,
    package.`amount_due` = CASE
        WHEN package.`paid_date` IS NULL THEN assignment.`invoice_cost`
        ELSE 0
    END;
