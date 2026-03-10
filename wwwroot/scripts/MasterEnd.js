/* MasterEnd.js - Bottom-of-page UI initialization */
(function ($) {
    'use strict';

    /* --- Right-bar (search/info panel) Toggle --- */
    function openRightBar() {
        $('body').addClass('right-bar-enabled');
        $('#right-bar').find('input[name="q"]').focus();
    }

    function closeRightBar() {
        $('body').removeClass('right-bar-enabled');
    }

    // Open: any search trigger in the top bar
    $(document).on('click', '.right-bar-toggle, [data-toggle="search"], #openSearch', function (e) {
        // The close button inside the panel also carries .right-bar-toggle — toggle accordingly
        if ($(this).closest('#right-bar').length) {
            closeRightBar();
        } else {
            e.preventDefault();
            openRightBar();
        }
    });

    // Close: overlay click
    $(document).on('click', '#rightbar-overlay', function () {
        closeRightBar();
    });

    // Close: Escape key
    $(document).on('keydown', function (e) {
        if (e.key === 'Escape' && $('body').hasClass('right-bar-enabled')) {
            closeRightBar();
        }
    });

    /* --- Mobile Nav Toggle --- */
    $(document).on('click', '.navbar-toggle', function () {
        $('#topnav-menu-content').toggleClass('show');
    });

    /* --- Dropdown Hover for Desktop --- */
    if ($(window).width() > 991) {
        $('.topnav-menu .dropdown').hover(
            function () { $(this).find('.dropdown-menu').first().stop(true, true).slideDown(150); },
            function () { $(this).find('.dropdown-menu').first().stop(true, true).slideUp(100); }
        );
    }

    /* --- Nested Dropdown Support --- */
    $(document).on('click', '.dropdown-menu .dropdown-toggle', function (e) {
        if ($(window).width() < 992) {
            e.preventDefault();
            e.stopPropagation();
            $(this).next('.dropdown-menu').toggleClass('show');
        }
    });

    /* --- Cookie Consent --- */
    var $gdpr = $('.gdpr');
    if ($gdpr.length && !localStorage.getItem('stb_cookie_consent')) {
        $gdpr.show();
    }
    $(document).on('click', '#acceptCookies', function () {
        localStorage.setItem('stb_cookie_consent', 'accepted');
        $gdpr.fadeOut(300);
        if (typeof gtag === 'function') {
            gtag('consent', 'update', { ad_storage: 'granted', analytics_storage: 'granted' });
        }
    });

    /* --- News Ticker ---
         The local jquery.jConveyorTicker.min.js stub auto-inits itself on
         DOMContentLoaded, so no explicit call is needed here. Calling it a
         second time would clone the list items twice (4× total) and cause the
         -50% CSS animation to scroll through two full copies per loop. */

    /* --- Carousel auto-init (Bootstrap 4) --- */
    if ($('#homeMainSlider').length) {
        $('#homeMainSlider').carousel({ interval: 5000, pause: 'hover' });
    }

    /* --- News carousel: keep numbered pagination in sync with active slide --- */
    $('#newsSlider').on('slide.bs.carousel', function (e) {
        var slideFrom = $(this).find('.carousel-item.active').index();
        var slideTo   = $(e.relatedTarget).index();
        $('#newsSlider' + slideFrom).removeClass('active');
        $('#newsSlider' + slideTo).addClass('active');
    });

})(jQuery);
