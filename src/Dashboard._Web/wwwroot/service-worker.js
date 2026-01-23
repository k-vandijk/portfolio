const CACHE_NAME = 'ticker-dashboard-v2';
const urlsToCache = [
  '/',
  '/css/site.css',
  '/js/site.js',
  '/js/alerts.js',
  '/js/skeleton.js',
  '/icon-192x192.png',
  '/icon-512x512.png',
  '/favicon.ico'
];

// Install event - cache static assets
self.addEventListener('install', event => {
  event.waitUntil(
    caches.open(CACHE_NAME)
      .then(cache => {
        console.log('Service Worker: Caching files');
        return cache.addAll(urlsToCache).catch(err => {
          console.log('Service Worker: Cache addAll error:', err);
        });
      })
  );
  // Force the waiting service worker to become the active service worker
  self.skipWaiting();
});

// Activate event - clean up old caches
self.addEventListener('activate', event => {
  event.waitUntil(
    caches.keys().then(cacheNames => {
      return Promise.all(
        cacheNames.map(cacheName => {
          if (cacheName !== CACHE_NAME) {
            console.log('Service Worker: Clearing old cache:', cacheName);
            return caches.delete(cacheName);
          }
        })
      );
    })
  );
  // Claim all clients immediately
  return self.clients.claim();
});

// Fetch event - use network-first for data, cache-first for static assets
self.addEventListener('fetch', event => {
  // Skip non-GET requests
  if (event.request.method !== 'GET') {
    return;
  }

  // Parse the URL and skip chrome-extension and authentication requests
  try {
    const url = new URL(event.request.url);
    if (url.protocol === 'chrome-extension:' || 
        url.hostname === 'login.microsoftonline.com' ||
        url.pathname.includes('/signin-oidc')) {
      return;
    }
  } catch (e) {
    // Invalid URL, skip
    return;
  }

  const url = new URL(event.request.url);
  const isStaticAsset = url.pathname.match(/\.(css|js|png|jpg|jpeg|svg|gif|webp|woff|woff2|ttf|eot|ico)$/);
  
  // Use cache-first for static assets only
  if (isStaticAsset) {
    event.respondWith(
      caches.match(event.request)
        .then(response => {
          if (response) {
            return response;
          }
          
          return fetch(event.request).then(networkResponse => {
            if (networkResponse && networkResponse.status === 200) {
              const responseToCache = networkResponse.clone();
              caches.open(CACHE_NAME).then(cache => {
                cache.put(event.request, responseToCache);
              });
            }
            return networkResponse;
          });
        })
        .catch(() => {
          return new Response('Asset unavailable offline', {
            status: 503,
            statusText: 'Service Unavailable'
          });
        })
    );
  } else {
    // Use network-first for HTML and data endpoints
    event.respondWith(
      fetch(event.request)
        .then(networkResponse => {
          // Check if valid response
          if (!networkResponse || networkResponse.status !== 200 || networkResponse.type === 'error') {
            return networkResponse;
          }

          // Clone the response
          const responseToCache = networkResponse.clone();

          // Cache the response for offline fallback
          caches.open(CACHE_NAME).then(cache => {
            // Only cache same-origin requests
            if (event.request.url.startsWith(self.location.origin)) {
              cache.put(event.request, responseToCache);
            }
          });

          return networkResponse;
        })
        .catch(() => {
          // Network failed, try to return cached version as fallback
          return caches.match(event.request).then(response => {
            if (response) {
              return response;
            }
            
            // No cache available either
            return new Response('Offline - no cached version available', {
              status: 503,
              statusText: 'Service Unavailable',
              headers: new Headers({
                'Content-Type': 'text/plain'
              })
            });
          });
        })
    );
  }
});
