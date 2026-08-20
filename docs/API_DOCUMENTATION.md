# ShopFresherz API Documentation

Frontend rebuild handoff for the current ASP.NET Core backend.

Generated from the current controller and DTO source on 2026-07-03.

## Quick Start

- API root: `/api/v1`
- Health check: `GET /health`
- Swagger UI: `/swagger`
- Auth header: `Authorization: Bearer <accessToken>`
- Guest cart header: `X-Session-Id: <uuid>`
- JSON casing: ASP.NET Core default camelCase output/input.
- Enums: serialized as strings, not numbers, because `JsonStringEnumConverter` is enabled.
- Admin authorization: endpoints marked `Admin` require the `RequireAdmin` policy, allowing `Admin` or `SuperAdmin`.
- Do not expose backend secrets from `appsettings.json` to a frontend agent or client bundle.

Typical frontend base URL setup:

```ts
const API_BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL ?? "https://your-api-host/api/v1";

async function api(path: string, init: RequestInit = {}) {
  const token = typeof window !== "undefined" ? localStorage.getItem("accessToken") : null;
  return fetch(`${API_BASE_URL}${path}`, {
    ...init,
    headers: {
      "Content-Type": "application/json",
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...(init.headers ?? {}),
    },
  });
}
```

## Common Response Shapes

`PagedResult<T>`:

```ts
type PagedResult<T> = {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
};
```

Error responses are usually:

```ts
type ApiError = {
  code: "NOT_FOUND" | "CONFLICT" | "UNAUTHORIZED" | "FORBIDDEN" | "VALIDATION" | "INTERNAL" | string;
  message: string;
};
```

Some legacy branches return `{ error: ... }` or a bare `500`; frontend code should defensively handle both.

## Enums

Send these as strings:

- `UserRole`: `Customer`, `Admin`, `SuperAdmin`
- `OrderStatus`: `Pending`, `AwaitingPayment`, `Paid`, `Processing`, `Shipped`, `Delivered`, `Cancelled`, `RefundRequested`, `Refunded`, `Draft`
- `PaymentStatus`: `Unpaid`, `Paid`, `Refunded`
- `PaymentMethod`: `Card`, `BankTransfer`, `USSD`, `PayOnDelivery`
- `DeliveryMethod`: `Standard`, `Express`, `Pickup`
- `CouponType`: `Percentage`, `Fixed`
- `LoyaltyEventType`: `Earned`, `Redeemed`, `Expired`, `Bonus`

## Endpoint Inventory

### Auth

| Method | Path | Auth | Request | Response |
| --- | --- | --- | --- | --- |
| POST | `/auth/register` | Public | `RegisterRequest` | `201 AuthResponse` |
| POST | `/auth/login` | Public | `LoginRequest` | `200 AuthResponse` |
| POST | `/auth/google` | Public | `GoogleSignInRequest` | `200 AuthResponse` |
| POST | `/auth/refresh` | Public | `RefreshTokenRequest` | `200 AuthResponse` |
| POST | `/auth/verify-email` | Public | `VerifyOtpRequest` | `204` |
| POST | `/auth/forgot-password` | Public | `ForgotPasswordRequest` | `204` |
| POST | `/auth/reset-password` | Public | `ResetPasswordRequest` | `204` |
| POST | `/auth/logout` | User | none | `204` |

Notes:

- Access tokens are RS256 JWTs.
- `RefreshTokenRequest` currently requires both `accessToken` and `refreshToken`.
- Google login expects a Google ID token, not an OAuth code.

### Catalog

