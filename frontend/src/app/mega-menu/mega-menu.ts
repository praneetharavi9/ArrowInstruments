import { Component } from '@angular/core';
import { ProductType } from '../models/product-type.model';
import { Product } from '../models/product.model';
import { ProductService } from '../services/product';

@Component({
  selector: 'app-mega-menu',
  standalone: false,
  templateUrl: './mega-menu.html',
  styleUrl: './mega-menu.css'
})
export class MegaMenu {
  products : Product [] = [];
  loading = true;
  productTypes : ProductType[] = [];

  constructor(private productService: ProductService) {}

  ngOnInit(): void {
    this.GetAllProductTypes();
  }

  GetAllProducts() : void{
  this.productService.getProducts().subscribe({
      next: data => {
        this.products = data;
        this.loading = false;
      },
      error: err => {
        console.error('Error fetching products', err);
        this.loading = false;
      }
    });
  }

  GetAllProductTypes() : void{
    this.productService.getAllProductsTypes().subscribe({
      next: data =>{
        this.productTypes = data;
        this.loading = false;
      }
    });
  }


    toggleProducts(productType: ProductType) {
    if (!productType.products) {
      // Lazy load products
      this.productService.getProductsByProductTypeId(productType.id)
        .subscribe(products => productType.products = products);
    }
    productType.expanded = !productType.expanded;
  }
}


