# TransitPay Deployment Guide - Render

This guide explains how to deploy the TransitPay API and database to Render with HTTPS enabled.

## Prerequisites

1. **GitHub account** with the TransitPay repository
2. **Render account** (sign up at https://render.com)
3. **PostgreSQL database** (will be provisioned on Render)

## Architecture

```
┌─────────────┐      HTTPS       ┌──────────────────┐      PostgreSQL      ┌─────────────┐
│ Android App │ ────────────────> │  Render Web API  │ ────────────────────> │  PostgreSQL  │
│ (Capacitor) │   (port 443)      │  (transitpay-api) │   (port 5432)        │  (transitpay-db) │
└─────────────┘                   └──────────────────┘                      └─────────────┘
```

## Step 1: Prepare Your Repository

Ensure these files are committed to your repository:

- ✅ `render.yaml` (created)
- ✅ `TransitPay.API/TransitPay.API.csproj`
- ✅ `TransitPay.API/Program.cs`
- ✅ `TransitPay.API/appsettings.json`

## Step 2: Deploy to Render

### Option A: Using Render Dashboard (Recommended)

1. **Go to Render Dashboard**
   - Visit https://dashboard.render.com
   - Sign in with your GitHub account

2. **Create a New Web Service**
   - Click **"New +"** → **"Web Service"**
   - Select your GitHub repository: `TransitPay-Prototype`
   - Click **"Connect"**

3. **Configure the Web Service**
   - **Name:** `transitpay-api`
   - **Runtime:** `Dotnet`
   - **Region:** Choose the closest region to your users (e.g., `Singapore` for Philippines)
   - **Branch:** `main` (or your default branch)
   - **Build Command:**
     ```bash
     dotnet publish TransitPay.API/TransitPay.API.csproj -c Release -o ./publish
     ```
   - **Start Command:**
     ```bash
     dotnet TransitPay.API.dll
     ```
   - **Plan:** 
     - Select `Free` for testing (spins down after 15 min inactivity)
     - Select `Starter` ($7/month) for production (always on)

4. **Add Environment Variables**
   Scroll down to the **"Environment Variables"** section and add:
   
   | Key | Value | Notes |
   |-----|-------|-------|
   | `ASPNETCORE_ENVIRONMENT` | `Production` | Required |
   | `DB_PASSWORD` | Click "Generate" | Render will auto-generate a secure password |
   | `JWT_KEY` | Click "Generate" | Render will auto-generate a 64-char secret |
   | `ADMIN_BOOTSTRAP_PASSWORD` | Click "Generate" | Render will auto-generate a secure password |

   **Important:** Copy the generated values for `DB_PASSWORD`, `JWT_KEY`, and `ADMIN_BOOTSTRAP_PASSWORD`. You'll need them later.

5. **Create the Web Service**
   - Click **"Create Web Service"**
   - Render will start building your app (takes 2-3 minutes)

### Option B: Using render.yaml (Automatic)

If you've committed `render.yaml` to your repository:

1. Go to https://dashboard.render.com
2. Click **"New +"** → **"Blueprint"**
3. Select your GitHub repository
4. Render will automatically detect `render.yaml` and configure:
   - Web service (`transitpay-api`)
   - PostgreSQL database (`transitpay-db`)
5. Click **"Apply"**
6. Render will deploy both services automatically

## Step 3: Verify Deployment

### Check Build Logs
- In Render dashboard, go to your web service
- Click **"Logs"** tab
- Look for:
  ```
  Build succeeded.
  Application started. Press Ctrl+C to shut down.
  ```

### Test HTTPS Endpoint
Once deployed, Render provides you with a URL like:
```
https://transitpay-api.onrender.com
```

Test the API:
```bash
# Health check
curl https://transitpay-api.onrender.com/api/auth/validate

# Expected response:
# {"success":true,"message":"API is running"}
```

### Verify SSL Certificate
Check that HTTPS is working correctly:
1. Visit https://www.ssllabs.com/ssltest/
2. Enter your Render URL: `transitpay-api.onrender.com`
3. Wait for the scan (1-2 minutes)
4. Verify you get an **A** or **A+** rating

## Step 4: Database Migrations

Render does **NOT** automatically run database migrations. You need to apply them manually.

### Option A: Using Render Shell (Recommended)

1. In Render dashboard, go to your web service
2. Click **"Shell"** tab
3. Run the migration command:
   ```bash
   dotnet ef database update --project TransitPay.API/TransitPay.API.csproj
   ```
4. You should see:
   ```
   Applying migration '20260809022622_InitialCreate'.
   Done.
   ```

### Option B: Using Post-Deploy Script

Add a `render-post-deploy.sh` script to your repository:

```bash
#!/bin/bash
# Install EF Core tools
dotnet tool install --global dotnet-ef

# Run migrations
dotnet ef database update --project TransitPay.API/TransitPay.API.csproj
```

Then in `render.yaml`, add:
```yaml
services:
  - type: web
    name: transitpay-api
    runtime: dotnet
    buildCommand: dotnet publish TransitPay.API/TransitPay.API.csproj -c Release -o ./publish
    startCommand: dotnet TransitPay.API.dll
    postDeployCommand: bash render-post-deploy.sh
    # ... rest of config
```

## Step 5: Update Frontend Apps

Update the API URL in your frontend apps to point to the Render deployment.

### passenger-app/.env
```env
VITE_APP_NAME=TransitPay
VITE_APP_ENV=production
VITE_API_URL=https://transitpay-api.onrender.com
```

### driver-app/.env
```env
VITE_APP_NAME=TransitPay Driver
VITE_APP_ENV=production
VITE_API_URL=https://transitpay-api.onrender.com
```

### Rebuild Frontend Apps
```bash
# Passenger app
cd TransitPay-Prototype/passenger-app
npm run build

# Driver app
cd TransitPay-Prototype/driver-app
npm run build
```

## Step 6: Test the Deployment

### Test Authentication
```bash
# Register a test user
curl -X POST https://transitpay-api.onrender.com/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "username": "testuser",
    "password": "TestPass123!",
    "firstName": "Test",
    "lastName": "User",
    "mobileNumber": "09171234567"
  }'

# Expected response:
# {"success":true,"message":"Registration successful","data":{"userId":1,"role":"Passenger"}}
```

### Test Login
```bash
curl -X POST https://transitpay-api.onrender.com/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "username": "09171234567",
    "password": "TestPass123!"
  }'

# Expected response:
# {"success":true,"data":{"token":"eyJhbGciOiJIUzI1NiIs...","refreshToken":"...","user":{...}}}
```

### Test CORS
From your frontend app, make an API call to verify CORS is working:
```typescript
const response = await fetch('https://transitpay-api.onrender.com/api/auth/validate');
console.log('CORS test:', response.status);
```

## Step 7: Custom Domain (Optional)

If you want to use a custom domain like `https://api.transitpay.ph`:

1. **Buy a domain** from a registrar (Namecheap, GoDaddy, etc.)

2. **Add domain to Render**
   - Go to your web service → **Settings**
   - Scroll to **"Custom Domains"**
   - Click **"Add Custom Domain"**
   - Enter your domain: `api.transitpay.ph`

3. **Update DNS Records**
   Render will provide you with DNS records to add to your domain registrar:
   ```
   Type: CNAME
   Name: api
   Value: transitpay-api.onrender.com
   TTL: 3600
   ```

4. **Wait for SSL Certificate**
   - Render automatically provisions an SSL certificate via Let's Encrypt
   - Takes 5-10 minutes
   - You'll receive an email when it's ready

5. **Update CORS**
   Add your custom domain to `appsettings.json`:
   ```json
   "Cors": {
     "AllowedOrigins": [
       "http://localhost:5173",
       "capacitor://localhost",
       "https://api.transitpay.ph"
     ]
   }
   ```

6. **Update Frontend Apps**
   ```env
   VITE_API_URL=https://api.transitpay.ph
   ```

## Troubleshooting

### Issue: "Application failed to start"
**Solution:** Check the logs in Render dashboard. Common causes:
- Missing environment variables
- Database connection string incorrect
- Port configuration issue

### Issue: "Database connection failed"
**Solution:** 
- Verify `DB_PASSWORD` environment variable is set
- Check that the PostgreSQL database is running in Render
- Verify the connection string format: `Host=...;Port=5432;Database=...;Username=...;Password=...`

### Issue: "CORS error in browser"
**Solution:**
- Verify the origin is in `appsettings.json` `Cors:AllowedOrigins`
- Ensure `AllowCredentials()` is enabled
- Check that the frontend is sending the correct `Origin` header

### Issue: "HTTPS not working"
**Solution:**
- Render automatically enables HTTPS for all web services
- If HTTPS is not working, check:
  - Is the web service running? (not stopped)
  - Is the plan active? (free tier may be suspended)
  - Wait 5-10 minutes for SSL certificate provisioning

### Issue: "Free tier spins down after inactivity"
**Solution:**
- Use the `Starter` plan ($7/month) for always-on service
- Or use a service like UptimeRobot to ping your API every 10 minutes

## Cost Estimate

### Development/Testing
- **Render Web Service (Free):** $0/month
  - Spins down after 15 minutes of inactivity
  - 512 MB RAM, 0.5 CPU
  - 100 GB bandwidth/month
- **Render PostgreSQL (Free):** $0/month
  - 256 MB storage
  - Expires after 90 days
- **Total:** $0/month

### Production
- **Render Web Service (Starter):** $7/month
  - Always on
  - 512 MB RAM, 0.5 CPU
  - 100 GB bandwidth/month
- **Render PostgreSQL (Shared):** $7/month
  - 256 MB storage
  - Persistent (doesn't expire)
- **Total:** $14/month

### Pro (High Traffic)
- **Render Web Service (Pro):** $25/month
  - 2 GB RAM, 1 CPU
  - 500 GB bandwidth/month
- **Render PostgreSQL (Pro):** $28/month
  - 4 GB storage
  - High availability
- **Total:** $53/month

## Security Checklist

- [ ] HTTPS is enabled (automatic on Render)
- [ ] SSL certificate is valid (check with SSL Labs)
- [ ] CORS is configured correctly
- [ ] Environment variables are set (DB_PASSWORD, JWT_KEY, ADMIN_BOOTSTRAP_PASSWORD)
- [ ] JWT_KEY is a strong, random 64+ character string
- [ ] DB_PASSWORD is a strong, random password
- [ ] ADMIN_BOOTSTRAP_PASSWORD is a strong, random password
- [ ] Database is not publicly accessible (Render handles this)
- [ ] Rate limiting is enabled (already configured)
- [ ] HTTPS redirect is enabled (already in Program.cs)

## Monitoring

### Render Dashboard
- **Logs:** View real-time application logs
- **Metrics:** Monitor CPU, memory, and bandwidth usage
- **Alerts:** Set up email alerts for downtime
- **Deployments:** View deployment history and rollback if needed

### Health Check
Render automatically pings your health check endpoint:
```
GET https://transitpay-api.onrender.com/api/auth/validate
```

If this returns a non-200 status, Render will restart your service.

## Next Steps

After successful deployment:

1. ✅ Test all API endpoints with Postman or curl
2. ✅ Update frontend apps with production API URL
3. ✅ Test frontend apps with production API
4. ✅ Set up monitoring and alerts in Render
5. ✅ Configure custom domain (optional)
6. ✅ Set up CI/CD (automatic deployments on git push)
7. ✅ Prepare for Android migration (see ANDROID_MIGRATION_AUDIT.md)

## Support

- **Render Documentation:** https://render.com/docs
- **Render Status:** https://status.render.com
- **Render Support:** support@render.com

---

**Deployment completed successfully!** 🚀

Your API is now live at: `https://transitpay-api.onrender.com`