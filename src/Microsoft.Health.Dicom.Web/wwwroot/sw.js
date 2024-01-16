/******/ (() => { // webpackBootstrap
var __webpack_exports__ = {};
navigator.serviceWorker.getRegistrations().then(function (registrations) {
  for (let registration of registrations) {
    registration.unregister();
  }
});

// https://developers.google.com/web/tools/workbox/guides/troubleshoot-and-debug
importScripts('https://storage.googleapis.com/workbox-cdn/releases/5.0.0-beta.1/workbox-sw.js');

// Install newest
// https://developers.google.com/web/tools/workbox/modules/workbox-core
workbox.core.skipWaiting();
workbox.core.clientsClaim();

// Cache static assets that aren't precached
workbox.routing.registerRoute(/\.(?:js|css|json5)$/, new workbox.strategies.StaleWhileRevalidate({
  cacheName: 'static-resources'
}));

// Cache the Google Fonts stylesheets with a stale-while-revalidate strategy.
workbox.routing.registerRoute(/^https:\/\/fonts\.googleapis\.com/, new workbox.strategies.StaleWhileRevalidate({
  cacheName: 'google-fonts-stylesheets'
}));

// Cache the underlying font files with a cache-first strategy for 1 year.
workbox.routing.registerRoute(/^https:\/\/fonts\.gstatic\.com/, new workbox.strategies.CacheFirst({
  cacheName: 'google-fonts-webfonts',
  plugins: [new workbox.cacheableResponse.CacheableResponsePlugin({
    statuses: [0, 200]
  }), new workbox.expiration.ExpirationPlugin({
    maxAgeSeconds: 60 * 60 * 24 * 365,
    // 1 Year
    maxEntries: 30
  })]
}));

// MESSAGE HANDLER
self.addEventListener('message', event => {
  if (event.data && event.data.type === 'SKIP_WAITING') {
    switch (event.data.type) {
      case 'SKIP_WAITING':
        // TODO: We'll eventually want this to be user prompted
        // workbox.core.skipWaiting();
        // workbox.core.clientsClaim();
        // TODO: Global notification to indicate incoming reload
        break;
      default:
        console.warn(`SW: Invalid message type: ${event.data.type}`);
    }
  }
});
workbox.precaching.precacheAndRoute([{'revision':null,'url':'/12.bundle.b965cc54108a0b38a022.js'},{'revision':null,'url':'/125.bundle.253395f320b72180da63.js'},{'revision':null,'url':'/181.bundle.ceb057236403bcb630ac.js'},{'revision':'8079c6447e119ba0680e8fab5875745d','url':'/181.css'},{'revision':null,'url':'/19.bundle.d961845411cf4e95d27c.js'},{'revision':'51b8ed55f5b8d448837222f03bdd6de8','url':'/19.css'},{'revision':null,'url':'/202.bundle.d3490836f71e001dd30f.js'},{'revision':null,'url':'/220.bundle.f7e1c96c94245e70f2be.js'},{'revision':null,'url':'/221.bundle.a331e2a9a29f9599fd40.js'},{'revision':'aa1d1f3e32367e42fe90399144d94577','url':'/221.css'},{'revision':null,'url':'/23.bundle.e008ad788170f2ed5569.js'},{'revision':null,'url':'/236.bundle.b09ef6a3c16be7ad1d05.js'},{'revision':null,'url':'/250.bundle.8084960e3318cda37317.js'},{'revision':'0afb25509c7f072fbd7eda42c6895dbf','url':'/250.css'},{'revision':null,'url':'/281.bundle.4f7c49673b5861436311.js'},{'revision':null,'url':'/342.bundle.3f9ebc45fdc6d6879adc.js'},{'revision':null,'url':'/359.bundle.aa2adce78c3935aa19c1.js'},{'revision':'c4ea120c6da08aa75348edfa3e57ece9','url':'/36785fbd89b0e17f6099.wasm'},{'revision':null,'url':'/370.bundle.31f3d861d96bdd540dc7.js'},{'revision':null,'url':'/410.bundle.12c1bc7cb765ef74d275.js'},{'revision':null,'url':'/417.bundle.af0a207c29b109f84159.js'},{'revision':null,'url':'/451.bundle.9fd36f52ff69594f0669.js'},{'revision':null,'url':'/471.bundle.b3d77b83b1593c09a504.js'},{'revision':'c377e1f5fe4a207d270c3f7a8dd3e3ca','url':'/5004fdc02f329ce53b69.wasm'},{'revision':null,'url':'/506.bundle.311783d53e8d64b84280.js'},{'revision':null,'url':'/530.bundle.a03b6f942ace3e1baa1e.js'},{'revision':'51b8ed55f5b8d448837222f03bdd6de8','url':'/579.css'},{'revision':null,'url':'/604.bundle.a51f83e64004bca5f497.js'},{'revision':'62b4ae8445d191d5aab5503ce475724d','url':'/610.min.worker.js'},{'revision':'3c2206525c18cd87dd28082949a4e43e','url':'/610.min.worker.js.map'},{'revision':null,'url':'/613.bundle.4359bc30c68b8f567140.js'},{'revision':'5800265b6831396572fb5d32c6bd8eef','url':'/62ab5d58a2bea7b5a1dc.wasm'},{'revision':'ce10eced3ce34e663d86569b27f5bffb','url':'/65916ef3def695744bda.wasm'},{'revision':null,'url':'/663.bundle.87300c41b902228496ec.js'},{'revision':null,'url':'/686.bundle.dccef1f36e4bc79bcc48.js'},{'revision':null,'url':'/687.bundle.a3caefcf2e55897bad75.js'},{'revision':null,'url':'/743.bundle.489f7df3a089d4d374e1.js'},{'revision':null,'url':'/757.bundle.ec8301d8e70d2b990f65.js'},{'revision':'cf3e4d4fa8884275461c195421812256','url':'/75788f12450d4c5ed494.wasm'},{'revision':'cc4a3a4da4ac1b863a714f93c66c6ef2','url':'/75a0c2dfe07b824c7d21.wasm'},{'revision':null,'url':'/774.bundle.4b2dc46a35012b898e1a.js'},{'revision':null,'url':'/775.bundle.2285e7e0e67878948c0d.js'},{'revision':null,'url':'/788.bundle.b9dabaea41cb029360b1.js'},{'revision':null,'url':'/814.bundle.a3d1fbc03a4a3ea3f23d.js'},{'revision':null,'url':'/82.bundle.ec05d3de5ac5b0c577fe.js'},{'revision':'185e5e0a10fa6ab2fc7b3c38e63d550b','url':'/82.css'},{'revision':null,'url':'/822.bundle.891f2e57b1b7bc2f4cb4.js'},{'revision':null,'url':'/886.bundle.4b3a7f2079d085fdbcb3.js'},{'revision':'74c9647440e51f149ad12923d6ead952','url':'/945.min.worker.js'},{'revision':'cdf6f0457d4af2cef04fc41816241bc1','url':'/945.min.worker.js.map'},{'revision':null,'url':'/957.bundle.9ea4506963ef8b2d84ba.js'},{'revision':null,'url':'/99.bundle.d77c8c0a957274c827da.js'},{'revision':'d1895aa7a4595dc279c382e5a31ef9f4','url':'/_headers'},{'revision':'e3bf0f3e9c34f51ad59836ae8e8eaf43','url':'/_redirects'},{'revision':'41bb4b36a914c2db5c383a627162b3da','url':'/app-config.js'},{'revision':'b08f6cf2917911325ec815f7575f9eb2','url':'/app.bundle.css'},{'revision':null,'url':'/app.bundle.dacb6768b481e9135f71.js'},{'revision':'cb4f64534cdf8dd88f1d7219d44490db','url':'/assets/android-chrome-144x144.png'},{'revision':'5cde390de8a619ebe55a669d2ac3effd','url':'/assets/android-chrome-192x192.png'},{'revision':'e7466a67e90471de05401e53b8fe20be','url':'/assets/android-chrome-256x256.png'},{'revision':'9bbe9b80156e930d19a4e1725aa9ddae','url':'/assets/android-chrome-36x36.png'},{'revision':'5698b2ac0c82fe06d84521fc5482df04','url':'/assets/android-chrome-384x384.png'},{'revision':'56bef3fceec344d9747f8abe9c0bba27','url':'/assets/android-chrome-48x48.png'},{'revision':'3e8b8a01290992e82c242557417b0596','url':'/assets/android-chrome-512x512.png'},{'revision':'517925e91e2ce724432d296b687d25e2','url':'/assets/android-chrome-72x72.png'},{'revision':'4c3289bc690f8519012686888e08da71','url':'/assets/android-chrome-96x96.png'},{'revision':'cf464289183184df09292f581df0fb4f','url':'/assets/apple-touch-icon-1024x1024.png'},{'revision':'0857c5282c594e4900e8b31e3bade912','url':'/assets/apple-touch-icon-114x114.png'},{'revision':'4208f41a28130a67e9392a9dfcee6011','url':'/assets/apple-touch-icon-120x120.png'},{'revision':'cb4f64534cdf8dd88f1d7219d44490db','url':'/assets/apple-touch-icon-144x144.png'},{'revision':'977d293982af7e9064ba20806b45cf35','url':'/assets/apple-touch-icon-152x152.png'},{'revision':'6de91b4d2a30600b410758405cb567b4','url':'/assets/apple-touch-icon-167x167.png'},{'revision':'87bff140e3773bd7479a620501c4aa5c','url':'/assets/apple-touch-icon-180x180.png'},{'revision':'647386c34e75f1213830ea9a38913525','url':'/assets/apple-touch-icon-57x57.png'},{'revision':'0c200fe83953738b330ea431083e7a86','url':'/assets/apple-touch-icon-60x60.png'},{'revision':'517925e91e2ce724432d296b687d25e2','url':'/assets/apple-touch-icon-72x72.png'},{'revision':'c9989a807bb18633f6dcf254b5b56124','url':'/assets/apple-touch-icon-76x76.png'},{'revision':'87bff140e3773bd7479a620501c4aa5c','url':'/assets/apple-touch-icon-precomposed.png'},{'revision':'87bff140e3773bd7479a620501c4aa5c','url':'/assets/apple-touch-icon.png'},{'revision':'05fa74ea9c1c0c3931ba96467999081d','url':'/assets/apple-touch-startup-image-1182x2208.png'},{'revision':'9e2cd03e1e6fd0520eea6846f4278018','url':'/assets/apple-touch-startup-image-1242x2148.png'},{'revision':'5591e3a1822cbc8439b99c1a40d53425','url':'/assets/apple-touch-startup-image-1496x2048.png'},{'revision':'337de578c5ca04bd7d2be19d24d83821','url':'/assets/apple-touch-startup-image-1536x2008.png'},{'revision':'cafb4ab4eafe6ef946bd229a1d88e7de','url':'/assets/apple-touch-startup-image-320x460.png'},{'revision':'d9bb9e558d729eeac5efb8be8d6111cc','url':'/assets/apple-touch-startup-image-640x1096.png'},{'revision':'038b5b02bac8b82444bf9a87602ac216','url':'/assets/apple-touch-startup-image-640x920.png'},{'revision':'2177076eb07b1d64d663d7c03268be00','url':'/assets/apple-touch-startup-image-748x1024.png'},{'revision':'4fc097443815fe92503584c4bd73c630','url':'/assets/apple-touch-startup-image-750x1294.png'},{'revision':'2e29914062dce5c5141ab47eea2fc5d9','url':'/assets/apple-touch-startup-image-768x1004.png'},{'revision':'f692ec286b3a332c17985f4ed38b1076','url':'/assets/browserconfig.xml'},{'revision':'f3d9a3b647853c45b0e132e4acd0cc4a','url':'/assets/coast-228x228.png'},{'revision':'ad6e1def5c66193d649a31474bbfe45d','url':'/assets/favicon-16x16.png'},{'revision':'84d1dcdb6cdfa55e2f46be0c80fa5698','url':'/assets/favicon-32x32.png'},{'revision':'95fb44c4998a46109e49d724c060db24','url':'/assets/favicon.ico'},{'revision':'5df2a5b0cee399ac0bc40af74ba3c2cb','url':'/assets/firefox_app_128x128.png'},{'revision':'11fd9098c4b07c8a07e1d2a1e309e046','url':'/assets/firefox_app_512x512.png'},{'revision':'27cddfc922dca3bfa27b4a00fc2f5e36','url':'/assets/firefox_app_60x60.png'},{'revision':'2017d95fae79dcf34b5a5b52586d4763','url':'/assets/manifest.webapp'},{'revision':'cb4f64534cdf8dd88f1d7219d44490db','url':'/assets/mstile-144x144.png'},{'revision':'334895225e16a7777e45d81964725a97','url':'/assets/mstile-150x150.png'},{'revision':'e295cca4af6ed0365cf7b014d91b0e9d','url':'/assets/mstile-310x150.png'},{'revision':'cbefa8c42250e5f2443819fe2c69d91e','url':'/assets/mstile-310x310.png'},{'revision':'aa411a69df2b33a1362fa38d1257fa9d','url':'/assets/mstile-70x70.png'},{'revision':'5609af4f69e40e33471aee770ea1d802','url':'/assets/yandex-browser-50x50.png'},{'revision':'dd001f21b3970d5a7f3e245cc10d21df','url':'/assets/yandex-browser-manifest.json'},{'revision':'52b9a07fe0541fe8c313d9788550bf51','url':'/b6b803111e2d06a825bd.wasm'},{'revision':'7edb59d2be7c993050cb31ded36afa31','url':'/c22b37c3488e1d6c3aa4.wasm'},{'revision':'5f5a189af3f93caac4d97cf63347d7df','url':'/cornerstoneDICOMImageLoader.min.js'},{'revision':'346733bc702ee77bf7243351d99974f8','url':'/cornerstoneDICOMImageLoader.min.js.map'},{'revision':null,'url':'/dicom-microscopy-viewer.bundle.2c146384eb9466d02ff8.js'},{'revision':'9d8c85b42d04bb117a3b583d654fbb08','url':'/dicomMicroscopyViewer.min.js'},{'revision':'450494c199cf8dd8e8c34d5e98bf5334','url':'/dicomMicroscopyViewer.min.js.LICENSE.txt'},{'revision':'4acdd19a35d759ec2669f1ba9490937d','url':'/es6-shim.min.js'},{'revision':'791565db341e8852807303918f5f9939','url':'/google.js'},{'revision':'cce12a27b8869d44b9a0c7ad67b7792e','url':'/index.html'},{'revision':'feee2d4ed9d00c64f0e4d6a46608fecf','url':'/index.worker.e62ecca63f1a2e124230.worker.js'},{'revision':'beaf37c564436e46bbcd825f0330cdbf','url':'/index.worker.e62ecca63f1a2e124230.worker.js.map'},{'revision':'71cec55513e051f0778ad89be760c11a','url':'/index.worker.min.worker.js'},{'revision':'fd1116add443fee52a935df926396e0f','url':'/index.worker.min.worker.js.map'},{'revision':'31c0367ca4160b2c6373e905739c5719','url':'/init-service-worker.js'},{'revision':'74fc9658b62903be2048c1f82a22b4d4','url':'/manifest.json'},{'revision':'3fa71aa0af3e34b4ebd9a71eee0f4bdd','url':'/ohif-logo-light.svg'},{'revision':'7e81da785c63e75650101db6c5d7560e','url':'/ohif-logo.svg'},{'revision':'eadf8bf1d85032a029e2c0df4b8938b0','url':'/oidc-client.min.js'},{'revision':'a1aef5311245f5864315443d12246c37','url':'/polyfill.min.js'},{'revision':'b1e488d9955b62bd2858874df11d5223','url':'/silent-refresh.html'}]);

// TODO: Cache API
// https://developers.google.com/web/fundamentals/instant-and-offline/web-storage/cache-api
// Store DICOMs?
// Clear Service Worker cache?
// navigator.storage.estimate().then(est => console.log(est)); (2GB?)
/******/ })()
;