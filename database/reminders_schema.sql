-- ============================================================
-- ARROW INSTRUMENTS - Email Reminders Schema
-- Run this in phpMyAdmin (or your MySQL client) after
-- companies_schema.sql. Powers the "Send Email Reminder" and
-- "Schedule Reminders" buttons on the customer ledger page.
-- ============================================================

CREATE TABLE IF NOT EXISTS reminder_schedules (
  id INT(11) NOT NULL AUTO_INCREMENT,
  company_id INT(11) NOT NULL,
  frequency VARCHAR(20) NOT NULL,          -- 'weekly' | 'monthly' | 'yearly'
  start_date DATE NOT NULL,
  end_date DATE NULL,
  to_emails TEXT NOT NULL,                 -- JSON array of recipient email strings
  cc_emails TEXT NULL,                     -- JSON array of cc email strings
  subject VARCHAR(255) NOT NULL,
  body TEXT NOT NULL,
  is_active TINYINT(1) NOT NULL DEFAULT 1,
  last_sent_at DATETIME NULL,
  next_run_at DATETIME NOT NULL,
  date_created DATETIME NOT NULL,
  date_updated DATETIME NULL,
  PRIMARY KEY (id),
  FOREIGN KEY (company_id) REFERENCES companies(company_id) ON DELETE CASCADE,
  INDEX idx_reminder_due (is_active, next_run_at)
);

CREATE TABLE IF NOT EXISTS reminder_attachments (
  id INT(11) NOT NULL AUTO_INCREMENT,
  reminder_schedule_id INT(11) NOT NULL,
  file_name VARCHAR(255) NOT NULL,         -- original name shown to the recipient
  stored_file_name VARCHAR(255) NOT NULL,  -- unique name on the FTP server
  content_type VARCHAR(100) NULL,
  date_created DATETIME NOT NULL,
  PRIMARY KEY (id),
  FOREIGN KEY (reminder_schedule_id) REFERENCES reminder_schedules(id) ON DELETE CASCADE
);
