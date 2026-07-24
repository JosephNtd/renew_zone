// contact.js — Trang /lien-he.
// Mọi kiểm tra ở đây CHỈ để phản hồi nhanh cho người dùng; server luôn validate lại
// (ContactController.TryStoreAttachmentAsync) — xem CLAUDE.md Lưu ý 5c.
(function () {
  "use strict";

  var form = document.getElementById("contactForm");
  if (!form) return; // trang đang ở trạng thái "đã gửi thành công", không có form

  // đếm ký tự
  var content = document.getElementById("Input_Content");
  var counter = document.getElementById("contentCounter");

  if (content && counter) {
    var max = parseInt(content.getAttribute("maxlength"), 10) || 1000;

    var updateCounter = function () {
      var len = content.value.length;
      counter.textContent = len + "/" + max;
      counter.classList.toggle("is-over", len >= max);
    };

    content.addEventListener("input", updateCounter);
    updateCounter();
  }

  // đính kèm file
  var fileInput = document.getElementById("attachment");
  var dropzone = document.getElementById("ctDropzone");
  var chip = document.getElementById("ctFileChip");
  var chipName = document.getElementById("ctFileName");
  var chipSize = document.getElementById("ctFileSize");
  var chipThumb = document.getElementById("ctFileThumb");
  var chipIcon = document.getElementById("ctFileIcon");
  var removeBtn = document.getElementById("ctFileRemove");

  var objectUrl = null;

  var maxMb = fileInput ? parseInt(fileInput.getAttribute("data-max-mb"), 10) || 5 : 5;
  var maxBytes = maxMb * 1024 * 1024;

  var allowedExt = (fileInput && fileInput.getAttribute("accept") || "")
    .split(",")
    .map(function (e) { return e.trim().toLowerCase(); })
    .filter(Boolean);

  function formatSize(bytes) {
    if (bytes < 1024) return bytes + " B";
    if (bytes < 1024 * 1024) return Math.round(bytes / 1024) + " KB";
    return (bytes / (1024 * 1024)).toFixed(1).replace(".", ",") + " MB";
  }

  function releaseThumb() {
    if (objectUrl) {
      URL.revokeObjectURL(objectUrl);
      objectUrl = null;
    }
  }

  function clearFile() {
    releaseThumb();
    if (fileInput) fileInput.value = "";
    if (chip) chip.hidden = true;
    if (chipThumb) { chipThumb.hidden = true; chipThumb.removeAttribute("src"); }
    if (chipIcon) chipIcon.hidden = false;
    if (dropzone) dropzone.hidden = false;
  }

  function showFile(file) {
    if (!chip) return;

    chipName.textContent = file.name;
    chipSize.textContent = formatSize(file.size);

    releaseThumb();
    if (file.type.indexOf("image/") === 0 && chipThumb) {
      objectUrl = URL.createObjectURL(file);
      chipThumb.src = objectUrl;
      chipThumb.hidden = false;
      if (chipIcon) chipIcon.hidden = true;
    } else {
      if (chipThumb) chipThumb.hidden = true;
      if (chipIcon) chipIcon.hidden = false;
    }

    chip.hidden = false;
    if (dropzone) dropzone.hidden = true;
  }

  function toast(message) {
    if (window.RZ && typeof window.RZ.toast === "function") {
      window.RZ.toast(message, "error");
    } else {
      alert(message);
    }
  }

  // Trả về true nếu file hợp lệ; sai thì báo và KHÔNG gán vào input
  function acceptFile(file) {
    var dot = file.name.lastIndexOf(".");
    var ext = dot >= 0 ? file.name.slice(dot).toLowerCase() : "";

    if (allowedExt.length > 0 && allowedExt.indexOf(ext) === -1) {
      toast("Chỉ nhận file " + allowedExt.join(", ") + ".");
      return false;
    }

    if (file.size > maxBytes) {
      toast("File đính kèm tối đa " + maxMb + "MB (file của bạn " + formatSize(file.size) + ").");
      return false;
    }

    return true;
  }

  if (fileInput) {
    fileInput.addEventListener("change", function () {
      var file = fileInput.files && fileInput.files[0];
      if (!file) { clearFile(); return; }

      if (!acceptFile(file)) { clearFile(); return; }
      showFile(file);
    });
  }

  if (removeBtn) {
    removeBtn.addEventListener("click", clearFile);
  }

  if (dropzone && fileInput) {
    ["dragenter", "dragover"].forEach(function (evt) {
      dropzone.addEventListener(evt, function (e) {
        e.preventDefault();
        dropzone.classList.add("is-dragover");
      });
    });

    ["dragleave", "drop"].forEach(function (evt) {
      dropzone.addEventListener(evt, function (e) {
        e.preventDefault();
        dropzone.classList.remove("is-dragover");
      });
    });

    dropzone.addEventListener("drop", function (e) {
      var files = e.dataTransfer && e.dataTransfer.files;
      if (!files || files.length === 0) return;

      if (!acceptFile(files[0])) return;

      // DataTransfer là cách duy nhất gán file thả vào <input type=file>
      if (typeof DataTransfer !== "undefined") {
        var dt = new DataTransfer();
        dt.items.add(files[0]);
        fileInput.files = dt.files;
      }
      showFile(files[0]);
    });
  }

  // chống double-submit + spinner
  var submitBtn = document.getElementById("contactSubmit");

  form.addEventListener("submit", function () {
    // Listener này chạy TRƯỚC jquery-validation ⇒ phải tự hỏi form có hợp lệ không,
    // nếu không thì form sai vẫn bị khóa nút (bẫy đã gặp ở checkout.js Giai đoạn 4).
    if (window.jQuery && jQuery(form).data("validator") && !jQuery(form).valid()) {
      return;
    }

    if (!submitBtn || submitBtn.disabled) return;

    submitBtn.disabled = true;
    submitBtn.innerHTML =
      '<span class="spinner-border" role="status" aria-hidden="true"></span> Đang gửi...';
  });

  // chấm "Đang mở cửa / Đã đóng cửa"
  var dot = document.getElementById("ctOpenDot");
  if (dot) {
    var hours = dot.getAttribute("data-hours") || "";

    // Bắt dạng "8h30 - 21h30", "8:30 - 21:30", "8h - 21h30"...
    var match = hours.match(/(\d{1,2})\s*[h:]\s*(\d{2})?\D+(\d{1,2})\s*[h:]\s*(\d{2})?/);

    if (match) {
      var openMin = parseInt(match[1], 10) * 60 + parseInt(match[2] || "0", 10);
      var closeMin = parseInt(match[3], 10) * 60 + parseInt(match[4] || "0", 10);

      var now = new Date();
      var nowMin = now.getHours() * 60 + now.getMinutes();
      var isOpen = nowMin >= openMin && nowMin < closeMin;

      dot.textContent = isOpen ? "Đang mở cửa" : "Đã đóng cửa";
      dot.classList.add(isOpen ? "is-open" : "is-closed");
      dot.hidden = false;
    }
    // Parse không được ⇒ để nguyên hidden (fallback an toàn, không đoán bừa)
  }
})();
