# ShopFresherz AI Agent Backend Build Specification

Version: 1.0  
Prepared for: AI Backend Build Agent  
Project: ShopFresherz Gadget Store  
Backend Target: ASP.NET Core 8 Clean Architecture API or equivalent production-grade backend  
Frontend Source: Figma export screens and existing backend handover  
Important: Do not hardcode secrets, admin passwords, payment keys, or production database credentials Use environment variables only.

---

## 1. Product Summary

ShopFresherz is a Nigerian gadget ecommerce platform selling phones, laptops, accessories, gaming consoles, electronics, smart watches, and related tech products. The frontend design includes a public ecommerce storefront, customer authentication, cart and checkout, order tracking, customer dashboard, wishlist, reviews, loyalty points, referrals, saved addresses, saved cards, notifications, chat widget, and admin dashboard.

The backend must support both guest and authenticated shopping flows, Paystack and Flutterwave payment flows, product browsing, search, coupons, flash deals, loyalty, referral, reviews, order management, and admin operations.

---

## 2. Design System From Figma

### 2.1 Brand

- Brand name: ShopFresherz
- Store type: Gadget Store
- Logo: SF mark with cart icon and ShopFresherz text
- Main visual style: clean ecommerce layout with black, orange, white, and neutral grays

### 2.2 Color Tokens

Use semantic tokens, not hardcoded random values.

```json
{
  "colors": {
    "brandPrimary": "#FF8A00",
    "brandPrimaryDark": "#E67800",
    "brandBlack": "#111111",
    "brandDark": "#1A1A1A",
    "background": "#FFFFFF",
    "surface": "#F8F8F8",
    "border": "#E5E5E5",
    "textPrimary": "#111111",
    "textSecondary": "#666666",
    "textMuted": "#999999",
    "success": "#16A34A",
    "warning": "#F59E0B",
    "danger": "#DC2626",
    "info": "#2563EB"
  }
}
```

### 2.3 Typography

The Figma includes a type-scale screen. Backend should not enforce typography, but API fields must support UI copy such as titles, subtitles, descriptions, CTA labels, banner copy, product descriptions, and notification messages.

---

## 3. Screen Inventory

The Figma export contains these screens:

1. Logo and design reference screens
2. Color Scheme
3. Type Scale
4. Home Page
5. Home Page Popups
6. Shop Page
7. Product Detail
8. Wishlist
9. Shopping Cart
10. Checkout Success
11. Track Order
12. Track Order Details
13. Sign In
14. Forgot Password
15. Reset Password
16. Sign Up
17. Customer Dashboard
18. Dashboard Order History
19. Dashboard Order Details
20. Billing Address modal
21. Add New Address modal
22. Add New Card modal
23. Dashboard Track Order
24. Dashboard Track Order Details
25. Dashboard Addresses
26. Dashboard Card & Create
27. Dashboard Profile Settings
28. Dashboard Notification
29. Dashboard Loyalty Point
30. Dashboard Loyalty Point detail/transactions
31. Dashboard Referral
32. My Reviews
33. My Reviews detail/empty state
34. Write A Review modal
35. Email Verification
36. 404 Error Page
37. Checkout step screens
38. Place Order
39. Checkout for Registered Users
40. Chatbox
41. Admin Dashboard
42. Product label/group asset

---

## 4. Core User Roles

```json
{
  "roles": [
    "Guest",
    "Customer",
    "Admin",
    "SuperAdmin"
  ]
}
```

### 4.1 Guest

Can browse, search, view products, add to cart with session ID, apply coupon, checkout as guest, track order by order number/email/phone.

### 4.2 Customer

Can do everything a guest can do, plus wishlist, saved addresses, saved cards where supported, order history, profile settings, loyalty points, referral dashboard, reviews, notifications.

### 4.3 Admin/SuperAdmin

Can manage products, categories, brands, banners, coupons, flash deals, orders, users, inventory, reviews, and dashboard metrics.

---

## 5. Global API Standards

### 5.1 Base URL

```text
/api/v1
```

### 5.2 Response Envelope

Use this standard response shape for all endpoints except webhooks and file streams.

```json
{
  "success": true,
  "message": "Request completed successfully",
  "data": {},
  "errors": [],
  "meta": {
    "requestId": "req_01HY...",
    "timestamp": "2026-05-14T07:00:00Z"
  }
}
```

### 5.3 Error Envelope

```json
{
  "success": false,
  "message": "Validation failed",
  "code": "SF-0001",
  "errors": [
    {
      "field": "email",
      "message": "Email is required"
    }
  ],
  "meta": {
    "requestId": "req_01HY...",
    "timestamp": "2026-05-14T07:00:00Z"
  }
}
```

### 5.4 Pagination Shape

```json
{
  "items": [],
  "page": 1,
  "pageSize": 20,
  "totalItems": 250,
  "totalPages": 13,
  "hasNextPage": true,
  "hasPreviousPage": false
}
```

### 5.5 Authentication

- Use JWT access tokens.
- Access token expiry: 15 minutes.
- Refresh token expiry: 7 days.
- Refresh tokens must be hashed before storage.
- Use Authorization header for authenticated requests.

```http
Authorization: Bearer {accessToken}
```

### 5.6 Guest Cart Session

For guest users, frontend sends:

```http
X-Session-Id: {uuid}
```

Backend must merge guest cart into user cart after login.

---

## 6. Global Data Models

### 6.1 Money

All prices should be stored in NGN minor units or decimal with explicit currency. Recommended response format:

```json
{
  "amount": 1200000,
  "currency": "NGN",
  "formatted": "₦1,200,000"
}
```

### 6.2 Image Asset

```json
{
  "id": "uuid",
  "altText": "Apple iPhone 16 Pro Max",
  "thumbUrl": "https://cdn.../thumb.jpg",
  "displayUrl": "https://cdn.../display.jpg",
  "zoomUrl": "https://cdn.../zoom.jpg",
  "originalUrl": "https://cdn.../original.jpg",
  "sortOrder": 1,
  "isPrimary": true
}
```

