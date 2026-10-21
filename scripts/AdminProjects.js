var AdminProjectsJs = (function () {
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
        $ApiRequest('AdminProjects/' + method, JSON.stringify(list));
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
        var t = el('pr-toast');
        t.textContent = msg;
        t.classList.add('pr-open');
        clearTimeout(ttimer);
        ttimer = setTimeout(function () {
            t.classList.remove('pr-open');
        }, 3400);
    }

    function openMenu() {
        el('pr-rail').classList.add('pr-open');
        el('pr-scrim').classList.add('pr-open');
    }

    function closeMenu() {
        el('pr-rail').classList.remove('pr-open');
        el('pr-scrim').classList.remove('pr-open');
    }

    function openModal(title) {
        var m = el('pr-modal');
        el('pr-mtitle').textContent = title || '';
        dirty = false;
        m.classList.add('pr-open');
        m.setAttribute('aria-hidden', 'false');
        m.scrollTop = 0;
    }

    function shut() {
        var m = el('pr-modal');
        if (m) {
            m.classList.remove('pr-open');
            m.setAttribute('aria-hidden', 'true');
        }
        dirty = false;
    }

    function closeModal() {
        var m = el('pr-modal');
        if (!m || !m.classList.contains('pr-open')) {
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
        if (!askBox || !askBox.classList.contains('pr-open')) {
            return;
        }
        var x = askParts(), f = askDone, v = x.i.value.trim();
        askBox.classList.remove('pr-open');
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
        askBox.className = 'pr-ask';
        askBox.innerHTML = '<div class="pr-askbox" role="alertdialog" aria-modal="true"><h2></h2><p></p><input type="text" maxlength="300" autocomplete="off"><em class="pr-err" role="alert"></em><div class="pr-askacts"><button type="button" class="pr-btn pr-btn-ghost"></button><button type="button" class="pr-btn"></button></div></div>';
        document.getElementById('pr-site-root').appendChild(askBox);
        var x = askParts();
        x.b[0].addEventListener('click', function () { askClose(false); });
        x.b[1].addEventListener('click', askOk);
        x.i.addEventListener('input', function () { x.e.textContent = ''; });
        askBox.addEventListener('mousedown', function (e) { if (e.target === askBox) { askClose(false); } });
        window.addEventListener('keydown', function (e) {
            if (!askBox.classList.contains('pr-open')) {
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
        x.b[1].className = 'pr-btn' + (o.danger ? ' pr-askdel' : '');
        askBox.firstChild.classList.toggle('pr-askin', input);
        x.i.value = input ? o.value : '';
        x.i.placeholder = o.place || '';
        askBox.classList.add('pr-open');
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
                if (askBox && askBox.classList.contains('pr-open')) {
                    return;
                }
                if (on && on()) {
                    return;
                }
                closeMenu();
                closeModal();
            }
        });
        var m = el('pr-modal');
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
        var d = el('pr-pk');
        return !!d && d.classList.contains('pr-open');
    }

    function pkOpen(cfg) {
        pkCfg = cfg;
        pkSel = (cfg.selected || []).slice();
        pkBack = document.activeElement;
        el('pr-pkt').textContent = cfg.title || 'Choose from the media library';
        el('pr-pkq').value = '';
        el('pr-pkmsg').textContent = '';
        el('pr-pkhint').textContent = cfg.hint || '';
        el('pr-pkuse').hidden = !cfg.multi;
        el('pr-pkfile').setAttribute('accept', 'image/jpeg,image/png,image/webp,image/gif' + (cfg.video ? ',video/mp4' : ''));
        var d = el('pr-pk');
        d.classList.add('pr-open');
        d.setAttribute('aria-hidden', 'false');
        pkList();
        setTimeout(function () {
            el('pr-pkq').focus();
        }, 30);
    }

    function pkList() {
        api('Pick', [{ key: 'pterm', vlu: val('pr-pkq') }, { key: 'pvid', vlu: pkCfg && pkCfg.video ? '1' : '0' }]);
    }

    function pkSearch() {
        clearTimeout(pkTimer);
        pkTimer = setTimeout(pkList, 300);
    }

    function pkShown() {
        document.querySelectorAll('#pr-pkgrid .pr-pki').forEach(function (t) {
            var at = -1;
            pkSel.forEach(function (s, i) {
                if (s.src === t.getAttribute('data-src')) {
                    at = i;
                }
            });
            t.classList.toggle('pr-sel', at >= 0);
            var n = t.querySelector('.pr-pkn');
            if (n) {
                n.remove();
            }
            if (at >= 0 && pkCfg && pkCfg.multi) {
                t.insertAdjacentHTML('beforeend', '<i class="pr-pkn">' + (at + 1) + '</i>');
            }
        });
        var u = el('pr-pkuse');
        if (u && pkCfg && pkCfg.multi) {
            u.textContent = 'Use ' + pkSel.length + (pkSel.length === 1 ? ' picture' : ' pictures');
        }
    }

    function pkClose() {
        var d = el('pr-pk');
        if (d) {
            d.classList.remove('pr-open');
            d.setAttribute('aria-hidden', 'true');
        }
        el('pr-pkfile').value = '';
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
            el('pr-pkmsg').textContent = 'You can choose up to ' + (pkCfg.max || 12) + ' pictures.';
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
        el('pr-pkmsg').textContent = 'Uploading.';
        api('Upload', [{ key: 'pvid', vlu: pkCfg && pkCfg.video ? '1' : '0' }]);
    }

    function pkUploaded(ok, msg) {
        el('pr-pkfile').value = '';
        el('pr-pkmsg').textContent = msg;
        if (ok) {
            el('pr-pkq').value = '';
        }
    }

    function pkWire() {
        var d = el('pr-pk');
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
        box.querySelector('.pr-picb').innerHTML = it ? '<img src="' + esc(it.thumb) + '" alt=""><span class="pr-picc">Change</span>' : '<span>Choose from the library</span>';
        box.querySelector('.pr-picx').hidden = !it;
        markDirty();
    }

    function picPick(btn) {
        var box = btn.closest('.pr-pic');
        pkOpen({ title: 'Choose a picture', multi: false, done: function (list) { picPaint(box, list[0]); } });
    }

    function picClear(btn) {
        picPaint(btn.closest('.pr-pic'), null);
    }

    function picVal(key) {
        var b = document.querySelector('[data-pic="' + key + '"]');
        return b ? b.getAttribute('data-src') || '' : '';
    }

    var kindNow = '';
    var pn = 1;
    var ptimer = null;

    function filters() {
        return [{ key: 'kind', vlu: kindNow }, { key: 'term', vlu: val('pr-q') }, { key: 'pn', vlu: String(pn) }];
    }

    function saveFilters() {
        return [{ key: 'fkind', vlu: kindNow }, { key: 'fterm', vlu: val('pr-q') }, { key: 'fpn', vlu: String(pn) }];
    }

    function rows() {
        api('Rows', filters());
    }

    function kind(k) {
        kindNow = k;
        pn = 1;
        document.querySelectorAll('#pr-chips .pr-chip').forEach(function (b) {
            b.classList.toggle('pr-act', b.getAttribute('data-kind') === k);
        });
        rows();
    }

    function type() {
        clearTimeout(ptimer);
        pn = 1;
        ptimer = setTimeout(rows, 250);
    }

    function go(n) {
        pn = n;
        rows();
    }

    function edit(id) {
        api('Edit', [{ key: 'pid', vlu: id }]);
    }

    function opened(title) {
        openModal(title);
    }

    function galItems() {
        return [].slice.call(document.querySelectorAll('#pr-gal .pr-gi'));
    }

    function galHtml(it, cap) {
        return '<div class="pr-gi" data-src="' + esc(it.src) + '" data-thumb="' + esc(it.thumb) + '"><img src="' + esc(it.thumb) + '" alt=""><input type="text" class="pr-gcap" maxlength="80" value="' + esc(cap) + '" placeholder="Caption (optional)" aria-label="Caption"><div class="pr-gctl"><button type="button" aria-label="Move earlier" onclick="AdminProjectsJs.galMove(this,-1)">&larr;</button><button type="button" aria-label="Move later" onclick="AdminProjectsJs.galMove(this,1)">&rarr;</button><button type="button" aria-label="Remove picture" onclick="AdminProjectsJs.galDrop(this)">&times;</button></div></div>';
    }

    function galAdd() {
        var now = galItems().map(function (g) {
            return { src: g.getAttribute('data-src'), thumb: g.getAttribute('data-thumb'), title: '' };
        });
        var caps = {};
        galItems().forEach(function (g) {
            caps[g.getAttribute('data-src')] = g.querySelector('.pr-gcap').value;
        });
        pkOpen({ title: 'Choose gallery pictures', hint: 'Click pictures to add or remove them. The number shows the order. Up to 12.', multi: true, max: 12, selected: now, done: function (list) {
            el('pr-gal').innerHTML = list.map(function (it) {
                return galHtml(it, caps[it.src] || '');
            }).join('');
            markDirty();
        } });
    }

    function galMove(btn, dir) {
        var g = btn.closest('.pr-gi');
        var other = dir < 0 ? g.previousElementSibling : g.nextElementSibling;
        if (!other) {
            return;
        }
        if (dir < 0) {
            g.parentNode.insertBefore(g, other);
        } else {
            g.parentNode.insertBefore(other, g);
        }
        markDirty();
    }

    function galDrop(btn) {
        btn.closest('.pr-gi').remove();
        markDirty();
    }

    function pairAdd() {
        var box = el('pr-pairs');
        if (box.children.length >= 4) {
            toast('A project can have up to 4 pairs.');
            return;
        }
        box.insertAdjacentHTML('beforeend', el('pr-pairtpl').innerHTML);
        markDirty();
    }

    function pairDrop(btn) {
        btn.closest('.pr-pair').remove();
        markDirty();
    }

    function gallery() {
        return galItems().map(function (g) {
            return { media: g.getAttribute('data-src'), caption: g.querySelector('.pr-gcap').value.trim() };
        });
    }

    function pairs() {
        return [].slice.call(document.querySelectorAll('#pr-pairs .pr-pair')).map(function (p) {
            return {
                before: p.querySelector('[data-pic="before"]').getAttribute('data-src') || '',
                after: p.querySelector('[data-pic="after"]').getAttribute('data-src') || '',
                caption: p.querySelector('.pr-pcap').value.trim()
            };
        });
    }

    function save(id) {
        var list = collect('pr-form', [{ key: 'pid', vlu: id }, { key: 'cover', vlu: picVal('cover') }, { key: 'gallery', vlu: JSON.stringify(gallery()) }, { key: 'pairs', vlu: JSON.stringify(pairs()) }]);
        list.push.apply(list, saveFilters());
        el('pr-save').disabled = true;
        api('Save', list);
        return false;
    }

    function failed() {
        var b = el('pr-save');
        if (b) {
            b.disabled = false;
        }
        var first = document.querySelector('#pr-form .pr-err:not(:empty)');
        if (first && first.scrollIntoView) {
            first.scrollIntoView({ block: 'center', behavior: 'smooth' });
        }
    }

    function saved(created) {
        clean();
        shut();
        toast(created ? 'Project added' : 'Project saved');
    }

    function move(id, dir) {
        api('Move', [{ key: 'pid', vlu: id }, { key: 'dir', vlu: String(dir) }, { key: 'pn', vlu: String(pn) }]);
    }

    function drop(id, name) {
        ask({ title: 'Delete ' + name + '?', text: 'The project is removed from the website. Its pictures stay in the media library.', ok: 'Delete project', no: 'Keep it', danger: true }, function () {
            var list = saveFilters();
            list.push({ key: 'pid', vlu: id });
            api('Drop', list);
        });
    }

    function dropped() {
        toast('Project deleted');
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
        if (new URLSearchParams(location.search).get('new')) {
            setTimeout(function () {
                edit('');
            }, 400);
        }
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
        kind: kind,
        type: type,
        go: go,
        edit: edit,
        opened: opened,
        galAdd: galAdd,
        galMove: galMove,
        galDrop: galDrop,
        pairAdd: pairAdd,
        pairDrop: pairDrop,
        save: save,
        failed: failed,
        saved: saved,
        move: move,
        drop: drop,
        dropped: dropped
    };
})();

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', AdminProjectsJs.reveal);
} else {
    AdminProjectsJs.reveal();
}
