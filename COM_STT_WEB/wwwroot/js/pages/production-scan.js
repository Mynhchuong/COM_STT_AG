(function () {
    document.addEventListener('DOMContentLoaded', function () {
        const config = window.scanConfig;
        if (!config) return;

        const seq = config.seq;
        const webBaseUrl = config.webBaseUrl;

        function getAntiForgeryToken() {
            const el = document.querySelector('input[name="__RequestVerificationToken"]');
            return el ? el.value : '';
        }

        // Initialize Layout Toggle FAB
        if (typeof initLayoutToggle === 'function') {
            initLayoutToggle('production-scan');
        }

        const idleSection = document.getElementById('idle-section');
        const scanSection = document.getElementById('scan-section');
        const scanReaderEl = document.getElementById('scan-reader');
        const btnStartScan = document.getElementById('btn-start-scan');
        const btnStopScan = document.getElementById('btn-stop-scan');
        
        const manualPcardInput = document.getElementById('manualPcardInput');
        const btnSubmitManual = document.getElementById('btnSubmitManual');
        const statusMessageEl = document.getElementById('scan-status-message');
        const totalScannedCountBadge = document.getElementById('totalScannedCount');
        const pcardsListContainer = document.getElementById('pcardsListContainer');
        const noDataRow = document.getElementById('noDataRow');

        let html5QrCode = null;
        let nativeStream = null;
        let nativeVideoEl = null;
        let nativeDetector = null;
        let nativeLoopHandle = null;
        let nativeDetecting = false;
        let usingNativeMulti = false;
        let scannerRunning = false;

        // Local cache of scanned PCards (to prevent redundant API calls)
        const scannedPcardsSet = new Set();
        document.querySelectorAll('tr[data-pcard]').forEach(function (row) {
            scannedPcardsSet.add(row.dataset.pcard.trim().toUpperCase());
        });

        updateScannedCountBadge();

        function updateScannedCountBadge() {
            if (totalScannedCountBadge) {
                totalScannedCountBadge.textContent = scannedPcardsSet.size + ' thẻ đã quét';
            }
        }

        // --- UX feedback: vibration, audio BEEP & color flashes ---
        function triggerFeedback(success, message) {
            // Text message
            if (statusMessageEl) {
                statusMessageEl.textContent = message;
                statusMessageEl.className = 'text-sm font-weight-bolder mt-2 d-block py-2 px-3 border-radius-md ' +
                    (success ? 'bg-gradient-success text-white' : 'bg-gradient-danger text-white');
            }

            // Play Web Audio Beep (essential for noisy Android factory floor)
            if (typeof playScanBeep === 'function') {
                playScanBeep(success);
            }

            // Color Flash on scan-reader wrapper
            if (scanReaderEl) {
                const flashClass = success ? 'flash-success' : 'flash-error';
                scanReaderEl.classList.add(flashClass);
                setTimeout(function () {
                    scanReaderEl.classList.remove(flashClass);
                }, 400);
            }

            // Mobile Vibration pattern
            if (navigator.vibrate) {
                if (success) {
                    navigator.vibrate(120); // 1 single strong vibration
                } else {
                    navigator.vibrate([100, 60, 100]); // 2 short error vibrations
                }
            }
        }

        // Initialize Android PDA barcode scanner auto-focus keeper
        if (typeof initAndroidPdaScannerFocus === 'function') {
            initAndroidPdaScannerFocus('manualPcardInput');
        }

        // --- Scanned event handler ---
        async function handlePcardScanned(code) {
            const pcardNo = (code || '').trim().toUpperCase();
            if (!pcardNo) return;

            // 1. Check local DOM cache first
            if (scannedPcardsSet.has(pcardNo)) {
                triggerFeedback(false, 'Thẻ PCard ' + pcardNo + ' đã được quét trong đợt này rồi!');
                return;
            }

            // Temporarily block to avoid double scans
            scannedPcardsSet.add(pcardNo);

            try {
                // 2. Check if registered in other batches
                const checkUrl = webBaseUrl + '/CheckPcard?pcardNo=' + encodeURIComponent(pcardNo);
                const checkRes = await fetch(checkUrl);
                if (checkRes.status === 200) {
                    scannedPcardsSet.delete(pcardNo); // Remove from temp cache
                    triggerFeedback(false, 'Thẻ PCard ' + pcardNo + ' đã đăng ký ở đợt sản xuất khác!');
                    return;
                }

                // 3. Get prod plan details
                const planUrl = webBaseUrl + '/ProdPlan?cardNo=' + encodeURIComponent(pcardNo);
                const planRes = await fetch(planUrl);
                if (!planRes.ok) {
                    scannedPcardsSet.delete(pcardNo); // Remove from temp cache
                    triggerFeedback(false, 'Không tìm thấy thông tin kế hoạch cho thẻ PCard ' + pcardNo);
                    return;
                }

                const planData = await planRes.json();
                const plan = planData.data;

                // 4. Save to head table
                const saveUrl = webBaseUrl + '/SavePcard';
                const saveRes = await fetch(saveUrl, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'RequestVerificationToken': getAntiForgeryToken()
                    },
                    body: JSON.stringify({
                        PCARD_NO: pcardNo,
                        C_GROUP_SUM: plan.MES_GROUP_SUM,
                        C_STYLE: plan.C_STYLE,
                        C_ORDER: plan.I_PO_NO,
                        I_PARTS_NO: plan.I_PARTS_NO,
                        C_SIZE: plan.C_SIZE,
                        SEQ: seq,
                        Q_QTY: plan.Q_QTY
                    })
                });

                if (!saveRes.ok) {
                    scannedPcardsSet.delete(pcardNo); // Remove from temp cache
                    const err = await saveRes.json();
                    triggerFeedback(false, err.message || 'Lỗi khi lưu PCard vào đợt!');
                    return;
                }

                // 5. Success! Add to DOM table & mobile card view
                const mobileCardContainer = document.getElementById('mobilePcardCardsContainer');
                const noDataMobileCard = document.getElementById('noDataMobileCard');

                if (noDataRow) noDataRow.remove();
                if (noDataMobileCard) noDataMobileCard.remove();

                // Create desktop row
                const tr = document.createElement('tr');
                tr.className = 'pcard-item new-scan';
                tr.id = 'row-' + pcardNo;
                tr.dataset.pcard = pcardNo;
                tr.innerHTML = `
                    <td>
                        <span class="text-sm font-weight-bold text-dark">${pcardNo}</span>
                    </td>
                    <td>
                        <div class="d-flex flex-column justify-content-center">
                            <h6 class="mb-0 text-xs">${plan.MES_GROUP_SUM ?? ''}</h6>
                            <p class="text-xxs text-secondary mb-0">${plan.C_STYLE ?? ''}</p>
                        </div>
                    </td>
                    <td>
                        <div class="d-flex flex-column justify-content-center">
                            <h6 class="mb-0 text-xs">Size ${plan.C_SIZE ?? ''} · Order ${plan.I_PO_NO ?? ''}</h6>
                            <p class="text-xxs text-secondary mb-0">Parts No: ${plan.I_PARTS_NO ?? ''}</p>
                        </div>
                    </td>
                    <td class="align-middle text-center">
                        <span class="text-xs font-weight-bold text-dark">${plan.Q_QTY} pcs</span>
                    </td>
                    <td class="align-middle text-center">
                        <button type="button" class="btn btn-link text-danger p-0 mb-0 btn-delete-pcard" data-pcard="${pcardNo}" aria-label="Xoá">
                            🗑️
                        </button>
                    </td>
                `;

                tr.querySelector('.btn-delete-pcard').addEventListener('click', function () {
                    deletePcard(pcardNo);
                });
                pcardsListContainer.insertBefore(tr, pcardsListContainer.firstChild);

                // Create mobile card item
                if (mobileCardContainer) {
                    const mCard = document.createElement('div');
                    mCard.className = 'mobile-pcard-item new-scan';
                    mCard.id = 'mobile-row-' + pcardNo;
                    mCard.dataset.pcard = pcardNo;
                    mCard.innerHTML = `
                        <div>
                            <strong class="text-dark text-sm d-block">${pcardNo}</strong>
                            <div class="text-xs text-secondary mt-1">
                                <strong>${plan.MES_GROUP_SUM ?? ''}</strong> · Style: ${plan.C_STYLE ?? ''}
                            </div>
                            <div class="text-xs text-secondary">
                                Size ${plan.C_SIZE ?? ''} · Order ${plan.I_PO_NO ?? ''} · Qty: <strong class="text-primary">${plan.Q_QTY} pcs</strong>
                            </div>
                        </div>
                        <div>
                            <button type="button" class="btn btn-link text-danger p-2 mb-0 btn-delete-pcard" data-pcard="${pcardNo}" aria-label="Xoá">
                                🗑️
                            </button>
                        </div>
                    `;
                    mCard.querySelector('.btn-delete-pcard').addEventListener('click', function () {
                        deletePcard(pcardNo);
                    });
                    mobileCardContainer.insertBefore(mCard, mobileCardContainer.firstChild);

                    setTimeout(function () {
                        mCard.classList.remove('new-scan');
                    }, 2000);
                }

                // Pulse color highlight transition
                setTimeout(function () {
                    tr.classList.remove('new-scan');
                }, 2000);

                updateScannedCountBadge();
                triggerFeedback(true, 'Đã thêm PCard ' + pcardNo + ' thành công! (' + plan.Q_QTY + ' pcs)');
            } catch (err) {
                scannedPcardsSet.delete(pcardNo);
                console.error(err);
                triggerFeedback(false, 'Lỗi kết nối máy chủ!');
            }
        }

        // --- Delete PCard action ---
        async function deletePcard(pcardNo) {
            if (!confirm('Xác nhận xoá PCARD ' + pcardNo + ' khỏi đợt sản xuất?')) {
                return;
            }

            try {
                const url = webBaseUrl + '/DeletePcard?pcardNo=' + encodeURIComponent(pcardNo);
                const response = await fetch(url, {
                    method: 'DELETE',
                    headers: { 'RequestVerificationToken': getAntiForgeryToken() }
                });

                if (response.ok) {
                    const row = document.getElementById('row-' + pcardNo);
                    const mRow = document.getElementById('mobile-row-' + pcardNo);

                    if (row) row.remove();
                    if (mRow) mRow.remove();

                    scannedPcardsSet.delete(pcardNo);
                    updateScannedCountBadge();

                    if (pcardsListContainer.children.length === 0) {
                        renderNoDataRow();
                    }
                    if (statusMessageEl) {
                        statusMessageEl.textContent = 'Đã xoá thẻ PCard ' + pcardNo;
                        statusMessageEl.className = 'text-xs font-weight-bold text-success mt-2 d-block';
                    }
                } else {
                    alert('Không thể xoá PCard này, vui lòng thử lại.');
                }
            } catch (err) {
                console.error(err);
                alert('Lỗi kết nối khi xoá PCard!');
            }
        }

        function renderNoDataRow() {
            const tr = document.createElement('tr');
            tr.id = 'noDataRow';
            tr.innerHTML = `
                <td colspan="5" class="text-center py-5">
                    <p class="text-secondary text-sm mb-0">Chưa quét mã PCard nào cho đợt sản xuất này.</p>
                </td>
            `;
            pcardsListContainer.appendChild(tr);

            const mContainer = document.getElementById('mobilePcardCardsContainer');
            if (mContainer) {
                mContainer.innerHTML = '<div class="text-center py-4 text-secondary text-sm" id="noDataMobileCard">Chưa quét mã PCard nào.</div>';
            }
        }

        // --- Manual submission ---
        function submitManualPcard() {
            const code = manualPcardInput.value.trim().toUpperCase();
            if (!code) return;

            handlePcardScanned(code);
            manualPcardInput.value = '';
            manualPcardInput.focus();
        }

        if (btnSubmitManual) {
            btnSubmitManual.addEventListener('click', submitManualPcard);
        }
        if (manualPcardInput) {
            manualPcardInput.addEventListener('keydown', function (e) {
                if (e.key === 'Enter') {
                    e.preventDefault();
                    submitManualPcard();
                }
            });
            // Auto focus on load
            manualPcardInput.focus();
        }

        // --- Attach delete events to existing list rows ---
        document.querySelectorAll('.btn-delete-pcard').forEach(function (btn) {
            btn.addEventListener('click', function () {
                const code = btn.dataset.pcard;
                deletePcard(code);
            });
        });

        // ============================================================
        // CAMERA SCANNER ENGINE REUSED FROM THE SCAN VIEW
        // ============================================================
        function onScanSuccess(decodedText) {
            const code = (decodedText || '').trim().toUpperCase();
            if (!code) return;
            handlePcardScanned(code);
        }

        function onScanFailure() {
            // Ignore camera frames with no decodable barcode
        }

        function isNativeMultiScanSupported() {
            return typeof window.BarcodeDetector !== 'undefined';
        }

        // --- Mode 1: Native BarcodeDetector (Chrome/Edge Android) ---
        function startNativeScanner() {
            return navigator.mediaDevices.getUserMedia({
                video: {
                    facingMode: 'environment',
                    width: { ideal: 1280 },
                    height: { ideal: 720 }
                }
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

        // --- Mode 2: HTML5-QRCode (iOS / Safari) ---
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
                if (statusMessageEl) {
                    statusMessageEl.textContent = 'Không mở được camera: ' + detail;
                    statusMessageEl.className = 'text-xs font-weight-bold text-danger mt-2';
                }
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
                    if (statusMessageEl) {
                        statusMessageEl.textContent = '';
                    }
                });
            });
        }

        window.addEventListener('beforeunload', function () {
            stopScanner();
        });
    });
})();