| Method | Path | Auth | Query/Request | Response |
| --- | --- | --- | --- | --- |
| GET | `/products` | Public | `page`, `pageSize`, `categoryId`, `brandId`, `priceMin`, `priceMax`, `ratingMin`, `sortBy` | `PagedResult<ProductSummaryDto>` |
| GET | `/products/{slug}` | Public | route `slug` | `ProductDetailDto` |
| POST | `/products` | Admin | `CreateProductRequest` | `201 { id }` |
| PUT | `/products/{id}` | Admin | `UpdateProductRequest` | `204` |
| DELETE | `/products/{id}` | Admin | route `id:guid` | `204` |
| POST | `/products/{id}/images` | Admin | multipart `file` | `201 ProductImageDto` |
| GET | `/categories` | Public | none | `CategoryDto[]` |
| GET | `/categories/{slug}` | Public | route `slug` | `CategoryDetailDto` |
| POST | `/categories` | Admin | `CreateCategoryRequest` | `201 { id }` |
| PUT | `/categories/{id}` | Admin | `UpdateCategoryRequest` | `204` |
| DELETE | `/categories/{id}` | Admin | route `id:int` | `204` |
| GET | `/brands` | Public | none | `BrandDto[]` |

Image upload constraints from controller comments: max 8MB, minimum 800x800px, multipart field name `file`.

### Search

| Method | Path | Auth | Query | Response |
| --- | --- | --- | --- | --- |
| GET | `/search` | Public | `q`, `page`, `pageSize`, `priceMin`, `priceMax`, `ratingMin`, `inStockOnly`, `sortBy` | `SearchResult` |
| GET | `/search/instant` | Public | `q` | `InstantSearchResult` |

Instant search returns empty arrays when `q` is blank or shorter than 2 chars. It is cached for 120 seconds.

### Cart

All cart endpoints are public but use either JWT or `X-Session-Id`.

| Method | Path | Auth | Request | Response |
| --- | --- | --- | --- | --- |
| GET | `/cart` | Public/JWT optional | header `X-Session-Id` | `CartDto` |
| POST | `/cart/items` | Public/JWT optional | `AddToCartRequest` + optional session header | `CartDto` |
| PUT | `/cart/items/{itemId}` | Public/JWT optional | `UpdateCartItemRequest` + optional session header | `204` |
| DELETE | `/cart/items/{itemId}` | Public/JWT optional | optional session header | `204` |
| POST | `/cart/coupon` | Public/JWT optional | query `couponCode`, optional session header | `CouponValidationDto` |
| DELETE | `/cart/coupon` | Public/JWT optional | optional session header | `204` |

Frontend guest cart flow:

1. Generate a UUID once, e.g. `crypto.randomUUID()`.
2. Store it client-side.
3. Send it as `X-Session-Id` on all cart requests until the user logs in.

### Checkout And Orders

Two checkout styles exist:

- `/checkout/*`: preferred two-step Flutterwave inline flow.
- `/orders`: direct order creation flow that can return `paymentUrl`, `paymentReference`, or bank transfer details.

| Method | Path | Auth | Request | Response |
| --- | --- | --- | --- | --- |
| POST | `/checkout/initiate-payment` | Public/JWT optional | `InitiatePaymentRequest` | `200 InitiatePaymentResponse` |
| POST | `/checkout/confirm-order` | Public/JWT optional | `ConfirmOrderRequest` | `200 ConfirmOrderResponse` |
| POST | `/orders` | Public/JWT optional | `CreateOrderRequest` | `201 CreateOrderResponse` |
| GET | `/orders` | User | `page`, `pageSize` | `PagedResult<OrderDto>` |
| GET | `/orders/{orderNumber}` | Public/JWT optional | route `orderNumber` | `OrderDto` |
| POST | `/orders/{orderNumber}/cancel` | User | route `orderNumber` | `204` |

Recommended Flutterwave card flow:

1. Call `POST /checkout/initiate-payment`.
2. If response has `flutterwaveConfig`, pass it to the Flutterwave inline SDK.
3. On successful popup result, call `POST /checkout/confirm-order` with `pendingOrderId`, `transactionId`, `txRef`, and `status`.
4. Show the returned `orderNumber`.

Bank transfer flow:

1. Call `POST /checkout/initiate-payment` with `paymentMethod: "BankTransfer"`.
2. Display `bankDetails`.
3. The response includes `pendingOrderId`; operational confirmation is manual unless backend logic is expanded.

### Account, Addresses, Payment Methods

