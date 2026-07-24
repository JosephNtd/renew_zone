/* admin-review.js — màn "Đánh giá sản phẩm" (Giai đoạn 8).
   Duyệt / từ chối / trả lời bằng AJAX (dùng RZAdmin.post — tự kèm anti-forgery).
   Xóa đi qua nút [data-confirm] chung của admin.js (submit form) nên KHÔNG xử lý ở đây. */
(function () {
    "use strict";

    var list = document.getElementById("reviewList");
    if (!list || !window.RZAdmin) return;

    var STATUS_LABEL = { Approved: "Đã duyệt", Rejected: "Đã từ chối", Pending: "Chờ duyệt" };
    var STATUS_CLASS = { Approved: "adm-badge-done", Rejected: "adm-badge-cancel", Pending: "adm-badge-new" };

    function cardOf(el) { return el.closest(".adm-review"); }

    // Đổi trạng thái (duyệt / từ chối)
    list.addEventListener("click", function (e) {
        var btn = e.target.closest(".js-review-status");
        if (!btn) return;

        var card = cardOf(btn);
        var id = card.getAttribute("data-id");
        var status = btn.getAttribute("data-status");

        setBusy(card, true);
        RZAdmin.post("/admin/danh-gia-san-pham/" + id + "/trang-thai", { status: status })
            .then(function (data) {
                if (!data.ok) { RZAdmin.toast(data.message, "error"); return; }

                var badge = card.querySelector('[data-role=status-badge]');
                if (badge) {
                    badge.textContent = STATUS_LABEL[status] || status;
                    badge.className = "adm-badge " + (STATUS_CLASS[status] || "adm-badge-muted");
                }
                card.classList.toggle("is-pending", status === "Pending");
                refreshActionButtons(card, status);
                RZAdmin.toast(data.message, "success");
            })
            .finally(function () { setBusy(card, false); });
    });

    // Mở/đóng ô trả lời
    list.addEventListener("click", function (e) {
        if (e.target.closest(".js-review-reply-toggle")) {
            var box = cardOf(e.target).querySelector('[data-role=reply-box]');
            if (box) { box.hidden = !box.hidden; if (!box.hidden) { var t = box.querySelector("textarea"); if (t) t.focus(); } }
            return;
        }
        if (e.target.closest(".js-review-reply-cancel")) {
            var box2 = cardOf(e.target).querySelector('[data-role=reply-box]');
            if (box2) box2.hidden = true;
        }
    });

    // Lưu phản hồi
    list.addEventListener("click", function (e) {
        var btn = e.target.closest(".js-review-reply-save");
        if (!btn) return;

        var card = cardOf(btn);
        var id = card.getAttribute("data-id");
        var input = card.querySelector('[data-role=reply-input]');
        var reply = input ? input.value : "";

        setBusy(card, true);
        RZAdmin.post("/admin/danh-gia-san-pham/" + id + "/tra-loi", { reply: reply })
            .then(function (data) {
                if (!data.ok) { RZAdmin.toast(data.message, "error"); return; }
                updateReplyView(card, reply.trim());
                var box = card.querySelector('[data-role=reply-box]');
                if (box) box.hidden = true;
                RZAdmin.toast(data.message, "success");
            })
            .finally(function () { setBusy(card, false); });
    });

    function setBusy(card, busy) {
        card.querySelectorAll("button").forEach(function (b) { b.disabled = busy; });
    }

    // Sau khi đổi trạng thái: ẩn nút của trạng thái hiện tại, hiện nút còn lại
    function refreshActionButtons(card, status) {
        var approve = card.querySelector('.js-review-status[data-status=Approved]');
        var reject = card.querySelector('.js-review-status[data-status=Rejected]');
        if (approve) approve.style.display = status === "Approved" ? "none" : "";
        if (reject) reject.style.display = status === "Rejected" ? "none" : "";
    }

    // Cập nhật khối "Phản hồi từ shop" tại chỗ (thêm/sửa/gỡ) — chống XSS bằng textContent
    function updateReplyView(card, replyText) {
        var view = card.querySelector('[data-role=reply-view]');

        if (!replyText) {
            if (view) view.remove();
            return;
        }

        if (!view) {
            view = document.createElement("div");
            view.className = "adm-review-reply";
            view.setAttribute("data-role", "reply-view");
            var head = document.createElement("div");
            head.className = "adm-review-reply-head";
            head.innerHTML = '<i class="bi bi-shop" aria-hidden="true"></i> Phản hồi từ shop';
            var p = document.createElement("p");
            view.appendChild(head);
            view.appendChild(p);
            var content = card.querySelector(".adm-review-content");
            if (content && content.parentNode) content.parentNode.insertBefore(view, content.nextSibling);
        }
        view.querySelector("p").textContent = replyText;
    }
})();
