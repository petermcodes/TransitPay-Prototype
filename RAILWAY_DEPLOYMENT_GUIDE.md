# Railway Deployment Guide

This guide explains how to deploy the TransitPay API to Railway.

## Prerequisites

- A Railway account (https://railway.app)
- Your database already created on Railway (PostgreSQL plugin)

## Step 1: Create New Project

1. Go to https://railway.app
2. Click "New Project"
3. Select "Deploy from GitHub repo"
4. Choose `petermcodes/TransitPay-Prototype`
5. Select the `main` branch

## Step 2: Configure Build Settings

Railway should auto-detect the `railway.json` file. If not, manually configure:

- **Root Directory**: Leave empty (repository root)
- **Dockerfile Path**: `TransitPay.API/Dockerfile`

## Step 3: Add PostgreSQL Database

1. In your Railway project, click "New"
2. Select "Database" → "PostgreSQL"
3. Railway will create a PostgreSQL instance and provide connection details

## Step 4: Configure Environment Variables

In your Railway project settings, add these environment variables:

### Required Variables

```env
# Database (Railway provides these automatically when you link the database)
DATABASE_HOST=${{Postgres.HOSTNAME}}
DATABASE_PORT=${{Postgres.PORT}}
DATABASE_NAME=${{Postgres.DATABASE}}
DATABASE_USER=${{Postgres.USER}}
DATABASE_PASSWORD=${{Postgres.PASSWORD}}

# JWT Configuration (generate a secure random string, at least 32 characters)
# Use a strong random string like: openssl rand -base64 32
JWT_KEY=your-secure-jwt-key-at-least-32-chars-long

# Admin Bootstrap Password (set a strong password for the initial admin account)
ADMIN_BOOTSTRAP_PASSWORD=your-secure-admin-password

# Environment
ASPNETCORE_ENVIRONMENT=Production
```

### Optional Variables

```env
# Rate Limiting (defaults shown - adjust as needed)
RATE_LIMITING__AUTH__PERMITLIMIT=10
RATE_LIMITING__AUTH__WINDOWMINUTES=1
```

## Step 5: Link Database to Service

1. Go to your service (the web service)
2. Click "Variables" tab
3. Click "Add Variable" → "Reference"
4. Select your PostgreSQL database
5. Railway will automatically add the `DATABASE_*` variables

## Step 6: Deploy

1. Click "Deploy" button
2. Railway will build and deploy your app
3. Monitor the logs for any errors

## Step 7: Verify Deployment

Once deployed, Railway will provide a public URL. Test it:

```bash
# Health check
curl https://your-app.up.railway.app/health

# Expected response:
# {"status":"healthy"}
```

### Post-Deployment Configuration

After successful deployment:

1. **Update CORS settings** in `appsettings.json` to include your Railway domain:
   - Add your Railway URL (e.g., `https://your-app.up.railway.app`) to the `Cors:AllowedOrigins` array
   - Commit and push the changes

2. **Test the API**:
   - Health endpoint: `https://your-app.up.railway.app/health`
   - Swagger UI: `https://your-app.up.railway.app/swagger`
   - Login with admin credentials (username: `Admin`, password: your `ADMIN_BOOTSTRAP_PASSWORD`)

## Step 8: Get Admin Credentials

After deployment, you can log in with:
- **Username**: `Admin`
- **Password**: The value you set for `ADMIN_BOOTSTRAP_PASSWORD`

## Troubleshooting

### Build Fails

- Check that `TransitPay.API/Dockerfile` exists
- Ensure all files are committed to GitHub
- Check build logs for specific errors

### Database Connection Fails

- Verify `DATABASE_*` variables are set correctly
- Check that the database is running
- Review application logs for connection errors

### App Crashes on Startup

- Check logs for missing environment variables
- Verify `JWT_KEY` is set (at least 32 characters)
- Ensure `ADMIN_BOOTSTRAP_PASSWORD` is set

## HTTPS

Railway automatically provides HTTPS for all deployments. Your app will be available at:
- `https://your-app-name.up.railway.app`

## Custom Domain (Optional)

To use a custom domain:
1. Go to Settings → Domains
2. Add your custom domain
3. Update your DNS settings as instructed

## Monitoring

Railway provides:
- Real-time logs
- Deployment history
- Resource usage metrics
- Automatic restarts on failure

## Cost

- Railway free tier: $5/month credit
- PostgreSQL plugin: $5/month (after free tier)
- Total: Free for development, ~$5-10/month for production

## Differences from Render

| Feature | Railway | Render |
|---------|---------|--------|
| Configuration | `railway.json` | `render.yaml` |
| Database linking | Automatic via UI | Manual env vars |
| Build system | Railpack | Docker |
| Default port | 8080 | 10000 |
| Free tier | $5 credit/month | Free tier available |
| HTTPS | Automatic | Automatic |

## Important Notes

- **Port Configuration**: The Dockerfile has been updated to expose port 8080 (Railway's default)
- **Database Migrations**: The app automatically applies migrations on startup
- **Data Seeding**: Initial data (roles, terminals, admin user, test card) is automatically seeded
- **Health Checks**: Monitor the `/health` endpoint to verify database connectivity
- **Logs**: Check Railway logs for detailed startup information and any errors