| Method | Path | Auth | Request/Query | Response |
| --- | --- | --- | --- | --- |
| GET | `/account/profile` | User | none | `UserProfileDto` |
| PUT | `/account/profile` | User | `UpdateProfileRequest` | `204` |
| PUT | `/account/password` | User | `ChangePasswordRequest` | `204` |
| GET | `/account/loyalty` | User | `page`, `pageSize` | `LoyaltyDto` |
| POST | `/account/notify-me/{productId}` | User | route `productId` | `204` |
| GET | `/account/notifications` | User | none | `NotificationPreferencesDto` |
| PUT | `/account/notifications` | User | `NotificationPreferencesDto` | `204` |
| GET | `/addresses` | User | none | `AddressDto[]` |
| POST | `/addresses` | User | `CreateAddressRequest` | `201` |
| PUT | `/addresses/{id}` | User | `UpdateAddressRequest` | `204` |
| DELETE | `/addresses/{id}` | User | route `id` | `204` |
| GET | `/payment-methods` | User | none | `PaymentMethodDto[]` |
| POST | `/payment-methods` | User | `CreatePaymentMethodRequest` | `201 { id }` |
| PUT | `/payment-methods/{id}` | User | `UpdatePaymentMethodRequest` | `204` |
| DELETE | `/payment-methods/{id}` | User | route `id` | `204` |

Important: `PaymentMethodDto` includes a `cardNumber` field. Treat it as sensitive in UI and logs. Do not store card data in frontend state longer than necessary.

### Wishlist And Favorites

`/favorites` is an alias over the wishlist feature for mobile/frontend naming.

| Method | Path | Auth | Response |
| --- | --- | --- | --- |
| GET | `/wishlist` | User | `ProductSummaryDto[]` |
| POST | `/wishlist/{productId}` | User | `204` |
| DELETE | `/wishlist/{productId}` | User | `204` |
| GET | `/favorites` | User | `ProductSummaryDto[]` |
| GET | `/favorites/check/{productId}` | User | `{ isFavorited, productId }` |
| POST | `/favorites/{productId}` | User | `204` |
| DELETE | `/favorites/{productId}` | User | `204` |

### Reviews

| Method | Path | Auth | Query/Request | Response |
| --- | --- | --- | --- | --- |
| GET | `/reviews/product/{productId}` | Public | `page`, `pageSize`, `rating` | `PagedResult<ReviewDto>` |
| GET | `/products/{productId}/reviews` | Public | `page`, `pageSize`, `rating` | `PagedResult<ReviewDto>` |
| POST | `/reviews` | User | `CreateReviewRequest` | `201 { id }` |

### Homepage Content

| Method | Path | Auth | Response |
| --- | --- | --- | --- |
| GET | `/best-deals` | Public | `ProductSummaryDto[]` |
| GET | `/new-arrivals` | Public | `ProductSummaryDto[]` |
| GET | `/banners` | Public | `BannerDto[]` |
| GET | `/flash-deals` | Public | `FlashDealDto[]` |
| GET | `/featured-sections/middle` | Public | `FeaturedSectionDto[]` |
| GET | `/featured-sections/bottom` | Public | `FeaturedSectionDto[]` |
| GET | `/promotions/hero` | Public | `HeroBannerDto[]` |
| GET | `/promotions/best-deal` | Public | `BestDealCardDto` |
| GET | `/promotions/best-deals` | Public | `BestDealCardDto` |
| GET | `/promotions/promo-banner` | Public | `PromoBannerDto` |
| GET | `/promotions/accessories-promo` | Public | `FeatureCardDto` |
| GET | `/promotions/laptop-promo` | Public | `LaptopPromoDto` |

Some promotional endpoints return `404` when no active content exists. Frontend should hide the corresponding section rather than treating it as a fatal page error.

### Admin

