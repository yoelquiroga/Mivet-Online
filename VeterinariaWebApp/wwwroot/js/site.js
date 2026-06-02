// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// ===== LOADING STATES =====
document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('form').forEach(function (form) {
        form.addEventListener('submit', function () {
            var btn = form.querySelector('button[type="submit"]');
            if (btn && !btn.disabled) {
                btn.disabled = true;
                btn.classList.add('btn-loading');
                var originalText = btn.innerHTML;
                btn.innerHTML = '<span class="btn-text">' + originalText + '</span>';
            }
        });
    });

    document.querySelectorAll('a.btn-loadable').forEach(function (link) {
        link.addEventListener('click', function () {
            if (!link.disabled && !link.classList.contains('disabled')) {
                link.classList.add('disabled');
                link.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span> Cargando...';
            }
        });
    });
});
