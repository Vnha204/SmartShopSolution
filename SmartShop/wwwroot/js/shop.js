/**
 * shop.js — client interaction for Shop filters
 * Handles: category drill-down, attribute checkboxes, price inputs, sort, clear.
 */

(function () {
    'use strict';

    // ── Constants ────────────────────────────────────────────────────────────
    var CAT_API      = '/Customers/api/categories';
    var _maxCatDepth = 0;

    document.addEventListener('DOMContentLoaded', function () {
        var filterForm = document.getElementById('filterForm');
        if (!filterForm) return;

        // ── Category drill-down ──────────────────────────────────────────────

        var selectedInput = document.getElementById('selectedCategoryId');
        var existingCatId = selectedInput && selectedInput.value
            ? parseInt(selectedInput.value)
            : null;

        if (existingCatId) {
            // Page loaded with a category already selected: re-expand the chain
            preloadCategoryChain(existingCatId);
        } else {
            renderCatLevel(null, 0);
        }

        // Clear category button (visible after server reload with a category set)
        var clearCatBtn = document.getElementById('clearCategory');
        if (clearCatBtn) {
            clearCatBtn.addEventListener('click', function () {
                selectedInput.value = '';
                // Hide server breadcrumb
                var sb = document.getElementById('serverBreadcrumb');
                if (sb) sb.style.display = 'none';
                clearCatBtn.style.display = 'none';
                removeCatDepthsFrom(0);
                renderCatLevel(null, 0);
                filterForm.submit();
            });
        }

        // ── Auto-submit on attribute checkbox change ─────────────────────────
        // (price inputs and newly rendered checkboxes are handled via delegation)
        filterForm.addEventListener('change', function (e) {
            var el = e.target;
            if (el.type === 'checkbox' && el.name === 'attributeValueIds') {
                filterForm.submit();
            }
            if (el.type === 'radio' && el.name === 'priceRange') {
                applyPricePreset(el.value);
                filterForm.submit();
            }
        });

        // ── Price inputs: submit on Enter / blur ─────────────────────────────
        var minInp = filterForm.querySelector('input[name="minPrice"]');
        var maxInp = filterForm.querySelector('input[name="maxPrice"]');
        [minInp, maxInp].forEach(function (el) {
            if (!el) return;
            el.addEventListener('keydown', function (e) {
                if (e.key === 'Enter') { e.preventDefault(); filterForm.submit(); }
            });
            el.addEventListener('blur', function () { filterForm.submit(); });
        });

        // ── Clear filters buttons ────────────────────────────────────────────
        document.querySelectorAll('#clearFilters, #clearFiltersEmpty').forEach(function (btn) {
            if (!btn) return;
            btn.addEventListener('click', function (e) {
                e.preventDefault();
                filterForm.reset();
                if (selectedInput) selectedInput.value = '';
                window.location.href = window.location.pathname;
            });
        });

        // ── SortBy select ────────────────────────────────────────────────────
        var sortByEl = document.getElementById('sortBy');
        if (sortByEl) {
            sortByEl.addEventListener('change', function () {
                var params = new URLSearchParams(window.location.search);
                params.set('sortBy', sortByEl.value);
                window.location.search = params.toString();
            });
        }
    });

    // =========================================================================
    // Category drill-down functions
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

                var placeholder       = document.createElement('option');
                placeholder.value     = '';
                placeholder.textContent = depth === 0 ? '-- Chọn danh mục --' : '-- Danh mục con --';
                sel.appendChild(placeholder);

                cats.forEach(function (c) {
                    var opt               = document.createElement('option');
                    opt.value             = c.id;
                    opt.textContent       = c.name;
                    opt.dataset.hasChildren = c.hasChildren ? '1' : '0';
                    sel.appendChild(opt);
                });

                wrapper.appendChild(sel);
                container.appendChild(wrapper);
                _maxCatDepth = Math.max(_maxCatDepth, depth);
            })
            .catch(function (err) { console.error('Category load error', err); });
    }

    function onCatLevelChange(sel) {
        var depth      = parseInt(sel.dataset.depth);
        var categoryId = sel.value ? parseInt(sel.value) : null;

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
            // Not a leaf: render children, but allow manual submit via button
            renderCatLevel(categoryId, depth + 1);
        } else {
            // Leaf: auto-submit so server can load matching attributes
            var filterForm = document.getElementById('filterForm');
            if (filterForm) filterForm.submit();
        }
    }

    // Restores the drill-down UI when a category is already selected (page reload).
    function preloadCategoryChain(leafCategoryId) {
        fetch(CAT_API + '/breadcrumb?categoryId=' + leafCategoryId)
            .then(function (r) { return r.json(); })
            .then(function (breadcrumb) {
                if (!breadcrumb || !breadcrumb.length) {
                    renderCatLevel(null, 0);
                    return;
                }

                // Render root level, then walk breadcrumb selecting each item
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
    // Price preset helper — maps "min-max" value to individual inputs
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
