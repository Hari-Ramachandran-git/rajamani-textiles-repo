# Hari Nari

A barebones textile marketplace: two React + Zustand web apps and a C# .NET 10 API backed by SQLite. Plain CSS, no template engine.

## Run locally

Requires .NET 10 SDK and Node.js 22.12+ (or Node 20.19+).

```sh
npm install
```

Run these in three terminals from the repository root:

```sh
npm run dev:api
npm run dev:client
npm run dev:admin
```

- Customer app: http://localhost:5173
- Admin app: http://localhost:5174
- API: http://localhost:5000/api/products

Enter `local-demo-admin` in the admin app to connect. This development key is for local use only. Set the `AdminKey` environment variable for a different key; it is required outside Development. The key is stored in browser session storage. This starter has no user accounts or production authentication.

## Features

- Browse, search, sort and filter 15 seeded home-textile products; prices are in INR per item or pack.
- Add products to a cart and place an order with delivery details.
- Admin: create, edit and delete products, manage stock, view orders and update fulfillment status.
- Orders preserve product names and prices; stock is deducted in a database transaction. Payment and shipping integrations are not included.

## Layout

- `apps/api`: .NET minimal API with Entity Framework Core / SQLite.
- `apps/client`: customer React / Zustand app.
- `apps/admin`: admin React / Zustand app.
- `packages/shared`: shared CSS and fetch helpers.

Vite proxies `/api` to the backend during development. SQLite creates and seeds `apps/api/shop.db` on first launch (relative to the API working directory). Delete the database to reset demo data. For deployment, serve the built apps with a reverse proxy forwarding `/api` to the API; the Vite proxy only runs during development. Use HTTPS and replace the shared admin key with proper authentication before public deployment.

```sh
npm run build
dotnet build apps/api
```

Validation: both frontend production builds and the .NET build pass. API smoke checks cover product CRUD, admin authorization, checkout, stock validation and order status updates. `npm audit` currently reports four high-severity advisories in the Vite development dependency chain (picomatch); these are not part of the bundled frontend runtime.

The storefront follows the cream, burgundy and gold direction of the supplied reference, with locally stored product photos in `apps/client/public/images`. The admin app can edit photo URLs. Startup upgrades the initial six-product demo catalog to home textiles while preserving order history; other existing catalogs are left intact.

## Deploy the backend

The root [Dockerfile](Dockerfile) deploys the API on Railway or any Docker host. It supports Railway's `PORT`, persistent SQLite storage, `/health`, and configurable CORS. Follow the [Railway setup](docs/railway.md) to attach a `/data` volume, set `AdminKey` and `AllowedOrigins`, and connect both frontends using `VITE_API_BASE_URL`.
