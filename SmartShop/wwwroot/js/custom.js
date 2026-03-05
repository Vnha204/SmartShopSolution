/*---------------------------------------------------------------------
    File Name: custom.js
---------------------------------------------------------------------*/

$(function () {

    "use strict";

    /* Preloader
    -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- */
    setTimeout(function () {
        $('.loader_bg').fadeToggle();
    }, 1500);

    /* Tooltip
    -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- */
    $('[data-toggle="tooltip"]').tooltip();

    /* Mouseover
    -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- */
    $(".main-menu ul li.megamenu").mouseover(function () {
        if (!$(this).parent().hasClass("#wrapper")) {
            $("#wrapper").addClass('overlay');
        }
    });
    $(".main-menu ul li.megamenu").mouseleave(function () {
        $("#wrapper").removeClass('overlay');
    });

    /* Toggle sidebar
    -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- */
    $('#sidebarCollapse').on('click', function () {
        $('#sidebar').toggleClass('active');
        $(this).toggleClass('active');
    });

    /* Product slider
    -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- */
    $('#blogCarousel').carousel({
        interval: 5000
    });

    /* Carousel Bootstrap 4 compat
    -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- */
    $('#banner1').carousel({ interval: 4000 });
    $('#myCarousel').carousel({ interval: 5000 });

});
