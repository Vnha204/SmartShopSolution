(function () {
    'use strict';

    var filterBar  = document.getElementById('glasses-filter-bar');
    if (!filterBar) return;

    var filterLinks = filterBar.querySelectorAll('a[data-filter]');
    var gridContainer = document.querySelector('.glasses-cards-container');

    function loadGrid(filter, pushState) {
        if (!gridContainer) return;

        // Show loading state
        gridContainer.style.opacity = '0.5';
        gridContainer.style.pointerEvents = 'none';

        fetch('/Customers/Glasses/Grid?filter=' + encodeURIComponent(filter), {
            headers: { 'X-Requested-With': 'XMLHttpRequest' }
        })
        .then(function (r) {
            if (!r.ok) throw new Error('Network error');
            return r.text();
        })
        .then(function (html) {
            gridContainer.innerHTML = html;

            // Update active filter button
            filterLinks.forEach(function (link) {
                if (link.dataset.filter === filter) {
                    link.classList.add('active');
                } else {
                    link.classList.remove('active');
                }
            });

            // Sync URL
            if (pushState) {
                var url = new URL(window.location.href);
                url.searchParams.set('filter', filter);
                history.pushState({ filter: filter }, '', url.toString());
            }
        })
        .catch(function () {
            // Fallback: full page navigation
            window.location.href = '?filter=' + encodeURIComponent(filter);
        })
        .finally(function () {
            gridContainer.style.opacity = '';
            gridContainer.style.pointerEvents = '';
        });
    }

    // Intercept filter link clicks
    filterLinks.forEach(function (link) {
        link.addEventListener('click', function (e) {
            e.preventDefault();
            var filter = link.dataset.filter;
            loadGrid(filter, true);
        });
    });

    // Handle browser back/forward
    window.addEventListener('popstate', function (e) {
        var filter = (e.state && e.state.filter) ? e.state.filter : 'all';
        loadGrid(filter, false);
    });

    // Set initial history state so popstate works on first back
    (function () {
        var url = new URL(window.location.href);
        var currentFilter = url.searchParams.get('filter') || 'all';
        history.replaceState({ filter: currentFilter }, '', window.location.href);
    }());

})();
