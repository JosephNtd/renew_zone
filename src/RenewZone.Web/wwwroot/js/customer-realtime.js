/* customer-realtime.js — nhận thông báo trạng thái đơn hàng theo thời gian thực (Giai đoạn 9).
   CHỈ nạp cho khách ĐÃ đăng nhập (_Layout tự kiểm) — khách vãng lai không có gì để nghe.

   Đối xứng với admin-realtime.js: server đẩy sự kiện "donCapNhat" qua CustomerNotifyHub, client
   hiện toast + cập nhật ngay nhãn trạng thái đang hiển thị trên trang, không cần F5.

   Mọi thứ FAIL-SOFT: thiếu thư viện signalR, mất mạng, lỗi audio... đều bỏ qua im lặng.
   Đây là tiện ích phụ — hỏng nó tuyệt đối không được ảnh hưởng việc mua hàng. */
(function () {
    "use strict";

    if (typeof signalR === "undefined") return;

    var connection = new signalR.HubConnectionBuilder()
        .withUrl("/hub/thong-bao")
        .withAutomaticReconnect()
        .build();

    /* Cập nhật tại chỗ mọi nhãn trạng thái của đơn đang hiển thị trên trang.
       Payload chỉ để HIỂN THỊ — nguồn sự thật vẫn là DB, F5 luôn là đường lùi đúng. */
    function repaintStatus(data) {
        document.querySelectorAll(".acc-status").forEach(function (el) {
            el.className = "acc-status " + data.statusClass;

            // Dựng lại bằng createElement/textContent chứ KHÔNG nội suy innerHTML: nhãn đến từ
            // server nhưng nguyên tắc là dữ liệu chạy qua mạng thì không bao giờ ghép thẳng vào HTML.
            el.textContent = "";
            var icon = document.createElement("i");
            icon.className = "bi " + data.statusIcon;
            el.appendChild(icon);
            el.appendChild(document.createTextNode(" " + data.statusText));
        });

        var hint = document.querySelector(".js-status-hint");
        if (hint) hint.textContent = data.statusHint || "";
    }

    connection.on("donCapNhat", function (data) {
        if (!data) return;

        if (window.RZ && RZ.toast) {
            RZ.toast("Đơn hàng của bạn: " + data.statusText, "success");
        }

        repaintStatus(data);
    });

    connection.start().catch(function () {
        // Không kết nối được (mạng chập chờn, server khởi động lại) — withAutomaticReconnect
        // sẽ tự thử lại. Không báo gì cho khách: họ không cần biết về kênh thông báo phụ này.
    });
})();
