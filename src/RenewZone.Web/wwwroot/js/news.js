// news.js — Trang /tin-tuc.
// Điều hướng + lọc đều là link thật (querystring) nên trang chạy đủ chức năng kể cả khi
// tắt JS; file này chỉ thêm phần trải nghiệm.
(function () {
  "use strict";

  // Chip đang chọn: cuộn vào tầm nhìn trên mobile (hàng chip cuộn ngang được)
  var activeChip = document.querySelector(".news-filter .rz-chip.is-active");
  if (activeChip && activeChip.scrollIntoView) {
    activeChip.scrollIntoView({ block: "nearest", inline: "center" });
  }
})();
