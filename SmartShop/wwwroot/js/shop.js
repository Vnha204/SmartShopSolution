/**
 * shop.js — client interaction for Shop filters
 *
 * A) Duplication fix: shop.js is now loaded ONLY via @section Scripts in Index.cshtml.
 *    The duplicate <script> in _Layout.cshtml has been removed.
 *    Additionally, renderCatLevel() has a stale-element guard before appending.
 *
 * B) AJAX filtering: all filter changes (category, attribute, price, keyword, sort,
 *    pagination) update products via fetch() without a full page reload.
 *    - ShopController returns PartialView("_ShopAjaxPartial") when partial=1.
 *    - JS replaces #sidebarAttrFilters and #shopResults from the parsed response.
 *    - URL is kept in sync via history.pushState / popstate.
 */

(function () {
    'use strict';

    var CAT_API   = '/Customers/api/categories';
    var SHOP_PATH = '/Customers/Shop';

    var _maxCatDepth = 0;

    // ── DOMContentLoaded init ─────────────────────────────────────────────────

    document.addEventListener('DOMContentLoaded', function () {
        var filterForm = document.getElementById('filterForm');
        if (!filterForm) return;   // not on shop page

        // Stamp current URL into history so popstate can restore it
        history.replaceState({}, '', window.location.href);

        // ── Category drill-down init ─────────────────────────────────────────
        var selectedInput = document.getElementById('selectedCategoryId');
        var existingCatId = selectedInput && selectedInput.value
            ? parseInt(selectedInput.value, 10)
            : null;

        if (existingCatId) {
            preloadCategoryChain(existingCatId);
        } else {
            renderCatLevel(null, 0);
        }

        // ── Attribute checkboxes & price-range radios (delegation on form) ───
        filterForm.addEventListener('change', function (e) {
            var el = e.target;
            if (el.type === 'checkbox' && el.name === 'attributeValueIds') {
                submitAjax();
            }
            if (el.type === 'radio' && el.name === 'priceRange') {
                applyPricePreset(el.value);
                submitAjax();
            }
        });

        // ── Price text inputs: submit on Enter / blur ────────────────────────
        var minInp = filterForm.querySelector('input[name="minPrice"]');
        var maxInp = filterForm.querySelector('input[name="maxPrice"]');
        [minInp, maxInp].forEach(function (el) {
            if (!el) return;
            el.addEventListener('keydown', function (e) {
                if (e.key === 'Enter') { e.preventDefault(); submitAjax(); }
            });
            el.addEventListener('blur', function () { submitAjax(); });
        });

        // ── Keyword input: submit on Enter ───────────────────────────────────
        var kwInp = filterForm.querySelector('input[name="keyword"]');
        if (kwInp) {
            kwInp.addEventListener('keydown', function (e) {
                if (e.key === 'Enter') { e.preventDefault(); submitAjax(); }
            });
        }

        // ── "Áp dụng bộ lọc" submit button ──────────────────────────────────
        filterForm.addEventListener('submit', function (e) {
            e.preventDefault();
            submitAjax();
        });

        // ── SortBy (inside #shopResults — may be replaced; use delegation) ───
        document.addEventListener('change', function (e) {
            if (e.target.id === 'sortBy') {
                submitAjax();
            }
        });

        // ── Pagination clicks (inside #shopResults — use delegation) ─────────
        document.addEventListener('click', function (e) {
            var link = e.target.closest('.page-link[data-page]');
            if (link) {
                e.preventDefault();
                var page = link.dataset.page;
                if (page) submitAjax({ page: page });
            }
        });

        // ── Clear ALL filters ────────────────────────────────────────────────
        document.addEventListener('click', function (e) {
            var btn = e.target.closest('#clearFilters, #clearFiltersEmpty');
            if (!btn) return;
            e.preventDefault();
            filterForm.reset();
            var si = document.getElementById('selectedCategoryId');
            if (si) si.value = '';
            removeCatDepthsFrom(0);
            renderCatLevel(null, 0);
            history.pushState({}, '', window.location.pathname);
            submitAjax();
        });

        // ── Clear category (server-rendered button; shown on initial full load) ──
        document.addEventListener('click', function (e) {
            var btn = e.target.closest('#clearCategory');
            if (!btn) return;
            e.preventDefault();
            var si = document.getElementById('selectedCategoryId');
            if (si) si.value = '';
            var sb = document.getElementById('serverBreadcrumb');
            if (sb) sb.style.display = 'none';
            btn.style.display = 'none';
            removeCatDepthsFrom(0);
            renderCatLevel(null, 0);
            submitAjax();
        });

        // ── Popstate: browser back / forward ────────────────────────────────
        window.addEventListener('popstate', function () {
            loadFromUrl(window.location.search);
        });
    });

    // =========================================================================
    // AJAX helpers
    // =========================================================================

    /** Collect current form state into URLSearchParams. */
    function buildParams(overrides) {
        var params = new URLSearchParams();

        var si = document.getElementById('selectedCategoryId');
        if (si && si.value) params.set('selectedCategoryId', si.value);

        var filterForm = document.getElementById('filterForm');
        if (filterForm) {
            var minInp = filterForm.querySelector('input[name="minPrice"]');
            var maxInp = filterForm.querySelector('input[name="maxPrice"]');
            if (minInp && minInp.value !== '') params.set('minPrice', minInp.value);
            if (maxInp && maxInp.value !== '') params.set('maxPrice', maxInp.value);

            var kw = filterForm.querySelector('input[name="keyword"]');
            if (kw && kw.value) params.set('keyword', kw.value);

            filterForm.querySelectorAll('input[name="attributeValueIds"]:checked')
                .forEach(function (cb) { params.append('attributeValueIds', cb.value); });
        }

        // sortBy lives inside #shopResults (replaced by AJAX) — query live DOM
        var sortEl = document.getElementById('sortBy');
        if (sortEl && sortEl.value) params.set('sortBy', sortEl.value);

        params.set('page', '1');

        if (overrides) {
            Object.keys(overrides).forEach(function (k) { params.set(k, overrides[k]); });
        }

        return params;
    }

    /** Fetch the partial and swap #sidebarAttrFilters + #shopResults. */
    function submitAjax(overrides) {
        var params   = buildParams(overrides);
        var queryStr = params.toString();

        fetch(SHOP_PATH + '?' + queryStr + '&partial=1', {
            headers: { 'X-Requested-With': 'XMLHttpRequest' }
        })
        .then(function (r) { return r.text(); })
        .then(function (html) {
            applyPartialHtml(html);
            // Push URL without partial=1
            history.pushState({}, '', window.location.pathname + (queryStr ? '?' + queryStr : ''));
        })
        .catch(function (err) { console.error('Shop AJAX error', err); });
    }

    /** Restore state from URL (used by popstate). */
    function loadFromUrl(search) {
        var params = new URLSearchParams(search);

        // Restore sidebar inputs
        var si = document.getElementById('selectedCategoryId');
        if (si) si.value = params.get('selectedCategoryId') || '';

        var filterForm = document.getElementById('filterForm');
        if (filterForm) {
            var kw = filterForm.querySelector('input[name="keyword"]');
            if (kw) kw.value = params.get('keyword') || '';
            var minInp = filterForm.querySelector('input[name="minPrice"]');
            if (minInp) minInp.value = params.get('minPrice') || '';
            var maxInp = filterForm.querySelector('input[name="maxPrice"]');
            if (maxInp) maxInp.value = params.get('maxPrice') || '';
        }

        // Rebuild category chain
        var catId = params.get('selectedCategoryId');
        removeCatDepthsFrom(0);
        if (catId) {
            preloadCategoryChain(parseInt(catId, 10));
        } else {
            renderCatLevel(null, 0);
        }

        // Fetch and apply partial
        params.set('partial', '1');
        fetch(SHOP_PATH + '?' + params.toString(), {
            headers: { 'X-Requested-With': 'XMLHttpRequest' }
        })
        .then(function (r) { return r.text(); })
        .then(function (html) { applyPartialHtml(html); })
        .catch(function (err) { console.error('Popstate fetch error', err); });
    }

    /** Parse partial HTML and swap the two target zones. */
    function applyPartialHtml(html) {
        var doc = new DOMParser().parseFromString(html, 'text/html');

        var ajaxAttrs    = doc.getElementById('ajaxAttrFilters');
        var sidebarAttrs = document.getElementById('sidebarAttrFilters');
        if (ajaxAttrs && sidebarAttrs) {
            sidebarAttrs.innerHTML = ajaxAttrs.innerHTML;
        }

        var ajaxGrid    = doc.getElementById('ajaxProductGrid');
        var shopResults = document.getElementById('shopResults');
        if (ajaxGrid && shopResults) {
            shopResults.innerHTML = ajaxGrid.innerHTML;
        }
    }

    // =========================================================================
    // Category drill-down
    // =========================================================================

    function removeCatDepthsFrom(fromDepth) {
        for (var d = fromDepth; d <= _maxCatDepth + 1; d++) {
            var el = document.getElementById('cat-level-' + d);
            if (el) el.remove();
        }
        _maxCatDepth = Math.max(fromDepth - 1, 0);
    }

    function renderCatLevel(parentId, depth) {
        var url = parentId != null
            ? CAT_API + '/children?parentId=' + parentId
            : CAT_API + '/children';

        return fetch(url)
            .then(function (r) { return r.json(); })
            .then(function (cats) {
                if (!cats || !cats.length) return;

                var container = document.getElementById('categoryChainContainer');
                if (!container) return;

                var wrapper       = document.createElement('div');
                wrapper.id        = 'cat-level-' + depth;
                wrapper.className = 'mb-2';

                var sel           = document.createElement('select');
                sel.className     = 'form-select form-select-sm';
                sel.dataset.depth = depth;
                sel.addEventListener('change', function () { onCatLevelChange(this); });

                var ph           = document.createElement('option');
                ph.value         = '';
                ph.textContent   = depth === 0 ? '-- Chọn danh mục --' : '-- Danh mục con --';
                sel.appendChild(ph);

                cats.forEach(function (c) {
                    var opt                 = document.createElement('option');
                    opt.value               = c.id;
                    opt.textContent         = c.name;
                    opt.dataset.hasChildren = c.hasChildren ? '1' : '0';
                    sel.appendChild(opt);
                });

                wrapper.appendChild(sel);
                // Race-condition guard: remove any stale element at this depth
                var stale = document.getElementById(wrapper.id);
                if (stale) stale.remove();
                container.appendChild(wrapper);
                _maxCatDepth = Math.max(_maxCatDepth, depth);
            })
            .catch(function (err) { console.error('Category load error', err); });
    }

    function onCatLevelChange(sel) {
        var depth      = parseInt(sel.dataset.depth, 10);
        var categoryId = sel.value ? parseInt(sel.value, 10) : null;

        removeCatDepthsFrom(depth + 1);

        var selectedInput = document.getElementById('selectedCategoryId');
        if (!categoryId) {
            if (selectedInput) selectedInput.value = '';
            return;
        }

        if (selectedInput) selectedInput.value = categoryId;

        var opt         = sel.querySelector('option[value="' + categoryId + '"]');
        var hasChildren = opt && opt.dataset.hasChildren === '1';

        if (hasChildren) {
            renderCatLevel(categoryId, depth + 1);
        } else {
            // Leaf selected — AJAX update (no full page reload)
            submitAjax();
        }
    }

    function preloadCategoryChain(leafCategoryId) {
        fetch(CAT_API + '/breadcrumb?categoryId=' + leafCategoryId)
            .then(function (r) { return r.json(); })
            .then(function (breadcrumb) {
                if (!breadcrumb || !breadcrumb.length) {
                    renderCatLevel(null, 0);
                    return;
                }
                renderCatLevel(null, 0).then(function () {
                    return walkBreadcrumb(breadcrumb, 0);
                });
            })
            .catch(function () { renderCatLevel(null, 0); });
    }

    function walkBreadcrumb(breadcrumb, index) {
        if (index >= breadcrumb.length) return Promise.resolve();

        var item = breadcrumb[index];
        var sel  = document.querySelector('#cat-level-' + index + ' select');
        if (sel) sel.value = item.id;

        if (index < breadcrumb.length - 1) {
            return renderCatLevel(item.id, index + 1).then(function () {
                return walkBreadcrumb(breadcrumb, index + 1);
            });
        }
        return Promise.resolve();
    }

    // =========================================================================
    // Price preset helper
    // =========================================================================

    function applyPricePreset(value) {
        var filterForm = document.getElementById('filterForm');
        if (!filterForm) return;
        var minInp = filterForm.querySelector('input[name="minPrice"]');
        var maxInp = filterForm.querySelector('input[name="maxPrice"]');
        if (!minInp || !maxInp) return;

        if (value.endsWith('+')) {
            minInp.value = value.slice(0, -1);
            maxInp.value = '';
        } else {
            var parts    = value.split('-');
            minInp.value = parts[0] || '';
            maxInp.value = parts[1] || '';
        }
    }

})();