| Method | Path | Query/Request | Response |
| --- | --- | --- | --- |
| GET | `/admin/dashboard` | none | `DashboardStatsDto` |
| GET | `/admin/orders` | `page`, `pageSize`, `status`, `paymentStatus`, `from`, `to` | `PagedResult<OrderDto>` |
| GET | `/admin/users` | `page`, `pageSize`, `search` | `PagedResult<AdminUserDto>` |
| GET | `/admin/customers` | `page`, `pageSize`, `search` | `PagedResult<AdminUserDto>` |
| GET | `/admin/analytics` | none | `AnalyticsDto` |
| GET | `/admin/reviews` | `page`, `pageSize` | `PagedResult<ReviewDto>` |
| GET | `/admin/notifications` | `page`, `pageSize`, `unreadOnly` | `NotificationResponseDto` |
| GET | `/admin/settings` | none | `AdminSettingsDto` |
| GET | `/admin/settings/{section}` | route `section` | section object |
| PUT | `/admin/settings` | `AdminSettingsUpdateRequest` | `AdminSettingsDto` |
| PUT | `/admin/settings/{section}` | `{ value: object }` | `AdminSettingsDto` |
| PATCH | `/admin/settings/{section}` | `{ value: object }` | `AdminSettingsDto` |
| PUT | `/admin/orders/{orderNumber}/status` | `AdminUpdateOrderStatusRequest` | `204` |
| POST | `/admin/users/{userId}/loyalty` | `AdjustLoyaltyRequest` | `204` |
| GET | `/admin/inventory/low-stock` | `threshold` default `5` | `LowStockDto[]` |
| GET | `/admin/banners` | none | `BannerDto[]` |
| POST | `/admin/banners` | `CreateBannerRequest` | `201 { id }` |
| PUT | `/admin/banners/{id}` | `UpdateBannerRequest` | `204` |
| DELETE | `/admin/banners/{id}` | route `id` | `204` |

### Admin Catalog And Marketing

| Method | Path | Request/Query | Response |
| --- | --- | --- | --- |
| GET | `/coupons` | none | `CouponDto[]` |
| GET | `/coupons/validate` | `code`, `orderSubtotal`, optional `userId` | `CouponValidationDto` |
| POST | `/coupons` | `CreateCouponRequest` | `201 { id }` |
| PUT | `/coupons/{id}` | `UpdateCouponRequest` | `204` |
| DELETE | `/coupons/{id}` | route `id:int` | `204` |
| POST | `/flash-deals` | `CreateFlashDealRequest` | `201 { id }` |
| PUT | `/flash-deals/{id}` | `UpdateFlashDealRequest` | `204` |
| PATCH | `/flash-deals/{id}/toggle` | query `isActive` | `204` |
| GET | `/featured-sections` | none | `FeaturedSectionDto[]` |
| POST | `/featured-sections` | `CreateFeaturedSectionRequest` | `201 { id }` |
| PUT | `/featured-sections/{id}` | `UpdateFeaturedSectionRequest` | `204` |
| DELETE | `/featured-sections/{id}` | route `id` | `204` |
| POST | `/promotions/admin/hero` | `SetHeroBannerRequest` | `200 AdminHeroBannerDto` |
| GET | `/promotions/admin/hero` | none | `AdminHeroBannerDto[]` |
| POST | `/promotions/admin/best-deal` | `SetBestDealRequest` | `200 AdminBestDealDto` |
| POST | `/promotions/admin/promo-banner` | `SetPromoBannerRequest` | `201 { id }` |
| POST | `/promotions/admin/accessories-promo` | `SetAccessoriesPromoRequest` | `201 { id }` |
| POST | `/promotions/admin/laptop-promo` | `SetLaptopPromoRequest` | `201 { id }` |
| GET | `/promotions/admin/all` | none | `PromotionalSectionAdminDto[]` |
| DELETE | `/promotions/admin/{id}` | route `id` | `204` |
| PUT | `/promotions/admin/hero/{slugId}` | `UpdateHeroBannerRequest` | `200 AdminHeroBannerDto` |
| DELETE | `/promotions/admin/hero/{slugId}` | route `slugId` | `204` |
| PUT | `/promotions/admin/best-deal/{id}` | `UpdateBestDealRequest` | `204` |
| PUT | `/promotions/admin/promo-banner/{id}` | `UpdatePromoBannerRequest` | `204` |
| PUT | `/promotions/admin/accessories-promo/{id}` | `UpdateAccessoriesPromoRequest` | `204` |
| PUT | `/promotions/admin/laptop-promo/{id}` | `UpdateLaptopPromoRequest` | `204` |