### 6.3 Product Summary

```json
{
  "id": "uuid",
  "sku": "IP16PM-256-BLK",
  "name": "Apple iPhone 16 Pro Max 256GB",
  "slug": "apple-iphone-16-pro-max-256gb",
  "brand": {
    "id": "uuid",
    "name": "Apple",
    "slug": "apple"
  },
  "category": {
    "id": "uuid",
    "name": "Mobile Phones",
    "slug": "mobile-phones"
  },
  "price": {
    "amount": 2800000,
    "currency": "NGN",
    "formatted": "₦2,800,000"
  },
  "compareAtPrice": {
    "amount": 3000000,
    "currency": "NGN",
    "formatted": "₦3,000,000"
  },
  "discountPercent": 7,
  "ratingAverage": 4.8,
  "reviewCount": 128,
  "stockStatus": "InStock",
  "stockQuantity": 25,
  "image": {
    "thumbUrl": "https://cdn.../thumb.jpg",
    "displayUrl": "https://cdn.../display.jpg"
  },
  "isFeatured": true,
  "isNewArrival": false,
  "isFlashDeal": true,
  "isWishlisted": false
}
```

### 6.4 Product Detail

```json
{
  "id": "uuid",
  "sku": "IP16PM-256-BLK",
  "name": "Apple iPhone 16 Pro Max 256GB",
  "slug": "apple-iphone-16-pro-max-256gb",
  "shortDescription": "Premium iPhone with advanced camera and A-series chip.",
  "description": "Long HTML-safe product description.",
  "brand": {},
  "category": {},
  "price": {},
  "compareAtPrice": {},
  "discountPercent": 7,
  "stockStatus": "InStock",
  "stockQuantity": 25,
  "lowStockThreshold": 5,
  "weightKg": 0.25,
  "warranty": "12 months manufacturer warranty",
  "keyFeatures": [
    "256GB storage",
    "6.9-inch display",
    "Advanced camera system"
  ],
  "whatIsIncluded": [
    "Phone",
    "USB-C cable",
    "Documentation"
  ],
  "whatIsNotIncluded": [
    "Power adapter",
    "Protective case"
  ],
  "specifications": [
    { "group": "Display", "name": "Screen Size", "value": "6.9 inches", "unit": null, "sortOrder": 1 },
    { "group": "Memory", "name": "Storage", "value": "256", "unit": "GB", "sortOrder": 2 }
  ],
  "dimensions": {
    "lengthCm": 16.3,
    "widthCm": 7.7,
    "heightCm": 0.85,
    "weightKg": 0.25
  },
  "images": [],
  "variants": [],
  "reviewsSummary": {
    "ratingAverage": 4.8,
    "reviewCount": 128,
    "ratingBreakdown": {
      "5": 80,
      "4": 30,
      "3": 10,
      "2": 5,
      "1": 3
    }
  },
  "relatedProducts": [],
  "recentlyViewedProducts": []
}
```

---

## 7. Screen-by-Screen Backend Requirements

## 7.1 Home Page

### Purpose

Public landing page showing top navigation, search, category menu, banners, featured products, deals, new arrivals, product sections, service trust cards, footer links, newsletter, and popup overlays.

### Required Data

- Header categories
- Brands
- Hero banners
- Featured products
- Flash deals
- New arrivals
- Best sellers
- Recommended products
- Recently viewed products if session exists
- Newsletter status
- Footer CMS links

### Endpoint

```http
GET /api/v1/home
```

### Response Payload

```json
{
  "banners": [
    {
      "id": "uuid",
      "title": "Freshest Tech in Nigeria",
      "subTitle": "Shop latest phones, laptops and accessories",
      "imageUrl": "https://cdn.../banner.jpg",
      "linkUrl": "/category/new-arrivals",
      "ctaText": "Shop Now",
      "sortOrder": 1
    }
  ],
  "categories": [],
  "brands": [],
  "featuredProducts": [],
  "flashDeals": [],
  "newArrivals": [],
  "bestSellers": [],
  "recommendedProducts": [],
  "trustBlocks": [
    {
      "title": "Fast Delivery",
      "description": "Quick delivery across Nigeria",
      "icon": "truck"
    }
  ],
  "footer": {
    "supportLinks": [],
    "companyLinks": [],
    "socialLinks": []
  }
}
```

### Supporting Endpoints

```http
GET /api/v1/categories
GET /api/v1/brands
GET /api/v1/banners
GET /api/v1/products?featured=true
GET /api/v1/flash-deals
GET /api/v1/products?sort=newest
GET /api/v1/search/instant?q={query}
POST /api/v1/newsletter/subscribe
```

### Backend Notes

- Cache homepage response for 30 to 120 seconds.
- Products must include image, price, rating, stock status, discount, and wishlist state when authenticated.
- Instant search should be rate limited.

---

## 7.2 Home Page Popups

### Purpose

Popups appear for cart preview, mini cart, product quick view, newsletter, or promo offers.

### Required APIs

```http
GET /api/v1/cart
POST /api/v1/cart/items
GET /api/v1/products/{slug}
POST /api/v1/newsletter/subscribe
```

### Cart Preview Response

```json
{
  "id": "uuid",
  "items": [
    {
      "id": "uuid",
      "productId": "uuid",
      "name": "Sony PlayStation 5 Slim",
      "slug": "sony-playstation-5-slim",
      "imageUrl": "https://cdn.../ps5.jpg",
      "unitPrice": { "amount": 850000, "currency": "NGN", "formatted": "₦850,000" },
      "quantity": 1,
      "lineTotal": { "amount": 850000, "currency": "NGN", "formatted": "₦850,000" },
      "stockStatus": "InStock"
    }
  ],
  "itemCount": 1,
  "subtotal": {},
  "deliveryFee": {},
  "discount": {},
  "vat": {},
  "total": {}
}
```

