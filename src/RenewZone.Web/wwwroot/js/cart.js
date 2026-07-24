/* cart.js — giỏ hàng phía client (Giai đoạn 4). NẠP TRÊN MỌI TRANG
   (sau site.js) vì icon giỏ + mini-cart + toast thêm giỏ nằm ở layout.

   Mở rộng window.RZ:
     RZ.addToCart(payload, sourceImgEl)  → gọi API + fly + toast + badge + refresh mini-cart
     RZ.cartToast(item) · RZ.flyToCart(imgEl) · RZ.refreshMiniCart() · RZ.syncCart(cart)

   Hợp đồng JSON xem CartController.CartJson — mọi số tiền do SERVER format sẵn. */
(function () {
    "use strict";

    var reduceMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    var FLY_MS = 600;
    var DEFAULT_MAX_QTY = 10; // khớp CartService.MaxQuantityPerItem (view truyền qua data-max)

    /* 1. Tiện ích gọi API */
    function antiForgeryToken() {
        var el = document.querySelector('input[name="__RequestVerificationToken"]');
        return el ? el.value : "";
    }

    function postJson(url, body) {
        return fetch(url, {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                "RequestVerificationToken": antiForgeryToken()
            },
            body: JSON.stringify(body)
        }).then(function (res) {
            if (!res.ok) throw new Error("HTTP " + res.status);
            return res.json();
        });
    }

    function toastError(message) {
        if (window.RZ && RZ.toast) RZ.toast(message || "Có lỗi xảy ra, vui lòng thử lại", "error");
    }

    /* 2. Đồng bộ UI theo giỏ mới */
    function syncCart(cart, badgeDelay) {
        if (!cart) return;

        var applyBadge = function () {
            if (window.RZ && RZ.cartBadge) RZ.cartBadge(cart.count);
        };
        if (badgeDelay) setTimeout(applyBadge, badgeDelay); else applyBadge();

        document.querySelectorAll(".js-cart-subtotal").forEach(function (el) {
            el.textContent = cart.subtotalText;
        });

        var countLabel = document.getElementById("cartCountLabel");
        if (countLabel) countLabel.textContent = cart.count + " sản phẩm";

        var miniCount = document.querySelector(".mini-cart-count");
        if (miniCount) miniCount.textContent = "(" + cart.count + ")";

        // Trang giỏ hàng: xóa dòng cuối cùng ⇒ hiện trạng thái trống, không reload
        var empty = document.getElementById("cartEmpty");
        var body = document.getElementById("cartBody");
        if (empty && body) {
            empty.hidden = !cart.isEmpty;
            body.hidden = cart.isEmpty;
        }
    }

    /* 3. Mini-cart offcanvas */
    function refreshMiniCart() {
        var host = document.getElementById("miniCartBody");
        if (!host) return Promise.resolve();

        return fetch("/gio-hang/mini", { headers: { "X-Requested-With": "XMLHttpRequest" } })
            .then(function (res) { return res.ok ? res.text() : null; })
            .then(function (html) {
                if (html === null) return;
                host.innerHTML = html;
                var inner = host.querySelector(".mini-cart-inner");
                var label = document.querySelector(".mini-cart-count");
                if (inner && label) label.textContent = "(" + inner.getAttribute("data-count") + ")";
            })
            .catch(function () { /* mini-cart chỉ là tiện ích — hỏng thì im lặng */ });
    }

    function openMiniCart() {
        var el = document.getElementById("miniCart");
        if (!el || !window.bootstrap) return false;
        bootstrap.Offcanvas.getOrCreateInstance(el).show();
        return true;
    }

    /* 4. Fly-to-cart (DESIGN 4.4) */
    function flyToCart(imgEl) {
        var target = document.querySelector(".cart-btn");
        if (reduceMotion || !imgEl || !target) return false;

        var from = imgEl.getBoundingClientRect();
        var to = target.getBoundingClientRect();
        if (!from.width || !from.height) return false;

        var clone = document.createElement("img");
        clone.src = imgEl.currentSrc || imgEl.src;
        clone.alt = "";
        clone.className = "fly-clone";
        clone.style.left = from.left + "px";
        clone.style.top = from.top + "px";
        clone.style.width = from.width + "px";
        clone.style.height = from.height + "px";
        document.body.appendChild(clone);

        var dx = (to.left + to.width / 2) - (from.left + from.width / 2);
        var dy = (to.top + to.height / 2) - (from.top + from.height / 2);

        requestAnimationFrame(function () {
            clone.style.transition = "transform " + FLY_MS + "ms cubic-bezier(.22,.61,.36,1), opacity " + FLY_MS + "ms ease-out";
            clone.style.transform = "translate(" + dx + "px," + dy + "px) scale(.15)";
            clone.style.opacity = "0.25";
        });
        setTimeout(function () { clone.remove(); }, FLY_MS + 60);
        return true;
    }

    /* 5. Toast "Đã thêm vào giỏ" (DESIGN 3.18) */
    function cartToast(item) {
        var stack = document.getElementById("toastStack");
        if (!stack) return;

        var el = document.createElement("div");
        el.className = "rz-toast toast-cart";

        if (item && item.imageUrl) {
            var thumb = document.createElement("img");
            thumb.className = "toast-cart-thumb";
            thumb.src = item.imageUrl;
            thumb.alt = "";
            el.appendChild(thumb);
        }

        var body = document.createElement("div");
        body.className = "toast-cart-body";

        var title = document.createElement("div");
        title.className = "toast-cart-title";
        title.innerHTML = '<i class="bi bi-check-circle-fill"></i>';
        title.append("Đã thêm vào giỏ");
        body.appendChild(title);

        var name = document.createElement("div");
        name.className = "toast-cart-name";
        name.textContent = item ? item.name : "";
        body.appendChild(name);
        el.appendChild(body);

        var link = document.createElement("button");
        link.type = "button";
        link.className = "toast-cart-link";
        link.textContent = "Xem giỏ";
        link.addEventListener("click", function () {
            if (!openMiniCart()) window.location.href = "/gio-hang";
        });
        el.appendChild(link);

        var progress = document.createElement("div");
        progress.className = "toast-progress";
        el.appendChild(progress);

        stack.appendChild(el);
        setTimeout(function () {
            el.classList.add("leaving");
            el.addEventListener("animationend", function () { el.remove(); }, { once: true });
        }, 3500);
    }

    /* 6. Thêm vào giỏ */
    function addToCart(payload, sourceImgEl) {
        return postJson("/gio-hang/them", {
            productId: payload.productId,
            optionValueIds: payload.optionValueIds || [],
            quantity: payload.quantity || 1
        }).then(function (data) {
            if (!data.ok) {
                toastError(data.message);
                return false;
            }
            var flew = flyToCart(sourceImgEl);
            syncCart(data.cart, flew ? FLY_MS : 0);
            cartToast(data.item);
            refreshMiniCart();
            return true;
        }).catch(function () {
            toastError();
            return false;
        });
    }

    /* 7. Trang giỏ hàng: stepper + xóa */
    function maxQtyOf(stepper) {
        return parseInt(stepper.getAttribute("data-max"), 10) || DEFAULT_MAX_QTY;
    }

    function setQuantity(stepper, qty) {
        var valEl = stepper.querySelector(".step-val");
        if (!valEl) return;
        valEl.textContent = qty;
        valEl.classList.remove("pop");
        void valEl.offsetWidth; // restart animation
        valEl.classList.add("pop");
        refreshStepperButtons(stepper);
    }

    function refreshStepperButtons(stepper) {
        var valEl = stepper.querySelector(".step-val");
        var qty = valEl ? parseInt(valEl.textContent, 10) || 1 : 1;
        var max = maxQtyOf(stepper);
        stepper.querySelectorAll(".step-btn").forEach(function (btn) {
            var step = parseInt(btn.getAttribute("data-step"), 10);
            btn.disabled = step < 0 ? qty <= 1 : qty >= max;
        });
    }

    function findLine(key) {
        var lines = document.querySelectorAll(".cart-line");
        for (var i = 0; i < lines.length; i++) {
            if (lines[i].getAttribute("data-key") === key) return lines[i];
        }
        return null;
    }

    function onStep(btn) {
        var stepper = btn.closest(".rz-stepper");
        if (!stepper) return;

        var key = stepper.getAttribute("data-key");
        var valEl = stepper.querySelector(".step-val");
        var current = valEl ? parseInt(valEl.textContent, 10) || 1 : 1;
        var next = current + (parseInt(btn.getAttribute("data-step"), 10) || 0);
        if (next < 1 || next > maxQtyOf(stepper)) return;

        // Cập nhật lạc quan: đổi UI ngay, sai thì revert theo phản hồi server.
        setQuantity(stepper, next);
        stepper.querySelectorAll(".step-btn").forEach(function (b) { b.disabled = true; });

        postJson("/gio-hang/cap-nhat", { key: key, quantity: next })
            .then(function (data) {
                if (!data.ok) {
                    setQuantity(stepper, current);
                    toastError(data.message);
                    return;
                }
                setQuantity(stepper, data.item.quantity);
                var line = findLine(key);
                var total = line ? line.querySelector(".js-line-total") : null;
                if (total) total.textContent = data.item.lineTotalText;
                syncCart(data.cart);
                refreshMiniCart();
            })
            .catch(function () {
                setQuantity(stepper, current);
                toastError();
            })
            .finally(function () { refreshStepperButtons(stepper); });
    }

    function onRemove(btn) {
        var key = btn.getAttribute("data-key");
        if (!key) return;
        btn.disabled = true;

        postJson("/gio-hang/xoa", { key: key })
            .then(function (data) {
                if (!data.ok) {
                    btn.disabled = false;
                    toastError(data.message);
                    return;
                }

                var line = findLine(key);
                if (line) {
                    // Gập chiều cao rồi mới bỏ khỏi DOM (DESIGN 3.13)
                    line.style.maxHeight = line.offsetHeight + "px";
                    requestAnimationFrame(function () { line.classList.add("is-removing"); });
                    setTimeout(function () {
                        line.remove();
                        syncCart(data.cart);
                    }, reduceMotion ? 0 : 320);
                } else {
                    syncCart(data.cart);
                }

                refreshMiniCart();
                if (window.RZ && RZ.toast) RZ.toast(data.message || "Đã xóa khỏi giỏ hàng");
            })
            .catch(function () {
                btn.disabled = false;
                toastError();
            });
    }

    /* 8. Gắn sự kiện (ủy quyền — nội dung mini-cart được thay động) */
    document.addEventListener("click", function (e) {
        var stepBtn = e.target.closest ? e.target.closest(".js-step") : null;
        if (stepBtn) { onStep(stepBtn); return; }

        var delBtn = e.target.closest ? e.target.closest(".js-cart-remove") : null;
        if (delBtn) { onRemove(delBtn); }
    });

    // Icon giỏ ở header mở offcanvas; giữ href="/gio-hang" làm fallback khi không có JS.
    var cartBtn = document.querySelector(".cart-btn");
    var miniEl = document.getElementById("miniCart");
    if (cartBtn && miniEl) {
        cartBtn.addEventListener("click", function (e) {
            if (window.location.pathname === "/gio-hang") return; // đang ở trang giỏ rồi
            if (!window.bootstrap) return;
            e.preventDefault();
            openMiniCart();
        });
        miniEl.addEventListener("show.bs.offcanvas", function () { refreshMiniCart(); });
    }

    /* 9. Xuất API */
    window.RZ = window.RZ || {};
    window.RZ.addToCart = addToCart;
    window.RZ.cartToast = cartToast;
    window.RZ.flyToCart = flyToCart;
    window.RZ.refreshMiniCart = refreshMiniCart;
    window.RZ.syncCart = syncCart;
})();
