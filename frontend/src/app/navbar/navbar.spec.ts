import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AppNavbar } from './navbar';

describe('AppNavbar', () => {
  let component: AppNavbar;
  let fixture: ComponentFixture<AppNavbar>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [AppNavbar]
    })
    .compileComponents();

    fixture = TestBed.createComponent(AppNavbar);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
