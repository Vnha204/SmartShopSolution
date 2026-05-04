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

            if ($(this.selectors.nav).length === 0) {
                return;
            }

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

                if ($tab.hasClass('active')) {
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

            $content.css('opacity', '0.5');

            $.ajax({
                url: url,
                type: 'GET',
                cache: false,
                success: function (data) {
                    $content.html(data).css('opacity', '1');
                    self.updateUrl(tabName);
                    self.updateTitle(tabName);
                    self.initBootstrapComponents();
                },
                error: function (xhr, status, error) {
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

    $(document).ready(function () {
        AccountModule.init();
    });

})(jQuery);