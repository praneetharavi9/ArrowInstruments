import { Component, OnInit } from '@angular/core';
import { Product } from '../models/product.model';
import { ProductService } from '../services/product';

@Component({
  selector: 'app-products',
  standalone: false,
  templateUrl: './products.html',
  styleUrl: './products.css'
})
export class Products implements OnInit {

  products : Product [] = [];
  loading = true;

  constructor(private productService: ProductService) {}

  ngOnInit(): void {
    debugger
    this.productService.getProducts().subscribe({
      next: data => {
        debugger
        console.log(data.length);
        this.products = data;
        this.loading = false;
      },
      error: err => {debugger
        console.error('Error fetching products', err);
        this.loading = false;
      }
    });
  }
}
