/**
 * admin.js - Admin panel scripts
 * SmartShop - E-commerce Platform
 */

(function () {
    'use strict';

    // Admin Module
    window.AdminModule = {
        init: function () {
            this.initToastAutoDismiss();
            this.initSidebar();
        },

        // Auto-dismiss toast notifications after 4 seconds
        initToastAutoDismiss: function () {
            setTimeout(function () {
                var toasts = document.querySelectorAll('.admin-toast-area .alert');
                toasts.forEach(function (el) {
                    var bsAlert = bootstrap.Alert.getOrCreateInstance(el);
                    if (bsAlert) {
                        bsAlert.close();
                    }
                });
            }, 4000);
        },

        // Sidebar toggle
        initSidebar: function () {
            var sidebarToggle = document.getElementById('sidebarToggle');
            var sidebar = document.querySelector('.admin-sidebar');
            
            if (sidebarToggle && sidebar) {
                sidebarToggle.addEventListener('click', function () {
                    sidebar.classList.toggle('collapsed');
                });
            }
        },

        // Filter products table
        filterProducts: function () {
            var keyword = document.getElementById('searchInput');
            var statusFilter = document.getElementById('statusFilter');

            if (!keyword || !statusFilter) return;

            this.filterTable(
                'productTableBody',
                keyword.value.toLowerCase(),
                statusFilter.value,
                'rowCount'
            );
        },

        // Filter orders table
        filterOrders: function (status, tabEl) {
            // Update tab active state
            var tabs = document.querySelectorAll('#orderTabs .nav-link');
            tabs.forEach(function (el) {
                el.classList.remove('active');
            });
            if (tabEl) {
                tabEl.classList.add('active');
            }

            // Filter rows
            var rows = document.querySelectorAll('#ordersTable tbody tr');
            rows.forEach(function (row) {
                if (status === 'all' || row.dataset.status === status) {
                    row.style.display = '';
                } else {
                    row.style.display = 'none';
                }
            });
        },

        // Generic table filter
        filterTable: function (tableBodyId, keyword, status, counterId) {
            var rows = document.querySelectorAll('#' + tableBodyId + ' tr[data-name]');
            var count = 0;

            rows.forEach(function (row) {
                var nameMatch = !keyword || row.dataset.name.toLowerCase().includes(keyword);
                var statusMatch = !status || status === 'all' || row.dataset.status === status;

                if (nameMatch && statusMatch) {
                    row.style.display = '';
                    count++;
                } else {
                    row.style.display = 'none';
                }
            });

            var counter = document.getElementById(counterId);
            if (counter) {
                counter.textContent = count;
            }
        }
    };

    // Global functions for onclick handlers
    window.searchProducts = function () {
        AdminModule.filterProducts();
    };

    window.filterByStatus = function () {
        AdminModule.filterProducts();
    };

    window.filterOrders = function (status, tabEl) {
        AdminModule.filterOrders(status, tabEl);
    };

    // Initialize on DOM ready
    document.addEventListener('DOMContentLoaded', function () {
        AdminModule.init();
    });

})();