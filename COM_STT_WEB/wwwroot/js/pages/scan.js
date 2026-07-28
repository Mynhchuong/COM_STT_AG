// Demo scan hàng loạt PCard.
// 3 trạng thái: idle (nút "Quét mã") -> scanning (camera + Huỷ/OK) -> summary (tổng số + danh sách).
// Quét từng mã một liên tục (không cố đọc nhiều mã/1 khung hình), lọc trùng theo giá trị.
//
// Camera chỉ được mở BÊN TRONG 1 sự kiện click của người dùng (bấm nút "Quét mã") —
// Safari/iOS chặn getUserMedia() nếu gọi tự động lúc tải trang (không có thao tác bấm),
// nên tuyệt đối không gọi startScanner() ngoài các nút bấm dưới đây.
(function () {
    document.addEventListener('DOMContentLoaded', function () {
        var readerId = 'scan-reader';
        if (!document.getElementById(readerId)) return;

        var scannedCodes = [];
        var scannedSet = new Set();
        var html5QrCode = null;
        var scannerRunning = false;

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
                '<i class="material-symbols-rounded text-success" aria-hidden="true">check_circle</i>' +
                '<span>' + code + '</span>' +
                '</span>' +
                '<button type="button" class="btn btn-link text-danger p-0 btn-remove" aria-label="Xoá">' +
                '<i class="material-symbols-rounded" aria-hidden="true">close</i>' +
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

        function startScanner() {
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

            html5QrCode.start(
                // Tham số đầu tiên CHỈ được đúng 1 key (facingMode hoặc deviceId) — thư viện tự throw lỗi
                // nếu kèm thêm key khác (từng là bug ở đây), nên width/height phải để ở videoConstraints bên dưới.
                { facingMode: 'environment' },
                {
                    fps: 10,
                    // Khung quét gần hết khung hình — không cần canh mã vào giữa như trước, đưa vào đâu trong
                    // khung hình cũng bắt được. Vùng quét lớn hơn thì mỗi lần xử lý sẽ chậm hơn 1 chút — đây là
                    // đánh đổi giữa "khỏi canh khung" và tốc độ, đã giảm fps xuống 10 để bù lại.
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
                    },
                    // Ưu tiên dùng bộ quét mã có sẵn của máy khi hỗ trợ (Chrome/Android) — nhanh hơn nhiều vì
                    // chạy native thay vì giải mã bằng JS; máy không hỗ trợ (iPhone/Safari) tự động dùng JS như cũ.
                    experimentalFeatures: {
                        useBarCodeDetectorIfSupported: true
                    }
                },
                onScanSuccess,
                onScanFailure
            ).then(function () {
                scannerRunning = true;
            }).catch(function (err) {
                var detail = (err && err.message) ? err.message : String(err);
                statusEl.textContent = 'Không thể truy cập camera: ' + detail;
                console.error('Không mở được camera:', err);
            });
        }

        function stopScanner() {
            if (!html5QrCode || !scannerRunning) return Promise.resolve();
            return html5QrCode.stop().then(function () {
                scannerRunning = false;
                html5QrCode.clear();
            }).catch(function () {
                scannerRunning = false;
            });
        }

        function showSummary() {
            summaryCount.textContent = scannedCodes.length;
            summaryList.innerHTML = '';
            scannedCodes.forEach(function (code) {
                var li = document.createElement('li');
                li.className = 'list-group-item d-flex align-items-center gap-2';
                li.innerHTML = '<i class="material-symbols-rounded text-success" aria-hidden="true">inventory_2</i><span>' + code + '</span>';
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
