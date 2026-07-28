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
});