`GET /coupons/validate` is public, but `GET /coupons` and coupon mutations are admin-only.

### Chatbot And Payments

| Method | Path | Auth | Request | Response |
| --- | --- | --- | --- | --- |
| POST | `/chatbot/message` | Public | `ChatMessageRequest` | `ChatMessageResponse` |
| POST | `/payments/webhook/flutterwave` | Public/provider | raw webhook JSON | `200` |

The only payment webhook route currently present in `PaymentsController` is Flutterwave.

## DTO Reference

Only fields relevant to frontend integration are listed. All property names are sent/received as camelCase unless a DTO explicitly uses `JsonPropertyName`.

### Auth DTOs

```ts
type UserDto = {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  phone?: string | null;
  avatarUrl?: string | null;
  role: "Customer" | "Admin" | "SuperAdmin";
  isVerified: boolean;
  loyaltyPoints: number;
};

type AuthResponse = {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  user: UserDto;
};

type RegisterRequest = {
  firstName: string;
  lastName: string;
  email: string;
  phone?: string | null;
  password: string;
  confirmPassword: string;
};

type LoginRequest = { email: string; password: string };
type GoogleSignInRequest = { idToken: string };
type RefreshTokenRequest = { accessToken: string; refreshToken: string };
type ForgotPasswordRequest = { email: string };
type VerifyOtpRequest = { email: string; otp: string };

type ResetPasswordRequest = {
  email: string;
  otp: string;
  newPassword: string;
  confirmNewPassword: string;
};
```

### Product DTOs

```ts
type BrandDto = {
  id: string;
  name: string;
  slug: string;
  logoUrl?: string | null;
};

type CategoryDto = {
  id: number;
  parentId?: number | null;
  name: string;
  slug: string;
  imageUrl?: string | null;
};

type CategoryDetailDto = CategoryDto & {
  sortOrder: number;
  isActive: boolean;
  metaTitle?: string | null;
  metaDescription?: string | null;
  children: CategoryDto[];
  createdAt: string;
};

type ProductImageDto = {
  id: string;
  thumbUrl: string;
  displayUrl: string;
  zoomUrl: string;
  originalUrl: string;
  sortOrder: number;
  isVideo: boolean;
};

type ProductSummaryDto = {
  id: string;
  name: string;
  slug: string;
  price: number;
  compareAtPrice?: number | null;
  primaryImageUrl?: string | null;
  averageRating: number;
  reviewCount: number;
  isActive: boolean;
  isFeatured: boolean;
  availableQty: number;
  brand?: BrandDto | null;
  category?: CategoryDto | null;
  imageUrls: string[];
};

type ProductDetailDto = ProductSummaryDto & {
  description?: string | null;
  shortDescription?: string | null;
  images: ProductImageDto[];
  variants: ProductVariantDto[];
  attributesJson?: string | null;
  tagsJson?: string | null;
  weightKg?: number | null;
  metaTitle?: string | null;
  metaDescription?: string | null;
  soldCount: number;
  sku: string;
};

type ProductVariantDto = {
  id: string;
  sku: string;
  attributes: { name: string; value: string }[];
  price: number;
  stockQty: number;
  availableQty: number;
};
```

Admin product/category requests:

```ts
type CreateProductRequest = {
  sku: string;
  name: string;
  slug?: string | null;
  brandId: string;
  categoryId: number;
  description?: string | null;
  shortDescription?: string | null;
  price: number;
  compareAtPrice?: number | null;
  costPrice?: number | null;
  stockQty: number;
  weightKg?: number | null;
  attributesJson?: string | null;
  tagsJson?: string | null;
  tag?: string | null;
  isActive: boolean;
  isFeatured: boolean;
  imageUrls: string[];
};

type UpdateProductRequest = Partial<Omit<CreateProductRequest, "sku" | "imageUrls">> & {
  metaTitle?: string | null;
  metaDescription?: string | null;
};

type CreateCategoryRequest = {
  name: string;
  slug?: string | null;
  parentId?: number | null;
  imageUrl?: string | null;
  sortOrder: number;
  isActive: boolean;
  metaTitle?: string | null;
  metaDescription?: string | null;
};

type UpdateCategoryRequest = Partial<CreateCategoryRequest>;
```

