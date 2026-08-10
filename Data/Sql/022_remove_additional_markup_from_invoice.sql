-- Keep invoice totals aligned with the customer-facing invoice lines.
-- Additional markup is stored for operational reference but is not invoiced.
UPDATE user_package_assignments assignment
INNER JOIN UserPackages package ON package.package_id = assignment.package_id
SET assignment.invoice_cost = ROUND(
    COALESCE(package.weight, 0) *
    (COALESCE(assignment.per_lb_cost, 0) + COALESCE(assignment.per_lb_markup, 0)) +
    COALESCE(package.customs_charges, 0),
    2
);

UPDATE UserPackages package
INNER JOIN user_package_assignments assignment ON assignment.package_id = package.package_id
SET package.invoice_amount = assignment.invoice_cost,
    package.amount_due = CASE
        WHEN package.paid_date IS NULL THEN assignment.invoice_cost
        ELSE 0
    END;
