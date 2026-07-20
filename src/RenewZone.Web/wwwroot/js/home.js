/* home.js — khởi tạo 3 carousel Swiper của trang chủ (hero, bán chạy, testimonials).
   Reveal-on-scroll do site.js xử lý qua class "reveal" đặt sẵn trong markup. */
(function () {
    "use strict";

    if (typeof Swiper === "undefined") return;

    // Người dùng chọn giảm chuyển động ⇒ tắt autoplay/hiệu ứng, giữ carousel vẫn kéo/bấm được.
    var reduceMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;

    if (document.querySelector(".hero-swiper")) {
        new Swiper(".hero-swiper", {
            effect: "fade",
            fadeEffect: { crossFade: true },
            loop: true,
            speed: 700,
            autoplay: reduceMotion ? false : { delay: 5000, disableOnInteraction: false, pauseOnMouseEnter: true },
            pagination: { el: ".hero-pagination", clickable: true },
            navigation: { nextEl: ".hero-next", prevEl: ".hero-prev" }
        });
    }

    if (document.querySelector(".bestseller-swiper")) {
        new Swiper(".bestseller-swiper", {
            slidesPerView: 2,
            spaceBetween: 16,
            navigation: { nextEl: ".bs-next", prevEl: ".bs-prev" },
            breakpoints: {
                768: { slidesPerView: 3 },
                992: { slidesPerView: 4 }
            }
        });
    }

    if (document.querySelector(".testimonial-swiper")) {
        new Swiper(".testimonial-swiper", {
            slidesPerView: 1,
            spaceBetween: 24,
            grabCursor: true,
            autoplay: reduceMotion ? false : { delay: 6000, disableOnInteraction: false },
            pagination: { el: ".tm-pagination", clickable: true },
            breakpoints: {
                768: { slidesPerView: 2 },
                992: { slidesPerView: 3 }
            }
        });
    }
})();