### Cart DTOs

```ts
type CartItemDto = {
  id: string;
  productId: string;
  productName: string;
  productSlug: string;
  productImageUrl?: string | null;
  variantId?: string | null;
  variantAttributesJson?: string | null;
  unitPrice: number;
  quantity: number;
  lineTotal: number;
};

type CartDto = {
  id: string;
  userId?: string | null;
  sessionId?: string | null;
  expiresAt: string;
  couponCode?: string | null;
  items: CartItemDto[];
  totalItems: number;
  subtotal: number;
};

type AddToCartRequest = {
  productId: string;
  variantId?: string | null;
  quantity: number;
};

type UpdateCartItemRequest = { quantity: number };
```

### Checkout And Order DTOs

```ts
type InitiatePaymentRequest = {
  items: { productId: string; variantId?: string | null; quantity: number }[];
  addressId?: string | null;
  inlineAddress?: InlineAddressRequest | null;
  deliveryMethod: "Standard" | "Express" | "Pickup";
  paymentMethod: "Card" | "BankTransfer";
  pricing?: CreateOrderPricingRequest | null;
  couponCode?: string | null;
  guestEmail?: string | null;
  guestName?: string | null;
  guestPhone?: string | null;
};

type InitiatePaymentResponse = {
  pendingOrderId: string;
  paymentMethod: string;
  bankDetails?: {
    bankName: string;
    accountNumber: string;
    accountName: string;
    instructions: string;
  } | null;
  flutterwaveConfig?: {
    public_key: string;
    tx_ref: string;
    amount: number;
    currency: "NGN" | string;
    customer: { email: string; name: string; phone_number: string };
    meta: { pendingOrderId: string };
    customizations: { title: string; description: string; logo: string };
  } | null;
};

type ConfirmOrderRequest = {
  pendingOrderId: string;
  transactionId: string;
  txRef: string;
  status: string;
};

type ConfirmOrderResponse = { orderNumber: string };

type CreateOrderRequest = {
  items: CreateOrderItemRequest[];
  addressId?: string | null;
  shippingAddressId?: string | null;
  inlineAddress?: InlineAddressRequest | null;
  deliveryMethod: "Standard" | "Express" | "Pickup";
  delivery?: { method: "standard" | "express" | "pickup"; fee: number } | null;
  paymentMethod: "Card" | "BankTransfer" | "USSD" | "PayOnDelivery";
  payment?: { method: "card" | "bank_transfer" | "pay_on_delivery"; savedCardId?: string | null } | null;
  pricing?: CreateOrderPricingRequest | null;
  couponCode?: string | null;
  coupon?: { code?: string | null; discountAmount: number } | null;
  notes?: string | null;
  guestEmail?: string | null;
  guestSessionId?: string | null;
};

type CreateOrderItemRequest = {
  productId: string;
  name?: string | null;
  quantity: number;
  price: number;
  image?: string | null;
};

type CreateOrderPricingRequest = {
  subtotal: number;
  discountAmount: number;
  deliveryFee: number;
  tax: number;
  total: number;
};

type InlineAddressRequest = {
  label: string;
  line1: string;
  line2?: string | null;
  city: string;
  state: string;
  postalCode?: string | null;
};

type CreateOrderResponse = {
  orderId: string;
  orderNumber: string;
  paymentUrl?: string | null;
  paymentReference?: string | null;
  bankDetails?: BankDetailsDto | null;
  total: number;
};

type OrderDto = {
  id: string;
  orderNumber: string;
  userId?: string | null;
  status: OrderStatus;
  paymentStatus: PaymentStatus;
  paymentMethod?: PaymentMethod | null;
  subtotal: number;
  discountAmount: number;
  deliveryFee: number;
  vatAmount: number;
  total: number;
  deliveryAddress?: DeliveryAddressSnapshot | null;
  deliveryMethod: DeliveryMethod;
  estimatedDelivery?: string | null;
  trackingNumber?: string | null;
  notes?: string | null;
  items: OrderItemDto[];
  createdAt: string;
};
```

