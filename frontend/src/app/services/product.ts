import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { ProductDto } from '../models/dto/product.dto';
import { Product } from '../models/product.model';

@Injectable({
  providedIn: 'root'
})
export class ProductService {
    private apiUrl = 'https://localhost:7275/api/Products';

  constructor(private http: HttpClient) {}

 getProducts(): Observable<Product[]> {
  debugger
    return this.http.get<ProductDto[]>(this.apiUrl).pipe(
      map(dtos => dtos.map(dto => new Product(dto)))
    );
  }
}