---

## 7.3 Shop Page

### Purpose

Product listing page with filters, categories, sorting, pagination, product cards, and search results.

### Query Parameters

```http
GET /api/v1/products?page=1&pageSize=24&brand=apple&category=mobile-phones&minPrice=50000&maxPrice=3000000&sort=newest&featured=false&inStock=true
```

### Sort Values

```json
[
  "newest",
  "price_asc",
  "price_desc",
  "rating_desc",
  "popular",
  "discount_desc"
]
```

### Filter Payload

```http
GET /api/v1/products/filters?category=mobile-phones
```

```json
{
  "categories": [],
  "brands": [],
  "priceRange": {
    "min": 0,
    "max": 5000000
  },
  "ratingOptions": [5, 4, 3, 2, 1],
  "stockOptions": ["InStock", "OutOfStock"],
  "dynamicAttributes": [
    {
      "key": "storage",
      "label": "Storage",
      "type": "multiSelect",
      "values": ["64GB", "128GB", "256GB", "512GB", "1TB"]
    },
    {
      "key": "ram",
      "label": "RAM",
      "type": "multiSelect",
      "values": ["4GB", "8GB", "16GB", "32GB"]
    }
  ]
}
```

### Product Listing Response

```json
{
  "items": [],
  "page": 1,
  "pageSize": 24,
  "totalItems": 120,
  "totalPages": 5,
  "facets": {
    "brands": [],
    "categories": [],
    "priceRange": {},
    "attributes": []
  }
}
```

### Backend Notes

- Support both category slug and brand slug filters.
- Return filters/facets for frontend sidebar.
- Product card must include wishlist state if user is logged in.

---

## 7.4 Product Detail Page

### Purpose

Product detail view with gallery, title, price, stock, quantity selector, add to cart, wishlist, specifications, description, reviews, related products, and recently viewed.

### Endpoint

```http
GET /api/v1/products/{slug}
```

### Response

Use Product Detail model in section 6.4.

### Actions

```http
POST /api/v1/cart/items
POST /api/v1/wishlist/{productId}
DELETE /api/v1/wishlist/{productId}
GET /api/v1/reviews/product/{productId}
POST /api/v1/account/notify-me/{productId}
POST /api/v1/products/{productId}/recently-viewed
```

### Add to Cart Request

```json
{
  "productId": "uuid",
  "variantId": "uuid-or-null",
  "quantity": 1
}
```

### Add to Cart Response

```json
{
  "cart": {},
  "message": "Item added to cart"
}
```

---

## 7.5 Wishlist

### Purpose

Customer wishlist table/list showing saved products with product name, price, stock status, and add-to-cart/remove actions.

### Endpoints

```http
GET /api/v1/wishlist
POST /api/v1/wishlist/{productId}
DELETE /api/v1/wishlist/{productId}
POST /api/v1/cart/items
```

### Response

```json
{
  "items": [
    {
      "id": "uuid",
      "product": {},
      "addedAt": "2026-05-14T07:00:00Z"
    }
  ]
}
```

---

## 7.6 Shopping Cart

### Purpose

Cart page showing selected items, coupon box, subtotal, delivery fee, VAT, discount, and checkout CTA.

### Endpoints

```http
GET /api/v1/cart
POST /api/v1/cart/items
PUT /api/v1/cart/items/{itemId}
DELETE /api/v1/cart/items/{itemId}
POST /api/v1/cart/coupon?couponCode=WELCOME10
DELETE /api/v1/cart/coupon
```

### Cart Response

```json
{
  "id": "uuid",
  "sessionId": "uuid-for-guest",
  "userId": "uuid-or-null",
  "items": [
    {
      "id": "uuid",
      "productId": "uuid",
      "variantId": null,
      "sku": "IP16PM-256-BLK",
      "name": "Apple iPhone 16 Pro Max 256GB",
      "slug": "apple-iphone-16-pro-max-256gb",
      "imageUrl": "https://cdn.../iphone.jpg",
      "unitPrice": {},
      "quantity": 1,
      "lineSubtotal": {},
      "stockStatus": "InStock",
      "maxQuantityAllowed": 5
    }
  ],
  "itemCount": 1,
  "subtotal": {},
  "coupon": {
    "code": "WELCOME10",
    "type": "Percentage",
    "value": 10,
    "discountAmount": {}
  },
  "deliveryFee": {},
  "vat": {},
  "total": {},
  "expiresAt": null
}
```

### Validation Rules

- Quantity must be greater than 0.
- Quantity must not exceed available stock.
- Coupon must not be expired, maxed out, or below minimum order.
- Cart must not be empty before checkout.

---

## 7.7 Checkout Screens

### Purpose

Multiple checkout states for guest and registered users. Screens show billing/shipping details, payment method, cart summary, coupon, terms agreement, place order, and final payment redirect.

### Checkout Flow

1. Get current cart.
2. Collect or select address.
3. Select delivery method if applicable.
4. Select payment method.
5. Validate coupon.
6. Place order.
7. Redirect to payment provider or show success for Pay on Delivery.

### Endpoints

```http
GET /api/v1/checkout/summary
POST /api/v1/orders
GET /api/v1/addresses
POST /api/v1/addresses
GET /api/v1/delivery/options?state=Lagos&city=Ikeja
GET /api/v1/coupons/validate?code=WELCOME10&cartTotal=100000
```

### Checkout Summary Response

