// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Tap outside the mobile drawer (on the backdrop) to close it.
// Reuses window.toggleSidenav() defined by assets/js/material-dashboard.js.
document.addEventListener('DOMContentLoaded', function () {
    var backdrop = document.getElementById('sidenavBackdrop');
    if (backdrop) {
        backdrop.addEventListener('click', function () {
            if (typeof toggleSidenav === 'function') {
                toggleSidenav();
            }
        });
    }

    // Ẩn/hiện sidenav trên desktop — nhớ trạng thái qua localStorage
    var btnCollapse = document.getElementById('btnSidenavCollapse');
    if (btnCollapse) {
        var STORAGE_KEY = 'sidenav_collapsed';

        function setIcon(collapsed) {
            btnCollapse.innerHTML = collapsed
                ? '<i class="material-symbols-rounded">menu</i>'
                : '<i class="material-symbols-rounded">menu_open</i>';
        }

        var collapsed = localStorage.getItem(STORAGE_KEY) === 'true';
        if (collapsed) {
            document.body.classList.add('sidenav-collapsed');
        }
        setIcon(collapsed);

        btnCollapse.addEventListener('click', function () {
            var isCollapsed = document.body.classList.toggle('sidenav-collapsed');
            localStorage.setItem(STORAGE_KEY, isCollapsed);
            setIcon(isCollapsed);
        });
    }
});
