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
            window.location.href = '?filter=' + encodeURIComponent(filter);
        })
        .finally(function () {
            gridContainer.style.opacity = '';
            gridContainer.style.pointerEvents = '';
        });
    }

    filterLinks.forEach(function (link) {
        link.addEventListener('click', function (e) {
            e.preventDefault();
            var filter = link.dataset.filter;
            loadGrid(filter, true);
        });
    });

    window.addEventListener('popstate', function (e) {
        var filter = (e.state && e.state.filter) ? e.state.filter : 'all';
        loadGrid(filter, false);
    });

    (function () {
        var url = new URL(window.location.href);
        var currentFilter = url.searchParams.get('filter') || 'all';
        history.replaceState({ filter: currentFilter }, '', window.location.href);
    }());

})();
