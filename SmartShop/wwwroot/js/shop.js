(function () {
    'use strict';

    var CAT_API   = '/Customers/api/categories';
    var SHOP_PATH = '/Customers/Shop';

    var _maxCatDepth = 0;

    document.addEventListener('DOMContentLoaded', function () {
        var filterForm = document.getElementById('filterForm');
        if (!filterForm) return;

        history.replaceState({}, '', window.location.href);

        var selectedInput = document.getElementById('selectedCategoryId');
        var existingCatId = selectedInput && selectedInput.value
            ? parseInt(selectedInput.value, 10)
            : null;

        if (existingCatId) {
            preloadCategoryChain(existingCatId);
        } else {
            renderCatLevel(null, 0);
        }

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

        var minInp = filterForm.querySelector('input[name="minPrice"]');
        var maxInp = filterForm.querySelector('input[name="maxPrice"]');
        [minInp, maxInp].forEach(function (el) {
            if (!el) return;
            el.addEventListener('keydown', function (e) {
                if (e.key === 'Enter') { e.preventDefault(); submitAjax(); }
            });
            el.addEventListener('blur', function () { submitAjax(); });
        });

        var shopKwInp = document.getElementById('shopKeywordInput');
        var shopKwBtn = document.getElementById('shopSearchBtn');
        if (shopKwInp) {
            shopKwInp.addEventListener('keydown', function (e) {
                if (e.key === 'Enter') { e.preventDefault(); submitAjax(); }
            });
        }
        if (shopKwBtn) {
            shopKwBtn.addEventListener('click', function () { submitAjax(); });
        }

        filterForm.addEventListener('submit', function (e) {
            e.preventDefault();
            submitAjax();
        });

        document.addEventListener('change', function (e) {
            if (e.target.id === 'sortBy') {
                submitAjax();
            }
        });

        document.addEventListener('click', function (e) {
            var link = e.target.closest('.page-link[data-page]');
            if (link) {
                e.preventDefault();
                var page = link.dataset.page;
                if (page) submitAjax({ page: page });
            }
        });

        document.addEventListener('click', function (e) {
            var btn = e.target.closest('#clearFilters, #clearFiltersEmpty');
            if (!btn) return;
            e.preventDefault();
            filterForm.reset();
            var si = document.getElementById('selectedCategoryId');
            if (si) si.value = '';
            var shopKw = document.getElementById('shopKeywordInput');
            if (shopKw) shopKw.value = '';
            removeCatDepthsFrom(0);
            renderCatLevel(null, 0);
            history.pushState({}, '', window.location.pathname);
            submitAjax();
        });

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

        window.addEventListener('popstate', function () {
            loadFromUrl(window.location.search);
        });
    });
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

            var kw = document.getElementById('shopKeywordInput')
                  || filterForm.querySelector('input[name="keyword"]');
            if (kw && kw.value) params.set('keyword', kw.value);

            filterForm.querySelectorAll('input[name="attributeValueIds"]:checked')
                .forEach(function (cb) { params.append('attributeValueIds', cb.value); });
        }

        var sortEl = document.getElementById('sortBy');
        if (sortEl && sortEl.value) params.set('sortBy', sortEl.value);

        params.set('page', '1');

        if (overrides) {
            Object.keys(overrides).forEach(function (k) { params.set(k, overrides[k]); });
        }

        return params;
    }

    function submitAjax(overrides) {
        var params   = buildParams(overrides);
        var queryStr = params.toString();

        fetch(SHOP_PATH + '?' + queryStr + '&partial=1', {
            headers: { 'X-Requested-With': 'XMLHttpRequest' }
        })
        .then(function (r) { return r.text(); })
        .then(function (html) {
            applyPartialHtml(html);
            history.pushState({}, '', window.location.pathname + (queryStr ? '?' + queryStr : ''));
        })
        .catch(function (err) { console.error('Shop AJAX error', err); });
    }
    function loadFromUrl(search) {
        var params = new URLSearchParams(search);
        var si = document.getElementById('selectedCategoryId');
        if (si) si.value = params.get('selectedCategoryId') || '';

        var filterForm = document.getElementById('filterForm');
        if (filterForm) {
            var kw = document.getElementById('shopKeywordInput')
                  || filterForm.querySelector('input[name="keyword"]');
            if (kw) kw.value = params.get('keyword') || '';
            var minInp = filterForm.querySelector('input[name="minPrice"]');
            if (minInp) minInp.value = params.get('minPrice') || '';
            var maxInp = filterForm.querySelector('input[name="maxPrice"]');
            if (maxInp) maxInp.value = params.get('maxPrice') || '';
        }

        var catId = params.get('selectedCategoryId');
        removeCatDepthsFrom(0);
        if (catId) {
            preloadCategoryChain(parseInt(catId, 10));
        } else {
            renderCatLevel(null, 0);
        }

        params.set('partial', '1');
        fetch(SHOP_PATH + '?' + params.toString(), {
            headers: { 'X-Requested-With': 'XMLHttpRequest' }
        })
        .then(function (r) { return r.text(); })
        .then(function (html) { applyPartialHtml(html); })
        .catch(function (err) { console.error('Popstate fetch error', err); });
    }

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
    document.addEventListener('click', function (e) {
        var btn = e.target.closest('.add_cart');
        if (!btn) return;

        var productId = btn.dataset.productId;
        if (!productId) return;

        var meta = document.querySelector('meta[name="csrf-token"]');
        if (!meta) return;

        var originalHtml = btn.innerHTML;
        btn.disabled = true;
        btn.innerHTML = '<i class="fa fa-spinner fa-spin"></i>';

        fetch('/Customers/Cart/Add', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/x-www-form-urlencoded',
                'X-Requested-With': 'XMLHttpRequest',
                'RequestVerificationToken': meta.content
            },
            body: 'productId=' + encodeURIComponent(productId) + '&quantity=1'
        })
        .then(function (r) {
            if (r.redirected && r.url.indexOf('/auth/login') !== -1) {
                window.location.href = r.url;
                return null;
            }
            return r.ok ? r.json() : null;
        })
        .then(function (data) {
            if (!data) return;
            showCartToast(data.success, data.message || (data.success ? 'Đã thêm vào giỏ hàng!' : 'Có lỗi xảy ra.'));
            if (data.success) incrementCartBadge();
        })
        .catch(function () {
            showCartToast(false, 'Có lỗi xảy ra. Vui lòng thử lại.');
        })
        .finally(function () {
            btn.disabled = false;
            btn.innerHTML = originalHtml;
        });
    });

    function showCartToast(success, message) {
        var toastEl = document.getElementById('cartToast');
        var body    = document.getElementById('cartToastBody');
        if (!toastEl || !body) return;

        body.textContent = message;
        toastEl.className = 'toast align-items-center border-0 text-white '
            + (success ? 'bg-success' : 'bg-danger');

        bootstrap.Toast.getOrCreateInstance(toastEl, { delay: 2500 }).show();
    }

    function incrementCartBadge() {
        var badge = document.querySelector('.cart-nav-link .cart-badge');
        if (badge) {
            badge.textContent = parseInt(badge.textContent || '0', 10) + 1;
        } else {
            var link = document.querySelector('.cart-nav-link');
            if (link) {
                var span = document.createElement('span');
                span.className = 'cart-badge';
                span.textContent = '1';
                link.appendChild(span);
            }
        }
    }
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
