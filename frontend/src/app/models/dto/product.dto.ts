export interface ProductDto {
  productId: number;
  productTypeId: number;
  productName: string;
  productDescription: string;
  imagePath: string;
  isActive: boolean;
  dateCreated: string;
  dateUpdated?: string | null;
}
