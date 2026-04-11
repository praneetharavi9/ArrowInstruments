import { ProductDto } from './dto/product.dto';
import { ProductSpec } from './product-spec.model';
import { environment } from '../../environments/environment';

export class Product {
  id: number;
  name: string;
  description: string;
  imageUrl: string;
  isActive: boolean;
  specs: ProductSpec[];

  constructor(dto: ProductDto) {
    this.id = dto.productId;
    this.name = dto.productName;
    this.description = dto.productDescription;
    this.imageUrl = dto.imagePath
      ? `${environment.imageBaseUrl}${dto.imagePath.replace(/^\/+/, '')}`
      : 'assets/placeholder.jpg';
    this.isActive = dto.isActive;
    this.specs = dto.specs ? dto.specs.map(s => new ProductSpec(s)) : [];
  }
}