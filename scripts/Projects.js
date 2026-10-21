var ProjectsJs = (function () {

    var ttimer = null;

    function el(id) {
        return document.getElementById(id);
    }

    function val(id) {
        var e = el(id);
        return e ? (e.value || '').trim() : '';
    }

    function toast(msg) {
        var t = el('pj-toast');
        if (!t) {
            return;
        }
        t.textContent = msg;
        t.classList.add('pj-open');
        clearTimeout(ttimer);
        ttimer = setTimeout(function () {
            t.classList.remove('pj-open');
        }, 3400);
    }

    function openNav() {
        el('pj-nav').classList.add('pj-open');
        el('pj-scrim').classList.add('pj-open');
    }

    function closeNav() {
        el('pj-nav').classList.remove('pj-open');
        el('pj-scrim').classList.remove('pj-open');
    }

    var kindNow = '';
    var timer = null;

    function call(method, list) {
        $ApiRequest('Projects/' + method, JSON.stringify(list || []));
    }

    function filter() {
        call('Filter', [{ key: 'kind', vlu: kindNow }, { key: 'yr', vlu: val('pj-yr') || '0' }, { key: 'term', vlu: val('pj-term') }]);
    }

    function kind(k) {
        kindNow = k;
        document.querySelectorAll('#pj-chips .pj-chip').forEach(function (b) {
            var on = b.getAttribute('data-kind') === k;
            b.classList.toggle('pj-act', on);
            b.setAttribute('aria-pressed', on ? 'true' : 'false');
        });
        filter();
    }

    function change() {
        filter();
    }

    function type() {
        clearTimeout(timer);
        timer = setTimeout(filter, 250);
    }

    function filtered(k, yr, term) {
        var q = [];
        if (k) {
            q.push('t=' + encodeURIComponent(k));
        }
        if (yr) {
            q.push('y=' + yr);
        }
        if (term) {
            q.push('s=' + encodeURIComponent(term));
        }
        history.replaceState(null, '', 'Projects' + (q.length ? '?' + q.join('&') : ''));
    }

    function reveal() {
        var on = document.querySelector('#pj-chips .pj-act');
        kindNow = on ? on.getAttribute('data-kind') || '' : '';
        document.addEventListener('keydown', function (e) {
            if (e.key === 'Escape') {
                closeNav();
            }
        });
    }

    return {
        reveal: reveal,
        openNav: openNav,
        closeNav: closeNav,
        toast: toast,
        kind: kind,
        change: change,
        type: type,
        filtered: filtered
    };
})();

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', ProjectsJs.reveal);
} else {
    ProjectsJs.reveal();
}
