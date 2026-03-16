/**
 * custom.js - Main site JavaScript
 * SmartShop - E-commerce Platform
 */

(function ($) {
    'use strict';

    $(function () {

        // Preloader - fade out after content loads
        setTimeout(function () {
            $('.loader_bg').fadeOut(300);
        }, 800);

        // Initialize Bootstrap 5 tooltips
        var tooltipTriggerList = document.querySelectorAll('[data-bs-toggle="tooltip"]');
        tooltipTriggerList.forEach(function (el) {
            new bootstrap.Tooltip(el);
        });

        // Megamenu overlay effect
        var $wrapper = $('#wrapper');
        $('.main-menu ul li.megamenu')
            .on('mouseenter', function () {
                $wrapper.addClass('overlay');
            })
            .on('mouseleave', function () {
                $wrapper.removeClass('overlay');
            });

        // Sidebar toggle (admin)
        $('#sidebarCollapse').on('click', function () {
            $('#sidebar').toggleClass('active');
            $(this).toggleClass('active');
        });

        // Carousel initialization
        var carousels = {
            '#blogCarousel': { interval: 5000 },
            '#banner1': { interval: 4000 },
            '#myCarousel': { interval: 5000 }
        };

        Object.keys(carousels).forEach(function (selector) {
            var $el = $(selector);
            if ($el.length) {
                $el.carousel(carousels[selector]);
            }
        });

    });

})(jQuery);