```json
{
  "cart": {},
  "addresses": [],
  "deliveryOptions": [
    {
      "id": "standard-lagos",
      "name": "Standard Delivery",
      "description": "2 to 4 business days",
      "fee": { "amount": 3000, "currency": "NGN", "formatted": "₦3,000" },
      "estimatedDeliveryDate": "2026-05-18"
    }
  ],
  "paymentMethods": [
    { "key": "Paystack", "label": "Card / Bank / USSD", "enabled": true },
    { "key": "Flutterwave", "label": "Flutterwave", "enabled": true },
    { "key": "PayOnDelivery", "label": "Pay on Delivery", "enabled": true }
  ],
  "totals": {
    "subtotal": {},
    "discount": {},
    "deliveryFee": {},
    "vat": {},
    "total": {}
  }
}
```

### Create Order Request

```json
{
  "cartId": "uuid-or-null",
  "items": [
    {
      "productId": "uuid",
      "variantId": null,
      "quantity": 1
    }
  ],
  "customer": {
    "firstName": "John",
    "lastName": "Doe",
    "email": "john@example.com",
    "phone": "+2348012345678"
  },
  "shippingAddress": {
    "fullName": "John Doe",
    "phone": "+2348012345678",
    "line1": "12 Example Street",
    "line2": "Apt 2",
    "city": "Ikeja",
    "state": "Lagos",
    "country": "Nigeria",
    "postalCode": "100001"
  },
  "billingAddressSameAsShipping": true,
  "billingAddress": null,
  "deliveryOptionId": "standard-lagos",
  "paymentMethod": "Paystack",
  "couponCode": "WELCOME10",
  "notes": "Call before delivery",
  "acceptedTerms": true
}
```

### Create Order Response

```json
{
  "orderId": "uuid",
  "orderNumber": "SFZ-2026-00001",
  "status": "PendingPayment",
  "paymentStatus": "Pending",
  "paymentMethod": "Paystack",
  "paymentUrl": "https://checkout.paystack.com/...",
  "total": { "amount": 1200000, "currency": "NGN", "formatted": "₦1,200,000" },
  "expiresAt": "2026-05-14T07:30:00Z"
}
```

---

## 7.8 Checkout Success

### Purpose

Order success screen after successful order creation or confirmed payment.

### Endpoint

```http
GET /api/v1/orders/{orderNumber}
```

### Response

```json
{
  "orderNumber": "SFZ-2026-00001",
  "status": "Confirmed",
  "paymentStatus": "Paid",
  "placedAt": "2026-05-14T07:00:00Z",
  "customer": {},
  "items": [],
  "totals": {},
  "tracking": {
    "currentStep": "Confirmed",
    "events": []
  }
}
```

---

## 7.9 Track Order and Track Order Details

### Purpose

Allows users or guests to track an order using order number and contact identifier.

### Endpoint

```http
POST /api/v1/orders/track
```

### Request

```json
{
  "orderNumber": "SFZ-2026-00001",
  "emailOrPhone": "john@example.com"
}
```

### Response

```json
{
  "orderNumber": "SFZ-2026-00001",
  "status": "Shipped",
  "paymentStatus": "Paid",
  "estimatedDeliveryDate": "2026-05-18",
  "trackingNumber": "SFZTRK123456",
  "courier": "Internal Delivery",
  "events": [
    {
      "status": "Pending",
      "title": "Order placed",
      "description": "Your order was received",
      "occurredAt": "2026-05-14T07:00:00Z",
      "isCompleted": true
    },
    {
      "status": "Confirmed",
      "title": "Order confirmed",
      "description": "Payment confirmed and order approved",
      "occurredAt": "2026-05-14T07:05:00Z",
      "isCompleted": true
    }
  ],
  "items": [],
  "shippingAddress": {},
  "totals": {}
}
```

### Order Status Enum

```json
[
  "PendingPayment",
  "Pending",
  "Confirmed",
  "Processing",
  "Packed",
  "Shipped",
  "OutForDelivery",
  "Delivered",
  "Cancelled",
  "Refunded",
  "Failed"
]
```

---

## 7.10 Sign In

### Endpoint

```http
POST /api/v1/auth/login
```

### Request

```json
{
  "email": "john@example.com",
  "password": "Test@12345"
}
```

### Response

```json
{
  "accessToken": "jwt",
  "refreshToken": "refresh-token",
  "expiresAt": "2026-05-14T07:15:00Z",
  "user": {
    "id": "uuid",
    "firstName": "John",
    "lastName": "Doe",
    "email": "john@example.com",
    "phone": "+2348012345678",
    "role": "Customer",
    "emailVerified": true
  },
  "mergedCart": true
}
```

---

## 7.11 Sign Up

### Endpoint

```http
POST /api/v1/auth/register
```

### Request

```json
{
  "firstName": "John",
  "lastName": "Doe",
  "email": "john@example.com",
  "phone": "+2348012345678",
  "password": "Test@12345",
  "confirmPassword": "Test@12345",
  "acceptedTerms": true,
  "referralCode": "KELVIN123"
}
```

### Response

```json
{
  "accessToken": "jwt",
  "refreshToken": "refresh-token",
  "expiresAt": "2026-05-14T07:15:00Z",
  "requiresEmailVerification": true,
  "user": {}
}
```

---

## 7.12 Email Verification

### Endpoint

```http
POST /api/v1/auth/verify-email
```

### Request

```json
{
  "email": "john@example.com",
  "otp": "123456"
}
```

### Response

```json
{
  "verified": true,
  "message": "Email verified successfully"
}
```

### Resend OTP

```http
POST /api/v1/auth/resend-verification
```

```json
{
  "email": "john@example.com"
}
```

---

## 7.13 Forgot Password

```http
POST /api/v1/auth/forgot-password
```

```json
{
  "email": "john@example.com"
}
```

Response:

```json
{
  "message": "If the email exists, a reset OTP has been sent"
}
```

---

## 7.14 Reset Password

```http
POST /api/v1/auth/reset-password
```

```json
{
  "email": "john@example.com",
  "otp": "123456",
  "newPassword": "NewTest@12345",
  "confirmPassword": "NewTest@12345"
}
```

---

## 7.15 Customer Dashboard

### Purpose

