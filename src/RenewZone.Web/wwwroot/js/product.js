/* product.js — trang chi tiết sản phẩm: đổi ảnh gallery, chọn variant → tính lại giá,
   chuyển tab nội dung, carousel sản phẩm liên quan, thêm giỏ / mua ngay.
   Việc gọi API giỏ hàng nằm ở cart.js (RZ.addToCart) — nạp global trong _Layout. */
(function () {
    "use strict";

    var vnd = new Intl.NumberFormat("vi-VN");
    function formatVnd(n) { return vnd.format(Math.round(n)) + "đ"; }

    /* 1. Gallery: click thumbnail đổi ảnh chính */
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

    /* 2. Variant → tính lại giá */
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

    /* 3. Tabs nội dung */
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

    /* 4. Carousel sản phẩm liên quan */
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

    /* 5. Stepper số lượng */
    var qtyBox = document.getElementById("pdpQty");

    function currentQty() {
        if (!qtyBox) return 1;
        var val = qtyBox.querySelector(".step-val");
        return val ? parseInt(val.textContent, 10) || 1 : 1;
    }

    if (qtyBox) {
        var maxQty = parseInt(qtyBox.getAttribute("data-max"), 10) || 10;
        qtyBox.querySelectorAll(".js-pdp-step").forEach(function (btn) {
            btn.addEventListener("click", function () {
                var val = qtyBox.querySelector(".step-val");
                var next = currentQty() + (parseInt(btn.getAttribute("data-step"), 10) || 0);
                if (next < 1 || next > maxQty || !val) return;

                val.textContent = next;
                val.classList.remove("pop");
                void val.offsetWidth; // restart animation
                val.classList.add("pop");

                qtyBox.querySelectorAll(".js-pdp-step").forEach(function (b) {
                    var step = parseInt(b.getAttribute("data-step"), 10);
                    b.disabled = step < 0 ? next <= 1 : next >= maxQty;
                });
            });
        });
    }

    /* 6. Thêm giỏ / Mua ngay */
    /** Id các OptionValue đang chọn — server dùng chính các id này để tính lại giá. */
    function selectedOptionValueIds() {
        var ids = [];
        document.querySelectorAll(".pdp-optgroup").forEach(function (group) {
            var active = group.querySelector(".pdp-opt.is-active[data-value-id]");
            if (active) ids.push(parseInt(active.getAttribute("data-value-id"), 10));
        });
        return ids.filter(function (id) { return !isNaN(id); });
    }

    function addToCartFrom(btn) {
        if (!window.RZ || !window.RZ.addToCart) return Promise.resolve(false);
        return window.RZ.addToCart({
            productId: parseInt(btn.getAttribute("data-product-id"), 10),
            optionValueIds: selectedOptionValueIds(),
            quantity: currentQty()
        }, mainImg);
    }

    document.querySelectorAll(".pdp-addcart").forEach(function (btn) {
        btn.addEventListener("click", function () {
            btn.disabled = true;
            addToCartFrom(btn).finally(function () { btn.disabled = false; });
        });
    });

    document.querySelectorAll(".pdp-buynow").forEach(function (btn) {
        btn.addEventListener("click", function () {
            btn.disabled = true;
            addToCartFrom(btn).then(function (ok) {
                if (ok) window.location.href = "/gio-hang";
                else btn.disabled = false;
            });
        });
    });

    /* 7. Đánh giá & bình luận: chọn sao + gửi AJAX (kết quả báo qua toast, PDP không đổi ngay
          vì đánh giá phải chờ admin duyệt). */
    var reviewSection = document.getElementById("danh-gia");
    var reviewForm = document.getElementById("reviewForm");
    if (reviewSection && reviewForm) {
        var starWrap = document.getElementById("reviewStars");
        var starBtns = starWrap ? Array.prototype.slice.call(starWrap.querySelectorAll(".rv-star-btn")) : [];
        var chosen = 0;

        function paintStars(upto) {
            starBtns.forEach(function (b, i) {
                var icon = b.querySelector("i");
                var on = i < upto;
                if (icon) icon.className = on ? "bi bi-star-fill" : "bi bi-star";
                b.setAttribute("aria-checked", (i + 1) === chosen ? "true" : "false");
            });
        }

        starBtns.forEach(function (btn, idx) {
            btn.addEventListener("click", function () { chosen = idx + 1; paintStars(chosen); });
            btn.addEventListener("mouseenter", function () { paintStars(idx + 1); });
        });
        if (starWrap) {
            starWrap.addEventListener("mouseleave", function () { paintStars(chosen); });
        }

        function reviewToast(message, type) {
            if (window.RZ && RZ.toast) RZ.toast(message, type || "success");
        }

        reviewForm.addEventListener("submit", function (e) {
            e.preventDefault();

            var nameEl = document.getElementById("rvName");
            var contentEl = document.getElementById("rvContent");
            var hpEl = reviewForm.querySelector('input[name="rvWebsite"]');
            var submitBtn = document.getElementById("reviewSubmit");

            // Kiểm nhanh phía client cho phản hồi tức thì; server luôn kiểm lại
            if (chosen < 1) { reviewToast("Vui lòng chọn số sao.", "error"); return; }
            if (!nameEl || nameEl.value.trim().length < 2) { reviewToast("Vui lòng nhập tên hiển thị.", "error"); return; }
            if (!contentEl || contentEl.value.trim().length < 10) { reviewToast("Nội dung cần ít nhất 10 ký tự.", "error"); return; }

            var tokenEl = document.querySelector('input[name="__RequestVerificationToken"]');
            if (submitBtn) submitBtn.disabled = true;

            fetch("/danh-gia/gui", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                    "RequestVerificationToken": tokenEl ? tokenEl.value : ""
                },
                body: JSON.stringify({
                    productId: parseInt(reviewSection.getAttribute("data-product-id"), 10),
                    productName: document.querySelector(".pdp-name") ? document.querySelector(".pdp-name").textContent.trim() : "",
                    authorName: nameEl.value,
                    rating: chosen,
                    content: contentEl.value,
                    website: hpEl ? hpEl.value : ""
                })
            }).then(function (res) {
                return res.ok ? res.json() : { ok: false, message: "Có lỗi xảy ra, vui lòng thử lại." };
            }).then(function (data) {
                reviewToast(data.message, data.ok ? "success" : "error");
                if (data.ok) {
                    reviewForm.reset();
                    chosen = 0;
                    paintStars(0);
                }
            }).catch(function () {
                reviewToast("Không kết nối được máy chủ. Vui lòng thử lại.", "error");
            }).finally(function () {
                if (submitBtn) submitBtn.disabled = false;
            });
        });
    }
})();
