// contacto.js - Manejo de formulario de contacto y respuestas
(function () {
    'use strict';

    // =============================================
    // FORMULARIO PÚBLICO (Home)
    // =============================================
    function initContactForm() {
        var form = document.getElementById('contactForm');
        if (!form) return;

        form.addEventListener('submit', function (e) {
            e.preventDefault();

            var tel = document.getElementById('telefono_con');
            if (tel) {
                var val = tel.value.replace(/\s/g, '');
                if (!/^9\d{8}$/.test(val)) {
                    Swal.fire({
                        icon: 'warning',
                        title: 'Teléfono inválido',
                        text: 'El teléfono debe tener 9 dígitos y comenzar con 9',
                        confirmButtonColor: '#4C3726'
                    });
                    tel.focus();
                    return;
                }
                tel.value = val;
            }

            var formData = new FormData(form);
            var params = new URLSearchParams();
            formData.forEach(function (value, key) { params.append(key, value); });

            fetch('/Contacto/EnviarMensaje', {
                method: 'POST',
                body: params
            })
            .then(function (response) {
                if (response.ok) return response.json().then(function (r) { r.status = response.status; return r; });
                if (response.status === 400) return response.json().then(function (r) { r.status = 400; return r; });
                if (response.status === 500) return response.json().then(function (r) { r.status = 500; return r; });
                return { success: false, message: 'Error inesperado', status: response.status };
            })
            .then(function (result) {
                if (result.success) {
                    Swal.fire({
                        icon: 'success',
                        title: '¡Gracias por contactarnos!',
                        text: result.message || 'Te responderemos pronto.',
                        timer: 3000,
                        showConfirmButton: true,
                        confirmButtonColor: '#4C3726'
                    });
                    form.reset();
                } else {
                    Swal.fire({
                        icon: 'error',
                        title: 'Error',
                        text: result.message || 'No se pudo enviar el mensaje.',
                        confirmButtonColor: '#4C3726'
                    });
                }
            })
            .catch(function () {
                Swal.fire({
                    icon: 'error',
                    title: 'Error de conexión',
                    text: 'No se pudo conectar con el servidor. Intente más tarde.',
                    confirmButtonColor: '#4C3726'
                });
            });
        });
    }

    // =============================================
    // DETALLE - Responder mensaje
    // =============================================
    function initDetallePage() {
        var btnEnviar = document.getElementById('btnEnviarRespuesta');
        if (btnEnviar) {
            btnEnviar.addEventListener('click', function () {
                var respuesta = document.getElementById('respuesta_con');
                if (!respuesta || !respuesta.value.trim()) {
                    Swal.fire({
                        icon: 'warning',
                        title: 'Respuesta vacía',
                        text: 'Escriba una respuesta antes de enviar.',
                        confirmButtonColor: '#4C3726'
                    });
                    return;
                }

                btnEnviar.disabled = true;
                btnEnviar.innerHTML = '<i class="fas fa-spinner fa-spin mr-1"></i> Enviando...';

                var respParams = new URLSearchParams();
                respParams.append('id_con', mensajeId);
                respParams.append('respuesta_con', respuesta.value.trim());

                fetch('/Contacto/Responder', {
                    method: 'POST',
                    body: respParams
                })
                .then(function (response) {
                    var status = response.status;
                    return response.json().then(function (r) { r.status = status; return r; });
                })
                .then(function (result) {
                    if (result.success && result.email_enviado) {
                        location.reload();
                    } else if (result.success && !result.email_enviado) {
                        Swal.fire({
                            icon: 'warning',
                            title: 'Respuesta guardada',
                            text: result.message || 'La respuesta se guardó pero el email no pudo ser enviado.',
                            confirmButtonColor: '#4C3726',
                            showCancelButton: true,
                            confirmButtonText: 'Reintentar envío',
                            cancelButtonText: 'Volver'
                        }).then(function (d) {
                            if (d.isConfirmed) reintentarEmail(mensajeId);
                            else location.reload();
                        });
                    } else {
                        Swal.fire({
                            icon: 'error',
                            title: 'Error',
                            text: result.message || 'No se pudo enviar la respuesta.',
                            confirmButtonColor: '#4C3726'
                        });
                    }
                })
                .catch(function () {
                    Swal.fire({
                        icon: 'error',
                        title: 'Error de conexión',
                        text: 'No se pudo conectar con el servidor.',
                        confirmButtonColor: '#4C3726'
                    });
                })
                .finally(function () {
                    btnEnviar.disabled = false;
                    btnEnviar.innerHTML = '<i class="fas fa-paper-plane mr-1"></i> Enviar Respuesta';
                });
            });
        }

        var btnReintentar = document.getElementById('btnReintentar');
        if (btnReintentar) {
            btnReintentar.addEventListener('click', function () {
                reintentarEmail(mensajeId);
            });
        }
    }

    function reintentarEmail(id) {
        var btn = document.getElementById('btnReintentar');
        if (btn) {
            btn.disabled = true;
            btn.innerHTML = '<i class="fas fa-spinner fa-spin mr-1"></i> Reintentando...';
        }

        var reParams = new URLSearchParams();
        reParams.append('id_con', id);

        fetch('/Contacto/ReintentarEmail', {
            method: 'POST',
            body: reParams
        })
        .then(function (response) {
            var status = response.status;
            return response.json().then(function (r) { r.status = status; return r; });
        })
        .then(function (result) {
            if (result.success && result.email_enviado) {
                Swal.fire({
                    icon: 'success',
                    title: 'Email enviado',
                    text: result.message || 'El email se ha enviado correctamente.',
                    confirmButtonColor: '#4C3726'
                }).then(function () { location.reload(); });
            } else {
                Swal.fire({
                    icon: 'warning',
                    title: 'No se pudo enviar',
                    text: result.message || 'El email no pudo ser enviado. Intente más tarde.',
                    confirmButtonColor: '#4C3726',
                    confirmButtonText: 'Entendido'
                });
            }
        })
        .catch(function () {
            Swal.fire({
                icon: 'error',
                title: 'Error de conexión',
                text: 'No se pudo conectar con el servidor.',
                confirmButtonColor: '#4C3726'
            });
        })
        .finally(function () {
            if (btn) {
                btn.disabled = false;
                btn.innerHTML = '<i class="fas fa-redo mr-1"></i> Reintentar Envío';
            }
        });
    }

    // =============================================
    // INIT
    // =============================================
    document.addEventListener('DOMContentLoaded', function () {
        initContactForm();
        initDetallePage();
    });
})();
