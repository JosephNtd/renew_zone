/* product.js — trang chi tiết sản phẩm: đổi ảnh gallery, chọn variant → tính lại giá,
   chuyển tab nội dung, carousel sản phẩm liên quan.
   Giỏ hàng thật làm ở Giai đoạn 4 — hiện nút "Thêm giỏ/Mua ngay" chỉ báo toast. */
(function () {
    "use strict";

    var vnd = new Intl.NumberFormat("vi-VN");
    function formatVnd(n) { return vnd.format(Math.round(n)) + "đ"; }

    /* ---------- 1. Gallery: click thumbnail đổi ảnh chính ---------- */
    var mainImg = document.getElementById("pdpMainImg");
    document.querySelectorAll(".pdp-thumb").forEach(function (thumb) {
        thumb.addEventListener("click", function () {
            var src = thumb.getAttribute("data-src");
            if (!src || !mainImg) return;
            mainImg.style.opacity = "0";
            setTimeout(function () {
                mainImg.src = src;
                mainImg.style.opacity = "1";
            }, 150);
            document.querySelectorAll(".pdp-thumb").forEach(function (t) { t.classList.remove("is-active"); });
            thumb.classList.add("is-active");
        });
    });

    /* ---------- 2. Variant → tính lại giá ---------- */
    var priceBox = document.querySelector(".pdp-price-box[data-price-base]");
    var priceEl = document.getElementById("pdpPrice");

    function recalcPrice() {
        if (!priceBox || !priceEl) return;
        var base = parseFloat(priceBox.getAttribute("data-price-base")) || 0;
        var total = base;
        document.querySelectorAll(".pdp-optgroup").forEach(function (group) {
            var active = group.querySelector(".pdp-opt.is-active");
            if (active) total += parseFloat(active.getAttribute("data-adjust")) || 0;
        });
        priceEl.textContent = formatVnd(total);
    }

    document.querySelectorAll(".pdp-optgroup").forEach(function (group) {
        group.querySelectorAll(".pdp-opt").forEach(function (opt) {
            opt.addEventListener("click", function () {
                // Mỗi nhóm chỉ 1 lựa chọn active
                group.querySelectorAll(".pdp-opt").forEach(function (o) { o.classList.remove("is-active"); });
                opt.classList.add("is-active");
                recalcPrice();
            });
        });
    });
    recalcPrice();

    /** Gom mô tả variant đang chọn, vd "Dung lượng: 256GB, Màu: Titan Đen". */
    function selectedVariantText() {
        var parts = [];
        document.querySelectorAll(".pdp-optgroup").forEach(function (group) {
            var active = group.querySelector(".pdp-opt.is-active");
            if (active) parts.push(group.getAttribute("data-group") + ": " + active.getAttribute("data-value"));
        });
        return parts.join(", ");
    }

    /* ---------- 3. Tabs nội dung ---------- */
    document.querySelectorAll(".pdp-tab").forEach(function (tab) {
        tab.addEventListener("click", function () {
            var key = tab.getAttribute("data-tab");
            document.querySelectorAll(".pdp-tab").forEach(function (t) { t.classList.remove("is-active"); });
            document.querySelectorAll(".pdp-panel").forEach(function (p) { p.classList.remove("is-active"); });
            tab.classList.add("is-active");
            var panel = document.querySelector('.pdp-panel[data-panel="' + key + '"]');
            if (panel) panel.classList.add("is-active");
        });
    });

    /* ---------- 4. Carousel sản phẩm liên quan ---------- */
    if (typeof Swiper !== "undefined" && document.querySelector(".related-swiper")) {
        new Swiper(".related-swiper", {
            slidesPerView: 2,
            spaceBetween: 16,
            navigation: { nextEl: ".related-next", prevEl: ".related-prev" },
            breakpoints: {
                768: { slidesPerView: 3 },
                992: { slidesPerView: 4 }
            }
        });
    }

    /* ---------- 5. Thêm giỏ / Mua ngay (placeholder tới Giai đoạn 4) ---------- */
    function pendingCart() {
        var variant = selectedVariantText();
        var msg = "Giỏ hàng đang được hoàn thiện" + (variant ? " (" + variant + ")" : "");
        if (window.RZ && window.RZ.toast) window.RZ.toast(msg, "success");
    }
    document.querySelectorAll(".pdp-addcart, .pdp-buynow").forEach(function (btn) {
        btn.addEventListener("click", pendingCart);
    });
})();
