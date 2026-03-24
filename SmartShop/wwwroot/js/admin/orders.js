// Admin orders — client-side tab filtering by status
(function () {
    'use strict';

    document.addEventListener('DOMContentLoaded', function () {
        var tabs = document.querySelectorAll('[data-order-filter]');
        var rows = document.querySelectorAll('#ordersTable tbody tr[data-status]');

        tabs.forEach(function (tab) {
            tab.addEventListener('click', function (e) {
                e.preventDefault();
                var filter = this.dataset.orderFilter;

                // Active tab
                tabs.forEach(function (t) { t.classList.remove('active'); });
                this.classList.add('active');

                // Show/hide rows
                rows.forEach(function (row) {
                    row.style.display = (filter === 'all' || row.dataset.status === filter) ? '' : 'none';
                });
            });
        });
    });
}());