Main customer dashboard with summary cards and recent orders.

### Endpoint

```http
GET /api/v1/account/dashboard
```

### Response

```json
{
  "profile": {
    "firstName": "John",
    "lastName": "Doe",
    "email": "john@example.com",
    "avatarUrl": null
  },
  "stats": {
    "totalOrders": 12,
    "pendingOrders": 2,
    "completedOrders": 8,
    "wishlistCount": 5,
    "loyaltyBalance": 420,
    "referralCount": 3
  },
  "recentOrders": [],
  "recentNotifications": []
}
```

---

## 7.16 Dashboard Order History

```http
GET /api/v1/orders?page=1&pageSize=10&status=Delivered
```

Response:

```json
{
  "items": [
    {
      "orderNumber": "SFZ-2026-00001",
      "placedAt": "2026-05-14T07:00:00Z",
      "status": "Delivered",
      "paymentStatus": "Paid",
      "itemCount": 3,
      "total": {},
      "canCancel": false,
      "canReview": true
    }
  ],
  "page": 1,
  "pageSize": 10,
  "totalItems": 12,
  "totalPages": 2
}
```

---

## 7.17 Dashboard Order Details

```http
GET /api/v1/orders/{orderNumber}
```

Response:

```json
{
  "id": "uuid",
  "orderNumber": "SFZ-2026-00001",
  "status": "Delivered",
  "paymentStatus": "Paid",
  "paymentMethod": "Paystack",
  "placedAt": "2026-05-14T07:00:00Z",
  "items": [
    {
      "productId": "uuid",
      "name": "Apple iPhone 16 Pro Max",
      "slug": "apple-iphone-16-pro-max",
      "imageUrl": "https://cdn...",
      "quantity": 1,
      "unitPrice": {},
      "lineTotal": {},
      "reviewed": false
    }
  ],
  "shippingAddress": {},
  "billingAddress": {},
  "totals": {
    "subtotal": {},
    "discount": {},
    "deliveryFee": {},
    "vat": {},
    "total": {}
  },
  "tracking": {},
  "canCancel": false,
  "canReturn": false,
  "canReview": true
}
```

---

## 7.18 Billing Address Modal

### Purpose

Add or edit billing address in checkout/dashboard.

```http
POST /api/v1/addresses
PUT /api/v1/addresses/{id}
```

Request:

```json
{
  "type": "Billing",
  "fullName": "John Doe",
  "phone": "+2348012345678",
  "line1": "12 Example Street",
  "line2": "Apt 2",
  "city": "Ikeja",
  "state": "Lagos",
  "country": "Nigeria",
  "postalCode": "100001",
  "isDefault": true
}
```

---

## 7.19 Add New Address Modal and Dashboard Addresses

```http
GET /api/v1/addresses
POST /api/v1/addresses
PUT /api/v1/addresses/{id}
DELETE /api/v1/addresses/{id}
PATCH /api/v1/addresses/{id}/default
```

Response:

```json
{
  "items": [
    {
      "id": "uuid",
      "type": "Shipping",
      "fullName": "John Doe",
      "phone": "+2348012345678",
      "line1": "12 Example Street",
      "line2": null,
      "city": "Ikeja",
      "state": "Lagos",
      "country": "Nigeria",
      "postalCode": "100001",
      "isDefault": true,
      "createdAt": "2026-05-14T07:00:00Z"
    }
  ]
}
```

---

## 7.20 Add New Card and Dashboard Card & Create

### Important Payment Compliance Note

Do not store raw card numbers, CVV, or expiry in ShopFresherz database. Use Paystack/Flutterwave tokenization or authorization codes. Backend may store only provider customer code, authorization code, card brand, last4, expiry month/year, and reusable authorization metadata.

### Endpoints

```http
GET /api/v1/payment-methods
POST /api/v1/payment-methods/initialize
DELETE /api/v1/payment-methods/{id}
PATCH /api/v1/payment-methods/{id}/default
```

### Initialize Saved Card Request

```json
{
  "provider": "Paystack",
  "callbackUrl": "https://shopfresherz.com/account/cards/callback"
}
```

### Response

```json
{
  "authorizationUrl": "https://checkout.paystack.com/...",
  "reference": "SFZCARD-2026-0001"
}
```

### Saved Card Response

```json
{
  "items": [
    {
      "id": "uuid",
      "provider": "Paystack",
      "brand": "Visa",
      "last4": "4242",
      "expMonth": 12,
      "expYear": 2028,
      "bank": "GTBank",
      "isDefault": true,
      "createdAt": "2026-05-14T07:00:00Z"
    }
  ]
}
```

---

## 7.21 Dashboard Profile Settings

```http
GET /api/v1/account/profile
PUT /api/v1/account/profile
PUT /api/v1/account/password
POST /api/v1/account/avatar
```

Profile Response:

```json
{
  "id": "uuid",
  "firstName": "John",
  "lastName": "Doe",
  "email": "john@example.com",
  "phone": "+2348012345678",
  "avatarUrl": "https://cdn.../avatar.jpg",
  "emailVerified": true,
  "phoneVerified": false,
  "createdAt": "2026-05-14T07:00:00Z"
}
```

Update Request:

```json
{
  "firstName": "John",
  "lastName": "Doe",
  "phone": "+2348012345678"
}
```

Change Password Request:

```json
{
  "currentPassword": "OldTest@12345",
  "newPassword": "NewTest@12345",
  "confirmPassword": "NewTest@12345"
}
```

---

## 7.22 Dashboard Notification

```http
GET /api/v1/notifications?page=1&pageSize=20&unreadOnly=false
PATCH /api/v1/notifications/{id}/read
PATCH /api/v1/notifications/read-all
DELETE /api/v1/notifications/{id}
PUT /api/v1/account/notification-preferences
```

Response:

