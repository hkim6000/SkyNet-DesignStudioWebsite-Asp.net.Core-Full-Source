var StudioJs = (function () {

    var ttimer = null;

    function el(id) {
        return document.getElementById(id);
    }

    function val(id) {
        var e = el(id);
        return e ? (e.value || '').trim() : '';
    }

    function toast(msg) {
        var t = el('su-toast');
        if (!t) {
            return;
        }
        t.textContent = msg;
        t.classList.add('su-open');
        clearTimeout(ttimer);
        ttimer = setTimeout(function () {
            t.classList.remove('su-open');
        }, 3400);
    }

    function openNav() {
        el('su-nav').classList.add('su-open');
        el('su-scrim').classList.add('su-open');
    }

    function closeNav() {
        el('su-nav').classList.remove('su-open');
        el('su-scrim').classList.remove('su-open');
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
        toast: toast
    };
})();

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', StudioJs.reveal);
} else {
    StudioJs.reveal();
}
