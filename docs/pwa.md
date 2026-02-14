# Progressive Web App (PWA)

Portfolio Insight Dashboard is installable as a Progressive Web App, providing an app-like experience with offline capabilities and native-like features.

## PWA Features

✅ **Installable** - Add to home screen on iOS, Android, and desktop  
✅ **Standalone Mode** - Runs without browser UI (looks like a native app)  
✅ **Offline Support** - Static assets cached for offline access  
✅ **Fast Performance** - Service worker caching for instant loads  
✅ **Responsive Design** - Optimized for mobile, tablet, and desktop  
✅ **App Icon** - Custom icon appears on home screen/app drawer  

## Installation

### iOS (iPhone/iPad)

1. Open **Safari** and navigate to the dashboard URL
2. Tap the **Share** button (square with arrow pointing up) at the bottom of the screen
3. Scroll down and tap **Add to Home Screen**
4. (Optional) Customize the app name
5. Tap **Add** in the top right corner
6. The app icon appears on your home screen

**Launch**: Tap the icon to open the dashboard in standalone mode (no Safari UI).

### Android

1. Open **Chrome** and navigate to the dashboard URL
2. Tap the **three-dot menu** (⋮) in the top right corner
3. Select **Add to Home screen** or **Install app**
4. Confirm the installation in the dialog
5. The app icon appears in your app drawer

**Launch**: Tap the icon to open the dashboard in standalone mode.

### Desktop (Chrome, Edge)

1. Navigate to the dashboard URL
2. Look for the **install icon** (⊕) in the address bar
3. Click the install icon
4. Confirm installation in the dialog
5. The app appears in your applications list

**Alternative**:
- Chrome: Menu → Install Portfolio Insight Dashboard
- Edge: Menu → Apps → Install this site as an app

## PWA Configuration

### Manifest File

**Location**: `src/Dashboard._Web/wwwroot/manifest.json`

```json
{
  "name": "Ticker API Dashboard",
  "short_name": "Ticker Dashboard",
  "description": "Track and visualize investment portfolio performance",
  "start_url": "/",
  "display": "standalone",
  "background_color": "#ffffff",
  "theme_color": "#1560BD",
  "icons": [
    {
      "src": "/icon-192x192.png",
      "sizes": "192x192",
      "type": "image/png",
      "purpose": "any maskable"
    },
    {
      "src": "/icon-512x512.png",
      "sizes": "512x512",
      "type": "image/png",
      "purpose": "any maskable"
    }
  ]
}
```

### Manifest Properties

| Property | Value | Description |
|---|---|---|
| `name` | Ticker API Dashboard | Full app name (shown during installation) |
| `short_name` | Ticker Dashboard | Short name (shown under icon) |
| `description` | Track and visualize... | App description |
| `start_url` | `/` | URL to open when launching the app |
| `display` | `standalone` | Runs without browser UI |
| `background_color` | `#ffffff` | Splash screen background color |
| `theme_color` | `#1560BD` | Status bar color (Android) |
| `icons` | Array | App icons for different sizes |

### Display Modes

| Mode | Description | Browser UI | Use Case |
|---|---|---|---|
| `standalone` | App-like experience | ❌ Hidden | Default (used by this app) |
| `fullscreen` | Full screen | ❌ Hidden | Immersive experiences |
| `minimal-ui` | Minimal browser UI | ⚠️ Minimal | Navigation controls needed |
| `browser` | Standard browser | ✅ Full | Fallback |

### Icons

The PWA requires icons in multiple sizes:

| Size | File | Purpose |
|---|---|---|
| 192×192 | `icon-192x192.png` | Home screen icon (Android) |
| 512×512 | `icon-512x512.png` | Splash screen, high-res displays |

**Location**: `src/Dashboard._Web/wwwroot/`

**Requirements**:
- Square aspect ratio
- PNG format
- Transparent or solid background
- `purpose: "any maskable"` - Works on all platforms

## Service Worker

### Strategy

The service worker (`wwwroot/service-worker.js` v3) implements a smart caching strategy:

**Cache-First** (Static Assets):
- CSS files
- JavaScript files
- Images
- Fonts
- Instant loading from cache, updates in background

**Network-First** (Dynamic Content):
- HTML pages
- API endpoints
- Fresh data with offline fallback

### Service Worker Lifecycle

**1. Installation**:
```javascript
self.addEventListener('install', (event) => {
  event.waitUntil(
    caches.open(CACHE_NAME).then((cache) => {
      return cache.addAll([
        '/',
        '/css/site.css',
        '/js/site.js',
        // ... other static assets
      ]);
    })
  );
});
```

**2. Activation**:
```javascript
self.addEventListener('activate', (event) => {
  event.waitUntil(
    caches.keys().then((cacheNames) => {
      return Promise.all(
        cacheNames
          .filter((name) => name !== CACHE_NAME)
          .map((name) => caches.delete(name))
      );
    })
  );
});
```

**3. Fetch Handling**:
```javascript
self.addEventListener('fetch', (event) => {
  if (isStaticAsset(event.request.url)) {
    // Cache-first strategy
    event.respondWith(
      caches.match(event.request).then((response) => {
        return response || fetch(event.request);
      })
    );
  } else {
    // Network-first strategy
    event.respondWith(
      fetch(event.request).catch(() => {
        return caches.match(event.request);
      })
    );
  }
});
```

### Cache Versioning

