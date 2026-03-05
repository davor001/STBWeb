/* MasterEnd.js - Bottom-of-page UI initialization */
(function ($) {
    'use strict';

    /* --- Search Panel Toggle --- */
    var $searchPanel = $('#searchPanel');

    // Open search when clicking the search button/link in top bar
    $(document).on('click', '[data-toggle="search"], .right-bar-toggle, #openSearch, a[href="#searchPanel"]', function (e) {
        e.preventDefault();
        $searchPanel.addClass('show').css('display', 'flex');
        $searchPanel.find('input[name="q"]').focus();
    });

    // Close search panel
    $(document).on('click', '#closeSearch, .rightbar-overlay', function () {
        $searchPanel.removeClass('show').css('display', 'none');
    });

    // Close on Escape key
    $(document).on('keydown', function (e) {
        if (e.key === 'Escape' && $searchPanel.hasClass('show')) {
            $searchPanel.removeClass('show').css('display', 'none');
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

    /* --- News Ticker --- */
    if ($.fn.jConveyorTicker && $('.js-conveyor-example').length) {
        $('.js-conveyor-example').jConveyorTicker({ speed: 80 });
    }

    /* --- Carousel auto-init (Bootstrap 4) --- */
    if ($('#homeMainSlider').length) {
        $('#homeMainSlider').carousel({ interval: 5000, pause: 'hover' });
    }

})(jQuery);
