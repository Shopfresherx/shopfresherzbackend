# TODO - ShopFresherz backend admin/search/product fixes

- [x] Step 1: Implement admin endpoints: GET /admin/analytics, /admin/reviews, /admin/customers, /admin/notifications
- [x] Step 2: Add MediatR queries/handlers + DTOs matching figmaspec.md response envelopes
- [x] Step 3: Fix 500 errors for GET /api/v1/search and GET /api/v1/search/instant (root-cause in query handlers / Elasticsearch service)
- [x] Step 4: Fix 403 Forbidden on /api/v1/admin/banners (role claim/policy mismatch)

- [x] Step 5: Update addProduct/create product schema to include imageUrls: string[]
- [x] Step 6: Update validation + persistence mapping for imageUrls
- [x] Step 7: Run dotnet build and ensure Swagger contracts compile (new arrivals/best deals updated)

- [x] Step 8: Final verification: status codes + validation adherence

