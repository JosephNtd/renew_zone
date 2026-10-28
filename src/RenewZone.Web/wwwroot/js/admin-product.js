/* admin-product.js — form sản phẩm 5 tab (chỉ nạp ở màn này, không vào admin.js)
   Gồm: uploader ảnh · repeater biến thể · xem trước giá.
   TinyMCE do admin-editor.js lo (dùng chung với form bài viết/trang tĩnh). */
(function (window, document) {
    "use strict";

    /* 1. Giá liên hệ khóa 2 ô giá */
    function initContactPrice() {
        var toggle = document.getElementById("IsContactPrice");
        var hint = document.getElementById("contactPriceHint");
        if (!toggle) return;

        function apply() {
            var on = toggle.checked;
            document.querySelectorAll(".js-price").forEach(function (input) {
                input.disabled = on;
            });
            if (hint) hint.hidden = !on;
            updatePreview();
        }

        toggle.addEventListener("change", apply);
        apply();
    }

    /* 2. Đếm ký tự mô tả ngắn */
    function initShortDescCount() {
        var input = document.getElementById("ShortDescription");
        var out = document.getElementById("shortDescCount");
        if (!input || !out) return;

        var render = function () { out.textContent = input.value.length; };
        input.addEventListener("input", render);
        render();
    }

    /* 3. Repeater biến thể — nhóm và giá trị CHỌN từ danh sách chung (Giai đoạn 10) */
    var groupList = document.getElementById("groupList");
    var attributes = readAttributes();

    function readAttributes() {
        var el = document.getElementById("attrData");
        if (!el) return {};
        var map = {};
        JSON.parse(el.textContent || "[]").forEach(function (a) { map[a.id] = a; });
        return map;
    }

    /** Đổ lại ô chọn giá trị theo thuộc tính của nhóm; giữ lựa chọn cũ nếu vẫn thuộc danh sách. */
    function fillValueSelect(select, attributeId) {
        var keep = select.value;
        var attr = attributes[attributeId];
        select.innerHTML = "";

        var placeholder = document.createElement("option");
        placeholder.value = "0";
        placeholder.textContent = attr ? "— Chọn giá trị —" : "— Chọn thuộc tính trước —";
        select.appendChild(placeholder);

        if (attr) {
            attr.values.forEach(function (v) {
                var opt = document.createElement("option");
                opt.value = v.id;
                opt.textContent = v.value;
                select.appendChild(opt);
            });
        }

        select.value = select.querySelector('option[value="' + keep + '"]') ? keep : "0";
    }

    /**
     * Khóa những lựa chọn đã dùng: một thuộc tính chỉ thuộc một nhóm, một giá trị chỉ chọn một lần
     * trong nhóm. Server vẫn kiểm lại — đây chỉ để admin không chọn nhầm ngay từ đầu.
     */
    function refreshDisabled() {
        var groups = groupList.querySelectorAll(".js-group");
        var usedAttrs = Array.prototype.map.call(groups, function (g) { return g.querySelector(".js-group-attr").value; });

        groups.forEach(function (group, gi) {
            group.querySelectorAll(".js-group-attr option").forEach(function (opt) {
                opt.disabled = opt.value !== "0" && usedAttrs.some(function (id, i) { return i !== gi && id === opt.value; });
            });

            var selects = group.querySelectorAll(".js-value-attr");
            var usedValues = Array.prototype.map.call(selects, function (s) { return s.value; });
            selects.forEach(function (select, vi) {
                select.querySelectorAll("option").forEach(function (opt) {
                    opt.disabled = opt.value !== "0" && usedValues.some(function (id, i) { return i !== vi && id === opt.value; });
                });
            });
        });
    }

    function initRepeater() {
        if (!groupList) return;

        document.getElementById("addGroup").addEventListener("click", function () {
            var node = document.getElementById("tplGroup").content.cloneNode(true);
            groupList.appendChild(node);
            addValueRow(groupList.lastElementChild); // nhóm mới luôn có sẵn 1 dòng giá trị
            reindex();
        });

        groupList.addEventListener("click", function (e) {
            var addValue = e.target.closest(".js-add-value");
            if (addValue) { addValueRow(addValue.closest(".js-group")); reindex(); return; }

            var delGroup = e.target.closest(".js-del-group");
            if (delGroup) { removeNode(delGroup.closest(".js-group")); return; }

            var delValue = e.target.closest(".js-del-value");
            if (delValue) { removeNode(delValue.closest(".js-value")); return; }
        });

        // Radio "Mặc định" chỉ gửi được 1 giá trị ⇒ phải đồng bộ sang hidden của TỪNG dòng
        groupList.addEventListener("change", function (e) {
            if (e.target.closest(".js-value-default")) { syncDefaults(); updatePreview(); }
            if (e.target.closest(".js-value-adj")) { updatePreview(); }

            var attrSelect = e.target.closest(".js-group-attr");
            if (attrSelect) {
                attrSelect.closest(".js-group").querySelectorAll(".js-value-attr").forEach(function (select) {
                    fillValueSelect(select, attrSelect.value);
                });
            }

            if (attrSelect || e.target.closest(".js-value-attr")) { refreshDisabled(); }
        });

        groupList.addEventListener("input", function (e) {
            if (e.target.closest(".js-value-adj")) { updatePreview(); }
        });
    }

    function addValueRow(group) {
        var list = group.querySelector(".js-value-list");
        var node = document.getElementById("tplValue").content.cloneNode(true);
        list.appendChild(node);
        fillValueSelect(list.lastElementChild.querySelector(".js-value-attr"),
            group.querySelector(".js-group-attr").value);

        // Nhóm chưa có mặc định nào ⇒ dòng đầu tiên tự làm mặc định
        if (list.querySelectorAll(".js-value-default:checked").length === 0) {
            list.querySelector(".js-value").querySelector(".js-value-default").checked = true;
        }
    }

    function removeNode(node) {
        // Gập lại 200ms rồi mới bỏ khỏi DOM — biến mất đột ngột dễ làm mất dấu chỗ vừa xóa
        node.style.transition = "opacity .2s, transform .2s";
        node.style.opacity = "0";
        node.style.transform = "translateY(-6px)";

        setTimeout(function () {
            node.remove();
            reindex();
            updatePreview();
        }, 200);
    }

    /**
     * ⚠️ ĐÁNH LẠI INDEX SAU MỌI THAY ĐỔI — model binder của ASP.NET dừng ở chỗ ĐỨT index:
     * xóa nhóm giữa mà không đánh lại thì OptionGroups[2], [3]... biến mất ÂM THẦM khi lưu.
     * Đây là bẫy số 6 trong kế hoạch Giai đoạn 6.
     */
    function reindex() {
        groupList.querySelectorAll(".js-group").forEach(function (group, gi) {
            group.dataset.index = gi;
            group.querySelector(".js-group-id").name = "OptionGroups[" + gi + "].Id";
            group.querySelector(".js-group-attr").name = "OptionGroups[" + gi + "].AttributeId";

            group.querySelectorAll(".js-value").forEach(function (row, vi) {
                row.dataset.index = vi;
                row.querySelector(".js-value-id").name = "OptionGroups[" + gi + "].Values[" + vi + "].Id";
                row.querySelector(".js-value-attr").name = "OptionGroups[" + gi + "].Values[" + vi + "].AttributeValueId";
                row.querySelector(".js-value-adj").name = "OptionGroups[" + gi + "].Values[" + vi + "].PriceAdjustment";
                row.querySelector(".js-value-default-field").name =
                    "OptionGroups[" + gi + "].Values[" + vi + "].IsDefault";

                // Radio phải cùng name trong CÙNG nhóm và khác name giữa các nhóm
                row.querySelector(".js-value-default").name = "default_group_" + gi;
            });
        });

        syncDefaults();
        refreshDisabled();
    }

    function syncDefaults() {
        groupList.querySelectorAll(".js-value").forEach(function (row) {
            var radio = row.querySelector(".js-value-default");
            row.querySelector(".js-value-default-field").value = radio.checked ? "true" : "false";
        });
    }

    /* 4. Xem trước giá theo lựa chọn mặc định */
    function updatePreview() {
        var preview = document.getElementById("variantPreview");
        if (!preview) return;

        var contact = document.getElementById("IsContactPrice");
        if (contact && contact.checked) {
            preview.textContent = "Liên hệ";
            return;
        }

        var sale = numberOf(document.getElementById("SalePrice"));
        var original = numberOf(document.getElementById("OriginalPrice"));
        var base = sale > 0 ? sale : original;

        var total = base;
        if (groupList) {
            groupList.querySelectorAll(".js-value").forEach(function (row) {
                if (row.querySelector(".js-value-default").checked) {
                    total += numberOf(row.querySelector(".js-value-adj"));
                }
            });
        }

        preview.textContent = total > 0 ? total.toLocaleString("vi-VN") + "đ" : "—";
    }

    function numberOf(input) {
        if (!input) return 0;
        var digits = String(input.value).replace(/[^\d-]/g, "");
        var n = parseInt(digits, 10);
        return isNaN(n) ? 0 : n;
    }

    /* 5. Uploader ảnh */
    function initUploader() {
        var zone = document.getElementById("imgDropzone");
        var input = document.getElementById("imgInput");
        var grid = document.getElementById("imgGrid");
        if (!zone || !input || !grid) return;

        var productId = grid.dataset.product;

        zone.addEventListener("click", function () { input.click(); });
        zone.addEventListener("keydown", function (e) {
            if (e.key === "Enter" || e.key === " ") { e.preventDefault(); input.click(); }
        });

        ["dragenter", "dragover"].forEach(function (evt) {
            zone.addEventListener(evt, function (e) {
                e.preventDefault(); zone.classList.add("is-over");
            });
        });
        ["dragleave", "drop"].forEach(function (evt) {
            zone.addEventListener(evt, function (e) {
                e.preventDefault(); zone.classList.remove("is-over");
            });
        });

        zone.addEventListener("drop", function (e) { upload(e.dataTransfer.files); });
        input.addEventListener("change", function () { upload(input.files); input.value = ""; });

        function upload(files) {
            if (!files || !files.length) return;

            var form = new FormData();
            for (var i = 0; i < files.length; i++) { form.append("files", files[i]); }

            // XMLHttpRequest chứ không phải fetch: cần sự kiện tiến trình upload
            var xhr = new XMLHttpRequest();
            xhr.open("POST", "/admin/san-pham/" + productId + "/anh");
            xhr.setRequestHeader("RequestVerificationToken", RZAdmin.token());

            var bar = document.createElement("div");
            bar.className = "adm-media-progress";
            bar.innerHTML = "<i></i>";
            zone.appendChild(bar);

            xhr.upload.onprogress = function (e) {
                if (e.lengthComputable) {
                    bar.querySelector("i").style.width = (e.loaded / e.total * 100) + "%";
                }
            };

            xhr.onload = function () {
                bar.remove();
                var data;
                try { data = JSON.parse(xhr.responseText); }
                catch (err) { RZAdmin.toast("Tải ảnh thất bại.", "error"); return; }

                RZAdmin.toast(data.message, data.ok ? "success" : "error");
                if (data.images) { render(data.images); }
            };

            xhr.onerror = function () {
                bar.remove();
                RZAdmin.toast("Không kết nối được máy chủ.", "error");
            };

            xhr.send(form);
        }

        grid.addEventListener("click", function (e) {
            var item = e.target.closest(".adm-media-item");
            if (!item) return;

            var imageId = item.dataset.id;

            if (e.target.closest(".js-set-main")) {
                RZAdmin.post("/admin/san-pham/" + productId + "/anh/" + imageId + "/chinh", {})
                    .then(function (res) {
                        RZAdmin.toast(res.message, res.ok ? "success" : "error");
                        if (res.images) render(res.images);
                    });
                return;
            }

            if (e.target.closest(".js-del-img")) {
                RZAdmin.confirm({
                    title: "Xóa ảnh?",
                    message: "Ảnh sẽ bị xóa vĩnh viễn khỏi máy chủ, không khôi phục được.",
                    okText: "Xóa ảnh"
                }).then(function (ok) {
                    if (!ok) return;

                    RZAdmin.post("/admin/san-pham/" + productId + "/anh/" + imageId + "/xoa", {})
                        .then(function (res) {
                            RZAdmin.toast(res.message, res.ok ? "success" : "error");
                            if (res.images) render(res.images);
                        });
                });
            }
        });

        function render(images) {
            grid.innerHTML = images.map(function (img) {
                return '<div class="adm-media-item' + (img.isMain ? " is-main" : "") + '" data-id="' + img.id + '">'
                    + '<img src="' + img.url + '" alt="" loading="lazy" />'
                    + (img.isMain ? '<span class="adm-media-badge">Ảnh chính</span>' : "")
                    + '<span class="adm-media-tools">'
                    + '<button type="button" class="adm-media-star js-set-main" title="Đặt làm ảnh chính">'
                    + '<i class="bi bi-star' + (img.isMain ? "-fill" : "") + '"></i></button>'
                    + '<button type="button" class="js-del-img" title="Xóa ảnh">'
                    + '<i class="bi bi-trash"></i></button>'
                    + "</span></div>";
            }).join("");

            initImageSort();
            updateImageCount(images.length);
        }

        function updateImageCount(count) {
            var tab = document.querySelector('[data-tab="images"] .adm-tab-count');
            if (tab) tab.textContent = count;
        }

        function initImageSort() {
            if (!window.Sortable) return;

            Sortable.create(grid, {
                animation: 150,
                ghostClass: "adm-sortable-ghost",
                onEnd: function () {
                    var ids = Array.prototype.map.call(grid.querySelectorAll("[data-id]"),
                        function (el) { return parseInt(el.dataset.id, 10); });

                    RZAdmin.post("/admin/san-pham/" + productId + "/anh/sap-xep", { ids: ids })
                        .then(function (res) { RZAdmin.toast(res.message, res.ok ? "success" : "error"); });
                }
            });
        }

        initImageSort();
    }

    /* Khởi động */
    function init() {
        initContactPrice();
        initShortDescCount();
        initRepeater();
        initUploader();

        if (groupList) { reindex(); }
        updatePreview();

        ["OriginalPrice", "SalePrice"].forEach(function (id) {
            var el = document.getElementById(id);
            if (el) el.addEventListener("input", updatePreview);
        });
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", init);
    } else {
        init();
    }
})(window, document);
