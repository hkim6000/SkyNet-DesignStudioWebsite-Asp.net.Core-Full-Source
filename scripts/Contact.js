var ContactJs = (function () {

    var ttimer = null;

    function el(id) {
        return document.getElementById(id);
    }

    function val(id) {
        var e = el(id);
        return e ? (e.value || '').trim() : '';
    }

    function toast(msg) {
        var t = el('ct-toast');
        if (!t) {
            return;
        }
        t.textContent = msg;
        t.classList.add('ct-open');
        clearTimeout(ttimer);
        ttimer = setTimeout(function () {
            t.classList.remove('ct-open');
        }, 3400);
    }

    function openNav() {
        el('ct-nav').classList.add('ct-open');
        el('ct-scrim').classList.add('ct-open');
    }

    function closeNav() {
        el('ct-nav').classList.remove('ct-open');
        el('ct-scrim').classList.remove('ct-open');
    }

    var busy = false;

    function send() {
        if (busy) {
            return false;
        }
        busy = true;
        el('ct-submit').disabled = true;
        $ApiRequest('Contact/Send', JSON.stringify([
            { key: 'name', vlu: val('ct-name') },
            { key: 'email', vlu: val('ct-email') },
            { key: 'phone', vlu: val('ct-phone') },
            { key: 'kind', vlu: val('ct-kind') },
            { key: 'budget', vlu: val('ct-budget') },
            { key: 'timeline', vlu: val('ct-timeline') },
            { key: 'place', vlu: val('ct-place') },
            { key: 'message', vlu: val('ct-message') },
            { key: 'hp', vlu: val('ct-hp') },
            { key: 'agree', vlu: el('ct-agree').checked ? '1' : '0' }
        ]));
        return false;
    }

    function failed() {
        busy = false;
        el('ct-submit').disabled = false;
        var first = document.querySelector('#ct-form .ct-err:not(:empty)');
        if (first) {
            var f = first.parentNode.querySelector('input, select, textarea');
            if (f) {
                f.focus();
            }
        }
    }

    function sent() {
        busy = false;
        el('ct-submit').disabled = false;
        el('ct-form').reset();
        el('ct-form').hidden = true;
        var d = el('ct-done');
        d.hidden = false;
        d.scrollIntoView({ block: 'center', behavior: 'smooth' });
        toast('Inquiry sent');
    }

    function again() {
        el('ct-done').hidden = true;
        el('ct-form').hidden = false;
        el('ct-name').focus();
    }

    function reveal() {
        document.addEventListener('keydown', function (e) {
            if (e.key === 'Escape') {
                closeNav();
            }
        });
        document.querySelectorAll('#ct-form input, #ct-form select, #ct-form textarea').forEach(function (n) {
            n.addEventListener('input', function () {
                var e = el('ct-e-' + n.id.replace('ct-', ''));
                if (e) {
                    e.textContent = '';
                }
            });
        });
    }

    return {
        reveal: reveal,
        openNav: openNav,
        closeNav: closeNav,
        toast: toast,
        send: send,
        failed: failed,
        sent: sent,
        again: again
    };
})();

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', ContactJs.reveal);
} else {
    ContactJs.reveal();
}
