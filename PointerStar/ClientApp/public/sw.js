const CACHE_NAME = 'pointerstar-static-v1'
const SHELL_FILES = [
  '/',
  '/index.html',
  '/manifest.webmanifest',
  '/favicon.svg',
  '/icon-192.png',
  '/icon-512.png',
  '/Pointer_Black_Apple_Mask_Icon.svg',
]

self.addEventListener('install', (event) => {
  event.waitUntil(
    caches
      .open(CACHE_NAME)
      .then((cache) => cache.addAll(SHELL_FILES))
      .then(() => self.skipWaiting()),
  )
})

self.addEventListener('activate', (event) => {
  event.waitUntil(
    caches
      .keys()
      .then((cacheNames) =>
        Promise.all(
          cacheNames
            .filter((cacheName) => cacheName !== CACHE_NAME)
            .map((cacheName) => caches.delete(cacheName)),
        ),
      )
      .then(() => self.clients.claim()),
  )
})

function isCacheableRequest(request) {
  if (request.method !== 'GET') {
    return false
  }

  const url = new URL(request.url)
  if (url.origin !== self.location.origin) {
    return false
  }

  return !url.pathname.startsWith('/api/') && !url.pathname.startsWith('/RoomHub')
}

self.addEventListener('fetch', (event) => {
  const { request } = event
  if (!isCacheableRequest(request)) {
    return
  }

  if (request.mode === 'navigate') {
    event.respondWith(
      fetch(request)
        .then((response) => {
          if (response.ok) {
            const responseClone = response.clone()
            void caches.open(CACHE_NAME).then((cache) => cache.put('/index.html', responseClone))
          }

          return response
        })
        .catch(async () => (await caches.match('/index.html')) ?? Response.error()),
    )

    return
  }

  event.respondWith(
    caches.match(request).then((cachedResponse) => {
      if (cachedResponse) {
        return cachedResponse
      }

      return fetch(request).then((response) => {
        if (response.ok) {
          const responseClone = response.clone()
          void caches.open(CACHE_NAME).then((cache) => cache.put(request, responseClone))
        }

        return response
      })
    }),
  )
})
