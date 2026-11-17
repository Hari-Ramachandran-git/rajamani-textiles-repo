# Deploy the API on Railway

The root Dockerfile builds only the .NET backend. The two React apps can be hosted separately.

## Configure the service

1. Create a Railway service from this Git repository. Keep its root directory at the repository root (`/`), so Railway detects `Dockerfile`. Leave build/start command overrides empty.
2. Attach a persistent volume mounted at `/data`. SQLite writes `/data/shop.db`; without a volume, redeploying loses changes made in the container filesystem. Keep this SQLite service at one replica.
3. Add these service variables:

   | Variable | Value |
   | --- | --- |
   | `AdminKey` | A long, randomly generated secret; enter it only in the admin app |
   | `AllowedOrigins` | Customer and admin origins, comma-separated, for example `https://shop.example.com,https://admin.example.com` |

   Origins must include the scheme and have no path or trailing slash. Localhost origins are automatically allowed only in Development. Do not put `AdminKey` in a `VITE_` variable or commit it.
4. Set the healthcheck path to `/health` and timeout to 120 seconds. The endpoint checks database connectivity and returns 200 when healthy or 503 on failure.
5. Deploy, then generate a public domain in the service's Networking settings. Check `https://YOUR-DOMAIN/health` and `https://YOUR-DOMAIN/api/products`.

Railway supplies `PORT`; the API binds to `0.0.0.0:$PORT`. Outside Railway, the container defaults to port 8080. Production refuses to start without `AdminKey`. The API initializes and upgrades its SQLite schema at application startup, when the volume is mounted.

`DataDirectory` overrides the database directory (the Docker image sets `/data`). Without that setting, the API uses `RAILWAY_VOLUME_MOUNT_PATH` when available, otherwise its content root. `ConnectionStrings__Shop` overrides the entire connection string; if you use it, ensure its directory exists on the mounted volume.

## Connect the React apps

Set this build-time variable for **both** frontend deployments, then rebuild:

```dotenv
VITE_API_BASE_URL=https://YOUR-API-DOMAIN/api
```

Copy the pattern from `apps/client/.env.example` and `apps/admin/.env.example` for local configuration. If the variable is unset, requests use `/api` on the frontend's origin. The Vite proxy is only for local development. Frontend product images remain bundled with each frontend.

## Check the container locally

From the repository root, with Docker running:

```sh
docker build -t hari-nari-api .
docker volume create hari-nari-data
# Set AdminKey in your shell first; -e passes its value without embedding it in the image.
docker run --rm -p 8080:8080 -e AdminKey \
  -e AllowedOrigins=http://localhost:5173,http://localhost:5174 \
  -v hari-nari-data:/data hari-nari-api
```

Visit `http://localhost:8080/health`. Use `VITE_API_BASE_URL=http://localhost:8080/api` for the local frontends if testing against this container instead of the usual port-5000 API.

References: [Railway Dockerfiles](https://docs.railway.com/builds/dockerfiles), [healthchecks](https://docs.railway.com/deployments/healthchecks), and [volumes](https://docs.railway.com/volumes/reference).
