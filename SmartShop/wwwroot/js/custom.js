(function ($) {
    'use strict';

    $(function () {

        // ẩn preloader sau khi tải xong
        setTimeout(function () {
            $('.loader_bg').fadeOut(300);
        }, 800);

        // khởi tạo tooltip bootstrap 5
        var tooltipTriggerList = document.querySelectorAll('[data-bs-toggle="tooltip"]');
        tooltipTriggerList.forEach(function (el) {
            new bootstrap.Tooltip(el);
        });

        // hiệu ứng overlay megamenu
        var $wrapper = $('#wrapper');
        $('.main-menu ul li.megamenu')
            .on('mouseenter', function () {
                $wrapper.addClass('overlay');
            })
            .on('mouseleave', function () {
                $wrapper.removeClass('overlay');
            });

        // toggle sidebar admin
        $('#sidebarCollapse').on('click', function () {
            $('#sidebar').toggleClass('active');
            $(this).toggleClass('active');
        });

        // khởi tạo carousel
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