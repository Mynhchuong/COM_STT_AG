/* =============================================================
   global-function.js
   Logic nút ẩn/hiện UI dùng chung (header, filter title…)

   Cách tích hợp vào 1 trang mới:
     1. Link  global-style.css  và  global-function.js  vào page
     2. Thêm class  ui-hideable  vào các block cần ẩn:
           <div class="pageTitleBlock ui-hideable">…</div>
           <div class="filter-card   ui-hideable">…</div>
     3. Gọi  initLayoutToggle('tên_page_của_ban')  sau khi DOM sẵn sàng.
        Tên page dùng làm key lưu localStorage → mỗi trang nhớ riêng.

   Ví dụ:
        initLayoutToggle('sample-status');
   ============================================================= */

/**
 * Khởi tạo nút toggle ẩn/hiện UI cho trang hiện tại.
 * @param {string} pageKey  – Tên duy nhất của page, dùng làm localStorage key.
 *                            VD: 'sample-status', 'andon-monitor', 'production'
 */
function initLayoutToggle(pageKey) {
    const storageKey = `hide_ui_${pageKey}`;

    // --- Tạo nút ---
    const btn = document.createElement('button');
    btn.id        = 'btnToggleLayout';
    btn.className = 'layout-toggle-btn';
    btn.title     = 'Show / Hide Header & Filter';
    document.body.appendChild(btn);

    // --- Helper render icon ---
    function setIcon(hidden) {
        btn.innerHTML = hidden
            ? '<span class="material-symbols-rounded" style="font-size:18px;color:#fff;">visibility</span>'
            : '<span class="material-symbols-rounded" style="font-size:18px;color:#fff;">visibility_off</span>';
    }

    // --- Áp trạng thái đã lưu ---
    const savedHidden = localStorage.getItem(storageKey) === 'true';
    if (savedHidden) {
        document.body.classList.add('hide-ui');
    }
    setIcon(savedHidden);

    // --- Xử lý click ---
    btn.addEventListener('click', function () {
        document.body.classList.toggle('hide-ui');
        const isHidden = document.body.classList.contains('hide-ui');
        localStorage.setItem(storageKey, isHidden);
        setIcon(isHidden);
    });
}
