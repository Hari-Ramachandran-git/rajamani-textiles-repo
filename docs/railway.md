# Deploy the complete site on Railway

One Railway service runs both React apps and the .NET API. The root Dockerfile builds the customer app at `/`, the admin app at `/admin/`, and serves the API under `/api`.

## Configure your existing service

1. Select `rajamani-textiles-repo` from GitHub, branch `main`. Keep Root Directory `/`.
2. Set `RAILWAY_DOCKERFILE_PATH` to `Dockerfile`. Remove custom build/start commands. If a builder is explicitly selected, use Dockerfile.
3. Attach a persistent volume with mount path `/data`. Keep one replica for SQLite.
4. Set `AdminKey` to a long random secret. This is required in Production; enter it in the admin page to connect.
5. Set the healthcheck path to `/health`, with a 120-second timeout. Keep the generated public domain's target port at 8080, or match the service's `PORT` if customized.
6. Deploy the latest commit and apply any pending Railway changes.

Both frontends call `/api` on the same domain. You do not need `VITE_API_BASE_URL`, `AllowedOrigins`, or additional frontend services for this setup. The root Dockerfile builds with the same-origin API URL explicitly. You can remove frontend URL variables left over from a separate-services setup.

## Open your site

For the existing Railway domain:

- Customer: https://rajamani-textiles-repo-production.up.railway.app/
- Admin: https://rajamani-textiles-repo-production.up.railway.app/admin/
- Health: https://rajamani-textiles-repo-production.up.railway.app/health
- Products: https://rajamani-textiles-repo-production.up.railway.app/api/products

The admin web page is public, while admin data and actions require `AdminKey`. Do not put that secret into a `VITE_` variable or commit it.

## Storage and runtime

The image uses `/data/shop.db`. The mounted volume retains products and orders through redeployments. Database initialization runs at application startup, when the volume is available. `DataDirectory` overrides the default directory; `ConnectionStrings__Shop` overrides the complete SQLite connection string.

The API listens on Railway's `PORT` at `0.0.0.0`; the container defaults to 8080 elsewhere. `/health` returns 200 when the database is reachable, or 503 on failure.

## Run the complete container locally

With Docker running, from the repository root:

```sh
docker build -t rajamani-textiles .
docker volume create rajamani-data
# Set AdminKey in your shell before running this command.
docker run --rm -p 8080:8080 -e AdminKey -v rajamani-data:/data rajamani-textiles
```

Open `http://localhost:8080/` and `http://localhost:8080/admin/`.

## Optional: separate frontend services

The standalone `apps/client/Dockerfile` and `apps/admin/Dockerfile` remain available if you later want separate frontend domains. Use repository root `/` for both services, select their Dockerfile using `RAILWAY_DOCKERFILE_PATH`, and clear custom build/start commands. Set `VITE_API_BASE_URL` to your API's public URL including `/api` before building. Set the API's `AllowedOrigins` to their exact HTTPS origins, comma-separated without trailing slashes. Standalone frontend containers use Nginx with `/health` and Railway's `PORT`, and need no volumes.

References: [Railway Dockerfiles](https://docs.railway.com/builds/dockerfiles), [healthchecks](https://docs.railway.com/deployments/healthchecks), and [volumes](https://docs.railway.com/volumes/reference).