```json
{
  "items": [
    {
      "id": "uuid",
      "type": "OrderStatus",
      "title": "Your order has shipped",
      "message": "Order SFZ-2026-00001 is on the way",
      "linkUrl": "/dashboard/orders/SFZ-2026-00001",
      "isRead": false,
      "createdAt": "2026-05-14T07:00:00Z"
    }
  ],
  "unreadCount": 3
}
```

---

## 7.23 Dashboard Loyalty Point

```http
GET /api/v1/account/loyalty
```

Response:

```json
{
  "balance": 420,
  "lifetimeEarned": 1000,
  "lifetimeRedeemed": 580,
  "conversionRate": {
    "earn": "1 point per ₦100 spent",
    "redeem": "100 points = ₦100"
  },
  "history": [
    {
      "id": "uuid",
      "type": "Earned",
      "points": 120,
      "reason": "Order SFZ-2026-00001",
      "orderNumber": "SFZ-2026-00001",
      "createdAt": "2026-05-14T07:00:00Z"
    }
  ]
}
```

### Optional Redemption Endpoint

```http
POST /api/v1/account/loyalty/redeem
```

```json
{
  "points": 100,
  "cartId": "uuid"
}
```

---

## 7.24 Dashboard Referral

### Purpose

Screen shows referral code, referral link, referral stats, referred users, and rewards.

```http
GET /api/v1/account/referral
```

Response:

```json
{
  "referralCode": "JOHN123",
  "referralLink": "https://shopfresherz.com/signup?ref=JOHN123",
  "stats": {
    "totalClicks": 100,
    "totalSignups": 8,
    "qualifiedReferrals": 3,
    "pendingRewards": 2,
    "earnedRewards": 1500
  },
  "rewards": [
    {
      "id": "uuid",
      "referredUserName": "Mary A.",
      "status": "Qualified",
      "rewardAmount": { "amount": 500, "currency": "NGN", "formatted": "₦500" },
      "createdAt": "2026-05-14T07:00:00Z"
    }
  ]
}
```

### Referral Rules

- Referral code generated on user creation.
- Reward should only qualify after referred user completes first paid order.
- Prevent self-referral.
- Prevent same device/IP abuse where feasible.

---

## 7.25 My Reviews and Write A Review

```http
GET /api/v1/account/reviews?page=1&pageSize=10
GET /api/v1/reviews/product/{productId}
POST /api/v1/reviews
PUT /api/v1/reviews/{id}
DELETE /api/v1/reviews/{id}
```

Create Review Request:

```json
{
  "productId": "uuid",
  "orderNumber": "SFZ-2026-00001",
  "rating": 5,
  "title": "Great product",
  "body": "The product arrived on time and works perfectly."
}
```

Review Response:

```json
{
  "id": "uuid",
  "product": {},
  "rating": 5,
  "title": "Great product",
  "body": "The product arrived on time and works perfectly.",
  "status": "PendingApproval",
  "isVerifiedPurchase": true,
  "createdAt": "2026-05-14T07:00:00Z"
}
```

### Rules

- Only authenticated users can write reviews.
- User can review a product once per completed order item.
- Reviews should be moderated before public display if admin workflow requires it.

---

## 7.26 404 Error Page

No special backend needed except API should return correct HTTP status codes and frontend handles unknown routes.

Recommended API error:

```json
{
  "success": false,
  "code": "SF-0002",
  "message": "Requested resource was not found"
}
```

---

## 7.27 Chatbox

### Purpose

Small support chat/contact box.

### Minimal Backend Option

```http
POST /api/v1/support/messages
```

Request:

```json
{
  "name": "John Doe",
  "email": "john@example.com",
  "phone": "+2348012345678",
  "subject": "Order issue",
  "message": "I need help with my order",
  "orderNumber": "SFZ-2026-00001"
}
```

Response:

```json
{
  "ticketNumber": "SFZSUP-2026-00001",
  "message": "Support request received"
}
```

### Advanced Option

Integrate a third-party live chat provider and only keep support ticket records in backend.

---

## 7.28 Admin Dashboard

### Purpose

Admin interface showing metrics, revenue, orders, sales charts, product/inventory health, user stats, and quick actions.

### Endpoint

```http
GET /api/v1/admin/dashboard?range=30d
```

### Response

```json
{
  "summary": {
    "totalRevenue": { "amount": 12500000, "currency": "NGN", "formatted": "₦12,500,000" },
    "totalOrders": 320,
    "pendingOrders": 24,
    "completedOrders": 260,
    "totalCustomers": 1200,
    "lowStockProducts": 12,
    "conversionRate": 2.4
  },
  "salesChart": [
    { "date": "2026-05-01", "revenue": 500000, "orders": 12 }
  ],
  "topProducts": [],
  "recentOrders": [],
  "lowStock": [],
  "recentCustomers": []
}
```

### Admin Supporting Endpoints

```http
GET /api/v1/admin/orders?page=1&pageSize=20&status=Pending&dateFrom=2026-05-01&dateTo=2026-05-14
PUT /api/v1/admin/orders/{orderNumber}/status
GET /api/v1/admin/users?page=1&pageSize=20&role=Customer&search=john
GET /api/v1/admin/inventory/low-stock
POST /api/v1/admin/users/{userId}/loyalty
GET /api/v1/admin/banners
POST /api/v1/admin/banners
PUT /api/v1/admin/banners/{id}
DELETE /api/v1/admin/banners/{id}
```

---

## 8. Admin Product Management API

Even if product admin screens are not fully visible in the export, the backend must support product creation and updates.

### Create Product

```http
POST /api/v1/products
```

Request:

```json
{
  "sku": "IP16PM-256-BLK",
  "name": "Apple iPhone 16 Pro Max 256GB",
  "slug": "apple-iphone-16-pro-max-256gb",
  "brandId": "uuid",
  "categoryId": "uuid",
  "shortDescription": "Premium iPhone with 256GB storage",
  "description": "Long product description",
  "price": 2800000,
  "compareAtPrice": 3000000,
  "costPrice": 2400000,
  "stockQuantity": 25,
  "lowStockThreshold": 5,
  "weightKg": 0.25,
  "status": "Active",
  "isFeatured": true,
  "isNewArrival": false,
  "warranty": "12 months manufacturer warranty",
  "keyFeatures": [],
  "whatIsIncluded": [],
  "whatIsNotIncluded": [],
  "specifications": [],
  "dimensions": {},
  "seoTitle": "Apple iPhone 16 Pro Max 256GB Nigeria",
  "seoDescription": "Buy Apple iPhone 16 Pro Max in Nigeria from ShopFresherz"
}
```

---

## 9. Database Schema Recommendations

### 9.1 Users

Fields:

- Id
- FirstName
- LastName
- Email
- NormalizedEmail
- Phone
- PasswordHash
- Role
- EmailVerified
- PhoneVerified
- AvatarUrl
- ReferralCode
- ReferredByUserId
- CreatedAt
- UpdatedAt
- LastLoginAt
- IsActive

### 9.2 RefreshTokens

- Id
- UserId
- TokenHash
- ExpiresAt
- RevokedAt
- CreatedAt
- CreatedByIp
- ReplacedByTokenHash

### 9.3 Categories

- Id
- ParentId
- Name
- Slug
- Description
- ImageUrl
- SortOrder
- IsActive

### 9.4 Brands

- Id
- Name
- Slug
- LogoUrl
- Description
- IsActive

### 9.5 Products

- Id
- SKU
- Name
- Slug
- BrandId
- CategoryId
- ShortDescription
- Description
- Price
- CompareAtPrice
- CostPrice
- StockQuantity
- ReservedQuantity
- LowStockThreshold
- StockStatus
- WeightKg
- Warranty
- Status
- IsFeatured
- IsNewArrival
- AttributesJson for backward compatibility
- SeoTitle
- SeoDescription
- CreatedAt
- UpdatedAt
- DeletedAt

### 9.6 ProductSpecifications

Use this instead of uncontrolled JSON where possible.

- Id
- ProductId
- Group
- Name
- Value
- Unit
- SortOrder
- IsVisible

### 9.7 ProductImages

- Id
- ProductId
- AltText
- ThumbUrl
- DisplayUrl
- ZoomUrl
- OriginalUrl
- SortOrder
- IsPrimary

### 9.8 ProductVariants

- Id
- ProductId
- SKU
- Name
- AttributesJson
- Price
- CompareAtPrice
- StockQuantity
- IsActive

### 9.9 Carts

- Id
- UserId nullable
- SessionId nullable
- CouponId nullable
- CreatedAt
- UpdatedAt
- ExpiresAt

### 9.10 CartItems

- Id
- CartId
- ProductId
- VariantId nullable
- Quantity
- UnitPriceSnapshot
- CreatedAt
- UpdatedAt

### 9.11 Orders

- Id
- OrderNumber
- UserId nullable
- CustomerEmail
- CustomerPhone
- CustomerName
- Status
- PaymentStatus
- PaymentMethod
- PaymentReference
- Subtotal
- DiscountAmount
- DeliveryFee
- VatAmount
- Total
- CouponCode
- Notes
- PlacedAt
- CancelledAt
- DeliveredAt

### 9.12 OrderItems

- Id
- OrderId
- ProductId
- VariantId nullable
- SKU
- ProductName
- ProductImageUrl
- UnitPrice
- Quantity
- LineTotal

### 9.13 Addresses

- Id
- UserId
- Type
- FullName
- Phone
- Line1
- Line2
- City
- State
- Country
- PostalCode
- IsDefault

### 9.14 Payments

- Id
- OrderId
- Provider
- Reference
- Status
- Amount
- Currency
- GatewayResponse
- PaidAt
- RawWebhookJson
- CreatedAt

### 9.15 WishlistItems

- Id
- UserId
- ProductId
- CreatedAt

### 9.16 Reviews

- Id
- UserId
- ProductId
- OrderId nullable
- Rating
- Title
- Body
- Status
- IsVerifiedPurchase
- CreatedAt
- UpdatedAt

### 9.17 LoyaltyTransactions

- Id
- UserId
- OrderId nullable
- Type
- Points
- Reason
- CreatedAt

### 9.18 ReferralRewards

- Id
- ReferrerUserId
- ReferredUserId
- Status
- RewardAmount
- QualifiedOrderId
- CreatedAt
- QualifiedAt
- PaidAt

### 9.19 Notifications

- Id
- UserId
- Type
- Title
- Message
- LinkUrl
- IsRead
- CreatedAt

### 9.20 Coupons

- Id
- Code
- Type
- Value
- MinOrderAmount
- MaxUses
- UsedCount
- StartsAt
- ExpiresAt
- IsActive

### 9.21 FlashDeals

- Id
- ProductId
- SalePrice
- OriginalPrice
- StartsAt
- EndsAt
- MaxQuantity
- SoldQuantity
- IsActive

### 9.22 Banners

- Id
- Title
- SubTitle
- ImageUrl
- LinkUrl
- CtaText
- SortOrder
- IsActive

### 9.23 SupportMessages

- Id
- TicketNumber
- UserId nullable
- Name
- Email
- Phone
- Subject
- Message
- OrderNumber nullable
- Status
- CreatedAt

---

## 10. Payment Flow Requirements

### 10.1 Paystack

1. User places order with paymentMethod Paystack.
2. Backend creates order in PendingPayment.
3. Backend initializes Paystack transaction.
4. Backend returns paymentUrl.
5. Frontend redirects user.
6. Paystack webhook confirms payment.
7. Backend verifies signature and reference.
8. Backend updates payment and order status.
9. Backend sends order confirmation notification.

### 10.2 Flutterwave

Same as Paystack, but use Flutterwave transaction initialization and webhook verification.

