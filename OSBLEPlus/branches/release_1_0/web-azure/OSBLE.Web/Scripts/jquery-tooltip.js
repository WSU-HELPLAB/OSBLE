/*
 * from http://jqueryfordesigners.com/coda-popup-bubbles/
 *   (with modifications)
 * 
 * see ToolTips.cs for usage
 */

$(function () {
    $('.popup-bubbleInfo').each(function () {
        var time = 250;
        var hideDelay = 500;

        var hideDelayTimer = null;

        var beingShown = false;
        var shown = false;
        var trigger = $('.popup-trigger', this);
        var info = $('.popup', this).css('opacity', 0);
        var contents = $('.popup-contents', this);


        $([trigger.get(0), info.get(0)]).mouseover(function () {
            if (hideDelayTimer) clearTimeout(hideDelayTimer);
            if (beingShown || shown) {
                // don't trigger the animation again
                return;
            } else {
                // reset position of info box
                beingShown = true;

                // show
                info.css({
                    top: 0,  // these could
                    left: 0, //   probably be removed
                    display: 'block'
                }).animate({
                    opacity: 1
                }, time, 'swing', function () {
                    beingShown = false;
                    shown = true;
                });


                // reposition to top right of trigger
                var calc_top = -info.height();
                var calc_left = trigger.width(); // (info.width() / 2);

                // fix position if it goes out of the window
                if (calc_top + trigger.offset().top < $(window).scrollTop()) {
                    calc_top = trigger.height();
                }

                if (trigger.offset().left + info.width() > $(window).width()) {
                    calc_left = -info.width();
                }


                info.css({
                    top: calc_top,
                    left: calc_left
                });

            }

            return false;
        }).mouseout(function () {
            if (hideDelayTimer) clearTimeout(hideDelayTimer);
            hideDelayTimer = setTimeout(function () {
                hideDelayTimer = null;
                info.animate({
                    opacity: 0
                }, time, 'swing', function () {
                    shown = false;
                    info.css('display', 'none');
                });

            }, hideDelay);

            return false;
        });
    });
});
