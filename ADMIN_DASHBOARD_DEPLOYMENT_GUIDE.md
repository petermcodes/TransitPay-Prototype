# Admin Dashboard Deployment Guide

This guide explains how to deploy the TransitPay Admin Dashboard to Railway.

## Prerequisites

- ✅ TransitPay API deployed: `https://transitpay-api-production.up.railway.app`
- ✅ Railway account with access to your project
- ✅ Code changes committed to GitHub

---

## Step 1: Verify Configuration Files

The following files have been created/updated:

### ✅ Created: `admin-dashboard/.env`
```env
VITE_API_URL=https://transitpay-api-production.up.railway.app
```

### ✅ Created: `admin-dashboard/railway.json`
```json
{
  "$schema": "https://railway.app/railway.schema.json",
  "build": {
    "builder": "NIXPACKS"
  },
  "deploy": {
    "startCommand": "npm run preview",
    "healthcheckPath": "/",
    "restartPolicyType": "ON_FAILURE",
    "restartPolicyMaxRetries": 3
  }
}
```

### ✅ Updated: `TransitPay.API/appsettings.json`
Added admin dashboard CORS origin (will be updated with actual URL after deployment).

---

## Step 2: Deploy to Railway

### Option A: Deploy as New Service in Existing Project (Recommended)

1. **Go to Railway Dashboard:**
   - Open your TransitPay project: https://railway.app
   - Click **"New"** → **"GitHub Repo"**
   - Select your repository: `petermcodes/TransitPay-Prototype`

2. **Configure Service:**
   - **Service Name**: `admin-dashboard`
   - **Root Directory**: `admin-dashboard`
   - **Build Command**: Leave empty (auto-detected by Nixpacks)
   - **Start Command**: `npm run preview`

3. **Add Environment Variables:**
   
   Click on the **"Variables"** tab and add:
   ```env
   VITE_API_URL=https://transitpay-api-production.up.railway.app
   ```

4. **Deploy:**
   - Click **"Deploy"** button
   - Wait for build to complete (2-3 minutes)
   - Monitor the build logs for any errors

---

## Step 3: Get Your Admin Dashboard URL

After successful deployment, Railway will provide a public URL:

**Format:** `https://admin-dashboard-production.up.railway.app`

**To find your URL:**
1. Go to your service in Railway dashboard
2. Click on **"Settings"** tab
3. Scroll to **"Domains"** section
4. Copy the public URL

---

## Step 4: Update API CORS Settings

**Important:** Update the API to allow your admin dashboard domain.

1. **Get your admin dashboard URL** from Step 3

2. **Update `TransitPay.API/appsettings.json`:**
   ```json
   "Cors": {
     "AllowedOrigins": [
       "http://localhost:5173",
       "http://localhost:5174",
       "http://localhost:5175",
       "http://192.168.50.28:5173",
       "https://five-unit-feminine.ngrok-free.dev",
       "capacitor://localhost",
       "https://transitpay-api.onrender.com",
       "https://admin-dashboard-production.up.railway.app"  ← Update this
     ]
   }
   ```

3. **Commit and push:**
   ```bash
   git add .
   git commit -m "Add admin dashboard CORS origin"
   git push
   ```

4. **Wait for API to redeploy** (1-2 minutes)

---

## Step 5: Verify Deployment

### Test 1: Access Admin Dashboard
```bash
curl https://admin-dashboard-production.up.railway.app
```
**Expected:** HTML content (index.html)

### Test 2: Open in Browser
1. Navigate to: `https://admin-dashboard-production.up.railway.app`
2. Should see the admin login page
3. No console errors

### Test 3: Test API Connection
1. Login with admin credentials:
   - **Username**: `Admin`
   - **Password**: (your `ADMIN_BOOTSTRAP_PASSWORD`)
2. Verify dashboard loads data
3. Check browser console for CORS errors (should be none)

---

## Step 6: Post-Deployment Configuration

### Update CORS for Production

Once confirmed working, you can remove the placeholder URL and use your actual URL:

**In `TransitPay.API/appsettings.json`:**
```json
"Cors": {
  "AllowedOrigins": [
    "http://localhost:5173",
    "http://localhost:5174",
    "http://localhost:5175",
    "https://admin-dashboard-production.up.railway.app"
  ]
}
```

**Remove old/unused origins:**
- `http://192.168.50.28:5173` (local network)
- `https://five-unit-feminine.ngrok-free.dev` (ngrok tunnel)
- `https://transitpay-api.onrender.com` (old Render URL)
- `capacitor://localhost` (will be added back when mobile apps are deployed)

### Commit Changes
```bash
git add .
git commit -m "Clean up CORS settings for production"
git push
```

---

## Troubleshooting

### Build Fails

**Check build logs in Railway:**
- Look for npm install errors
- Verify Node.js version compatibility
- Check for missing dependencies

**Common issues:**
- Missing `package-lock.json` → Run `npm install` locally and commit
- Build script fails → Verify `npm run build` works locally

### App Crashes on Load

**Check browser console:**
- CORS errors → Update API CORS settings
- API connection failed → Verify `VITE_API_URL` is correct
- 404 errors → Check that `dist/` folder is built correctly

### API Calls Fail

**Verify:**
1. API is running: `curl https://transitpay-api-production.up.railway.app/health`
2. CORS is configured correctly
3. `VITE_API_URL` environment variable is set in Railway
4. Admin credentials are correct

### Environment Variables Not Working

**Vite requires `VITE_` prefix:**
- ✅ Correct: `VITE_API_URL`
- ❌ Wrong: `API_URL` or `REACT_APP_API_URL`

**Rebuild after changing env vars:**
- Railway automatically rebuilds when env vars change
- Or manually trigger redeploy

---

## Monitoring

### Railway Dashboard

**Monitor these metrics:**
- **CPU/Memory Usage**: Should be low for static site
- **Request Count**: Monitor traffic
- **Response Time**: Should be < 100ms
- **Error Rate**: Should be 0%

### Logs

**Check logs for:**
- Build errors
- Runtime errors
- Failed requests

---

## Custom Domain (Optional)

To use a custom domain (e.g., `admin.transitpay.com`):

1. **Go to Railway:**
   - Service → Settings → Domains
   - Click "Add Domain"
   - Enter your domain

2. **Update DNS:**
   - Add CNAME record pointing to Railway
   - Or update nameservers

3. **Update CORS:**
   - Add custom domain to API CORS settings
   - Commit and push

---

## Cost

- **Railway Free Tier**: $5/month credit
- **Static Site**: ~$0.50-1/month (very low usage)
- **Total**: Free for development, ~$1-2/month for production

---

## Next Steps

After admin dashboard is deployed:

1. ✅ **Test all admin features**
2. ✅ **Verify data synchronization with API**
3. ✅ **Monitor for 24 hours**
4. ⏭️ **Proceed to Phase 2: Mobile App Setup**

---

## Quick Reference

| Item | Value |
|------|-------|
| **Admin Dashboard URL** | `https://admin-dashboard-production.up.railway.app` |
| **API URL** | `https://transitpay-api-production.up.railway.app` |
| **Environment Variable** | `VITE_API_URL=https://transitpay-api-production.up.railway.app` |
| **Build Command** | `npm run build` (auto) |
| **Start Command** | `npm run preview` |
| **Publish Directory** | `dist` |

---

## Support

If you encounter issues:
1. Check Railway build logs
2. Check browser console for errors
3. Verify environment variables are set
4. Test API connectivity separately
5. Check CORS configuration

**Ready to deploy? Follow the steps above and let me know if you need any assistance!**