import { ProductSpecDto } from './dto/product-spec.dto';

export class ProductSpec {
  specId: number;
  productId: number;
  specName: string;
  specValue: string;
  displayOrder: number;

  constructor(dto: ProductSpecDto) {
    this.specId = dto.specId;
    this.productId = dto.productId;
    this.specName = dto.specName;
    this.specValue = dto.specValue;
    this.displayOrder = dto.displayOrder;
  }
}