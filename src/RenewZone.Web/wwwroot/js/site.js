/* site.js — Header shrink · Back-to-top · Reveal-on-scroll · Menu tài khoản · Toast · Cart badge */
(function () {
    "use strict";

    /* 0. Menu tài khoản mở khi RÊ CHUỘT (Giai đoạn 9).
       Menu chính của site vốn đã mở bằng hover (thuần CSS), nên menu tài khoản cũng hover cho
       nhất quán. Nhưng ở đây KHÔNG làm bằng CSS thuần được vì 3 lý do:

         • Chỉ bật trên thiết bị THẬT SỰ có chuột. Trên điện thoại/máy tính bảng, cú chạm đầu
           tiên bị trình duyệt giả lập thành hover ⇒ menu mở rồi đóng ngay, phải chạm hai lần.
         • Menu này có mục "Đăng xuất". Nó nằm sát nút giỏ hàng, chuột đi ngang qua là menu bật
           ra — nên phải có ĐỘ TRỄ khi rời chuột, tránh vừa lỡ tay vừa nhấp trúng Đăng xuất.
         • Có khoảng hở giữa nút và panel; rời nút là menu đóng trước khi chuột kịp tới panel.
           Vì vậy vùng theo dõi hover là cả cụm .acc-menu (nút + panel), không riêng cái nút.

       Vẫn đi qua API Dropdown của Bootstrap chứ không tự thêm/bớt class: bấm chuột, phím Esc,
       bấm ra ngoài, bẫy focus… đều giữ nguyên hành vi sẵn có. */
    const accMenu = document.querySelector(".acc-menu");
    const canHover = window.matchMedia("(hover: hover) and (pointer: fine)").matches;

    if (accMenu && canHover && window.bootstrap) {
        const toggle = accMenu.querySelector('[data-bs-toggle="dropdown"]');
        if (toggle) {
            const dropdown = bootstrap.Dropdown.getOrCreateInstance(toggle);
            let closeTimer = null;

            accMenu.addEventListener("mouseenter", () => {
                clearTimeout(closeTimer);
                dropdown.show();
            });

            accMenu.addEventListener("mouseleave", () => {
                // 220ms đủ để chuột đi chéo từ nút xuống panel mà không bị đóng giữa chừng
                closeTimer = setTimeout(() => dropdown.hide(), 220);
            });
        }
    }

    /* 1. Header shrink khi cuộn */
    const header = document.getElementById("siteHeader");
    const backToTop = document.getElementById("backToTop");

    function onScroll() {
        const y = window.scrollY;
        if (header) header.classList.toggle("scrolled", y > 80);
        if (backToTop) backToTop.classList.toggle("show", y > 400);
    }
    window.addEventListener("scroll", onScroll, { passive: true });
    onScroll();

    /* 2. Back to top */
    if (backToTop) {
        backToTop.addEventListener("click", () => window.scrollTo({ top: 0, behavior: "smooth" }));
    }

    /* 3. Reveal-on-scroll (thay animate.css của site gốc).
       Dùng: thêm class "reveal" vào phần tử. Các phần tử reveal cùng cha
       được stagger 60ms theo thứ tự xuất hiện. */
    const revealEls = document.querySelectorAll(".reveal");
    if (revealEls.length && "IntersectionObserver" in window) {
        const io = new IntersectionObserver((entries) => {
            entries.forEach((entry) => {
                if (!entry.isIntersecting) return;
                const el = entry.target;
                const siblings = el.parentElement
                    ? [...el.parentElement.children].filter(c => c.classList.contains("reveal"))
                    : [el];
                el.style.setProperty("--reveal-delay", (siblings.indexOf(el) * 60) + "ms");
                el.classList.add("is-inview");
                io.unobserve(el);
            });
        }, { threshold: 0.12 });
        revealEls.forEach(el => io.observe(el));
    } else {
        revealEls.forEach(el => el.classList.add("is-inview"));
    }

    /* 4. API dùng chung (toast + cart badge) */
    window.RZ = {
        /** Hiện toast góc phải. type: "success" | "error" */
        toast(message, type = "success") {
            const stack = document.getElementById("toastStack");
            if (!stack) return;
            const el = document.createElement("div");
            el.className = "rz-toast";
            if (type === "error") el.style.borderLeftColor = "var(--red-500)";
            el.innerHTML =
                '<div class="d-flex align-items-center gap-2">' +
                (type === "success"
                    ? '<i class="bi bi-check-circle-fill" style="color:var(--success)"></i>'
                    : '<i class="bi bi-exclamation-circle-fill" style="color:var(--red-500)"></i>') +
                '<div class="small fw-semibold">' + message + "</div>" +
                "</div>" +
                '<div class="toast-progress"></div>';
            stack.appendChild(el);
            setTimeout(() => {
                el.classList.add("leaving");
                el.addEventListener("animationend", () => el.remove(), { once: true });
            }, 3500);
        },

        /** Cập nhật số trên badge giỏ hàng kèm hiệu ứng pop (Giai đoạn 4 gọi qua AJAX). */
        cartBadge(count) {
            document.querySelectorAll(".badge-count").forEach((b) => {
                b.textContent = count;
                b.classList.remove("pop");
                void b.offsetWidth; // restart animation
                b.classList.add("pop");
            });
        }
    };
})();
