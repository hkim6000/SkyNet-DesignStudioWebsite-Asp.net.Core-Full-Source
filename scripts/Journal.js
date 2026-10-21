var JournalJs = (function () {

    var ttimer = null;

    function el(id) {
        return document.getElementById(id);
    }

    function val(id) {
        var e = el(id);
        return e ? (e.value || '').trim() : '';
    }

    function toast(msg) {
        var t = el('jn-toast');
        if (!t) {
            return;
        }
        t.textContent = msg;
        t.classList.add('jn-open');
        clearTimeout(ttimer);
        ttimer = setTimeout(function () {
            t.classList.remove('jn-open');
        }, 3400);
    }

    function openNav() {
        el('jn-nav').classList.add('jn-open');
        el('jn-scrim').classList.add('jn-open');
    }

    function closeNav() {
        el('jn-nav').classList.remove('jn-open');
        el('jn-scrim').classList.remove('jn-open');
    }

    function cat(c) {
        document.querySelectorAll('#jn-chips .jn-chip').forEach(function (b) {
            var on = (b.getAttribute('data-cat') || '') === c;
            b.classList.toggle('jn-act', on);
            b.setAttribute('aria-pressed', on ? 'true' : 'false');
        });
        $ApiRequest('Journal/Filter', JSON.stringify([{ key: 'cat', vlu: c }]));
    }

    function filtered(c) {
        history.replaceState(null, '', 'Journal' + (c ? '?c=' + encodeURIComponent(c) : ''));
    }

    function reveal() {
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
        cat: cat,
        filtered: filtered
    };
})();

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', JournalJs.reveal);
} else {
    JournalJs.reveal();
}
