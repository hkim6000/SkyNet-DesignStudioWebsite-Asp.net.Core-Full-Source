var LoginJs = (function () {

    var choosing = false;

    function el(id) {
        return document.getElementById(id);
    }

    function val(id) {
        var e = el(id);
        return e ? e.value : '';
    }

    function base() {
        return location.pathname.replace(/[^\/]*$/, '');
    }

    function submit() {
        var go = el('lg-go');
        go.disabled = true;
        if (choosing) {
            $ApiRequest('AdminLogin/SetPassword', JSON.stringify([{ key: 'login', vlu: val('lg-login').trim() }, { key: 'pass', vlu: val('lg-pass') }, { key: 'npass', vlu: val('lg-npass') }, { key: 'npass2', vlu: val('lg-npass2') }]));
        } else {
            $ApiRequest('AdminLogin/SignIn', JSON.stringify([{ key: 'login', vlu: val('lg-login').trim() }, { key: 'pass', vlu: val('lg-pass') }]));
        }
        return false;
    }

    function failed() {
        el('lg-go').disabled = false;
    }

    function choose() {
        choosing = true;
        el('lg-choose').hidden = false;
        el('lg-go').disabled = false;
        el('lg-go').textContent = 'Save and sign in';
        el('lg-npass').focus();
    }

    function signedIn(tok) {
        document.cookie = 'ds_tok=' + tok + '; path=' + base() + '; max-age=28800; samesite=lax';
        location.href = 'Admin';
    }

    function reveal() {
        var l = el('lg-login');
        if (l && !l.value) {
            l.focus();
        }
    }

    return {
        reveal: reveal,
        submit: submit,
        failed: failed,
        choose: choose,
        signedIn: signedIn
    };
})();

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', LoginJs.reveal);
} else {
    LoginJs.reveal();
}
