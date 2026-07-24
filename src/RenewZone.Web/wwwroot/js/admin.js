/* admin.js — runtime dùng chung của trang quản trị. Admin KHÔNG nạp site.js (theme + API
   khác hẳn) nên toast/ajax ở đây được định nghĩa lại dưới namespace RZAdmin riêng.

   Nguyên tắc: không hiệu ứng nào được CHẶN thao tác. Người nhập liệu 100 sản phẩm
   sẽ ghét animation 400ms lặp lại 100 lần ⇒ admin dùng --t-fast/--t-base là chính. */
(function (window, document) {
    "use strict";

    var reduceMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;

    var RZAdmin = {};

    /* 1. Toast */
    var ICONS = {
        success: "bi-check-circle-fill",
        error: "bi-x-circle-fill",
        warning: "bi-exclamation-triangle-fill",
        info: "bi-info-circle-fill"
    };

    RZAdmin.toast = function (message, type, timeout) {
        var stack = document.getElementById("admToastStack");
        if (!stack || !message) return;

        type = type || "success";

        var el = document.createElement("div");
        el.className = "adm-toast is-" + type;
        el.setAttribute("role", type === "error" ? "alert" : "status");

        var icon = document.createElement("i");
        icon.className = "bi " + (ICONS[type] || ICONS.info);
        icon.setAttribute("aria-hidden", "true");

        var msg = document.createElement("div");
        msg.className = "adm-toast-msg";
        msg.textContent = message;

        var close = document.createElement("button");
        close.type = "button";
        close.className = "adm-toast-close";
        close.setAttribute("aria-label", "Đóng thông báo");
        close.innerHTML = '<i class="bi bi-x-lg" aria-hidden="true"></i>';

        el.appendChild(icon);
        el.appendChild(msg);
        el.appendChild(close);
        stack.appendChild(el);

        var timer = setTimeout(dismiss, timeout || 4000);
        close.addEventListener("click", function () { clearTimeout(timer); dismiss(); });

        function dismiss() {
            el.classList.add("is-out");
            setTimeout(function () { el.remove(); }, reduceMotion ? 0 : 200);
        }
    };

    /* 2. AJAX có anti-forgery.
       [AutoValidateAntiforgeryToken] ở BaseAdminController bảo vệ MỌI POST, kể cả AJAX,
       nên request nào thiếu header này cũng nhận 400 rất khó hiểu. */
    RZAdmin.token = function () {
        var el = document.querySelector('input[name="__RequestVerificationToken"]');
        return el ? el.value : "";
    };

    RZAdmin.post = function (url, data) {
        return fetch(url, {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                "RequestVerificationToken": RZAdmin.token(),
                "X-Requested-With": "XMLHttpRequest"
            },
            body: JSON.stringify(data || {})
        }).then(function (res) {
            if (!res.ok) {
                // Phiên hết hạn giữa chừng: đưa về trang đăng nhập thay vì báo lỗi vô nghĩa
                if (res.status === 401 || res.status === 403) {
                    window.location.href = "/admin/login";
                    return { ok: false, message: "Phiên làm việc đã hết hạn." };
                }
                return res.json().catch(function () {
                    return { ok: false, message: "Có lỗi xảy ra (mã " + res.status + ")." };
                });
            }
            return res.json();
        }).catch(function () {
            return { ok: false, message: "Không kết nối được máy chủ. Kiểm tra lại mạng rồi thử lần nữa." };
        });
    };

    /* 3. Thông báo cho trình đọc màn hình */
    RZAdmin.announce = function (message) {
        var live = document.getElementById("admLive");
        if (live) live.textContent = message;
    };

    /* 4. Sidebar */
    var SIDEBAR_KEY = "rz_adm_sidebar";

    function initSidebar() {
        var shell = document.getElementById("admShell");
        var toggle = document.getElementById("admSidebarToggle");
        var backdrop = document.getElementById("admSidebarBackdrop");
        if (!shell) return;

        var isMobile = function () { return window.innerWidth < 992; };

        if (localStorage.getItem(SIDEBAR_KEY) === "mini" && !isMobile()) {
            shell.classList.add("is-mini");
        }

        if (toggle) {
            toggle.addEventListener("click", function () {
                if (isMobile()) {
                    shell.classList.toggle("is-open");
                    return;
                }
                var mini = shell.classList.toggle("is-mini");
                localStorage.setItem(SIDEBAR_KEY, mini ? "mini" : "full");
            });
        }

        if (backdrop) {
            backdrop.addEventListener("click", function () { shell.classList.remove("is-open"); });
        }

        document.addEventListener("keydown", function (e) {
            if (e.key === "Escape") shell.classList.remove("is-open");
        });

        initSidebarTooltips(shell);
    }

    /* Sidebar thu gọn chỉ còn icon ⇒ phải có tooltip, không thì không đoán được mục nào là mục nào */
    function initSidebarTooltips(shell) {
        var tip = document.createElement("div");
        tip.className = "adm-nav-tip";
        tip.setAttribute("role", "tooltip");
        document.body.appendChild(tip);

        document.querySelectorAll(".adm-nav-item").forEach(function (item) {
            var show = function () {
                var collapsed = shell.classList.contains("is-mini") || window.innerWidth < 1200;
                if (!collapsed || window.innerWidth < 992) return;

                var label = item.querySelector("span");
                tip.textContent = label ? label.textContent.trim() : "";
                var rect = item.getBoundingClientRect();
                tip.style.top = (rect.top + rect.height / 2 - 14) + "px";
                tip.style.left = (rect.right + 10) + "px";
                tip.classList.add("is-on");
            };
            var hide = function () { tip.classList.remove("is-on"); };

            item.addEventListener("mouseenter", show);
            item.addEventListener("focus", show);
            item.addEventListener("mouseleave", hide);
            item.addEventListener("blur", hide);
        });
    }

    /* 5. Count-up cho stat card */
    function initCountUp() {
        document.querySelectorAll("[data-countup]").forEach(function (el) {
            var target = parseFloat(el.getAttribute("data-countup")) || 0;
            var suffix = el.getAttribute("data-suffix") || "";

            if (reduceMotion || target === 0) {
                el.textContent = format(target) + suffix;
                return;
            }

            var start = performance.now();
            var duration = 800;

            function step(now) {
                var p = Math.min((now - start) / duration, 1);
                var eased = 1 - Math.pow(1 - p, 3); // ease-out cubic
                el.textContent = format(Math.round(target * eased)) + suffix;
                if (p < 1) requestAnimationFrame(step);
            }
            requestAnimationFrame(step);
        });

        function format(n) { return n.toLocaleString("vi-VN"); }
    }

    /* 6. Modal xác nhận.
       Trả Promise. Mặc định resolve BOOLEAN: gọi RZAdmin.confirm({...}).then(ok => ...).
       Nếu truyền options.note ({ label, placeholder }) thì modal hiện thêm ô ghi chú tùy chọn
       và resolve OBJECT { confirmed: bool, note: string } — dùng khi đổi trạng thái đơn hàng. */
    RZAdmin.confirm = function (options) {
        options = options || {};
        var wantNote = !!options.note;

        return new Promise(function (resolve) {
            var modal = document.getElementById("admConfirmModal");
            if (!modal) {
                var fallback = window.confirm(options.message || "Xác nhận?");
                resolve(wantNote ? { confirmed: fallback, note: "" } : fallback);
                return;
            }

            var titleEl = modal.querySelector("[data-role=title]");
            var msgEl = modal.querySelector("[data-role=message]");
            var okBtn = modal.querySelector("[data-role=ok]");
            var cancelBtn = modal.querySelector("[data-role=cancel]");
            var typeWrap = modal.querySelector("[data-role=type-wrap]");
            var typeInput = modal.querySelector("[data-role=type-input]");
            var typeHint = modal.querySelector("[data-role=type-hint]");
            var noteWrap = modal.querySelector("[data-role=note-wrap]");
            var noteInput = modal.querySelector("[data-role=note-input]");
            var noteLabel = modal.querySelector("[data-role=note-label]");

            titleEl.textContent = options.title || "Xác nhận";
            msgEl.innerHTML = options.message || "";
            okBtn.querySelector(".adm-btn-label").textContent = options.okText || "Xóa";
            okBtn.className = "adm-btn " + (options.okClass || "adm-btn-danger");

            // Hành động KHÔNG hoàn tác được thì phải khó bấm nhầm: bắt gõ đúng tên
            var needType = !!options.confirmText;
            typeWrap.hidden = !needType;
            okBtn.disabled = needType;
            typeInput.value = "";
            if (needType) typeHint.textContent = options.confirmText;

            noteWrap.hidden = !wantNote;
            noteInput.value = "";
            if (wantNote) {
                noteLabel.textContent = options.note.label || "Ghi chú (tùy chọn)";
                noteInput.placeholder = options.note.placeholder || "";
            }

            var lastFocused = document.activeElement;
            modal.classList.add("is-open");
            (needType ? typeInput : okBtn).focus();

            function onType() {
                okBtn.disabled = typeInput.value.trim() !== options.confirmText;
            }

            function close(confirmed) {
                var note = noteInput.value.trim();
                modal.classList.remove("is-open");
                okBtn.removeEventListener("click", onOk);
                cancelBtn.removeEventListener("click", onCancel);
                typeInput.removeEventListener("input", onType);
                document.removeEventListener("keydown", onKey);
                if (lastFocused && lastFocused.focus) lastFocused.focus(); // trả focus về nút vừa bấm
                resolve(wantNote ? { confirmed: confirmed, note: note } : confirmed);
            }

            function onOk() { if (!okBtn.disabled) close(true); }
            function onCancel() { close(false); }

            function onKey(e) {
                if (e.key === "Escape") { close(false); return; }
                if (e.key !== "Tab") return;

                // Bẫy focus trong modal (gồm cả ô ghi chú nếu đang hiện)
                var items = modal.querySelectorAll("button:not([disabled]), input:not([hidden]), textarea:not([hidden])");
                var visible = Array.prototype.filter.call(items, function (n) { return n.offsetParent !== null; });
                if (!visible.length) return;

                var first = visible[0], last = visible[visible.length - 1];
                if (e.shiftKey && document.activeElement === first) { e.preventDefault(); last.focus(); }
                else if (!e.shiftKey && document.activeElement === last) { e.preventDefault(); first.focus(); }
            }

            okBtn.addEventListener("click", onOk);
            cancelBtn.addEventListener("click", onCancel);
            typeInput.addEventListener("input", onType);
            document.addEventListener("keydown", onKey);
        });
    };

    /* Nút có data-confirm-* tự gắn modal — không phải viết JS cho từng màn */
    function initConfirmButtons() {
        document.addEventListener("click", function (e) {
            var btn = e.target.closest("[data-confirm]");
            if (!btn) return;

            e.preventDefault();

            RZAdmin.confirm({
                title: btn.getAttribute("data-confirm-title") || "Xác nhận xóa",
                message: btn.getAttribute("data-confirm"),
                okText: btn.getAttribute("data-confirm-ok") || "Xóa",
                confirmText: btn.getAttribute("data-confirm-type") || null
            }).then(function (ok) {
                if (!ok) return;

                var form = btn.closest("form");
                if (form) { form.submit(); return; }

                var url = btn.getAttribute("data-confirm-url");
                if (url) postForm(url);
            });
        });
    }

    /* POST thật (không phải AJAX) kèm token — dùng cho xóa/khôi phục từ nút icon */
    function postForm(url) {
        var form = document.createElement("form");
        form.method = "post";
        form.action = url;

        var input = document.createElement("input");
        input.type = "hidden";
        input.name = "__RequestVerificationToken";
        input.value = RZAdmin.token();

        form.appendChild(input);
        document.body.appendChild(form);
        form.submit();
    }
    RZAdmin.postForm = postForm;

    /* 7. Bảng: hiệu ứng vào + chế độ gọn */
    var COMPACT_KEY = "rz_adm_compact";

    function initTable() {
        var compact = localStorage.getItem(COMPACT_KEY) === "1";
        document.querySelectorAll(".adm-table").forEach(function (t) {
            if (compact) t.classList.add("is-compact");
        });

        var btn = document.getElementById("admCompactToggle");
        if (btn) {
            btn.setAttribute("aria-pressed", compact ? "true" : "false");
            btn.addEventListener("click", function () {
                var on = !document.querySelector(".adm-table.is-compact");
                document.querySelectorAll(".adm-table").forEach(function (t) {
                    t.classList.toggle("is-compact", on);
                });
                localStorage.setItem(COMPACT_KEY, on ? "1" : "0");
                btn.setAttribute("aria-pressed", on ? "true" : "false");
            });
        }

        if (reduceMotion) return;

        // Stagger TỐI ĐA 12 hàng: 50 hàng lệch nhau 20ms trông như trang bị lag
        document.querySelectorAll(".adm-table tbody tr").forEach(function (tr, i) {
            if (i >= 12) return;
            tr.style.opacity = "0";
            tr.style.transform = "translateY(4px)";
            tr.style.transition = "opacity .2s ease-out, transform .2s ease-out";
            setTimeout(function () { tr.style.opacity = ""; tr.style.transform = ""; }, i * 20);
        });
    }

    /* 8. Ô tìm kiếm có debounce */
    function initSearchDebounce() {
        document.querySelectorAll("[data-autosubmit]").forEach(function (input) {
            var timer = null;
            input.addEventListener("input", function () {
                clearTimeout(timer);
                timer = setTimeout(function () {
                    var form = input.closest("form");
                    if (form) form.submit();
                }, 400);
            });
        });
    }

    /* 9. Nút submit: chống bấm 2 lần */
    function initFormSubmit() {
        document.querySelectorAll("form[data-adm-form]").forEach(function (form) {
            form.addEventListener("submit", function () {
                // ⚠️ Listener của mình chạy TRƯỚC jquery-validation ⇒ phải tự hỏi valid()
                // rồi mới khóa nút, không thì form sai vẫn bị khóa (bẫy đã gặp ở checkout.js/contact.js)
                if (window.jQuery && window.jQuery(form).data("validator")
                    && !window.jQuery(form).valid()) {
                    return;
                }

                form.querySelectorAll("button[type=submit]").forEach(function (btn) {
                    btn.classList.add("is-loading");
                    btn.disabled = true;
                });

                form.setAttribute("data-submitting", "1"); // để guard "chưa lưu" không cảnh báo nhầm
            });
        });
    }

    /* 10. Cảnh báo rời trang khi chưa lưu */
    function initDirtyGuard() {
        document.querySelectorAll("form[data-dirty-guard]").forEach(function (form) {
            var dirty = false;

            form.addEventListener("input", function () { dirty = true; });
            form.addEventListener("change", function () { dirty = true; });

            window.addEventListener("beforeunload", function (e) {
                if (!dirty || form.getAttribute("data-submitting")) return;
                e.preventDefault();
                e.returnValue = "";
            });
        });
    }

    /* 11. Ô nhập tiền */
    function initMoneyInputs() {
        document.querySelectorAll("[data-money]").forEach(function (input) {
            var render = function () {
                var digits = input.value.replace(/\D/g, "");
                input.value = digits ? parseInt(digits, 10).toLocaleString("vi-VN") : "";
            };

            render();
            input.addEventListener("input", render);

            // Gửi số THUẦN về server. Server vẫn phải parse phòng thủ — JS có thể bị tắt.
            var form = input.closest("form");
            if (form) {
                form.addEventListener("submit", function () {
                    input.value = input.value.replace(/\D/g, "");
                });
            }
        });
    }

    /* 12. Ô slug: tự sinh + kiểm realtime */
    function initSlugInputs() {
        document.querySelectorAll("[data-slug-input]").forEach(function (wrap) {
            var input = wrap.querySelector("input");
            var state = wrap.querySelector("[data-role=state]");
            var msg = wrap.parentElement.querySelector("[data-role=slug-msg]");
            var regen = wrap.querySelector("[data-role=regen]");
            var sourceSel = input.getAttribute("data-slug-source");
            var source = sourceSel ? document.querySelector(sourceSel) : null;
            var type = input.getAttribute("data-slug-type") || "";
            var id = input.getAttribute("data-slug-id") || "";
            var original = input.value;

            // Chỉ tự sinh khi người dùng CHƯA tự gõ slug — sửa tay xong mà bị ghi đè thì rất bực
            var userEdited = input.value.trim().length > 0;
            input.addEventListener("input", function () { userEdited = true; check(); });

            if (source) {
                source.addEventListener("input", function () {
                    if (userEdited) return;
                    input.value = slugify(source.value);
                    check();
                });
            }

            if (regen) {
                regen.addEventListener("click", function () {
                    if (source) { input.value = slugify(source.value); userEdited = false; check(); }
                });
            }

            var timer = null;
            function check() {
                clearTimeout(timer);
                var value = input.value.trim();

                if (!value) { setState("", ""); return; }

                setState("busy", "");
                timer = setTimeout(function () {
                    var url = "/admin/api/slug/kiem-tra?slug=" + encodeURIComponent(value)
                        + (type ? "&type=" + encodeURIComponent(type) : "")
                        + (id ? "&id=" + encodeURIComponent(id) : "");

                    fetch(url, { headers: { "X-Requested-With": "XMLHttpRequest" } })
                        .then(function (r) { return r.json(); })
                        .then(function (data) {
                            if (data.isAvailable) {
                                setState("ok", "Slug hợp lệ.");
                            } else {
                                setState(data.isInTrash ? "warn" : "bad", data.message || "Slug đã được dùng.");
                            }

                            // Đổi slug của bản ghi đã tồn tại ⇒ link cũ chết (redirect 301 để Giai đoạn 7)
                            if (data.isAvailable && original && value !== original) {
                                setState("warn", "Đổi slug sẽ làm link cũ không còn truy cập được.");
                            }
                        })
                        .catch(function () { setState("", ""); });
                }, 400);
            }

            function setState(kind, text) {
                state.className = "adm-slug-state" + (kind ? " is-" + kind : "");
                state.innerHTML = kind === "busy" ? '<span class="adm-spin-sm"></span>'
                    : kind === "ok" ? '<i class="bi bi-check-circle-fill"></i>'
                    : kind === "bad" ? '<i class="bi bi-x-circle-fill"></i>'
                    : kind === "warn" ? '<i class="bi bi-exclamation-triangle-fill"></i>' : "";

                if (msg) {
                    msg.textContent = text;
                    msg.className = "adm-slug-msg" + (kind && kind !== "busy" ? " is-" + kind : "");
                }
            }

            if (input.value) check();
        });
    }

    /* Bản JS của SlugService.Slugify — chỉ để xem trước. Server LUÔN chuẩn hóa lại. */
    function slugify(text) {
        return (text || "")
            .replace(/đ/g, "d").replace(/Đ/g, "D")
            .normalize("NFD").replace(/[̀-ͯ]/g, "")
            .toLowerCase()
            .replace(/[^a-z0-9]+/g, "-")
            .replace(/-{2,}/g, "-")
            .replace(/^-|-$/g, "");
    }
    RZAdmin.slugify = slugify;

    /* 13. Tab trong form */
    function initFormTabs() {
        document.querySelectorAll("[data-tabs]").forEach(function (group) {
            var tabs = group.querySelectorAll(".adm-form-tab");
            var panels = group.querySelectorAll(".adm-tab-panel");

            tabs.forEach(function (tab) {
                tab.addEventListener("click", function () {
                    var target = tab.getAttribute("data-tab");
                    tabs.forEach(function (t) {
                        var on = t === tab;
                        t.classList.toggle("is-active", on);
                        t.setAttribute("aria-selected", on ? "true" : "false");
                    });
                    panels.forEach(function (p) {
                        p.classList.toggle("is-active", p.getAttribute("data-panel") === target);
                    });
                });
            });

            // Lỗi validate nằm ở tab đang ẩn ⇒ chấm đỏ trên nhãn tab, nếu không người dùng
            // bấm Lưu mãi mà không hiểu vì sao không được
            panels.forEach(function (p) {
                if (!p.querySelector(".input-validation-error, .field-validation-error:not(:empty)")) return;
                var tab = group.querySelector('[data-tab="' + p.getAttribute("data-panel") + '"]');
                if (tab) tab.classList.add("has-error");
            });
        });
    }

    /* Khởi động */
    function init() {
        initSidebar();
        initCountUp();
        initConfirmButtons();
        initTable();
        initSearchDebounce();
        initFormSubmit();
        initDirtyGuard();
        initMoneyInputs();
        initSlugInputs();
        initFormTabs();

        // Toast từ TempData sau PRG
        var pending = document.getElementById("admPendingToast");
        if (pending) {
            RZAdmin.toast(pending.getAttribute("data-message"), pending.getAttribute("data-type"));
        }
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", init);
    } else {
        init();
    }

    window.RZAdmin = RZAdmin;
})(window, document);
