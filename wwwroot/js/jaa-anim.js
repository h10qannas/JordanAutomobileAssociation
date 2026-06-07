/* jaa-anim.js — JAA background animations & scroll-reveal
   Adds:
   1. Global fixed canvas: gradient orbs + floating particle network
   2. Per-page canvas inside .auth-page / .shop-auth-page
   3. IntersectionObserver scroll-reveal for all card elements
*/
(function () {
  'use strict';

  if (window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches) return;

  /* ─── Color tokens ────────────────────────────────────────── */
  var R = 'rgba(230,57,70,',
      B = 'rgba(88,166,255,',
      P = 'rgba(167,139,250,',
      W = 'rgba(240,246,252,';

  /* ─── Build a canvas painter ──────────────────────────────── */
  function makeCanvas(el, fixed, orbScale, dotCount, connect) {
    var cv  = document.createElement('canvas');
    cv.setAttribute('aria-hidden', 'true');
    cv.style.cssText = fixed
      ? 'position:fixed;top:0;left:0;width:100%;height:100%;pointer-events:none;z-index:0;'
      : 'position:absolute;top:0;left:0;width:100%;height:100%;pointer-events:none;z-index:0;';

    el.insertBefore(cv, el.firstChild);

    var ctx = cv.getContext('2d');
    var W2, H2, orbs = [], dots = [];
    var raf;

    function resize() {
      if (fixed) {
        W2 = cv.width  = window.innerWidth;
        H2 = cv.height = window.innerHeight;
      } else {
        W2 = cv.width  = el.offsetWidth  || 800;
        H2 = cv.height = el.offsetHeight || 600;
      }
      buildOrbs();
    }

    function buildOrbs() {
      var s = Math.max(W2, H2) * orbScale;
      orbs = [
        { x: W2 * .82, y: H2 * .12, r: s * .9,  col: R, a: .08, vx: -.16, vy:  .13 },
        { x: W2 * .12, y: H2 * .82, r: s * .8,  col: B, a: .05, vx:  .13, vy: -.16 },
        { x: W2 * .52, y: H2 * .48, r: s * .65, col: P, a: .03, vx:  .11, vy:  .11 },
        { x: W2 * .22, y: H2 * .22, r: s * .45, col: R, a: .04, vx: -.09, vy: -.09 },
      ];
    }

    function buildDots() {
      dots = [];
      for (var i = 0; i < dotCount; i++) {
        dots.push({
          x:  Math.random() * W2,
          y:  Math.random() * H2,
          r:  Math.random() * 1.1 + .35,
          vx: (Math.random() - .5) * .22,
          vy: (Math.random() - .5) * .22,
          o:  Math.random() * .18 + .05,
        });
      }
    }

    function tick() {
      ctx.clearRect(0, 0, W2, H2);

      /* orbs */
      for (var i = 0; i < orbs.length; i++) {
        var o = orbs[i];
        o.x += o.vx; o.y += o.vy;
        if (o.x < -o.r * .4 || o.x > W2 + o.r * .4) o.vx *= -1;
        if (o.y < -o.r * .4 || o.y > H2 + o.r * .4) o.vy *= -1;
        var g = ctx.createRadialGradient(o.x, o.y, 0, o.x, o.y, o.r);
        g.addColorStop(0, o.col + o.a + ')');
        g.addColorStop(1, o.col + '0)');
        ctx.beginPath();
        ctx.arc(o.x, o.y, o.r, 0, Math.PI * 2);
        ctx.fillStyle = g;
        ctx.fill();
      }

      /* particle connections */
      if (connect) {
        ctx.lineWidth = .5;
        for (var a = 0; a < dots.length; a++) {
          for (var b = a + 1; b < dots.length; b++) {
            var dx = dots[a].x - dots[b].x,
                dy = dots[a].y - dots[b].y,
                dist = Math.sqrt(dx * dx + dy * dy);
            if (dist < 130) {
              ctx.beginPath();
              ctx.moveTo(dots[a].x, dots[a].y);
              ctx.lineTo(dots[b].x, dots[b].y);
              ctx.strokeStyle = W + (.04 * (1 - dist / 130)) + ')';
              ctx.stroke();
            }
          }
        }
      }

      /* dots */
      for (var j = 0; j < dots.length; j++) {
        var d = dots[j];
        d.x = (d.x + d.vx + W2) % W2;
        d.y = (d.y + d.vy + H2) % H2;
        ctx.beginPath();
        ctx.arc(d.x, d.y, d.r, 0, Math.PI * 2);
        ctx.fillStyle = W + d.o + ')';
        ctx.fill();
      }

      raf = requestAnimationFrame(tick);
    }

    resize();
    buildDots();

    if (fixed) {
      window.addEventListener('resize', resize);
    } else {
      try { new ResizeObserver(resize).observe(el); } catch (e) { /* ignore */ }
    }

    tick();

    return { canvas: cv, stop: function () { cancelAnimationFrame(raf); } };
  }

  /* ─── 1. Global background canvas ────────────────────────── */
  makeCanvas(document.body, true, .44, 65, true);

  /* ─── 2. Per-auth-page canvas ─────────────────────────────── */
  function initAuthCanvases() {
    var pages = document.querySelectorAll('.auth-page, .shop-auth-page');
    for (var i = 0; i < pages.length; i++) {
      var p = pages[i];
      if (getComputedStyle(p).position === 'static') p.style.position = 'relative';
      /* Ensure the card wrap is above the canvas */
      var wrap = p.querySelector('.auth-wrap, .shop-wrap');
      if (wrap) wrap.style.zIndex = '1';
      makeCanvas(p, false, .5, 40, false);
    }
  }

  /* ─── 3. Scroll-reveal ────────────────────────────────────── */
  function initReveal() {
    if (!window.IntersectionObserver) return;

    var SELECTORS = [
      '.feature-card', '.step-card', '.serve-card', '.testimonial-card',
      '.about-card',   '.hero-card', '.card-jaa',   '.stat-card',
      '.shop-card',    '.auth-card', '.sl-feature',  '.sl-stat',
    ].join(',');

    var obs = new IntersectionObserver(function (entries) {
      entries.forEach(function (e) {
        if (!e.isIntersecting) return;
        var delay = parseInt(e.target.dataset.jaaDelay || 0, 10);
        setTimeout(function () {
          e.target.classList.add('jaa-in');
        }, delay);
        obs.unobserve(e.target);
      });
    }, { threshold: 0.1 });

    var els = document.querySelectorAll(SELECTORS);
    els.forEach(function (el) {
      /* stagger siblings inside the same parent */
      var siblings = el.parentElement
        ? Array.from(el.parentElement.children).filter(function (c) {
            return c.tagName === el.tagName && c.className === el.className;
          })
        : [];
      var idx = siblings.indexOf(el);
      el.dataset.jaaDelay = Math.max(0, idx) * 75;
      el.classList.add('jaa-reveal');
      obs.observe(el);
    });
  }

  /* ─── Run ─────────────────────────────────────────────────── */
  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', function () {
      initAuthCanvases();
      initReveal();
    });
  } else {
    initAuthCanvases();
    initReveal();
  }

})();
