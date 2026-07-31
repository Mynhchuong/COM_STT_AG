(function () {
    document.addEventListener('DOMContentLoaded', function () {
        const cardNoInput = document.getElementById('cardNoInput');
        const checkStatusMsg = document.getElementById('checkStatusMsg');

        const btnStartScan = document.getElementById('btn-start-scan');
        const btnStopScan = document.getElementById('btn-stop-scan');
        const idleSection = document.getElementById('idle-section');
        const scanSection = document.getElementById('scan-section');
        const scanReaderEl = document.getElementById('scan-reader');

        let html5QrCode = null;
        let isCameraRunning = false;
        let isChecking = false;

        function showStatus(text, isError) {
            checkStatusMsg.textContent = text;
            checkStatusMsg.className = 'text-sm font-weight-bold ' + (isError ? 'text-danger' : 'text-secondary');
        }

        async function stopCamera() {
            if (html5QrCode && isCameraRunning) {
                try {
                    await html5QrCode.stop();
                } catch (e) {
                    console.warn('Stop camera error:', e);
                }
                isCameraRunning = false;
            }
            scanSection.classList.add('d-none');
            idleSection.classList.remove('d-none');
        }

        async function checkCard(cardNo) {
            if (isChecking) return;
            isChecking = true;
            showStatus('Đang tra basket cho PCard ' + cardNo + '...', false);

            try {
                const res = await fetch('/Production2/GetBasketIdByCard?cardNo=' + encodeURIComponent(cardNo));
                const data = await res.json();

                if (res.ok && data.success) {
                    window.PdaHelper.feedback(true, `Tìm thấy basket ${data.basketId} — đang mở...`, { flashEl: scanReaderEl });
                    await stopCamera();
                    window.location.href = '/Production2/Pending2?basketId=' + data.basketId;
                } else {
                    const msg = data.message || 'Không tìm thấy basket cho PCard này.';
                    showStatus(msg, true);
                    window.PdaHelper.feedback(false, msg, { flashEl: scanReaderEl });
                    isChecking = false;
                    cardNoInput.value = '';
                    cardNoInput.focus();
                }
            } catch (err) {
                console.error(err);
                showStatus('Lỗi kết nối máy chủ!', true);
                window.PdaHelper.feedback(false, 'Lỗi kết nối máy chủ!', { flashEl: scanReaderEl });
                isChecking = false;
                cardNoInput.value = '';
                cardNoInput.focus();
            }
        }

        // 1. CAMERA
        btnStartScan.addEventListener('click', async function () {
            if (window.PdaHelper) window.PdaHelper.unlockAudio();

            idleSection.classList.add('d-none');
            scanSection.classList.remove('d-none');

            try {
                if (!html5QrCode) {
                    html5QrCode = new Html5Qrcode('scan-reader');
                }

                await html5QrCode.start(
                    { facingMode: 'environment' },
                    { fps: 15, qrbox: { width: 260, height: 260 } },
                    function onScanSuccess(decodedText) {
                        checkCard(decodedText.trim().toUpperCase());
                    },
                    function onScanError() {
                        // bỏ qua log đọc dở dang khung hình
                    }
                );
                isCameraRunning = true;
            } catch (err) {
                console.error('Lỗi mở camera:', err);
                showStatus('Không thể truy cập camera thiết bị! Vui lòng cấp quyền truy cập camera trong trình duyệt.', true);
                await stopCamera();
            }
        });

        btnStopScan.addEventListener('click', stopCamera);

        window.addEventListener('beforeunload', function () {
            if (html5QrCode && isCameraRunning) {
                html5QrCode.stop().catch(function () {});
            }
        });

        // 2. SÚNG BẮN MÃ VẠCH PDA
        window.PdaHelper.initScanner('cardNoInput', function (code) {
            checkCard(code);
        }, { autoFocus: true, clearOnScan: true });
    });
})();
