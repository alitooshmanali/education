/**
 * Global logo loader for any slow fetch/XHR across the site & platform.
 */
(function () {
  if (window.__asatirGlobalLoader) return;
  window.__asatirGlobalLoader = true;

  var pending = 0;
  var showTimer = null;
  var DELAY_MS = 180; // only show if request lasts longer than this

  function ensureDom() {
    var el = document.getElementById('site-loader');
    if (el) return el;
    el = document.createElement('div');
    el.id = 'site-loader';
    el.className = 'site-loader';
    el.hidden = true;
    el.setAttribute('aria-live', 'polite');
    el.innerHTML =
      '<div class="site-loader__card">' +
      '<img class="site-loader__logo" src="/site/img/logo.png" onerror="this.onerror=null;this.src=\'/site/img/logo.svg\'" alt="">' +
      '<p class="site-loader__text">در حال بارگذاری…</p>' +
      '</div>';
    document.body.appendChild(el);
    return el;
  }

  function setVisible(show, text) {
    var el = ensureDom();
    var t = el.querySelector('.site-loader__text');
    if (t && text) t.textContent = text;
    el.hidden = !show;
  }

  window.showSiteLoader = function (show, text) {
    if (show) {
      pending++;
      if (!showTimer) {
        showTimer = setTimeout(function () {
          showTimer = null;
          if (pending > 0) setVisible(true, text || 'در حال بارگذاری…');
        }, DELAY_MS);
      }
    } else {
      pending = Math.max(0, pending - 1);
      if (pending === 0) {
        if (showTimer) { clearTimeout(showTimer); showTimer = null; }
        setVisible(false);
      }
    }
  };

  // Force show/hide without counter (manual)
  window.showSiteLoaderForced = function (show, text) {
    if (showTimer) { clearTimeout(showTimer); showTimer = null; }
    if (!show) pending = 0;
    setVisible(!!show, text);
  };

  var origFetch = window.fetch.bind(window);
  window.fetch = function () {
    var args = arguments;
    var url = '';
    try {
      url = typeof args[0] === 'string' ? args[0] : (args[0] && args[0].url) || '';
    } catch (_) {}
    // Login has its own inline submit spinner; avoid covering the form with the global overlay.
    if (/\/api\/auth\/login(?:\?|$)/i.test(url)) {
      return origFetch.apply(window, args);
    }
    // skip tiny static assets
    if (/\.(png|jpg|jpeg|gif|svg|webp|css|woff2?)(\?|$)/i.test(url)) {
      return origFetch.apply(window, args);
    }
    window.showSiteLoader(true);
    return origFetch.apply(window, args).then(
      function (res) { window.showSiteLoader(false); return res; },
      function (err) { window.showSiteLoader(false); throw err; }
    );
  };

  // XHR (login form posts etc.)
  var XO = XMLHttpRequest.prototype.open;
  var XS = XMLHttpRequest.prototype.send;
  XMLHttpRequest.prototype.open = function () {
    this.__asatirTrack = true;
    return XO.apply(this, arguments);
  };
  XMLHttpRequest.prototype.send = function () {
    var xhr = this;
    if (xhr.__asatirTrack) {
      window.showSiteLoader(true);
      xhr.addEventListener('loadend', function () {
        window.showSiteLoader(false);
      });
    }
    return XS.apply(this, arguments);
  };
})();
