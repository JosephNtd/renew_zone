/* admin-realtime.js (Giai đoạn 8)
   Nhận thông báo realtime qua SignalR và cập nhật trang admin KHÔNG cần reload:
     • "donMoi" (đơn hàng mới): chuông #admOrderBell nhảy số + toast + ting + chèn dòng đơn.
     • "binhLuanMoi" (đánh giá mới): chuông #admReviewBell nhảy số + toast + ting.

   Nạp SAU admin.js (cần RZAdmin) và signalr.min.js (cần window.signalR).
   Mọi thứ đều "fail-soft": thiếu SignalR / không có chuông / lỗi audio đều bỏ qua im lặng,
   không bao giờ ném ra console-noise làm hỏng các script khác của trang. */
(function (window, document) {
    "use strict";

    var reduceMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    var SOUND_KEY = "rz_adm_sound"; // "on" | "off" — mặc định BẬT (giá trị khác "off")

    /* Nút loa bật/tắt tiếng */
    function soundEnabled() {
        return localStorage.getItem(SOUND_KEY) !== "off";
    }

    function initSoundToggle() {
        var btn = document.getElementById("admSoundToggle");
        if (!btn) return;

        var icon = btn.querySelector("i");

        function render() {
            var on = soundEnabled();
            btn.setAttribute("aria-pressed", on ? "true" : "false");
            btn.title = on ? "Tắt tiếng thông báo" : "Bật tiếng thông báo";
            if (icon) icon.className = "bi " + (on ? "bi-volume-up" : "bi-volume-mute");
        }

        render();
        btn.addEventListener("click", function () {
            localStorage.setItem(SOUND_KEY, soundEnabled() ? "off" : "on");
            render();
            // Bấm bật ⇒ phát thử 1 tiếng để admin biết đã bật (đồng thời "mở khóa" AudioContext).
            if (soundEnabled()) playTing();
        });
    }

    /* Tiếng "ting" bằng WebAudio (không cần asset) */
    var audioCtx = null;

    function playTing() {
        if (!soundEnabled()) return;

        try {
            var Ctx = window.AudioContext || window.webkitAudioContext;
            if (!Ctx) return;
            if (!audioCtx) audioCtx = new Ctx();

            // Trước tương tác đầu tiên trình duyệt có thể để AudioContext ở trạng thái "suspended".
            // Cố resume; nếu vẫn chưa được thì bỏ qua im lặng (không ném).
            if (audioCtx.state === "suspended") audioCtx.resume();

            var now = audioCtx.currentTime;
            // Hai nốt ngắn đi lên: nghe như "ting" nhẹ nhàng, không chói.
            beep(880, now, 0.12);
            beep(1174, now + 0.11, 0.16);
        } catch (e) {
            // Autoplay policy hoặc thiết bị không có audio — không phải lỗi cần báo.
        }
    }

    function beep(freq, startAt, duration) {
        var osc = audioCtx.createOscillator();
        var gain = audioCtx.createGain();
        osc.type = "sine";
        osc.frequency.value = freq;
        gain.gain.setValueAtTime(0.0001, startAt);
        gain.gain.exponentialRampToValueAtTime(0.16, startAt + 0.02);
        gain.gain.exponentialRampToValueAtTime(0.0001, startAt + duration);
        osc.connect(gain).connect(audioCtx.destination);
        osc.start(startAt);
        osc.stop(startAt + duration + 0.02);
    }

    /* Tăng badge của một chuông trên topbar (dùng chung cho chuông đơn + chuông đánh giá) */
    function bumpBell(bellId, titleFor) {
        var bell = document.getElementById(bellId);
        if (!bell) return;

        var count = (parseInt(bell.getAttribute("data-count"), 10) || 0) + 1;
        bell.setAttribute("data-count", count);
        bell.title = titleFor(count);

        var badge = bell.querySelector('[data-role="count"]');
        if (badge) {
            badge.textContent = count > 99 ? "99+" : String(count);
            badge.hidden = false;
            if (!reduceMotion) {
                badge.classList.remove("is-bump");
                void badge.offsetWidth; // ép reflow để animation chạy lại nếu badge đã bump trước đó
                badge.classList.add("is-bump");
            }
        }
    }

    /* Chèn dòng đơn mới vào đầu bảng danh sách đơn */
    function isOrderListPage() {
        // Chỉ chèn khi đang thực sự ở màn danh sách đơn (có bảng đơn dạng thẻ trên đường dẫn đúng).
        return window.location.pathname.indexOf("/admin/don-hang") === 0
            && !!document.querySelector(".adm-table-cards tbody");
    }

    function el(tag, attrs, text) {
        var node = document.createElement(tag);
        if (attrs) Object.keys(attrs).forEach(function (k) { node.setAttribute(k, attrs[k]); });
        if (text != null) node.textContent = text; // textContent ⇒ dữ liệu khách nhập được escape, chống XSS
        return node;
    }

    function prependOrderRow(p) {
        var tbody = document.querySelector(".adm-table-cards tbody");
        if (!tbody) return;

        // ⚠️ Nếu màn đang LỌC (vd tab "Đã hủy") thì đơn "Mới" có thể không khớp bộ lọc; ta vẫn
        // chèn lên đầu cho đơn giản — F5 sẽ hiển thị đúng theo bộ lọc. Chấp nhận được cho MVP.

        var detailUrl = "/admin/don-hang/" + p.id;
        var tr = el("tr", { "data-order-id": p.id, "class": "adm-row-fresh" });

        // 1. Mã đơn (link)
        var tdCode = el("td", { "data-label": "Mã đơn" });
        tdCode.appendChild(el("a", { "class": "fw-bold", href: detailUrl }, p.code));
        tr.appendChild(tdCode);

        // 2. Khách hàng + SĐT
        var tdCust = el("td", { "data-label": "Khách hàng" });
        tdCust.appendChild(el("div", null, p.customerName));
        tdCust.appendChild(el("div", { "class": "adm-small adm-muted adm-num" }, p.phone));
        tr.appendChild(tdCust);

        // 3. Số món
        tr.appendChild(el("td", { "data-label": "Số món", "class": "adm-col-right adm-num" }, String(p.itemCount)));

        // 4. Tổng tiền (đã format ở server)
        tr.appendChild(el("td", { "data-label": "Tổng tiền", "class": "adm-col-right adm-num fw-bold" }, p.totalText));

        // 5. Thanh toán (icon + nhãn)
        var tdPay = el("td", { "data-label": "Thanh toán" });
        var paySpan = el("span", { "class": "adm-small" });
        paySpan.appendChild(el("i", { "class": "bi " + p.paymentIcon, "aria-hidden": "true" }));
        paySpan.appendChild(document.createTextNode(" " + p.paymentLabel));
        tdPay.appendChild(paySpan);
        tr.appendChild(tdPay);

        // 6. Trạng thái — badge "Mới" + dropdown đổi trạng thái (khớp markup view để handler
        //    đổi-trạng-thái sẵn có ở Order/Index tự hoạt động: class js-status-badge/menu/item)
        var tdStatus = el("td", { "data-label": "Trạng thái" });
        tdStatus.appendChild(buildStatusCell(p));
        tr.appendChild(tdStatus);

        // 7. Ngày đặt
        var tdDate = el("td", { "data-label": "Ngày đặt" });
        var d = p.createdAt ? new Date(p.createdAt) : new Date();
        tdDate.appendChild(el("div", { "class": "adm-small" }, formatDate(d)));
        tdDate.appendChild(el("div", { "class": "adm-small adm-muted" }, formatTime(d)));
        tr.appendChild(tdDate);

        // 8. Thao tác
        var tdAct = el("td", { "data-label": "Thao tác", "class": "adm-col-actions" });
        var actWrap = el("span", { "class": "adm-row-actions" });
        var view = el("a", { "class": "adm-act", href: detailUrl, title: "Xem chi tiết đơn " + p.code });
        view.appendChild(el("i", { "class": "bi bi-eye", "aria-hidden": "true" }));
        view.appendChild(el("span", { "class": "visually-hidden" }, "Xem chi tiết"));
        actWrap.appendChild(view);
        tdAct.appendChild(actWrap);
        tr.appendChild(tdAct);

        tbody.insertBefore(tr, tbody.firstChild);

        // Nếu trang trước đó rỗng và đang hiện empty-state, người dùng vẫn nên F5 để đồng bộ
        // phân trang/số đếm — nhưng dòng vừa chèn đã cho thấy đơn mới ngay.
    }

    function buildStatusCell(p) {
        var next = p.allowedNext || [];
        if (!next.length) {
            return el("span", { "class": "adm-badge " + p.statusClass + " js-status-badge" }, p.statusText);
        }

        var wrap = el("div", { "class": "dropdown" });

        var btn = el("button", {
            type: "button",
            "class": "adm-badge " + p.statusClass + " js-status-badge",
            "data-bs-toggle": "dropdown",
            "aria-expanded": "false",
            title: "Đổi trạng thái đơn " + p.code
        });
        btn.appendChild(document.createTextNode(p.statusText + " "));
        btn.appendChild(el("i", { "class": "bi bi-chevron-down", "aria-hidden": "true" }));
        wrap.appendChild(btn);

        var menu = el("div", { "class": "dropdown-menu adm-dropdown-menu js-status-menu" });
        next.forEach(function (s) {
            var item = el("button", { type: "button", "class": "dropdown-item js-status-item", "data-status": s.value });
            item.appendChild(el("i", { "class": "bi " + s.icon + " me-2", "aria-hidden": "true" }));
            item.appendChild(document.createTextNode("Chuyển sang “" + s.text + "”"));
            menu.appendChild(item);
        });
        wrap.appendChild(menu);

        return wrap;
    }

    function pad2(n) { return n < 10 ? "0" + n : String(n); }
    function formatDate(d) { return pad2(d.getDate()) + "/" + pad2(d.getMonth() + 1) + "/" + d.getFullYear(); }
    function formatTime(d) { return pad2(d.getHours()) + ":" + pad2(d.getMinutes()); }

    /* Kết nối SignalR */
    function connect() {
        if (!window.signalR) return; // client chưa nạp — bỏ qua, trang vẫn dùng bình thường (F5 thấy đơn)

        var connection = new signalR.HubConnectionBuilder()
            .withUrl("/admin/hub/thong-bao")
            .withAutomaticReconnect()
            .build();

        connection.on("donMoi", function (payload) {
            bumpBell("admOrderBell", function (n) { return n + " đơn mới chờ xử lý"; });

            var code = payload && payload.code ? payload.code : "";
            if (window.RZAdmin && RZAdmin.toast) {
                RZAdmin.toast("Đơn mới #" + code, "info", 6000);
            }
            if (window.RZAdmin && RZAdmin.announce) {
                RZAdmin.announce("Có đơn hàng mới, mã " + code);
            }

            playTing();

            if (isOrderListPage()) {
                try { prependOrderRow(payload); } catch (e) { /* markup lệch cũng không được vỡ trang */ }
            }
        });

        connection.on("binhLuanMoi", function (payload) {
            bumpBell("admReviewBell", function (n) { return n + " đánh giá chờ duyệt"; });

            var name = payload && payload.authorName ? payload.authorName : "Khách";
            var product = payload && payload.productName ? payload.productName : "sản phẩm";
            if (window.RZAdmin && RZAdmin.toast) {
                RZAdmin.toast("Đánh giá mới từ " + name + " cho " + product, "info", 6000);
            }
            if (window.RZAdmin && RZAdmin.announce) {
                RZAdmin.announce("Có đánh giá sản phẩm mới đang chờ duyệt.");
            }

            playTing();
        });

        // Kết nối thất bại (phiên hết hạn, mạng...) không phải lỗi chặn — realtime chỉ là phụ trợ.
        connection.start().catch(function () { /* im lặng: F5 vẫn là đường lùi luôn đúng */ });
    }

    function init() {
        initSoundToggle();
        // Chỉ kết nối khi ở trong khu admin đã đăng nhập (có chuông đơn trên topbar).
        if (document.getElementById("admOrderBell")) connect();
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", init);
    } else {
        init();
    }
})(window, document);
