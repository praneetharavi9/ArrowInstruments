import { Component, OnInit } from '@angular/core';
import { Product } from '../models/product.model';
import { ProductService } from '../services/product';
import { ProductType } from '../models/product-type.model';
import { ActivatedRoute } from '@angular/router';

@Component({
  selector: 'app-products',
  standalone: false,
  templateUrl: './products.html',
  styleUrl: './products.css'
})
export class Products implements OnInit {

  products: Product[] = [];
  productTypes: ProductType[] = [];
  selectedProduct: Product | null = null;
  loading = true;

  constructor(
    private productService: ProductService,
    private route: ActivatedRoute
  ) {}

  ngOnInit(): void {
  this.route.queryParams.subscribe(params => {
    const typeId = params['type'];
    this.GetAllProductTypes(typeId ? +typeId : null);
  });
}

GetAllProductTypes(autoExpandTypeId: number | null = null): void {
  this.productService.getAllProductsTypes().subscribe({
    next: data => {
      this.productTypes = data;
      this.loading = false;

      // Auto expand after types are loaded
      if (autoExpandTypeId) {
        const type = this.productTypes.find(t => t.id === autoExpandTypeId);
        if (type) this.toggleProducts(type);
      }
    },
    error: () => {
      this.loading = false;
    }
  });
}

  toggleProducts(productType: ProductType): void {
    if (!productType.products) {
      // Lazy load products for this type
      this.productService.getProductsByProductTypeId(productType.id)
        .subscribe({
          next: products => {
            productType.products = products;
            productType.expanded = true;
          },
          error: () => {}
        });
    } else {
      productType.expanded = !productType.expanded;
    }
  }

selectProduct(product: Product): void {
  this.selectedProduct = this.selectedProduct?.id === product.id ? null : product;

  setTimeout(() => {
    const detailPanel = document.getElementById('product-detail-panel');
    if (detailPanel) {
      const navbarHeight = 100; // match your navbar height from app.css margin-top
      const top = detailPanel.getBoundingClientRect().top + window.scrollY - navbarHeight;
      window.scrollTo({ top, behavior: 'smooth' });
    }
  }, 50);
}
}