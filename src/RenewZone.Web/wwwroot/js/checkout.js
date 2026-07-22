/* checkout.js — trang /dat-hang (radio-card thanh toán, panel chuyển khoản, chống double-submit)
   và /dat-hang-thanh-cong (copy mã đơn). Nạp qua @section Scripts của 2 view đó. */
(function () {
    "use strict";

    /* ---------- 1. Panel thông tin chuyển khoản hé lộ theo lựa chọn ---------- */
    var bankPanel = document.getElementById("bankPanel");
    var payRadios = document.querySelectorAll(".pay-radio");

    function syncBankPanel() {
        if (!bankPanel) return;
        var checked = document.querySelector(".pay-radio:checked");
        var isTransfer = !!checked && checked.value === "BankTransfer";
        bankPanel.classList.toggle("is-open", isTransfer);
    }

    payRadios.forEach(function (radio) {
        radio.addEventListener("change", syncBankPanel);
    });
    syncBankPanel();

    /* ---------- 2. Chặn double-submit ---------- */
    var form = document.getElementById("checkoutForm");
    var submitBtn = document.getElementById("checkoutSubmit");

    if (form && submitBtn) {
        form.addEventListener("submit", function () {
            // Listener này chạy TRƯỚC handler của jquery-validation, nên phải tự hỏi
            // "form có hợp lệ không" — nếu không, đừng khóa nút (khách còn phải sửa).
            if (window.jQuery && jQuery(form).data("validator") && !jQuery(form).valid()) return;

            // setTimeout để nút chưa bị disabled lúc trình duyệt gom dữ liệu form.
            setTimeout(function () {
                submitBtn.disabled = true;
                submitBtn.innerHTML =
                    '<span class="spinner-border" role="status" aria-hidden="true"></span> Đang xử lý...';
            }, 0);
        });
    }

    /* ---------- 3. Trang cảm ơn: copy mã đơn ---------- */
    var copyBtn = document.getElementById("copyOrderCode");
    if (copyBtn) {
        copyBtn.addEventListener("click", function () {
            var code = copyBtn.getAttribute("data-code") || "";

            var done = function () {
                if (window.RZ && RZ.toast) RZ.toast("Đã copy mã đơn " + code);
                copyBtn.innerHTML = '<i class="bi bi-check-lg"></i>';
                setTimeout(function () { copyBtn.innerHTML = '<i class="bi bi-clipboard"></i>'; }, 1800);
            };

            if (navigator.clipboard && window.isSecureContext) {
                navigator.clipboard.writeText(code).then(done).catch(fallbackCopy);
            } else {
                fallbackCopy();
            }

            function fallbackCopy() {
                // http:// (không secure context) không có Clipboard API — dùng cách cũ
                var tmp = document.createElement("textarea");
                tmp.value = code;
                tmp.setAttribute("readonly", "");
                tmp.style.position = "fixed";
                tmp.style.opacity = "0";
                document.body.appendChild(tmp);
                tmp.select();
                try { document.execCommand("copy"); done(); } catch (e) { /* bỏ qua */ }
                tmp.remove();
            }
        });
    }
})();
