/* =====================================================================
   admin-product.js — form sản phẩm 5 tab (chỉ nạp ở màn này, không vào admin.js)
   Gồm: TinyMCE · uploader ảnh · repeater biến thể · xem trước giá
   ===================================================================== */
(function (window, document) {
    "use strict";

    /* ============ 1. TinyMCE ============ */
    function initEditor() {
        if (!window.tinymce) return;

        tinymce.init({
            selector: "textarea.js-editor",
            height: 420,
            menubar: false,
            branding: false,
            promotion: false,
            // KHÔNG đặt language:"vi" — gói ngôn ngữ không nằm trong package npm của TinyMCE,
            // khai báo mà thiếu file thì editor gọi /langs/vi.js rồi 404. Muốn tiếng Việt thì
            // tải gói ngôn ngữ về wwwroot/lib/tinymce/langs/ rồi mới bật dòng này.
            plugins: "lists link image table code fullscreen advlist autolink wordcount",
            toolbar: "undo redo | blocks | bold italic | bullist numlist | link image table | code fullscreen",
            license_key: "gpl",

            // Ép upload đàng hoàng thay vì nhúng base64 làm phình cột DescriptionHtml trong DB
            paste_data_images: false,
            automatic_uploads: true,
            images_upload_handler: uploadEditorImage,

            /* ⚠️ Whitelist thẻ/thuộc tính: KHÔNG cho <script>, <iframe>, thuộc tính on*.
               Đây là lớp phòng vệ ở client nên KHÔNG phải biện pháp bảo mật thật —
               web khách render bằng @Html.Raw. Rủi ro này chấp nhận được vì chỉ có
               1 tài khoản admin tin cậy; mở thêm role Editor thì BẮT BUỘC thêm
               HtmlSanitizer ở tầng service trước khi lưu (xem CLAUDE.md mục 4.9). */
            valid_elements:
                "p[style|class],br,strong/b,em/i,u,s,h1,h2,h3,h4,h5,h6,"
                + "ul,ol,li,blockquote,hr,"
                + "a[href|title|target|rel],img[src|alt|width|height|loading],"
                + "table[class|border|style],thead,tbody,tfoot,tr,th[colspan|rowspan|style],td[colspan|rowspan|style],"
                + "span[class|style],div[class|style],code,pre",
            invalid_elements: "script,iframe,object,embed,form,input,style,link",

            /* Nội dung trong editor trông giống hệt lúc lên web khách — chi tiết nhỏ
               nhưng bớt hẳn cảnh "trên admin thấy đẹp, ra web lại khác" */
            content_style:
                "body{font-family:'Nunito Sans',system-ui,sans-serif;font-size:15px;line-height:1.7;color:#212529}"
                + "h2,h3,h4{font-family:'Be Vietnam Pro',system-ui,sans-serif;font-weight:700;color:#17141A;margin:1.2em 0 .5em}"
                + "table{width:100%;border-collapse:collapse;margin:1em 0}"
                + "th,td{border:1px solid #ECE7EC;padding:8px 12px;text-align:left}"
                + "th{background:#FAF7FA;font-weight:700}"
                + "a{color:#B54983}"
        });
    }

    function uploadEditorImage(blobInfo) {
        return new Promise(function (resolve, reject) {
            var form = new FormData();
            form.append("file", blobInfo.blob(), blobInfo.filename());

            fetch("/admin/media/upload-editor", {
                method: "POST",
                headers: { "RequestVerificationToken": RZAdmin.token() },
                body: form
            })
                .then(function (res) { return res.json(); })
                .then(function (data) {
                    if (data.location) { resolve(data.location); }
                    else { reject({ message: data.message || "Tải ảnh thất bại.", remove: true }); }
                })
                .catch(function () { reject({ message: "Không kết nối được máy chủ.", remove: true }); });
        });
    }

    /* ============ 2. Chèn bảng thông số mẫu ============ */
    function initSpecTable() {
        var btn = document.getElementById("insertSpecTable");
        if (!btn) return;

        btn.addEventListener("click", function () {
            var editor = tinymce.get("SpecsHtml");
            if (!editor) return;

            editor.insertContent(
                "<h3>Thông số kỹ thuật</h3>"
                + "<table><thead><tr><th>Hạng mục</th><th>Chi tiết</th></tr></thead><tbody>"
                + "<tr><td>Màn hình</td><td>&nbsp;</td></tr>"
                + "<tr><td>Chip xử lý</td><td>&nbsp;</td></tr>"
                + "<tr><td>Camera</td><td>&nbsp;</td></tr>"
                + "<tr><td>Pin &amp; sạc</td><td>&nbsp;</td></tr>"
                + "<tr><td>Thiết kế &amp; chất liệu</td><td>&nbsp;</td></tr>"
                + "</tbody></table>");

            editor.focus();
        });
    }

    /* ============ 3. Giá liên hệ khóa 2 ô giá ============ */
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

    /* ============ 4. Đếm ký tự mô tả ngắn ============ */
    function initShortDescCount() {
        var input = document.getElementById("ShortDescription");
        var out = document.getElementById("shortDescCount");
        if (!input || !out) return;

        var render = function () { out.textContent = input.value.length; };
        input.addEventListener("input", render);
        render();
    }

    /* ============ 5. Repeater biến thể ============ */
    var groupList = document.getElementById("groupList");

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
        });

        groupList.addEventListener("input", function (e) {
            if (e.target.closest(".js-value-adj")) { updatePreview(); }
        });
    }

    function addValueRow(group) {
        var list = group.querySelector(".js-value-list");
        var node = document.getElementById("tplValue").content.cloneNode(true);
        list.appendChild(node);

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
            group.querySelector(".js-group-name").name = "OptionGroups[" + gi + "].Name";

            group.querySelectorAll(".js-value").forEach(function (row, vi) {
                row.dataset.index = vi;
                row.querySelector(".js-value-id").name = "OptionGroups[" + gi + "].Values[" + vi + "].Id";
                row.querySelector(".js-value-text").name = "OptionGroups[" + gi + "].Values[" + vi + "].Value";
                row.querySelector(".js-value-adj").name = "OptionGroups[" + gi + "].Values[" + vi + "].PriceAdjustment";
                row.querySelector(".js-value-default-field").name =
                    "OptionGroups[" + gi + "].Values[" + vi + "].IsDefault";

                // Radio phải cùng name trong CÙNG nhóm và khác name giữa các nhóm
                row.querySelector(".js-value-default").name = "default_group_" + gi;
            });
        });

        syncDefaults();
    }

    function syncDefaults() {
        groupList.querySelectorAll(".js-value").forEach(function (row) {
            var radio = row.querySelector(".js-value-default");
            row.querySelector(".js-value-default-field").value = radio.checked ? "true" : "false";
        });
    }

    /* ============ 6. Xem trước giá theo lựa chọn mặc định ============ */
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

    /* ============ 7. Uploader ảnh ============ */
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

    /* ============ Khởi động ============ */
    function init() {
        initEditor();
        initSpecTable();
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

        // TinyMCE giữ nội dung trong iframe riêng ⇒ phải đẩy về textarea trước khi submit,
        // không thì mô tả/thông số vừa gõ sẽ KHÔNG được lưu
        var form = document.querySelector("form[data-adm-form]");
        if (form) {
            form.addEventListener("submit", function () {
                if (window.tinymce) tinymce.triggerSave();
            }, true);
        }
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", init);
    } else {
        init();
    }
})(window, document);
