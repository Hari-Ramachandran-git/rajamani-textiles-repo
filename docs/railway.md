# Deploy on Railway

Use three services from the same repository. All three use the repository root (`/`) as their root directory because the frontends share workspace dependencies, helpers and images.

| Service | `RAILWAY_DOCKERFILE_PATH` variable | Storage |
| --- | --- | --- |
| API | `Dockerfile` | Persistent volume at `/data` |
| Client | `apps/client/Dockerfile` | No volume |
| Admin | `apps/admin/Dockerfile` | No volume |

For **every service**, remove custom build and start commands. The Dockerfiles supply the correct runtime. In particular, an `npm` start command fails inside the .NET API image with “The executable npm could not be found.”

Set the Dockerfile path variable explicitly for each service; a service name such as `@hari-nari/admin` does not select the correct image. See [Railway custom Dockerfile paths](https://docs.railway.com/variables/reference).

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

## Deploy the client and admin

Create or reuse the two frontend services. Configure their Dockerfile path using the table above, keep the root directory at `/`, and clear the custom build/start commands. If the service is explicitly set to Railpack, select Dockerfile as the builder. Both Dockerfiles use Node only to build; Nginx serves the final static app and binds to Railway's `PORT`.

Set `/health` as each frontend's healthcheck path. Generate public domains for both services and add those exact origins to the API's `AllowedOrigins` variable. Redeploy the API after changing origins.

## Connect the React apps

Set this build-time variable for **both** frontend deployments, then rebuild:

```dotenv
VITE_API_BASE_URL=https://YOUR-API-DOMAIN/api
```

Railway passes this variable to the frontend Docker builds as a build argument. The build stops with a clear error if it is missing. Updating the URL requires a new build.

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


To build the frontend containers locally:

```sh
docker build -f apps/client/Dockerfile --build-arg VITE_API_BASE_URL=http://localhost:8080/api -t hari-nari-client .
docker build -f apps/admin/Dockerfile --build-arg VITE_API_BASE_URL=http://localhost:8080/api -t hari-nari-admin .
docker run --rm -p 5173:8080 hari-nari-client
# In another terminal:
docker run --rm -p 5174:8080 hari-nari-admin
```