**Version**: `v3` (defined in service worker)

**Update Process**:
1. Modify service worker file
2. Increment version number: `const CACHE_NAME = 'v4';`
3. Deploy updated service worker
4. Browser detects new version and updates automatically

**Cache Cleanup**:
Old caches are automatically deleted during the `activate` event.

### Skipped Requests

The service worker skips caching for:
- Authentication requests (`/signin-oidc`, `/signout-oidc`)
- Browser extension requests (`chrome-extension://`, `moz-extension://`)
- Non-HTTP(S) requests

## Offline Support

### What Works Offline

✅ **Previously visited pages** - Loaded from cache  
✅ **Static assets** - CSS, JavaScript, images, fonts  
✅ **App shell** - Basic layout and navigation  

### What Requires Internet

❌ **API calls** - Market data, transactions (requires Ticker API and Azure Table Storage)  
❌ **Authentication** - Azure AD sign-in  
❌ **New pages** - Pages not previously visited  

### Offline Fallback

When offline and accessing uncached content:
1. Service worker attempts network request
2. Network fails → Service worker checks cache
3. Cache miss → Browser shows default offline page

**Future Enhancement**: Create custom offline page with helpful messaging.

## Testing PWA Features

### Installation Test

1. Navigate to the dashboard
2. Look for install prompt or address bar icon
3. Install the app
4. Launch from home screen/app drawer
5. Verify standalone mode (no browser UI)

### Offline Test

1. Install the PWA
2. Visit all main pages (Dashboard, Transactions, etc.)
3. Disconnect from internet
4. Reload the app
5. Navigate between cached pages
6. Verify static assets load correctly

### Cache Test

1. Open the installed PWA
2. Open browser DevTools (if available)
3. Go to **Application** → **Cache Storage**
4. Verify `v3` cache exists
5. Check cached resources list

### Service Worker Test

**Chrome DevTools**:
1. Navigate to the dashboard
2. Open DevTools (F12)
3. Go to **Application** tab
4. Click **Service Workers** in sidebar
5. Verify service worker is **activated and running**
6. Use **Update** button to force update
7. Use **Unregister** to remove (for testing)

## Troubleshooting

### PWA Not Installing

**Symptoms**: No install prompt or address bar icon

**Solutions**:
1. Verify `manifest.json` is accessible: `{site-url}/manifest.json`
2. Check manifest is linked in `_Layout.cshtml`:
   ```html
   <link rel="manifest" href="~/manifest.json" />
   ```
3. Verify HTTPS is enabled (required for PWA)
4. Check browser console for manifest errors
5. Clear browser cache and reload

### Service Worker Not Updating

**Symptoms**: Old version of site cached, changes not appearing

**Solutions**:
1. Increment cache version in `service-worker.js`
2. Clear browser cache
3. Unregister service worker in DevTools
4. Force refresh (Ctrl+Shift+R or Cmd+Shift+R)
5. Close and reopen the app

### Icons Not Displaying

**Symptoms**: Default icon shown instead of custom icon

**Solutions**:
1. Verify icon files exist: `/icon-192x192.png`, `/icon-512x512.png`
2. Check icon paths in `manifest.json`
3. Verify icon dimensions (must be exact: 192×192, 512×512)
4. Check file format (must be PNG)
5. Clear app data and reinstall

### Offline Mode Issues

**Symptoms**: App doesn't work offline

**Solutions**:
1. Verify service worker is registered and active
2. Check cache storage includes required assets
3. Visit pages while online to cache them
4. Check console for service worker errors
5. Verify cache-first strategy for static assets

## Best Practices

### For Developers

**Updating the PWA**:
1. Increment cache version in service worker
2. Test installation and offline functionality
3. Verify cache cleanup removes old versions
4. Test on multiple devices/browsers

**Adding New Assets**:
1. Add to service worker precache list if critical
2. Use cache-first for static assets
3. Use network-first for dynamic content
4. Test offline functionality

**Debugging**:
1. Use Chrome DevTools → Application tab
2. Check Service Workers status
3. Inspect Cache Storage
4. Monitor Console for errors
5. Test on real devices (not just desktop)

### For Users

**Performance Tips**:
- Install as PWA for best performance
- Visit all pages while online to cache them
- Keep app updated (reinstall periodically)
- Clear app data if experiencing issues

**Privacy**:
- PWA caches data locally on your device
- Uninstalling removes all cached data
- Authentication tokens are not cached

## Future Enhancements

Potential improvements to PWA functionality:

- [ ] **Custom offline page** - Helpful messaging when offline
- [ ] **Background sync** - Queue transactions while offline, sync when online
- [ ] **Push notifications** - Portfolio alerts, price changes
- [ ] **Share target** - Share content to the app
- [ ] **Shortcuts** - Quick actions from home screen icon
- [ ] **Periodic background sync** - Auto-update market data in background

## Resources

- **MDN PWA Guide**: [developer.mozilla.org/en-US/docs/Web/Progressive_web_apps](https://developer.mozilla.org/en-US/docs/Web/Progressive_web_apps)
- **Google PWA Checklist**: [web.dev/pwa-checklist/](https://web.dev/pwa-checklist/)
- **Manifest Generator**: [simicart.com/manifest-generator.html](https://www.simicart.com/manifest-generator.html/)
- **Icon Generator**: [favicon.io](https://favicon.io/)

---

[← Back to Documentation Index](./README.md)
