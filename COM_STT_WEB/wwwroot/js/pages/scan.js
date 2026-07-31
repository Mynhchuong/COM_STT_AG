// Demo scan hàng loạt PCard.
// 3 trạng thái: idle (nút "Quét mã") -> scanning (camera + Huỷ/OK) -> summary (tổng số + danh sách).
//
// 2 chế độ quét, tự động chọn theo máy:
// - Máy có BarcodeDetector gốc (Android Chrome/Edge, 1 số Chrome desktop): quét NHIỀU mã cùng lúc
//   trong 1 khung hình (detect() trả về mảng), vì có phần cứng hỗ trợ nên không lo quá tải CPU.
// - Máy không có (iPhone/Safari — Safari chưa bao giờ hỗ trợ BarcodeDetector): dùng html5-qrcode,
//   quét LIÊN TỤC từng mã một (không cố đọc nhiều mã/1 khung hình) để tránh quá tải CPU trên JS thuần.
//
// Camera chỉ được mở BÊN TRONG 1 sự kiện click của người dùng (bấm nút "Quét mã") —
// Safari/iOS chặn getUserMedia() nếu gọi tự động lúc tải trang (không có thao tác bấm),
// nên tuyệt đối không gọi startScanner() ngoài các nút bấm dưới đây.
(function () {
    document.addEventListener('DOMContentLoaded', function () {
        var readerId = 'scan-reader';
        var readerEl = document.getElementById(readerId);
        if (!readerEl) return;

        var scannedCodes = [];
        var scannedSet = new Set();
        var scannerRunning = false;
        var usingNativeMulti = false;

        // --- chế độ html5-qrcode (fallback, iPhone/Safari) ---
        var html5QrCode = null;

        // --- chế độ BarcodeDetector gốc (Android, quét nhiều mã/khung hình) ---
        var nativeStream = null;
        var nativeVideoEl = null;
        var nativeDetector = null;
        var nativeLoopHandle = null;
        var nativeDetecting = false;

        var idleSection = document.getElementById('idle-section');
        var scanSection = document.getElementById('scan-section');
        var summarySection = document.getElementById('summary-section');

        var statusEl = document.getElementById('scan-status');
        var listWrapperEl = document.getElementById('scan-list-wrapper');
        var listEl = document.getElementById('scan-list');
        var btnStartScan = document.getElementById('btn-start-scan');
        var btnCancel = document.getElementById('btn-cancel');
        var btnFinish = document.getElementById('btn-finish');
        var btnRescan = document.getElementById('btn-rescan');
        var summaryCount = document.getElementById('summary-count');
        var summaryList = document.getElementById('summary-list');

        function showSection(section) {
            [idleSection, scanSection, summarySection].forEach(function (s) {
                s.classList.toggle('d-none', s !== section);
            });
        }

        function resetState() {
            scannedCodes = [];
            scannedSet = new Set();
            listEl.innerHTML = '';
            listWrapperEl.classList.add('d-none');
            statusEl.textContent = 'Chưa quét mã nào — đưa tem vào khung hình camera';
            btnFinish.textContent = 'OK (0 mã)';
            btnFinish.disabled = true;
        }

        function updateStatus() {
            if (scannedCodes.length === 0) {
                statusEl.textContent = 'Chưa quét mã nào — đưa tem vào khung hình camera';
            } else {
                statusEl.textContent = 'Đã quét ' + scannedCodes.length + ' PCard — tiếp tục đưa tem khác vào camera';
            }
            btnFinish.textContent = 'OK (' + scannedCodes.length + ' mã)';
            btnFinish.disabled = scannedCodes.length === 0;
            listWrapperEl.classList.toggle('d-none', scannedCodes.length === 0);
        }

        function addRow(code) {
            var li = document.createElement('li');
            li.className = 'list-group-item d-flex align-items-center justify-content-between';
            li.dataset.code = code;
            li.innerHTML =
                '<span class="d-flex align-items-center gap-2">' +
                '✅' +
                '<span>' + code + '</span>' +
                '</span>' +
                '<button type="button" class="btn btn-link text-danger p-0 btn-remove" aria-label="Xoá">' +
                '✖️' +
                '</button>';
            li.querySelector('.btn-remove').addEventListener('click', function () {
                removeCode(code);
                li.remove();
            });
            listEl.appendChild(li);
        }

        function removeCode(code) {
            var idx = scannedCodes.indexOf(code);
            if (idx !== -1) scannedCodes.splice(idx, 1);
            scannedSet.delete(code);
            updateStatus();
        }

        function onScanSuccess(decodedText) {
            var code = (decodedText || '').trim();
            if (!code || scannedSet.has(code)) return;

            scannedSet.add(code);
            scannedCodes.push(code);
            addRow(code);
            updateStatus();

            if (navigator.vibrate) navigator.vibrate(80);
        }

        function onScanFailure() {
            // Không tìm thấy mã trong khung hình hiện tại — bỏ qua, đây là chuyện bình thường mỗi frame.
        }

        function isNativeMultiScanSupported() {
            return typeof window.BarcodeDetector !== 'undefined';
        }

        // ============================================================
        // CHẾ ĐỘ 1: BarcodeDetector gốc — quét NHIỀU mã cùng lúc trong 1 khung hình.
        // ============================================================
        function startNativeScanner() {
            return navigator.mediaDevices.getUserMedia({
                video: {
                    facingMode: 'environment',
                    width: { ideal: 1280 },
                    height: { ideal: 720 }
                }
            }).then(function (stream) {
                nativeStream = stream;
                readerEl.innerHTML = '';
                nativeVideoEl = document.createElement('video');
                nativeVideoEl.setAttribute('playsinline', 'true');
                nativeVideoEl.setAttribute('autoplay', 'true');
                nativeVideoEl.muted = true;
                nativeVideoEl.style.width = '100%';
                nativeVideoEl.style.display = 'block';
                nativeVideoEl.srcObject = stream;
                readerEl.appendChild(nativeVideoEl);
                return nativeVideoEl.play();
            }).then(function () {
                nativeDetector = new BarcodeDetector({
                    formats: ['code_128', 'code_39', 'code_93', 'codabar', 'ean_13', 'ean_8', 'itf', 'upc_a', 'upc_e', 'qr_code', 'data_matrix']
                });
                scannerRunning = true;

                // Quét lặp lại nhiều lần/giây, mỗi lần detect() trả về TẤT CẢ mã tìm thấy trong khung hình hiện tại
                // (không phải chỉ 1 mã) — nhờ chạy bằng phần cứng nên không sợ quá tải như giải mã bằng JS thuần.
                nativeLoopHandle = setInterval(function () {
                    if (nativeDetecting || !nativeVideoEl) return;
                    nativeDetecting = true;
                    nativeDetector.detect(nativeVideoEl).then(function (barcodes) {
                        barcodes.forEach(function (b) { onScanSuccess(b.rawValue); });
                    }).catch(function (err) {
                        console.error('Lỗi detect() BarcodeDetector:', err);
                    }).finally(function () {
                        nativeDetecting = false;
                    });
                }, 300);
            });
        }

        function stopNativeScanner() {
            if (nativeLoopHandle) {
                clearInterval(nativeLoopHandle);
                nativeLoopHandle = null;
            }
            if (nativeStream) {
                nativeStream.getTracks().forEach(function (t) { t.stop(); });
                nativeStream = null;
            }
            if (nativeVideoEl) {
                nativeVideoEl.remove();
                nativeVideoEl = null;
            }
            scannerRunning = false;
            return Promise.resolve();
        }

        // ============================================================
        // CHẾ ĐỘ 2: html5-qrcode (fallback) — quét LIÊN TỤC từng mã một.
        // ============================================================
        function startHtml5QrcodeScanner() {
            readerEl.innerHTML = '';
            html5QrCode = new Html5Qrcode(readerId, {
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
                // Tham số đầu tiên CHỈ được đúng 1 key (facingMode hoặc deviceId) — thư viện tự throw lỗi
                // nếu kèm thêm key khác (từng là bug ở đây), nên width/height phải để ở videoConstraints bên dưới.
                { facingMode: 'environment' },
                {
                    fps: 10,
                    // Khung quét gần hết khung hình — không cần canh mã vào giữa, đưa vào đâu trong khung
                    // hình cũng bắt được. Vùng quét lớn hơn thì mỗi lần xử lý sẽ chậm hơn 1 chút — đã giảm
                    // fps xuống 10 để bù lại.
                    qrbox: function (viewfinderWidth, viewfinderHeight) {
                        var width = Math.floor(viewfinderWidth * 0.95);
                        var height = Math.floor(viewfinderHeight * 0.85);
                        return { width: width, height: height };
                    },
                    aspectRatio: 1.7777778,
                    // Độ phân giải camera vừa đủ đọc mã vạch, không xin quá cao (1080p) để đỡ tốn CPU xử lý mỗi khung hình.
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

        // ============================================================
        // Chọn chế độ + điểm vào chung
        // ============================================================
        function startScanner() {
            usingNativeMulti = isNativeMultiScanSupported();
            var starter = usingNativeMulti ? startNativeScanner() : startHtml5QrcodeScanner();

            starter.catch(function (err) {
                var detail = (err && err.message) ? err.message : String(err);
                statusEl.textContent = 'Không thể truy cập camera: ' + detail;
                console.error('Không mở được camera:', err);
            });
        }

        function stopScanner() {
            return usingNativeMulti ? stopNativeScanner() : stopHtml5QrcodeScanner();
        }

        function showSummary() {
            summaryCount.textContent = scannedCodes.length;
            summaryList.innerHTML = '';
            scannedCodes.forEach(function (code) {
                var li = document.createElement('li');
                li.className = 'list-group-item d-flex align-items-center gap-2';
                li.innerHTML = '📦<span>' + code + '</span>';
                summaryList.appendChild(li);
            });
            showSection(summarySection);
        }

        // ===== Nút "Quét mã" — bắt đầu quét, PHẢI gọi trực tiếp trong handler click này =====
        btnStartScan.addEventListener('click', function () {
            resetState();
            showSection(scanSection);
            startScanner();
        });

        // ===== Nút "Huỷ" — bỏ hết, quay về màn hình ban đầu =====
        btnCancel.addEventListener('click', function () {
            stopScanner().then(function () {
                resetState();
                showSection(idleSection);
            });
        });

        // ===== Nút "OK" — kết thúc, hiện kết quả =====
        btnFinish.addEventListener('click', function () {
            stopScanner().then(showSummary);
        });

        // ===== Nút "Quét lại" (từ màn kết quả) — quét mẻ mới =====
        btnRescan.addEventListener('click', function () {
            resetState();
            showSection(scanSection);
            startScanner();
        });

        window.addEventListener('beforeunload', function () {
            stopScanner();
        });

        showSection(idleSection);
    });
})();
