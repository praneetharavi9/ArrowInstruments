import { Component, EventEmitter, Input, Output } from '@angular/core';

@Component({
  selector: 'app-date-range-filter',
  standalone: false,
  templateUrl: './date-range-filter.html',
  styleUrl: './date-range-filter.css'
})
export class DateRangeFilter {
  @Input() startDate = '';
  @Input() endDate = '';
  @Output() rangeChange = new EventEmitter<{ start: string; end: string }>();

  showPanel = false;
  draftStart = '';
  draftEnd = '';

  togglePanel(): void {
    if (!this.showPanel) {
      this.draftStart = this.startDate;
      this.draftEnd = this.endDate;
    }
    this.showPanel = !this.showPanel;
  }

  closePanel(): void {
    this.showPanel = false;
  }

  applyCustom(): void {
    if (!this.draftStart || !this.draftEnd) return;
    this.rangeChange.emit({ start: this.draftStart, end: this.draftEnd });
    this.showPanel = false;
  }

  applyPreset(preset: 'today' | 'week' | 'month' | 'year'): void {
    const today = new Date();
    const end = new Date(today);
    let start: Date;

    switch (preset) {
      case 'today':
        start = new Date(today);
        break;
      case 'week': {
        const dayOffset = (today.getDay() + 6) % 7; // Monday = start of week
        start = new Date(today);
        start.setDate(today.getDate() - dayOffset);
        break;
      }
      case 'month':
        start = new Date(today.getFullYear(), today.getMonth(), 1);
        break;
      case 'year':
        start = new Date(today.getFullYear(), 0, 1);
        break;
    }

    this.rangeChange.emit({ start: this.toDateString(start), end: this.toDateString(end) });
    this.showPanel = false;
  }

  private toDateString(d: Date): string {
    const y = d.getFullYear();
    const m = (d.getMonth() + 1).toString().padStart(2, '0');
    const day = d.getDate().toString().padStart(2, '0');
    return `${y}-${m}-${day}`;
  }
}