### 10.3 Pay on Delivery

- Order status: Pending or Confirmed based on admin policy.
- Payment status: Unpaid.
- No payment redirect.

### 10.4 Webhook Safety

- Verify signatures.
- Idempotency required by payment reference.
- Never trust frontend payment success alone.
- Store raw webhook event for audit.

---

## 11. Background Jobs

### 11.1 Pending Order Expiry

Runs every 5 minutes.

- Find PendingPayment orders older than 30 minutes.
- Cancel order.
- Release reserved stock.
- Notify customer where relevant.

### 11.2 Flash Deal Expiry

Runs every 1 minute.

- Deactivate expired flash deals.

### 11.3 Back In Stock Notifications

Runs every 10 minutes.

- Find products restocked from zero.
- Notify users who requested notification.
- Clear or mark requests as sent.

### 11.4 Abandoned Cart Reminder

Recommended additional job.

- Find carts inactive for 2 hours.
- Send reminder to authenticated users.

---

## 12. Error Codes

```json
[
  { "code": "SF-0001", "meaning": "Validation Error" },
  { "code": "SF-0002", "meaning": "Not Found" },
  { "code": "SF-0003", "meaning": "Unauthorized" },
  { "code": "SF-0004", "meaning": "Forbidden" },
  { "code": "SF-0005", "meaning": "Conflict" },
  { "code": "SF-0006", "meaning": "Out of Stock" },
  { "code": "SF-0007", "meaning": "Cart Empty" },
  { "code": "SF-0008", "meaning": "Invalid Coupon" },
  { "code": "SF-0009", "meaning": "Payment Failed" },
  { "code": "SF-0010", "meaning": "OTP Expired" },
  { "code": "SF-0011", "meaning": "OTP Invalid" },
  { "code": "SF-0012", "meaning": "Rate Limited" },
  { "code": "SF-0013", "meaning": "Order Not Cancellable" },
  { "code": "SF-0014", "meaning": "Address Not Found" },
  { "code": "SF-0015", "meaning": "Review Already Exists" },
  { "code": "SF-0016", "meaning": "Payment Webhook Invalid" },
  { "code": "SF-0017", "meaning": "Insufficient Loyalty Points" },
  { "code": "SF-0018", "meaning": "Referral Not Qualified" }
]
```

---

## 13. Security Requirements

- Disable Swagger in production unless behind admin authentication.
- Do not hardcode admin password.
- Store all secrets in environment variables.
- Hash refresh tokens.
- Hash passwords with strong password hasher.
- Rate limit auth, OTP, search, and checkout endpoints.
- Validate webhook signatures.
- Do not store raw card details.
- Use HTTPS only.
- Validate upload file type, size, and content.
- Use role-based authorization for admin endpoints.
- Add audit logs for admin changes.

---

## 14. Observability

Log these events:

- User registered
- Login success/failure
- OTP requested/verified/failed
- Product viewed
- Search performed
- Add to cart
- Coupon applied/failed
- Checkout started
- Order created
- Payment initialized
- Webhook received
- Payment confirmed/failed
- Order status changed
- Review submitted
- Admin product changed

Recommended fields:

```json
{
  "eventName": "OrderCreated",
  "userId": "uuid-or-null",
  "sessionId": "uuid-or-null",
  "orderNumber": "SFZ-2026-00001",
  "amount": 1200000,
  "currency": "NGN",
  "timestamp": "2026-05-14T07:00:00Z"
}
```

---

## 15. AI Agent Build Order

Build in this order:

1. Domain entities and database schema
2. Auth and roles
3. Categories and brands
4. Product catalog and product images
5. Search and filters
6. Cart and guest session cart
7. Addresses
8. Checkout summary
9. Orders
10. Payments and webhooks
11. Wishlist
12. Reviews
13. Dashboard APIs
14. Loyalty and referrals
15. Notifications
16. Admin dashboard
17. Admin order/product/coupon/banner/flash deal management
18. Background jobs
19. Security hardening
20. Seed data and integration tests

---

## 16. Minimum Acceptance Criteria

The backend is acceptable when:

- User can register, verify email, login, refresh token, and reset password.
- Guest can browse products and add to cart with X-Session-Id.
- Logged-in user cart merges with guest cart.
- Shop page filters work by category, brand, price, rating, stock, and dynamic attributes.
- Product detail page returns full gallery, specs, included items, warranty, reviews, and related products.
- User can apply coupon and see correct total.
- User can checkout with Paystack, Flutterwave, or Pay on Delivery.
- Payment webhooks update orders idempotently.
- User can track order by order number.
- User dashboard returns order history, addresses, profile, notifications, loyalty, referrals, reviews, and cards.
- Admin can view dashboard, orders, users, low stock, banners, coupons, and flash deals.
- Background jobs expire pending orders and flash deals.
- Swagger and secrets are secured before production.

---

## 17. Open Questions for Product Owner

1. Should card saving be fully supported, or should payment be one-time only?
2. Should loyalty points be redeemable at checkout immediately?
3. What is the exact referral reward amount and qualification rule?
4. Does ShopFresherz support returns/refunds from the dashboard?
5. Should users be able to cancel all pending orders or only unpaid orders?
6. Should guest order tracking require email/phone, or only order number?
7. Should admin review moderation be required before reviews go public?
8. Should delivery fees be flat by state/city or calculated by weight and location?
9. Should products support variants such as color, storage, and RAM as separate purchasable SKUs?
10. Should there be a CMS for footer/about/support pages?

---

## 18. Final Implementation Instruction for AI Agent

Build the backend as a modular ecommerce API. Do not simply create endpoints that return mock data. Implement real persistence, validation, authentication, authorization, payment webhook verification, stock reservation, guest cart merging, order tracking, and admin workflows. Keep the API response payloads stable and frontend-friendly. Where the Figma shows a screen but the existing API does not support it, implement the recommended endpoint in this document.
