import { Component, OnInit } from '@angular/core';
import { AdminProductService, AdminProduct, ProductFormData, ProductSavePayload } from '../services/admin-product.service';
import { AdminProductTypeService, AdminProductType } from '../services/admin-product-type.service';
import { ToastService } from '../services/toast.service';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-admin-products',
  standalone: false,
  templateUrl: './admin-products.html',
  styleUrl: './admin-products.css'
})
export class AdminProductsComponent implements OnInit {
  productTypes: AdminProductType[] = [];
  products: AdminProduct[] = [];
  selectedTypeId: number | null = null;
  loading = false;
  typesLoading = true;

  // Modal
  showModal = false;
  modalMode: 'add' | 'edit' = 'add';
  editingProduct: AdminProduct | null = null;
  formLoading = false;

  // Form fields
  formData: ProductFormData = this.emptyForm();
  specRows: { specName: string; specValue: string }[] = [];
  selectedFile: File | null = null;
  previewUrl: string | null = null;

  // Delete confirm
  showDeleteConfirm = false;
  deletingProduct: AdminProduct | null = null;
  deleteLoading = false;

  constructor(
    private productService: AdminProductService,
    private productTypeService: AdminProductTypeService,
    private toastService: ToastService
  ) {}

  ngOnInit(): void {
    this.productTypeService.getAll().subscribe({
      next: types => {
        this.productTypes = types;
        this.typesLoading = false;
        if (types.length > 0) {
          this.selectedTypeId = types[0].productTypeId;
          this.loadProducts();
        }
      },
      error: () => {
        this.typesLoading = false;
        this.toastService.show('Failed to load product categories.', 'danger');
      }
    });
  }

  private emptyForm(): ProductFormData {
    return { productName: '', productDescription: '', productTypeId: 0, isActive: true };
  }

  loadProducts(): void {
    if (!this.selectedTypeId) return;
    this.loading = true;
    this.productService.getByTypeId(this.selectedTypeId).subscribe({
      next: products => {
        this.products = products;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.toastService.show('Failed to load products.', 'danger');
      }
    });
  }

  onTypeChange(): void {
    this.loadProducts();
  }

  getImageUrl(imagePath: string): string {
    return imagePath ? `${environment.imageBaseUrl}${imagePath}` : 'assets/placeholder.jpg';
  }

  openAddModal(): void {
    this.modalMode = 'add';
    this.editingProduct = null;
    this.formData = { ...this.emptyForm(), productTypeId: this.selectedTypeId || 0 };
    this.specRows = [];
    this.selectedFile = null;
    this.previewUrl = null;
    this.showModal = true;
  }

  openEditModal(product: AdminProduct): void {
    this.modalMode = 'edit';
    this.editingProduct = product;
    this.formData = {
      productName: product.productName,
      productDescription: product.productDescription,
      productTypeId: product.productTypeId,
      isActive: product.isActive
    };
    this.specRows = product.specs
      ? product.specs.map(s => ({ specName: s.specName, specValue: s.specValue }))
      : [];
    this.selectedFile = null;
    this.previewUrl = null;
    this.showModal = true;
  }

  closeModal(): void {
    this.showModal = false;
  }

  onFileChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files?.length) {
      this.selectedFile = input.files[0];
      const reader = new FileReader();
      reader.onload = e => { this.previewUrl = e.target?.result as string; };
      reader.readAsDataURL(this.selectedFile);
    }
  }

  addSpec(): void {
    this.specRows.push({ specName: '', specValue: '' });
  }

  removeSpec(index: number): void {
    this.specRows.splice(index, 1);
  }

  saveProduct(): void {
    if (!this.formData.productName.trim() || !this.formData.productTypeId) return;
    this.formLoading = true;

    const payload: ProductSavePayload = {
      ...this.formData,
      specs: this.specRows
        .filter(s => s.specName.trim())
        .map((s, i) => ({ specName: s.specName, specValue: s.specValue, displayOrder: i }))
    };

    if (this.modalMode === 'add') {
      this.productService.create(payload).subscribe({
        next: created => {
          if (this.selectedFile) {
            this.uploadImage(created.productId, () => this.afterSave('Product added successfully.'));
          } else {
            this.afterSave('Product added successfully.');
          }
        },
        error: () => {
          this.formLoading = false;
          this.toastService.show('Failed to add product.', 'danger');
        }
      });
    } else {
      const id = this.editingProduct!.productId;
      this.productService.update(id, payload).subscribe({
        next: () => {
          if (this.selectedFile) {
            this.uploadImage(id, () => this.afterSave('Product updated successfully.'));
          } else {
            this.afterSave('Product updated successfully.');
          }
        },
        error: () => {
          this.formLoading = false;
          this.toastService.show('Failed to update product.', 'danger');
        }
      });
    }
  }

  private uploadImage(productId: number, callback: () => void): void {
    const fd = new FormData();
    fd.append('image', this.selectedFile!);
    this.productService.uploadImage(productId, fd).subscribe({
      next: callback,
      error: () => {
        this.formLoading = false;
        this.toastService.show('Product saved but image upload failed.', 'warning');
        this.showModal = false;
        this.loadProducts();
      }
    });
  }

  private afterSave(message: string): void {
    this.formLoading = false;
    this.showModal = false;
    this.toastService.show(message, 'success');
    if (this.selectedTypeId === this.formData.productTypeId) {
      this.loadProducts();
    }
  }

  confirmDelete(product: AdminProduct): void {
    this.deletingProduct = product;
    this.showDeleteConfirm = true;
  }

  cancelDelete(): void {
    this.showDeleteConfirm = false;
    this.deletingProduct = null;
  }

  doDelete(): void {
    if (!this.deletingProduct) return;
    this.deleteLoading = true;
    this.productService.delete(this.deletingProduct.productId).subscribe({
      next: () => {
        this.deleteLoading = false;
        this.showDeleteConfirm = false;
        this.toastService.show('Product deleted.', 'success');
        this.loadProducts();
      },
      error: () => {
        this.deleteLoading = false;
        this.toastService.show('Delete failed. Please try again.', 'danger');
      }
    });
  }
}
