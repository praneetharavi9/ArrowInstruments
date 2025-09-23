// src/app/models/product.model.ts
import { ProductDto } from './dto/product.dto';

const BASE_IMAGE_URL = 'https://arrowinstruments.in/product_images/';

export class Product {
  id: number;
  name: string;
  description: string;
  imageUrl: string;
  isActive: boolean;

  constructor(dto: ProductDto) {
    this.id = dto.productId;
    this.name = dto.productName;
    this.description = dto.productDescription;
    this.imageUrl = `${BASE_IMAGE_URL}${dto.imagePath.replace(/^\/+/, '')}`;
    this.isActive = dto.isActive;
  }
}
