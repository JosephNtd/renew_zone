/* auth.js — hộp thoại đăng nhập/đăng ký + "làm tiếp việc đang dở" (Giai đoạn 9).
   NẠP TRÊN MỌI TRANG, SAU cart.js.

   Mở rộng window.RZ:
     RZ.requireLogin(retry)  → mở hộp thoại; đăng nhập/đăng ký xong thì gọi lại retry()
     RZ.toggleWishlist(productId, btnEl)

   Ý đồ chính: khách vãng lai bấm "Thêm vào giỏ" thì KHÔNG bị rời trang. Server trả cờ
   requireLogin, client mở hộp thoại, xong việc thì chạy lại đúng thao tác vừa bị chặn —
   khách không phải tự nhớ mình đang định mua gì. */
(function () {
    "use strict";

    var pendingAction = null; // hàm cần gọi lại sau khi đăng nhập thành công
    var modalEl = document.getElementById("authModal");
    var modal = null;

    function antiForgeryToken() {
        var el = document.querySelector('input[name="__RequestVerificationToken"]');
        return el ? el.value : "";
    }

    function postJson(url, body) {
        return fetch(url, {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                // Nói rõ "tôi muốn JSON" để khi chưa đăng nhập, server trả 401 thay vì chuyển
                // hướng sang trang đăng nhập — fetch đi theo redirect rồi res.json() sẽ vỡ vì
                // nhận HTML, và ta mất luôn cơ hội mở hộp thoại đăng nhập (xem Program.cs).
                "Accept": "application/json",
                "X-Requested-With": "XMLHttpRequest",
                "RequestVerificationToken": antiForgeryToken()
            },
            body: JSON.stringify(body)
        }).then(function (res) {
            if (!res.ok) {
                // Gắn kèm mã lỗi để chỗ gọi phân biệt được "mạng hỏng" với "phiên đã đổi".
                var err = new Error("HTTP " + res.status);
                err.status = res.status;
                throw err;
            }
            return res.json();
        });
    }

    /**
     * Thông báo cho lỗi HTTP. Tách riêng vì 400 ở đây gần như luôn là ANTI-FORGERY HẾT HIỆU LỰC,
     * không phải "sai dữ liệu": token được sinh gắn với DANH TÍNH của người đang đăng nhập, nên
     * chỉ cần đăng nhập/đăng xuất ở MỘT TAB KHÁC là token của mọi tab đang mở thành vô giá trị.
     * Trước đây mọi lỗi đều rơi vào catch chung và hiện "Không kết nối được máy chủ" — sai hoàn
     * toàn về nguyên nhân, khiến người dùng bấm lại mãi mà không bao giờ được.
     */
    function describeError(err) {
        return err && err.status === 400
            ? { message: "Phiên làm việc đã thay đổi (bạn vừa đăng nhập hoặc đăng xuất ở tab khác). Đang tải lại trang…", reload: true }
            : { message: "Không kết nối được máy chủ, vui lòng thử lại.", reload: false };
    }

    /** Hiện lỗi trong hộp thoại; nếu là loại phải tải lại thì tự làm sau ~1,6 giây. */
    function failWith(err) {
        var info = describeError(err);
        showAlert(info.message);
        if (info.reload) {
            setTimeout(function () { window.location.reload(); }, 1600);
        }
    }

    function toast(message, type) {
        if (window.RZ && RZ.toast) RZ.toast(message, type);
    }

    /* 1. Điều khiển hộp thoại */
    function getModal() {
        if (!modalEl || !window.bootstrap) return null;
        if (!modal) modal = new bootstrap.Modal(modalEl);
        return modal;
    }

    function showAlert(message) {
        var box = document.getElementById("authAlert");
        if (!box) return;
        box.textContent = message;
        box.hidden = !message;
    }

    function switchPane(name) {
        showAlert("");

        document.querySelectorAll(".auth-tab").forEach(function (tab) {
            var active = tab.getAttribute("data-auth-pane") === name;
            tab.classList.toggle("is-active", active);
            tab.setAttribute("aria-selected", active ? "true" : "false");
        });

        var login = document.getElementById("authLoginPane");
        var register = document.getElementById("authRegisterPane");
        if (login) login.classList.toggle("is-active", name === "login");
        if (register) register.classList.toggle("is-active", name === "register");

        // Đưa con trỏ vào ô đầu tiên của khung vừa mở — khách gõ được ngay, không phải bấm thêm
        var first = document.querySelector(".auth-pane.is-active .rz-input");
        if (first) setTimeout(function () { first.focus(); }, 120);
    }

    /**
     * Mở hộp thoại và ghi nhớ việc đang dở.
     * @param {Function} retry hàm sẽ được gọi lại sau khi đăng nhập/đăng ký thành công
     * @param {string}   reason câu giải thích hiển thị dưới tiêu đề
     */
    function requireLogin(retry, reason) {
        pendingAction = typeof retry === "function" ? retry : null;

        var reasonEl = document.getElementById("authModalReason");
        if (reasonEl && reason) reasonEl.textContent = reason;

        var m = getModal();
        if (!m) {
            // Không có hộp thoại (JS/bootstrap lỗi, hoặc trang không nạp layout chính) ⇒ lùi về
            // trang đăng nhập đầy đủ kèm returnUrl. Fail-soft: khách vẫn đăng nhập được.
            window.location.href = "/dang-nhap?returnUrl=" + encodeURIComponent(window.location.pathname + window.location.search);
            return Promise.resolve(false);
        }

        switchPane("login");
        m.show();
        return Promise.resolve(false);
    }

    function onAuthSuccess(message) {
        var m = getModal();
        if (m) m.hide();

        toast(message || "Đăng nhập thành công!", "success");

        var action = pendingAction;
        pendingAction = null;

        if (action) {
            // Cookie đã được đặt ở response vừa rồi nên request tiếp theo đã là "đã đăng nhập".
            // Chạy lại việc đang dở, rồi tải lại trang để header/nút tim/khối đánh giá... vẽ
            // theo trạng thái mới (những phần đó do server render, JS không dựng lại được hết).
            Promise.resolve(action()).then(function () {
                setTimeout(function () { window.location.reload(); }, 1200);
            });
            return;
        }

        setTimeout(function () { window.location.reload(); }, 700);
    }

    function setBusy(form, busy) {
        var btn = form.querySelector('button[type="submit"]');
        if (!btn) return;
        btn.disabled = busy;
        btn.classList.toggle("is-loading", busy);
    }

    /* 2. Gắn sự kiện cho hộp thoại */
    if (modalEl) {
        modalEl.addEventListener("click", function (e) {
            var tab = e.target.closest("[data-auth-pane]");
            if (tab) {
                e.preventDefault();
                switchPane(tab.getAttribute("data-auth-pane"));
                return;
            }

            var eye = e.target.closest(".auth-pass-toggle");
            if (eye) {
                var input = eye.parentElement.querySelector("input");
                if (!input) return;
                var show = input.type === "password";
                input.type = show ? "text" : "password";
                eye.querySelector("i").className = show ? "bi bi-eye-slash" : "bi bi-eye";
                eye.setAttribute("aria-label", show ? "Ẩn mật khẩu" : "Hiện mật khẩu");
            }
        });

        // Bấm nút/liên kết "Đăng nhập" ở header: mở hộp thoại thay vì rời trang. Vẫn là thẻ <a>
        // trỏ tới /dang-nhap nên bấm chuột giữa / JS hỏng vẫn đi được tới trang thật.
        document.addEventListener("click", function (e) {
            var opener = e.target.closest(".js-auth-open");
            if (!opener) return;
            e.preventDefault();
            requireLogin(null, "Đăng nhập để mua hàng và theo dõi đơn của bạn.");
        });

        var loginForm = document.getElementById("authLoginPane");
        if (loginForm) {
            loginForm.addEventListener("submit", function (e) {
                e.preventDefault();
                showAlert("");

                var phone = (document.getElementById("authLoginPhone").value || "").trim();
                var password = document.getElementById("authLoginPassword").value || "";

                if (!phone || !password) {
                    showAlert("Vui lòng nhập số điện thoại và mật khẩu.");
                    return;
                }

                setBusy(loginForm, true);
                postJson("/tai-khoan/dang-nhap-ajax", { phone: phone, password: password, rememberMe: true })
                    .then(function (data) {
                        setBusy(loginForm, false);
                        if (!data.ok) { showAlert(data.message); return; }
                        onAuthSuccess("Chào " + (data.displayName || "bạn") + "!");
                    })
                    .catch(function (err) {
                        setBusy(loginForm, false);
                        failWith(err);
                    });
            });
        }

        var registerForm = document.getElementById("authRegisterPane");
        if (registerForm) {
            registerForm.addEventListener("submit", function (e) {
                e.preventDefault();
                showAlert("");

                var body = {
                    fullName: (document.getElementById("authRegName").value || "").trim(),
                    phone: (document.getElementById("authRegPhone").value || "").trim(),
                    email: (document.getElementById("authRegEmail").value || "").trim(),
                    password: document.getElementById("authRegPassword").value || "",
                    confirmPassword: document.getElementById("authRegConfirm").value || "",
                    website: document.getElementById("authRegWebsite").value || ""
                };

                // Kiểm sơ bộ ở client cho phản hồi nhanh — chốt chặn THẬT nằm ở AuthController
                if (body.fullName.length < 2) { showAlert("Vui lòng nhập họ tên."); return; }
                if (!/^(0|\+84)\d{9,10}$/.test(body.phone)) { showAlert("Số điện thoại không hợp lệ (VD: 0912345678)."); return; }
                if (body.password.length < 6) { showAlert("Mật khẩu cần ít nhất 6 ký tự."); return; }
                if (body.password !== body.confirmPassword) { showAlert("Hai mật khẩu không khớp nhau."); return; }

                setBusy(registerForm, true);
                postJson("/tai-khoan/dang-ky-ajax", body)
                    .then(function (data) {
                        setBusy(registerForm, false);
                        if (!data.ok) { showAlert(data.message); return; }
                        onAuthSuccess("Chào mừng " + (data.displayName || "bạn") + "!");
                    })
                    .catch(function (err) {
                        setBusy(registerForm, false);
                        failWith(err);
                    });
            });
        }
    }

    /* 3. Nút tim yêu thích (card sản phẩm + PDP) */
    function toggleWishlist(productId, btn) {
        return postJson("/tai-khoan/yeu-thich/bat-tat", { productId: productId })
            .then(function (data) {
                if (!data.ok) { toast(data.message, "error"); return false; }

                if (btn) {
                    btn.classList.toggle("is-on", data.isFavorite);
                    btn.setAttribute("aria-pressed", data.isFavorite ? "true" : "false");
                    btn.setAttribute("aria-label", data.isFavorite ? "Bỏ yêu thích" : "Thêm vào yêu thích");
                    var icon = btn.querySelector("i");
                    if (icon) icon.className = data.isFavorite ? "bi bi-heart-fill" : "bi bi-heart";
                }

                toast(data.message, "success");
                return true;
            })
            .catch(function (err) {
                // Chưa đăng nhập: cookie event ở Program.cs trả 401 cho request AJAX (không
                // chuyển hướng), postJson ném lỗi ⇒ bắt tại đây để mở hộp thoại và thử lại.
                if (err && err.status === 401) {
                    return requireLogin(function () { return toggleWishlist(productId, btn); },
                        "Đăng nhập để lưu sản phẩm yêu thích của bạn.");
                }
                var info = describeError(err);
                toast(info.message, "error");
                if (info.reload) setTimeout(function () { window.location.reload(); }, 1600);
                return false;
            });
    }

    document.addEventListener("click", function (e) {
        var btn = e.target.closest(".js-wishlist");
        if (!btn) return;

        // Nút tim nằm TRONG card sản phẩm có .stretched-link phủ toàn bộ ⇒ không chặn thì bấm
        // tim sẽ mở luôn trang sản phẩm.
        e.preventDefault();
        e.stopPropagation();

        var id = parseInt(btn.getAttribute("data-product-id"), 10);
        if (id > 0) toggleWishlist(id, btn);
    });

    window.RZ = window.RZ || {};
    window.RZ.requireLogin = requireLogin;
    window.RZ.toggleWishlist = toggleWishlist;
})();
