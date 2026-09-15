-- ============================================================
-- ARROW INSTRUMENTS - Companies Schema
-- Run this in BigRock cPanel phpMyAdmin (or your MySQL client)
-- before using the admin Companies page.
-- ============================================================

CREATE TABLE IF NOT EXISTS companies (
  company_id INT(11) NOT NULL AUTO_INCREMENT,
  company_name VARCHAR(255) NOT NULL,
  gst_number VARCHAR(50) NOT NULL,
  address1 VARCHAR(255) NULL,
  address2 VARCHAR(255) NULL,
  city VARCHAR(100) NULL,
  state VARCHAR(100) NULL,
  zipcode VARCHAR(20) NULL,
  opening_balance DECIMAL(12,2) NOT NULL DEFAULT 0,
  is_active TINYINT(1) NOT NULL DEFAULT 1,
  date_created DATETIME NOT NULL,
  date_updated DATETIME NULL,
  PRIMARY KEY (company_id)
);

CREATE TABLE IF NOT EXISTS company_phones (
  phone_id INT(11) NOT NULL AUTO_INCREMENT,
  company_id INT(11) NOT NULL,
  phone_number VARCHAR(30) NOT NULL,
  is_primary TINYINT(1) NOT NULL DEFAULT 0,
  display_order INT(11) DEFAULT 0,
  PRIMARY KEY (phone_id),
  FOREIGN KEY (company_id) REFERENCES companies(company_id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS company_emails (
  email_id INT(11) NOT NULL AUTO_INCREMENT,
  company_id INT(11) NOT NULL,
  email_address VARCHAR(255) NOT NULL,
  is_primary TINYINT(1) NOT NULL DEFAULT 0,
  display_order INT(11) DEFAULT 0,
  PRIMARY KEY (email_id),
  FOREIGN KEY (company_id) REFERENCES companies(company_id) ON DELETE CASCADE
);
