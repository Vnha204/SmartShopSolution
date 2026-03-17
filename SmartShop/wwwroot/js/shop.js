/**
 * shop.js - client interaction for Shop filters
 */

(function () {
    'use strict';

    document.addEventListener('DOMContentLoaded', function () {
        var filterForm = document.getElementById('filterForm');
        if (!filterForm) return;

        // Submit form when any filter checkbox/radio changes
        filterForm.querySelectorAll('input[type="checkbox"], input[type="radio"]').forEach(function (el) {
            el.addEventListener('change', function () {
                // Delay a bit to let UI update
                setTimeout(function () {
                    // submit preserving existing query params like sortBy and page
                    filterForm.submit();
                }, 100);
            });
        });

        // Price inputs: submit on enter or blur
        var minInp = filterForm.querySelector('input[name="minPrice"]');
        var maxInp = filterForm.querySelector('input[name="maxPrice"]');
        [minInp, maxInp].forEach(function (el) {
            if (!el) return;
            el.addEventListener('keydown', function (e) {
                if (e.key === 'Enter') {
                    e.preventDefault();
                    filterForm.submit();
                }
            });
            el.addEventListener('blur', function () {
                filterForm.submit();
            });
        });

        // Clear filters buttons
        document.querySelectorAll('#clearFilters, #clearFiltersEmpty').forEach(function (btn) {
            if (!btn) return;
            btn.addEventListener('click', function (e) {
                e.preventDefault();
                filterForm.reset();
                // remove query params by navigating to shop index
                window.location.href = window.location.pathname;
            });
        });

        // sortBy select
        var sortBy = document.getElementById('sortBy');
        if (sortBy) {
            sortBy.addEventListener('change', function () {
                // Append sortBy to current query and reload
                var params = new URLSearchParams(window.location.search);
                params.set('sortBy', sortBy.value);
                window.location.search = params.toString();
            });
        }

        // Top filter bar dropdown items (if present) - they mirror checkboxes in form using data attributes
        document.querySelectorAll('[data-shop-filter]').forEach(function (el) {
            el.addEventListener('click', function (e) {
                e.preventDefault();
                var inputSelector = el.getAttribute('data-target-input');
                if (!inputSelector) return;
                var input = document.querySelector(inputSelector);
                if (!input) return;
                // toggle checkbox/radio
                if (input.type === 'checkbox') input.checked = !input.checked;
                else input.checked = true;
                filterForm.submit();
            });
        });
    });
})();