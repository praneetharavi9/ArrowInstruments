import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export type ReminderFrequency = 'daily' | 'weekly' | 'monthly' | 'yearly';

export interface ReminderScheduleAttachment {
  id: number;
  fileName: string;
}

export interface ReminderSchedule {
  id: number;
  companyId: number;
  frequency: ReminderFrequency;
  startDate: string;
  endDate: string | null;
  sendTime: string;
  attachLedgerStatement: boolean;
  to: string[];
  cc: string[];
  subject: string;
  body: string;
  isActive: boolean;
  lastSentAt: string | null;
  nextRunAt: string;
  dateCreated: string;
  attachments: ReminderScheduleAttachment[];
}

export interface SendReminderPayload {
  to: string[];
  cc: string[];
  subject: string;
  body: string;
  files: File[];
  attachLedgerStatement: boolean;
  ledgerStartDate?: string;
  ledgerEndDate?: string;
}

export interface ScheduleReminderPayload extends SendReminderPayload {
  frequency: ReminderFrequency;
  startDate: string;
  endDate?: string | null;
  sendTime: string;
}

@Injectable({ providedIn: 'root' })
export class AdminReminderService {
  private apiUrl = `${environment.apiUrl}/reminders`;

  constructor(private http: HttpClient) {}

  sendReminder(companyId: number, payload: SendReminderPayload): Observable<{ success: boolean; message: string }> {
    const form = this.buildRecipientForm(payload);
    return this.http.post<{ success: boolean; message: string }>(
      `${this.apiUrl}/customers/${companyId}/send`, form
    );
  }

  createSchedule(companyId: number, payload: ScheduleReminderPayload): Observable<{ success: boolean; message: string; scheduleId: number }> {
    const form = this.buildRecipientForm(payload);
    form.set('frequency', payload.frequency);
    form.set('startDate', payload.startDate);
    if (payload.endDate) {
      form.set('endDate', payload.endDate);
    }
    form.set('sendTime', payload.sendTime);
    form.set('attachLedgerStatement', String(payload.attachLedgerStatement));
    return this.http.post<{ success: boolean; message: string; scheduleId: number }>(
      `${this.apiUrl}/customers/${companyId}/schedules`, form
    );
  }

  getSchedules(companyId: number): Observable<ReminderSchedule[]> {
    return this.http.get<ReminderSchedule[]>(`${this.apiUrl}/customers/${companyId}/schedules`);
  }

  toggleSchedule(id: number): Observable<{ success: boolean; isActive: boolean }> {
    return this.http.put<{ success: boolean; isActive: boolean }>(`${this.apiUrl}/schedules/${id}/toggle`, {});
  }

  deleteSchedule(id: number): Observable<{ success: boolean; message: string }> {
    return this.http.delete<{ success: boolean; message: string }>(`${this.apiUrl}/schedules/${id}`);
  }

  private buildRecipientForm(payload: SendReminderPayload): FormData {
    const form = new FormData();
    form.set('to', payload.to.join(','));
    form.set('cc', payload.cc.join(','));
    form.set('subject', payload.subject);
    form.set('body', payload.body);
    form.set('attachLedgerStatement', String(payload.attachLedgerStatement));
    if (payload.ledgerStartDate) form.set('ledgerStartDate', payload.ledgerStartDate);
    if (payload.ledgerEndDate) form.set('ledgerEndDate', payload.ledgerEndDate);
    for (const file of payload.files) {
      form.append('files', file, file.name);
    }
    return form;
  }
}
