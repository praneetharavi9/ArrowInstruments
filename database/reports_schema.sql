-- ============================================================
-- ARROW INSTRUMENTS - Reports / Customer Ledger Schema
-- Run this in phpMyAdmin (or your MySQL client) after
-- companies_schema.sql, before using the admin Reports page.
-- ============================================================

-- If you already ran an earlier version of companies_schema.sql
-- (before opening_balance existed), add the column here:
ALTER TABLE companies ADD COLUMN opening_balance DECIMAL(12,2) NOT NULL DEFAULT 0 AFTER zipcode;

CREATE TABLE IF NOT EXISTS ledger_entries (
  entry_id INT(11) NOT NULL AUTO_INCREMENT,
  company_id INT(11) NOT NULL,
  entry_date DATE NOT NULL,
  description VARCHAR(255) NULL,
  type VARCHAR(50) NULL,
  trans_no VARCHAR(50) NULL,
  debit DECIMAL(12,2) NOT NULL DEFAULT 0,
  credit DECIMAL(12,2) NOT NULL DEFAULT 0,
  date_created DATETIME NOT NULL,
  PRIMARY KEY (entry_id),
  FOREIGN KEY (company_id) REFERENCES companies(company_id) ON DELETE CASCADE,
  INDEX idx_ledger_company_date (company_id, entry_date)
);

-- If you already ran an earlier version of this file (before type/trans_no
-- existed), add the columns here:
ALTER TABLE ledger_entries ADD COLUMN type VARCHAR(50) NULL AFTER description;
ALTER TABLE ledger_entries ADD COLUMN trans_no VARCHAR(50) NULL AFTER type;
