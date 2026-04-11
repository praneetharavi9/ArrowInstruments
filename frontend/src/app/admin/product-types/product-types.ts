import { Component, OnInit } from '@angular/core';
import { AdminProductTypeService, AdminProductType } from '../services/admin-product-type.service';
import { AdminProductService } from '../services/admin-product.service';
import { ToastService } from '../services/toast.service';
import { catchError, of } from 'rxjs';

@Component({
  selector: 'app-product-types',
  standalone: false,
  templateUrl: './product-types.html',
  styleUrl: './product-types.css'
})
export class ProductTypesComponent implements OnInit {
  productTypes: (AdminProductType & { productCount: number })[] = [];
  loading = true;

  // Modal state
  showModal = false;
  modalMode: 'add' | 'edit' = 'add';
  editingId: number | null = null;
  formName = '';
  formDescription = '';
  formLoading = false;

  // Delete confirm
  showDeleteConfirm = false;
  deletingId: number | null = null;
  deletingName = '';
  deleteLoading = false;

  constructor(
    private productTypeService: AdminProductTypeService,
    private productService: AdminProductService,
    private toastService: ToastService
  ) {}

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    this.loading = true;
    this.productTypeService.getAll().subscribe({
      next: (types) => {
        this.productService.getAll().pipe(catchError(() => of([]))).subscribe(products => {
          this.productTypes = types.map(t => ({
            ...t,
            productCount: products.filter(p => p.productTypeId === t.productTypeId).length
          }));
          this.loading = false;
        });
      },
      error: () => {
        this.loading = false;
        this.toastService.show('Failed to load product types.', 'danger');
      }
    });
  }

  openAddModal(): void {
    this.modalMode = 'add';
    this.editingId = null;
    this.formName = '';
    this.formDescription = '';
    this.showModal = true;
  }

  openEditModal(type: AdminProductType): void {
    this.modalMode = 'edit';
    this.editingId = type.productTypeId;
    this.formName = type.productTypeName;
    this.formDescription = type.productTypeDescription || '';
    this.showModal = true;
  }

  closeModal(): void {
    this.showModal = false;
  }

  saveProductType(): void {
    if (!this.formName.trim()) return;
    this.formLoading = true;

    const data = { productTypeName: this.formName.trim(), productTypeDescription: this.formDescription.trim() };
    const obs = this.modalMode === 'add'
      ? this.productTypeService.create(data)
      : this.productTypeService.update(this.editingId!, data);

    obs.subscribe({
      next: () => {
        this.formLoading = false;
        this.showModal = false;
        this.toastService.show(
          this.modalMode === 'add' ? 'Product type added successfully.' : 'Product type updated.',
          'success'
        );
        this.loadData();
      },
      error: () => {
        this.formLoading = false;
        this.toastService.show('Failed to save. Please try again.', 'danger');
      }
    });
  }

  confirmDelete(type: AdminProductType & { productCount: number }): void {
    this.deletingId = type.productTypeId;
    this.deletingName = type.productTypeName;
    this.showDeleteConfirm = true;
  }

  cancelDelete(): void {
    this.showDeleteConfirm = false;
    this.deletingId = null;
    this.deletingName = '';
  }

  doDelete(): void {
    if (!this.deletingId) return;
    this.deleteLoading = true;
    this.productTypeService.delete(this.deletingId).subscribe({
      next: () => {
        this.deleteLoading = false;
        this.showDeleteConfirm = false;
        this.toastService.show('Product type deleted.', 'success');
        this.loadData();
      },
      error: () => {
        this.deleteLoading = false;
        this.toastService.show('Delete failed. It may have products attached to it.', 'danger');
      }
    });
  }
}