### Account DTOs

```ts
type UserProfileDto = UserDto & { createdAt: string };

type LoyaltyDto = {
  balance: number;
  transactions: PagedResult<LoyaltyTransactionDto>;
};

type LoyaltyTransactionDto = {
  id: string;
  eventType: LoyaltyEventType;
  points: number;
  description?: string | null;
  createdAt: string;
  expiresAt: string;
  remainingPoints: number;
};

type AddressDto = {
  id: string;
  label: string;
  line1: string;
  line2?: string | null;
  city: string;
  state: string;
  postalCode?: string | null;
  isDefault: boolean;
};

type UpdateProfileRequest = {
  firstName?: string | null;
  lastName?: string | null;
  phone?: string | null;
  avatarUrl?: string | null;
};

type ChangePasswordRequest = {
  currentPassword: string;
  newPassword: string;
  confirmNewPassword: string;
};

type CreateAddressRequest = Omit<AddressDto, "id">;
type UpdateAddressRequest = Partial<Omit<AddressDto, "id">>;

type NotificationPreferencesDto = {
  orderUpdates: boolean;
  promotions: boolean;
  backInStock: boolean;
  wishlistReminders: boolean;
  reviewReminders: boolean;
};
```

### Payment Method DTOs

```ts
type PaymentMethodDto = {
  id: string;
  cardType: string;
  cardNumber: string;
  cardHolderName: string;
  expiryMonth: number;
  expiryYear: number;
  isDefault: boolean;
  createdAt: string;
};

type CreatePaymentMethodRequest = {
  cardType: string;
  cardNumber: string;
  cardHolderName: string;
  expiryMonth: number;
  expiryYear: number;
  isDefault: boolean;
};

type UpdatePaymentMethodRequest = {
  isDefault?: boolean | null;
  cardHolderName?: string | null;
  expiryMonth?: number | null;
  expiryYear?: number | null;
};
```

### Search DTOs

```ts
type ProductSearchDocument = {
  id: string;
  name: string;
  slug: string;
  brand?: string | null;
  category?: string | null;
  description?: string | null;
  tags?: string | null;
  price: number;
  compareAtPrice?: number | null;
  averageRating: number;
  stockQty: number;
  soldCount: number;
  isActive: boolean;
  thumbUrl?: string | null;
  createdAt: string;
};

type SearchResult = {
  items: ProductSearchDocument[];
  total: number;
  page: number;
  pageSize: number;
};

type InstantSearchResult = {
  products: { id: string; name: string; slug: string; thumbUrl?: string | null; price: number }[];
  categories: string[];
};
```

### Marketing DTOs

```ts
type BannerDto = {
  id: string;
  title: string;
  imageUrl: string;
  linkUrl?: string | null;
  subTitle?: string | null;
  ctaText?: string | null;
  tag?: string | null;
  sortOrder: number;
};

type FlashDealDto = {
  id: string;
  productId: string;
  productName: string;
  productSlug: string;
  productImageUrl?: string | null;
  salePrice: number;
  originalPrice: number;
  discountPercent: number;
  startsAt: string;
  endsAt: string;
  maxQuantity?: number | null;
  soldQuantity: number;
  remainingQuantity?: number | null;
  isLive: boolean;
  timeRemaining: string;
};

type FeaturedSectionDto = {
  id: string;
  section: "middle" | "bottom" | string;
  productId: string;
  title: string;
  description: string;
  slug: string;
  imageUrl?: string | null;
  price?: string | null;
  badge?: string | null;
  tag?: string | null;
  ctaText?: string | null;
};

type HeroBannerDto = {
  id: string;
  tag?: string | null;
  badge?: string | null;
  title?: string | null;
  price?: string | null;
  ctaText?: string | null;
  imageUrl?: string | null;
  sortOrder: number;
  slug?: string | null;
};

type BestDealCardDto = {
  id: string;
  imageUrl?: string | null;
  name?: string | null;
  rating?: number | null;
  originalPrice?: string | null;
  salePrice?: string | null;
  description?: string | null;
  badge?: string | null;
  slug?: string | null;
};

type PromoBannerDto = {
  id: string;
  title?: string | null;
  subtitle?: string | null;
  ctaText?: string | null;
  imageUrl?: string | null;
  imageAlt?: string | null;
  badge?: string | null;
};

type FeatureCardDto = {
  id: string;
  type: string;
  title?: string | null;
  subtitle?: string | null;
  buttonText?: string | null;
  imageUrl?: string | null;
  priceLabel?: string | null;
  priceValue?: string | null;
  slug?: string | null;
};

type LaptopPromoDto = {
  id: string;
  title?: string | null;
  subtitle?: string | null;
  ctaText?: string | null;
  imageUrl?: string | null;
  imageAlt?: string | null;
  priceBadge?: string | null;
  priceValue?: string | null;
  slug?: string | null;
};
```

