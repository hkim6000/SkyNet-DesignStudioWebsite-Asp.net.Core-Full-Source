var ProjectJs = (function () {

    var ttimer = null;

    function el(id) {
        return document.getElementById(id);
    }

    function val(id) {
        var e = el(id);
        return e ? (e.value || '').trim() : '';
    }

    function toast(msg) {
        var t = el('pd-toast');
        if (!t) {
            return;
        }
        t.textContent = msg;
        t.classList.add('pd-open');
        clearTimeout(ttimer);
        ttimer = setTimeout(function () {
            t.classList.remove('pd-open');
        }, 3400);
    }

    function openNav() {
        el('pd-nav').classList.add('pd-open');
        el('pd-scrim').classList.add('pd-open');
    }

    function closeNav() {
        el('pd-nav').classList.remove('pd-open');
        el('pd-scrim').classList.remove('pd-open');
    }

    var items = [];
    var at = 0;
    var back = null;
    var sx = null;

    function compare(input) {
        var v = Math.max(0, Math.min(100, parseInt(input.value, 10) || 0));
        var stage = input.parentNode;
        stage.querySelector('.pd-ba-before').style.clipPath = 'inset(0 ' + (100 - v) + '% 0 0)';
        stage.querySelector('.pd-ba-line').style.left = v + '%';
        stage.querySelector('.pd-ba-knob').style.left = v + '%';
    }

    function show(i) {
        if (!items.length) {
            return;
        }
        at = (i + items.length) % items.length;
        var it = items[at];
        var img = el('pd-lb-img');
        img.src = it.getAttribute('data-full');
        img.alt = it.getAttribute('data-alt') || '';
        el('pd-lb-cap').textContent = it.getAttribute('data-cap') || '';
        el('pd-lb-c').textContent = (at + 1) + ' / ' + items.length;
    }

    function open(btn) {
        var lb = el('pd-lb');
        items = Array.prototype.slice.call(document.querySelectorAll('.pd-lbitem'));
        back = btn;
        show(items.indexOf(btn));
        lb.hidden = false;
        document.body.style.overflow = 'hidden';
        lb.querySelector('.pd-lb-x').focus();
    }

    function close() {
        var lb = el('pd-lb');
        if (!lb || lb.hidden) {
            return;
        }
        lb.hidden = true;
        document.body.style.overflow = '';
        if (back && back.focus) {
            back.focus();
        }
    }

    function step(n) {
        show(at + n);
    }

    function reveal() {
        document.addEventListener('keydown', function (e) {
            var lb = el('pd-lb');
            if (e.key === 'Escape') {
                closeNav();
                close();
            } else if (lb && !lb.hidden && e.key === 'ArrowLeft') {
                step(-1);
            } else if (lb && !lb.hidden && e.key === 'ArrowRight') {
                step(1);
            } else if (lb && !lb.hidden && e.key === 'Tab') {
                var f = Array.prototype.slice.call(lb.querySelectorAll('button'));
                var i = f.indexOf(document.activeElement);
                e.preventDefault();
                f[(i + (e.shiftKey ? f.length - 1 : 1)) % f.length].focus();
            }
        });
        var lb = el('pd-lb');
        if (lb) {
            lb.addEventListener('mousedown', function (e) {
                if (e.target === lb) {
                    close();
                }
            });
            lb.addEventListener('touchstart', function (e) {
                sx = e.touches[0].clientX;
            }, { passive: true });
            lb.addEventListener('touchend', function (e) {
                var dx = sx === null ? 0 : e.changedTouches[0].clientX - sx;
                sx = null;
                if (Math.abs(dx) > 50) {
                    step(dx < 0 ? 1 : -1);
                }
            });
        }
    }

    return {
        reveal: reveal,
        openNav: openNav,
        closeNav: closeNav,
        toast: toast,
        compare: compare,
        open: open,
        close: close,
        step: step
    };
})();

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', ProjectJs.reveal);
} else {
    ProjectJs.reveal();
}
