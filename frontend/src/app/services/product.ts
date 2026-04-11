import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { ProductDto } from '../models/dto/product.dto';
import { Product } from '../models/product.model';
import { ProductType } from '../models/product-type.model';
import { ProductTypeDto } from '../models/dto/product-type.dto';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class ProductService {
  private apiUrl = `${environment.apiUrl}`;

  constructor(private http: HttpClient) { }

  getProducts(): Observable<Product[]> {
    const url = `${this.apiUrl}/products`;
    return this.http.get<ProductDto[]>(url).pipe(
      map(dtos => dtos.map(dto => new Product(dto)))
    );
  }

  getAllProductsTypes(): Observable<ProductType[]> {
    const url = `${this.apiUrl}/producttype`;
    return this.http.get<ProductTypeDto[]>(url).pipe(
      map(dtos => dtos.map(dto => new ProductType(dto)))
    );
  }

  getProductsByProductTypeId(productTypeId: number): Observable<Product[]> {
    const url = `${this.apiUrl}/products/GetProductsByProductId/${productTypeId}`;

    return this.http.get<ProductDto[]>(url).pipe(
      map(dtos => dtos.map(dto => new Product(dto)))
    );
  }

}
