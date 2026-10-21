var AdminTeamJs = (function () {
    var ttimer = null;
    var askBox = null, askDone = null, askCheck = null, askBack = null;
    var dirty = false;

    function el(id) {
        return document.getElementById(id);
    }

    function val(id) {
        var e = el(id);
        return e ? (e.value || '').trim() : '';
    }

    function esc(s) {
        return String(s == null ? '' : s).replace(/[&<>"']/g, function (c) {
            return { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c];
        });
    }

    function base() {
        return location.pathname.replace(/[^\/]*$/, '');
    }

    function getCookie(name) {
        var m = document.cookie.match(new RegExp('(?:^|; )' + name + '=([^;]*)'));
        return m ? decodeURIComponent(m[1]) : '';
    }

    function api(method, list) {
        list = list || [];
        list.push({ key: 'tok', vlu: getCookie('ds_tok') });
        $ApiRequest('AdminTeam/' + method, JSON.stringify(list));
    }

    function expired() {
        document.cookie = 'ds_tok=; path=' + base() + '; max-age=0';
        location.href = 'AdminLogin';
    }

    function signout() {
        api('SignOut');
        document.cookie = 'ds_tok=; path=' + base() + '; max-age=0';
        setTimeout(function () {
            location.href = 'AdminLogin';
        }, 150);
    }

    function toast(msg) {
        var t = el('tm-toast');
        t.textContent = msg;
        t.classList.add('tm-open');
        clearTimeout(ttimer);
        ttimer = setTimeout(function () {
            t.classList.remove('tm-open');
        }, 3400);
    }

    function openMenu() {
        el('tm-rail').classList.add('tm-open');
        el('tm-scrim').classList.add('tm-open');
    }

    function closeMenu() {
        el('tm-rail').classList.remove('tm-open');
        el('tm-scrim').classList.remove('tm-open');
    }

    function openModal(title) {
        var m = el('tm-modal');
        el('tm-mtitle').textContent = title || '';
        dirty = false;
        m.classList.add('tm-open');
        m.setAttribute('aria-hidden', 'false');
        m.scrollTop = 0;
    }

    function shut() {
        var m = el('tm-modal');
        if (m) {
            m.classList.remove('tm-open');
            m.setAttribute('aria-hidden', 'true');
        }
        dirty = false;
    }

    function closeModal() {
        var m = el('tm-modal');
        if (!m || !m.classList.contains('tm-open')) {
            return;
        }
        if (dirty) {
            ask({ title: 'Leave without saving?', text: 'Your changes will be lost.', ok: 'Leave', no: 'Keep editing', danger: true }, shut);
            return;
        }
        shut();
    }

    function markDirty() {
        dirty = true;
    }

    function clean() {
        dirty = false;
    }

    function askParts() {
        return { h: askBox.querySelector('h2'), p: askBox.querySelector('p'), i: askBox.querySelector('input'), e: askBox.querySelector('em'), b: askBox.querySelectorAll('button') };
    }

    function askClose(ok) {
        if (!askBox || !askBox.classList.contains('tm-open')) {
            return;
        }
        var x = askParts(), f = askDone, v = x.i.value.trim();
        askBox.classList.remove('tm-open');
        askDone = null;
        askCheck = null;
        if (askBack && askBack.focus && document.body.contains(askBack)) {
            askBack.focus();
        }
        askBack = null;
        if (ok && f) {
            f(v);
        }
    }

    function askOk() {
        var x = askParts();
        var m = askCheck ? askCheck(x.i.value.trim()) : '';
        if (m) {
            x.e.textContent = m;
            x.i.focus();
            return;
        }
        askClose(true);
    }

    function askBuild() {
        askBox = document.createElement('div');
        askBox.className = 'tm-ask';
        askBox.innerHTML = '<div class="tm-askbox" role="alertdialog" aria-modal="true"><h2></h2><p></p><input type="text" maxlength="300" autocomplete="off"><em class="tm-err" role="alert"></em><div class="tm-askacts"><button type="button" class="tm-btn tm-btn-ghost"></button><button type="button" class="tm-btn"></button></div></div>';
        document.getElementById('tm-site-root').appendChild(askBox);
        var x = askParts();
        x.b[0].addEventListener('click', function () { askClose(false); });
        x.b[1].addEventListener('click', askOk);
        x.i.addEventListener('input', function () { x.e.textContent = ''; });
        askBox.addEventListener('mousedown', function (e) { if (e.target === askBox) { askClose(false); } });
        window.addEventListener('keydown', function (e) {
            if (!askBox.classList.contains('tm-open')) {
                return;
            }
            if (e.key === 'Escape') {
                e.preventDefault();
                e.stopPropagation();
                askClose(false);
            } else if (e.key === 'Enter' && e.target !== x.b[0]) {
                e.preventDefault();
                e.stopPropagation();
                askOk();
            } else if (e.key === 'Tab') {
                var f = [x.i, x.b[0], x.b[1]].filter(function (n) { return n.offsetParent !== null; });
                var at = f.indexOf(document.activeElement);
                e.preventDefault();
                f[(at + (e.shiftKey ? f.length - 1 : 1)) % f.length].focus();
            }
        }, true);
    }

    function ask(o, done) {
        if (!askBox) {
            askBuild();
        }
        var x = askParts(), input = typeof o.value === 'string';
        askBack = document.activeElement;
        askDone = done;
        askCheck = o.check || null;
        x.h.textContent = o.title || '';
        x.p.textContent = o.text || '';
        x.e.textContent = '';
        x.b[0].textContent = o.no || 'Cancel';
        x.b[1].textContent = o.ok || 'OK';
        x.b[1].className = 'tm-btn' + (o.danger ? ' tm-askdel' : '');
        askBox.firstChild.classList.toggle('tm-askin', input);
        x.i.value = input ? o.value : '';
        x.i.placeholder = o.place || '';
        askBox.classList.add('tm-open');
        if (input) {
            x.i.focus();
        } else {
            x.b[o.danger ? 0 : 1].focus();
        }
    }

    function collect(root, extra) {
        var list = extra || [];
        document.querySelectorAll('#' + root + ' [data-k]').forEach(function (n) {
            if (n.type === 'checkbox') {
                list.push({ key: n.getAttribute('data-k'), vlu: n.checked ? '1' : '0' });
            } else if (n.type === 'password') {
                list.push({ key: n.getAttribute('data-k'), vlu: n.value });
            } else {
                list.push({ key: n.getAttribute('data-k'), vlu: (n.value || '').trim() });
            }
        });
        return list;
    }

    function baseKeys(on) {
        document.addEventListener('keydown', function (e) {
            if (e.key === 'Escape') {
                if (askBox && askBox.classList.contains('tm-open')) {
                    return;
                }
                if (on && on()) {
                    return;
                }
                closeMenu();
                closeModal();
            }
        });
        var m = el('tm-modal');
        if (m) {
            m.addEventListener('input', markDirty);
            m.addEventListener('change', markDirty);
            m.addEventListener('mousedown', function (e) {
                if (e.target === m) {
                    closeModal();
                }
            });
        }
    }

    var pkCfg = null, pkSel = [], pkBack = null, pkTimer = null;

    function pkIsOpen() {
        var d = el('tm-pk');
        return !!d && d.classList.contains('tm-open');
    }

    function pkOpen(cfg) {
        pkCfg = cfg;
        pkSel = (cfg.selected || []).slice();
        pkBack = document.activeElement;
        el('tm-pkt').textContent = cfg.title || 'Choose from the media library';
        el('tm-pkq').value = '';
        el('tm-pkmsg').textContent = '';
        el('tm-pkhint').textContent = cfg.hint || '';
        el('tm-pkuse').hidden = !cfg.multi;
        el('tm-pkfile').setAttribute('accept', 'image/jpeg,image/png,image/webp,image/gif' + (cfg.video ? ',video/mp4' : ''));
        var d = el('tm-pk');
        d.classList.add('tm-open');
        d.setAttribute('aria-hidden', 'false');
        pkList();
        setTimeout(function () {
            el('tm-pkq').focus();
        }, 30);
    }

    function pkList() {
        api('Pick', [{ key: 'pterm', vlu: val('tm-pkq') }, { key: 'pvid', vlu: pkCfg && pkCfg.video ? '1' : '0' }]);
    }

    function pkSearch() {
        clearTimeout(pkTimer);
        pkTimer = setTimeout(pkList, 300);
    }

    function pkShown() {
        document.querySelectorAll('#tm-pkgrid .tm-pki').forEach(function (t) {
            var at = -1;
            pkSel.forEach(function (s, i) {
                if (s.src === t.getAttribute('data-src')) {
                    at = i;
                }
            });
            t.classList.toggle('tm-sel', at >= 0);
            var n = t.querySelector('.tm-pkn');
            if (n) {
                n.remove();
            }
            if (at >= 0 && pkCfg && pkCfg.multi) {
                t.insertAdjacentHTML('beforeend', '<i class="tm-pkn">' + (at + 1) + '</i>');
            }
        });
        var u = el('tm-pkuse');
        if (u && pkCfg && pkCfg.multi) {
            u.textContent = 'Use ' + pkSel.length + (pkSel.length === 1 ? ' picture' : ' pictures');
        }
    }

    function pkClose() {
        var d = el('tm-pk');
        if (d) {
            d.classList.remove('tm-open');
            d.setAttribute('aria-hidden', 'true');
        }
        el('tm-pkfile').value = '';
        pkCfg = null;
        if (pkBack && pkBack.focus && document.body.contains(pkBack)) {
            pkBack.focus();
        }
    }

    function pkPick(b) {
        var it = { src: b.getAttribute('data-src'), thumb: b.getAttribute('data-thumb'), title: b.getAttribute('data-title'), kind: b.getAttribute('data-kind') };
        if (!pkCfg.multi) {
            var f = pkCfg.done;
            pkClose();
            f([it]);
            return;
        }
        var at = -1;
        pkSel.forEach(function (s, i) {
            if (s.src === it.src) {
                at = i;
            }
        });
        if (at >= 0) {
            pkSel.splice(at, 1);
        } else if (pkSel.length >= (pkCfg.max || 12)) {
            el('tm-pkmsg').textContent = 'You can choose up to ' + (pkCfg.max || 12) + ' pictures.';
        } else {
            pkSel.push(it);
        }
        pkShown();
    }

    function pkUse() {
        var f = pkCfg.done, list = pkSel;
        pkClose();
        f(list);
    }

    function pkUpload() {
        el('tm-pkmsg').textContent = 'Uploading.';
        api('Upload', [{ key: 'pvid', vlu: pkCfg && pkCfg.video ? '1' : '0' }]);
    }

    function pkUploaded(ok, msg) {
        el('tm-pkfile').value = '';
        el('tm-pkmsg').textContent = msg;
        if (ok) {
            el('tm-pkq').value = '';
        }
    }

    function pkWire() {
        var d = el('tm-pk');
        if (d) {
            d.addEventListener('mousedown', function (e) {
                if (e.target === d) {
                    pkClose();
                }
            });
        }
    }

    function picPaint(box, it) {
        box.setAttribute('data-src', it ? it.src : '');
        box.querySelector('.tm-picb').innerHTML = it ? '<img src="' + esc(it.thumb) + '" alt=""><span class="tm-picc">Change</span>' : '<span>Choose from the library</span>';
        box.querySelector('.tm-picx').hidden = !it;
        markDirty();
    }

    function picPick(btn) {
        var box = btn.closest('.tm-pic');
        pkOpen({ title: 'Choose a picture', multi: false, done: function (list) { picPaint(box, list[0]); } });
    }

    function picClear(btn) {
        picPaint(btn.closest('.tm-pic'), null);
    }

    function picVal(key) {
        var b = document.querySelector('[data-pic="' + key + '"]');
        return b ? b.getAttribute('data-src') || '' : '';
    }

    var pn = 1;
    var qtimer = null;

    function saveFilters() {
        return [{ key: 'fterm', vlu: val('tm-q') }, { key: 'fpn', vlu: String(pn) }];
    }

    function rows() {
        api('Rows', [{ key: 'term', vlu: val('tm-q') }, { key: 'pn', vlu: String(pn) }]);
    }

    function type() {
        clearTimeout(qtimer);
        pn = 1;
        qtimer = setTimeout(rows, 250);
    }

    function go(n) {
        pn = n;
        rows();
    }

    function edit(id) {
        api('Edit', [{ key: 'mid', vlu: id }]);
    }

    function opened(title) {
        openModal(title);
    }

    function save(id) {
        var list = collect('tm-form', [{ key: 'mid', vlu: id }, { key: 'photo', vlu: picVal('photo') }].concat(saveFilters()));
        el('tm-save').disabled = true;
        api('Save', list);
        return false;
    }

    function failed() {
        var b = el('tm-save');
        if (b) {
            b.disabled = false;
        }
    }

    function saved(created) {
        clean();
        shut();
        toast(created ? 'Person added' : 'Person saved');
    }

    function move(id, dir) {
        api('Move', [{ key: 'mid', vlu: id }, { key: 'dir', vlu: String(dir) }].concat(saveFilters()));
    }

    function drop(id, name) {
        ask({ title: 'Delete ' + name + '?', text: 'The person is removed from the Studio page. Their portrait stays in the media library.', ok: 'Delete person', no: 'Keep it', danger: true }, function () {
            api('Drop', [{ key: 'mid', vlu: id }].concat(saveFilters()));
        });
    }

    function dropped() {
        toast('Person deleted');
    }

    function reveal() {
        baseKeys(function () {
            if (pkIsOpen()) {
                pkClose();
                return true;
            }
            return false;
        });
        pkWire();
        api('Load');
    }

    return {
        reveal: reveal,
        expired: expired,
        signout: signout,
        toast: toast,
        openMenu: openMenu,
        closeMenu: closeMenu,
        openModal: openModal,
        closeModal: closeModal,
        shut: shut,
        clean: clean,
        pkClose: pkClose,
        pkSearch: pkSearch,
        pkPick: pkPick,
        pkUse: pkUse,
        pkUpload: pkUpload,
        pkUploaded: pkUploaded,
        pkShown: pkShown,
        pkIsOpen: pkIsOpen,
        pkWire: pkWire,
        picPick: picPick,
        picClear: picClear,
        picVal: picVal,
        type: type,
        go: go,
        edit: edit,
        opened: opened,
        save: save,
        failed: failed,
        saved: saved,
        move: move,
        drop: drop,
        dropped: dropped
    };
})();

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', AdminTeamJs.reveal);
} else {
    AdminTeamJs.reveal();
}
