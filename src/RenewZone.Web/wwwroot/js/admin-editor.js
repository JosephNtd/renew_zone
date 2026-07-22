/* =====================================================================
   admin-editor.js — khởi tạo TinyMCE dùng chung cho MỌI form có rich text
   (sản phẩm, bài viết, trang tĩnh). Tách riêng khỏi admin-product.js để form
   bài viết không phải nạp cả đống code repeater/uploader của sản phẩm.

   Nạp file này TRƯỚC admin-product.js ở những trang cần cả hai.
   ===================================================================== */
(function (window, document) {
    "use strict";

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

    /* ---- Nút chèn bảng thông số mẫu (chỉ có ở form sản phẩm) ---- */
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


    function init() {
        initEditor();
        initSpecTable();

        // TinyMCE giữ nội dung trong iframe riêng ⇒ phải đẩy về textarea trước khi submit,
        // không thì nội dung vừa gõ sẽ KHÔNG được lưu
        document.querySelectorAll("form[data-adm-form]").forEach(function (form) {
            form.addEventListener("submit", function () {
                if (window.tinymce) tinymce.triggerSave();
            }, true);
        });
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", init);
    } else {
        init();
    }
})(window, document);
