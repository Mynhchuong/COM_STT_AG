(function () {
    document.addEventListener('DOMContentLoaded', function () {
        const btnScanQr = document.getElementById('btnScanQr');
        const btnCancelScan = document.getElementById('btnCancelScan');
        const qrScanArea = document.getElementById('qrScanArea');
        const empCdInput = document.getElementById('EmpCd');
        const passwordInput = document.getElementById('Password');

        if (!btnScanQr || !qrScanArea || !empCdInput) return;

        let html5QrCode = null;
        let scannerRunning = false;

        function stopScanner() {
            qrScanArea.classList.add('d-none');
            if (html5QrCode && scannerRunning) {
                scannerRunning = false;
                html5QrCode.stop().then(function () {
                    html5QrCode.clear();
                }).catch(function () {
                    // Camera có thể đã tắt sẵn, bỏ qua lỗi
                });
            }
        }

        function onScanSuccess(decodedText) {
            const code = (decodedText || '').trim();
            if (!code) return;

            // Quét QR chỉ điền mã nhân viên vào ô — KHÔNG tự đăng nhập.
            // Vẫn bắt buộc gõ mật khẩu để tránh việc quét nhầm/quét thẻ người khác dẫn tới đăng nhập giùm người đó.
            empCdInput.value = code;
            stopScanner();
            if (passwordInput) passwordInput.focus();
        }

        function onScanFailure() {
            // Bỏ qua khung hình không đọc được mã
        }

        btnScanQr.addEventListener('click', function () {
            qrScanArea.classList.remove('d-none');

            html5QrCode = new Html5Qrcode('qr-reader');
            html5QrCode.start(
                { facingMode: 'environment' },
                {
                    fps: 10,
                    qrbox: function (viewfinderWidth, viewfinderHeight) {
                        const size = Math.floor(Math.min(viewfinderWidth, viewfinderHeight) * 0.7);
                        return { width: size, height: size };
                    }
                },
                onScanSuccess,
                onScanFailure
            ).then(function () {
                scannerRunning = true;
            }).catch(function (err) {
                const detail = (err && err.message) ? err.message : String(err);
                alert('Không mở được camera: ' + detail);
                qrScanArea.classList.add('d-none');
            });
        });

        if (btnCancelScan) {
            btnCancelScan.addEventListener('click', stopScanner);
        }

        window.addEventListener('beforeunload', stopScanner);
    });
})();
