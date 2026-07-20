/* =====================================================================
   RenewZone Clone — site.js
   Header shrink · Back-to-top · Reveal-on-scroll · Toast · Cart badge
   ===================================================================== */
(function () {
    "use strict";

    /* ---------- 1. Header shrink khi cuộn ---------- */
    const header = document.getElementById("siteHeader");
    const backToTop = document.getElementById("backToTop");

    function onScroll() {
        const y = window.scrollY;
        if (header) header.classList.toggle("scrolled", y > 80);
        if (backToTop) backToTop.classList.toggle("show", y > 400);
    }
    window.addEventListener("scroll", onScroll, { passive: true });
    onScroll();

    /* ---------- 2. Back to top ---------- */
    if (backToTop) {
        backToTop.addEventListener("click", () => window.scrollTo({ top: 0, behavior: "smooth" }));
    }

    /* ---------- 3. Reveal-on-scroll (thay animate.css của site gốc) ----------
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

    /* ---------- 4. API dùng chung (toast + cart badge) ---------- */
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
