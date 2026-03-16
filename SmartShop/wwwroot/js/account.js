/**
 * account.js - Account section AJAX navigation
 * SmartShop - E-commerce Platform
 */

(function ($) {
    'use strict';

    // Account Tab Navigation Module
    var AccountModule = {
        selectors: {
            nav: '#accountNav',
            tab: '.account-tab',
            contentInner: '#account-content-inner'
        },

        titles: {
            'profile': 'Tài khoản của tôi',
            'orders': 'Đơn hàng của tôi',
            'addresses': 'Địa chỉ của tôi'
        },

        init: function () {
            var self = this;

            // Check if we're on account page
            if ($(this.selectors.nav).length === 0) {
                return;
            }

            console.log('AccountModule: Initializing...');
            this.bindEvents();
            this.handlePopState();
        },

        bindEvents: function () {
            var self = this;

            // Tab click event using event delegation
            $(document).on('click', this.selectors.tab, function (e) {
                e.preventDefault();
                e.stopPropagation();

                var $tab = $(this);
                var url = $tab.data('url');
                var tabName = $tab.data('tab');

                console.log('AccountModule: Tab clicked -', tabName, url);

                // Skip if already active
                if ($tab.hasClass('active')) {
                    console.log('AccountModule: Tab already active, skipping');
                    return;
                }

                // Update active state immediately
                $(self.selectors.tab).removeClass('active');
                $tab.addClass('active');

                // Load content via AJAX
                self.loadContent(url, tabName);
            });
        },

        loadContent: function (url, tabName) {
            var self = this;
            var $content = $(this.selectors.contentInner);

            console.log('AccountModule: Loading content from', url);

            // Show loading state
            $content.css('opacity', '0.5');

            $.ajax({
                url: url,
                type: 'GET',
                cache: false,
                success: function (data) {
                    console.log('AccountModule: Content loaded successfully');
                    
                    // Update content
                    $content.html(data).css('opacity', '1');

                    // Update browser URL without reload
                    self.updateUrl(tabName);

                    // Update page title
                    self.updateTitle(tabName);

                    // Re-initialize Bootstrap components
                    self.initBootstrapComponents();
                },
                error: function (xhr, status, error) {
                    console.error('AccountModule: Error loading content', error);
                    
                    $content.html(
                        '<div class="alert alert-danger m-3">' +
                        '<i class="fa fa-exclamation-circle me-2"></i>' +
                        'Không thể tải nội dung. Vui lòng thử lại.' +
                        '</div>'
                    ).css('opacity', '1');
                }
            });
        },

        updateUrl: function (tabName) {
            var newUrl = '/Account';
            
            switch (tabName) {
                case 'orders':
                    newUrl = '/Account/Orders';
                    break;
                case 'addresses':
                    newUrl = '/Account/Addresses';
                    break;
                default:
                    newUrl = '/Account/Profile';
            }

            history.pushState({ tab: tabName }, '', newUrl);
        },

        updateTitle: function (tabName) {
            var title = this.titles[tabName] || 'Tài khoản';
            document.title = title + ' - SmartShop';
        },

        handlePopState: function () {
            var self = this;

            window.addEventListener('popstate', function (e) {
                if (e.state && e.state.tab) {
                    var $tab = $(self.selectors.tab + '[data-tab="' + e.state.tab + '"]');
                    if ($tab.length && !$tab.hasClass('active')) {
                        $tab.trigger('click');
                    }
                }
            });
        },

        initBootstrapComponents: function () {
            // Re-initialize tooltips
            var tooltips = document.querySelectorAll('[data-bs-toggle="tooltip"]');
            tooltips.forEach(function (el) {
                var existingTooltip = bootstrap.Tooltip.getInstance(el);
                if (existingTooltip) existingTooltip.dispose();
                new bootstrap.Tooltip(el);
            });

            // Re-initialize popovers
            var popovers = document.querySelectorAll('[data-bs-toggle="popover"]');
            popovers.forEach(function (el) {
                var existingPopover = bootstrap.Popover.getInstance(el);
                if (existingPopover) existingPopover.dispose();
                new bootstrap.Popover(el);
            });
        }
    };

    // Initialize on document ready
    $(document).ready(function () {
        AccountModule.init();
    });

    // Expose module globally for debugging
    window.AccountModule = AccountModule;

})(jQuery);