### Coupon, Review, Notification DTOs

```ts
type CouponValidationDto = {
  isValid: boolean;
  code?: string | null;
  type?: "Percentage" | "Fixed" | null;
  value?: number | null;
  discountAmount?: number | null;
  message?: string | null;
};

type CouponDto = {
  id: number;
  code: string;
  type: "Percentage" | "Fixed";
  value: number;
  minimumOrderAmount?: number | null;
  maxUses?: number | null;
  usedCount: number;
  maxUsesPerUser?: number | null;
  isStackable: boolean;
  expiresAt?: string | null;
  isActive: boolean;
  restrictToProductId?: string | null;
  restrictToCategoryId?: number | null;
  createdAt: string;
};

type ReviewDto = {
  id: string;
  userId: string;
  reviewerName: string;
  rating: number;
  title?: string | null;
  body?: string | null;
  isVerifiedPurchase: boolean;
  createdAt: string;
};

type CreateReviewRequest = {
  productId: string;
  rating: number;
  title?: string | null;
  body?: string | null;
};

type NotificationDto = {
  id: string;
  type: string;
  title: string;
  message: string;
  linkUrl: string;
  isRead: boolean;
  createdAt: string;
};

type NotificationResponseDto = {
  items: NotificationDto[];
  unreadCount: number;
};

type ChatMessageRequest = {
  message: string;
  conversationId?: string | null;
};

type ChatMessageResponse = {
  reply: string;
  conversationId: string;
  quickReplies?: string[] | null;
};
```

## Frontend Build Guidance

- Prefer `/products?categoryId=...&brandId=...` over slug filters; this backend does not accept `category` or `brand` slug query params on product listing.
- Use `/categories/{slug}` first if a category page route only has the slug and you need the numeric `categoryId`.
- For product cards, use `primaryImageUrl` first, then fallback to the first `imageUrls` item.
- `attributesJson`, `tagsJson`, and `variantAttributesJson` are JSON strings. Parse carefully with fallback to `null` on invalid JSON.
- Cart totals currently include `subtotal` only. Checkout/order endpoints return delivery, VAT, discounts, and total.
- `GET /orders/{orderNumber}` is public but accepts JWT when available; pass auth for private order pages.
- Promotional homepage endpoints may return `404` when no content exists. Hide missing sections.
- Admin settings section names are lower-case logical names such as `store`, `payment`, `shipping`, `tax`, `notifications`, `seo`, `security`, `maintenance`.
- The backend CORS config allows local frontend origins including `localhost:3000`, `localhost:3001`, and `localhost:8080`, plus configured production domains.

## Open Questions / Backend Caveats

- The old `API_HANDOVER.txt` mentions Paystack webhook routes, but the current `PaymentsController` only exposes `POST /api/v1/payments/webhook/flutterwave`.
- Some DTOs are declared inside feature query files, not only under `Dtos/`, so generated Swagger is the safest source of truth after backend changes.
- `PaymentMethodDto` appears to store/return `cardNumber`; this should be reviewed before production frontend work.
- `appsettings.json` currently contains values that look like real credentials. Treat them as backend-only secrets and rotate them if they have been shared.
