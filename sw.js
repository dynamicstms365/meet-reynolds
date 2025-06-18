// Reynolds Service Worker - Maximum Effort™ Offline Experience
const CACHE_NAME = 'reynolds-hub-v1.0.0';
const REYNOLDS_ASSETS = [
  '/',
  '/index.html',
  '/3d-demo/',
  '/3d-demo/index.html',
  '/3d-demo/models/CAUCASIAN MAN.glb',
  '/manifest.json',
  'https://cdnjs.cloudflare.com/ajax/libs/three.js/r128/three.min.js',
  'https://cdn.jsdelivr.net/npm/three@0.128.0/examples/js/loaders/GLTFLoader.js',
  'https://cdn.jsdelivr.net/npm/three@0.128.0/examples/js/controls/OrbitControls.js'
];

// Reynolds Installation - Parallel Caching Strategy
self.addEventListener('install', event => {
  console.log('🎭 Reynolds Service Worker installing with Maximum Effort™');
  
  event.waitUntil(
    caches.open(CACHE_NAME)
      .then(cache => {
        console.log('📦 Pre-caching Reynolds assets...');
        return cache.addAll(REYNOLDS_ASSETS);
      })
      .then(() => {
        console.log('✅ Reynolds cache ready for parallel serving!');
        return self.skipWaiting();
      })
      .catch(err => {
        console.error('❌ Cache installation failed:', err);
      })
  );
});

// Reynolds Activation - Cache Management Orchestration
self.addEventListener('activate', event => {
  console.log('🎭 Reynolds Service Worker activating...');
  
  event.waitUntil(
    caches.keys()
      .then(cacheNames => {
        return Promise.all(
          cacheNames.map(cacheName => {
            if (cacheName !== CACHE_NAME) {
              console.log('🗑️ Removing old cache:', cacheName);
              return caches.delete(cacheName);
            }
          })
        );
      })
      .then(() => {
        console.log('✅ Reynolds Service Worker activated with Maximum Effort™');
        return self.clients.claim();
      })
  );
});

// Reynolds Fetch Handler - Parallel Network/Cache Strategy
self.addEventListener('fetch', event => {
  // Skip non-GET requests and chrome-extension requests
  if (event.request.method !== 'GET' || event.request.url.startsWith('chrome-extension')) {
    return;
  }

  event.respondWith(
    caches.match(event.request)
      .then(cachedResponse => {
        // Reynolds Strategy: Cache first for assets, network first for HTML
        const isHTML = event.request.headers.get('accept')?.includes('text/html');
        
        if (isHTML) {
          // Network first for HTML - always get fresh content when possible
          return fetch(event.request)
            .then(networkResponse => {
              if (networkResponse.ok) {
                const responseClone = networkResponse.clone();
                caches.open(CACHE_NAME)
                  .then(cache => cache.put(event.request, responseClone));
              }
              return networkResponse;
            })
            .catch(() => {
              console.log('🎭 Serving cached HTML - offline Reynolds mode!');
              return cachedResponse || new Response('Reynolds offline - Maximum Effort™ will return!', {
                headers: { 'Content-Type': 'text/html' }
              });
            });
        } else {
          // Cache first for assets
          if (cachedResponse) {
            return cachedResponse;
          }
          
          return fetch(event.request)
            .then(networkResponse => {
              if (networkResponse.ok) {
                const responseClone = networkResponse.clone();
                caches.open(CACHE_NAME)
                  .then(cache => cache.put(event.request, responseClone));
              }
              return networkResponse;
            })
            .catch(() => {
              console.log('❌ Asset not available offline:', event.request.url);
              return new Response('Resource not available offline', { status: 404 });
            });
        }
      })
  );
});

// Reynolds Background Sync for Maximum Effort™
self.addEventListener('sync', event => {
  console.log('🎭 Reynolds background sync triggered:', event.tag);
  
  if (event.tag === 'reynolds-analytics') {
    event.waitUntil(
      // Parallel analytics sync when back online
      Promise.all([
        syncUsageData(),
        syncPerformanceMetrics(),
        syncUserPreferences()
      ])
    );
  }
});

// Reynolds Push Notifications - Coordinated Messaging
self.addEventListener('push', event => {
  const options = {
    body: event.data ? event.data.text() : 'Reynolds update available with Maximum Effort™!',
    icon: '/icon-192.png',
    badge: '/badge-72.png',
    tag: 'reynolds-update',
    actions: [
      {
        action: 'view',
        title: 'View Update',
        icon: '/icon-view.png'
      },
      {
        action: 'dismiss',
        title: 'Dismiss',
        icon: '/icon-dismiss.png'
      }
    ],
    requireInteraction: true,
    vibrate: [200, 100, 200]
  };

  event.waitUntil(
    self.registration.showNotification('Reynolds Hub Update', options)
  );
});

// Helper functions for parallel operations
async function syncUsageData() {
  console.log('📊 Syncing usage data...');
  // Implementation would sync usage analytics
}

async function syncPerformanceMetrics() {
  console.log('⚡ Syncing performance metrics...');
  // Implementation would sync performance data
}

async function syncUserPreferences() {
  console.log('⚙️ Syncing user preferences...');
  // Implementation would sync user settings
}

// Reynolds Error Handler - Graceful Failure with Style
self.addEventListener('error', event => {
  console.error('🎭 Reynolds Service Worker error:', event.error);
  // Could implement error reporting here
});

self.addEventListener('unhandledrejection', event => {
  console.error('🎭 Reynolds Service Worker unhandled rejection:', event.reason);
  event.preventDefault();
});

console.log('🎭 Reynolds Service Worker loaded - Ready for Maximum Effort™ offline orchestration!');