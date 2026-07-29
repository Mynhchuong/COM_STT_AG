(function () {
    document.addEventListener('DOMContentLoaded', function () {
        const idleSection = document.getElementById('idle-section');
        const scanSection = document.getElementById('scan-section');
        const scanReaderEl = document.getElementById('scan-reader');
        const btnStartScan = document.getElementById('btn-start-scan');
        const btnStopScan = document.getElementById('btn-stop-scan');

        const cardNoInput = document.getElementById('cardNoInput');
        const btnLookup = document.getElementById('btnLookup');
        const lookupMessage = document.getElementById('lookupMessage');
        const cardInfoPanel = document.getElementById('cardInfoPanel');
        const emptyState = document.getElementById('emptyState');

        let html5QrCode = null;
        let nativeStream = null;
        let nativeVideoEl = null;
        let nativeDetector = null;
        let nativeLoopHandle = null;
        let nativeDetecting = false;
        let usingNativeMulti = false;
        let scannerRunning = false;
        let lastLookupCode = '';

        function showMessage(text, isError) {
            if (!lookupMessage) return;
            lookupMessage.textContent = text;
            lookupMessage.style.display = text ? 'block' : 'none';
            lookupMessage.className = 'text-sm font-weight-bold mb-3 ' + (isError ? 'text-danger' : 'text-secondary');
        }

        function flashReader(success) {
            if (!scanReaderEl) return;
            const flashClass = success ? 'flash-success' : 'flash-error';
            scanReaderEl.classList.add(flashClass);
            setTimeout(function () { scanReaderEl.classList.remove(flashClass); }, 400);
            if (navigator.vibrate) navigator.vibrate(success ? 120 : [100, 60, 100]);
        }

        function fillInfo(data) {
            document.getElementById('infoICardNo').textContent = data.I_CARD_NO ?? '';
            document.getElementById('infoStatus').textContent = data.STATUS ?? '';
            document.getElementById('infoQQty').textContent = data.Q_QTY ?? '';
            document.getElementById('infoCOrdOp').textContent = data.C_ORD_OP ?? '';
            document.getElementById('infoCLine').textContent = data.C_LINE ?? '';
            document.getElementById('infoIPartsNo').textContent = data.I_PARTS_NO ?? '';
            document.getElementById('infoCStyle').textContent = data.C_STYLE ?? '';
            document.getElementById('infoFClose').textContent = data.F_CLOSE ?? '';
            document.getElementById('infoQPlan').textContent = data.Q_PLAN ?? '';
            document.getElementById('infoQGather').textContent = data.Q_GATHER ?? '';
            emptyState.style.display = 'none';
            cardInfoPanel.style.display = 'block';
        }

        async function lookupCard(cardNo) {
            if (!cardNo) return;
            // Chặn quét trùng liên tiếp cùng 1 mã (camera bắt liên tục nhiều khung hình/giây)
            if (cardNo === lastLookupCode) return;
            lastLookupCode = cardNo;

            showMessage('Đang tra cứu ' + cardNo + '...', false);

            try {
                const res = await fetch('/Cutting/CardInfo?cardNo=' + encodeURIComponent(cardNo));
                const data = await res.json();

                if (res.ok && data.success) {
                    showMessage('', false);
                    fillInfo(data.data);
                    flashReader(true);
                } else {
                    cardInfoPanel.style.display = 'none';
                    emptyState.style.display = 'block';
                    showMessage(data.message || 'Không tìm thấy PCARD này.', true);
                    flashReader(false);
                }
            } catch (err) {
                console.error(err);
                showMessage('Lỗi kết nối máy chủ!', true);
                flashReader(false);
            } finally {
                // Cho phép quét lại cùng mã sau 1.5s (VD: quét nhầm rồi quét lại)
                setTimeout(function () {
                    if (lastLookupCode === cardNo) lastLookupCode = '';
                }, 1500);
            }
        }

        // --- Bắn súng / nhập tay ---
        function submitManual() {
            const code = cardNoInput.value.trim().toUpperCase();
            if (!code) return;
            lastLookupCode = ''; // nhập tay luôn cho tra cứu, kể cả trùng mã lần trước
            lookupCard(code);
            cardNoInput.value = '';
            cardNoInput.focus();
        }

        if (btnLookup) btnLookup.addEventListener('click', submitManual);
        if (cardNoInput) {
            cardNoInput.addEventListener('keydown', function (e) {
                if (e.key === 'Enter') {
                    e.preventDefault();
                    submitManual();
                }
            });
            cardNoInput.focus();
        }

        // ============================================================
        // CAMERA SCANNER: native BarcodeDetector (Android/Chrome) hoặc html5-qrcode (iOS/Safari, desktop)
        // ============================================================
        function onScanSuccess(decodedText) {
            const code = (decodedText || '').trim().toUpperCase();
            if (!code) return;
            lookupCard(code);
        }

        function onScanFailure() {
            // Bỏ qua khung hình không đọc được mã
        }

        function isNativeMultiScanSupported() {
            return typeof window.BarcodeDetector !== 'undefined';
        }

        function startNativeScanner() {
            return navigator.mediaDevices.getUserMedia({
                video: { facingMode: 'environment', width: { ideal: 1280 }, height: { ideal: 720 } }
            }).then(function (stream) {
                nativeStream = stream;
                scanReaderEl.innerHTML = '';
                nativeVideoEl = document.createElement('video');
                nativeVideoEl.setAttribute('playsinline', 'true');
                nativeVideoEl.setAttribute('autoplay', 'true');
                nativeVideoEl.muted = true;
                nativeVideoEl.style.width = '100%';
                nativeVideoEl.style.display = 'block';
                nativeVideoEl.srcObject = stream;
                scanReaderEl.appendChild(nativeVideoEl);
                return nativeVideoEl.play();
            }).then(function () {
                nativeDetector = new BarcodeDetector({
                    formats: ['code_128', 'code_39', 'code_93', 'codabar', 'ean_13', 'ean_8', 'itf', 'upc_a', 'upc_e', 'qr_code', 'data_matrix']
                });
                scannerRunning = true;

                nativeLoopHandle = setInterval(function () {
                    if (nativeDetecting || !nativeVideoEl) return;
                    nativeDetecting = true;
                    nativeDetector.detect(nativeVideoEl).then(function (barcodes) {
                        barcodes.forEach(function (b) { onScanSuccess(b.rawValue); });
                    }).catch(function (err) {
                        console.error('BarcodeDetector error:', err);
                    }).finally(function () {
                        nativeDetecting = false;
                    });
                }, 300);
            });
        }

        function stopNativeScanner() {
            if (nativeLoopHandle) { clearInterval(nativeLoopHandle); nativeLoopHandle = null; }
            if (nativeStream) { nativeStream.getTracks().forEach(function (t) { t.stop(); }); nativeStream = null; }
            if (nativeVideoEl) { nativeVideoEl.remove(); nativeVideoEl = null; }
            scannerRunning = false;
            return Promise.resolve();
        }

        function startHtml5QrcodeScanner() {
            scanReaderEl.innerHTML = '';
            html5QrCode = new Html5Qrcode('scan-reader', {
                formatsToSupport: [
                    Html5QrcodeSupportedFormats.CODE_128,
                    Html5QrcodeSupportedFormats.CODE_39,
                    Html5QrcodeSupportedFormats.CODE_93,
                    Html5QrcodeSupportedFormats.CODABAR,
                    Html5QrcodeSupportedFormats.EAN_13,
                    Html5QrcodeSupportedFormats.EAN_8,
                    Html5QrcodeSupportedFormats.ITF,
                    Html5QrcodeSupportedFormats.UPC_A,
                    Html5QrcodeSupportedFormats.UPC_E,
                    Html5QrcodeSupportedFormats.QR_CODE,
                    Html5QrcodeSupportedFormats.DATA_MATRIX
                ],
                verbose: false
            });

            return html5QrCode.start(
                { facingMode: 'environment' },
                {
                    fps: 10,
                    qrbox: function (viewfinderWidth, viewfinderHeight) {
                        var width = Math.floor(viewfinderWidth * 0.95);
                        var height = Math.floor(viewfinderHeight * 0.85);
                        return { width: width, height: height };
                    },
                    aspectRatio: 1.7777778,
                    videoConstraints: {
                        facingMode: 'environment',
                        width: { ideal: 1280 },
                        height: { ideal: 720 }
                    }
                },
                onScanSuccess,
                onScanFailure
            ).then(function () {
                scannerRunning = true;
            });
        }

        function stopHtml5QrcodeScanner() {
            if (!html5QrCode || !scannerRunning) return Promise.resolve();
            return html5QrCode.stop().then(function () {
                scannerRunning = false;
                html5QrCode.clear();
            }).catch(function () {
                scannerRunning = false;
            });
        }

        function startScanner() {
            usingNativeMulti = isNativeMultiScanSupported();
            const starter = usingNativeMulti ? startNativeScanner() : startHtml5QrcodeScanner();

            starter.catch(function (err) {
                const detail = (err && err.message) ? err.message : String(err);
                showMessage('Không mở được camera: ' + detail, true);
                console.error('Camera startup failed:', err);
            });
        }

        function stopScanner() {
            return usingNativeMulti ? stopNativeScanner() : stopHtml5QrcodeScanner();
        }

        if (btnStartScan) {
            btnStartScan.addEventListener('click', function () {
                idleSection.classList.add('d-none');
                scanSection.classList.remove('d-none');
                startScanner();
            });
        }

        if (btnStopScan) {
            btnStopScan.addEventListener('click', function () {
                stopScanner().then(function () {
                    scanSection.classList.add('d-none');
                    idleSection.classList.remove('d-none');
                    scanReaderEl.innerHTML = '';
                });
            });
        }

        window.addEventListener('beforeunload', function () {
            stopScanner();
        });
    });
})();
