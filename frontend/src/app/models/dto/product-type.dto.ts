export interface ProductTypeDto {
  productTypeId: number;
  productTypeName: string;
  productTypeDescription?: string;
  isActive: boolean;
  dateCreated: Date;
  dateUpdated?: Date;
}
