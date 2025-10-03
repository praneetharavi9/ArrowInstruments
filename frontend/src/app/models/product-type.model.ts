import { ProductTypeDto } from './dto/product-type.dto';
import { Product } from './product.model';

export class ProductType {
    id: number;
    name: string;
    description?: string;
    isActive: boolean;
    dateCreated: Date;
    dateUpdated?: Date;
    expanded: boolean = false;
    products?: Product[];

    constructor(dto: ProductTypeDto) {
        this.id = dto.productTypeId;
        this.name = dto.productTypeName;
        this.description = dto.productTypeDescription ?? '';
        this.isActive = dto.isActive;
        this.dateCreated = new Date(dto.dateCreated);
        this.dateUpdated = dto.dateUpdated ? new Date(dto.dateUpdated) : undefined;
    }

}
