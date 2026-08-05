(function () {
    document.addEventListener('DOMContentLoaded', function () {
        const cardNoInput   = document.getElementById('cardNoInput');
        const btnFind       = document.getElementById('btnFindBasket');
        const basketStatus  = document.getElementById('basketStatus');

        const btnStartScan  = document.getElementById('btn-start-scan');
        const btnStopScan   = document.getElementById('btn-stop-scan');
        const idleSection   = document.getElementById('idle-section');
        const scanSection   = document.getElementById('scan-section');
        const scanReaderEl  = document.getElementById('scan-reader');

        const headerSection = document.getElementById('headerSection');
        const headerCards    = document.getElementById('headerCards');

        const destinationSection = document.getElementById('destinationSection');
        const nhaButtons     = document.querySelectorAll('.nha-btn');
        const lineSelect     = document.getElementById('lineSelect');

        const detailTable = document.getElementById('basketDetailTable');
        const detailBody  = document.getElementById('detailBody');
        const detailEmpty = document.getElementById('detailEmpty');

        const btnConfirmOut = document.getElementById('btnConfirmOut');

        const saveLoadingOverlay = document.getElementById('saveLoadingOverlay');

        const confirmOverlay = document.getElementById('confirmOverlay');
        const confirmMessage = document.getElementById('confirmMessage');
        const confirmOkBtn   = document.getElementById('confirmOk');
        const confirmCancelBtn = document.getElementById('confirmCancel');

        let currentBasketId = null;
        let currentDetailRows = [];
        let selectedNha = null;
        let isSaving = false;

        function showSaveOverlay() { isSaving = true; saveLoadingOverlay.classList.add('show'); }
        function hideSaveOverlay() { isSaving = false; saveLoadingOverlay.classList.remove('show'); }

        function getAntiForgeryToken() {
            const el = document.querySelector('input[name="__RequestVerificationToken"]');
            return el ? el.value : '';
        }

        function showStatus(text, isError) {
            basketStatus.textContent = text;
            basketStatus.className = 'text-sm mt-2 ' + (isError ? 'text-danger font-weight-bold' : 'text-secondary');
        }

        // Popup xác nhận riêng của app — KHÔNG dùng confirm() gốc trình duyệt.
        let confirmResolver = null;
        function showConfirm(message) {
            confirmMessage.textContent = message;
            confirmOverlay.classList.add('show');
            return new Promise(function (resolve) { confirmResolver = resolve; });
        }
        function closeConfirm(result) {
            confirmOverlay.classList.remove('show');
            if (confirmResolver) { confirmResolver(result); confirmResolver = null; }
        }
        confirmOkBtn.addEventListener('click', function () { closeConfirm(true); });
        confirmCancelBtn.addEventListener('click', function () { closeConfirm(false); });

        // Nhà B và nhà E đều phải chọn thêm Line cụ thể, nhưng 2 nhà có 2 danh sách Line RIÊNG
        // (số trùng nhau giữa 2 nhà, VD cả 2 đều có "2-10") — nên khi lưu phải ghép thêm chữ nhà
        // phía trước (VD "B-2-10" / "E-2-10") để không lẫn lộn nhà nào ra nhà nào.
        const NHA_REQUIRE_LINE = ['B', 'E'];

        // Danh sách Line của nhà B — 13 nhóm, mỗi nhóm 8 line đánh số liên tục + 1 line "-CB".
        // Nhóm 13 dùng số 08-11/13-16 (bỏ qua 12) — giữ nguyên đúng số line thật ngoài xưởng.
        function buildLineOptionsB() {
            const options = [];
            for (let g = 1; g <= 12; g++) {
                const start = (g - 1) * 8 + 1;
                for (let n = start; n < start + 8; n++) {
                    options.push(`${g}-${n}`);
                }
                options.push(`${g}-CB`);
            }
            options.push('13-08', '13-09', '13-10', '13-11', '13-13', '13-14', '13-15', '13-16', '13-CB');
            return options;
        }

        // Danh sách Line của nhà E — 9 nhóm, mỗi nhóm 10 line đánh số 2 chữ số (01..10), riêng
        // nhóm 9 chỉ có 7 line (9-01..9-07).
        function buildLineOptionsE() {
            const options = [];
            for (let g = 1; g <= 9; g++) {
                const count = g === 9 ? 7 : 10;
                for (let n = 1; n <= count; n++) {
                    options.push(`${g}-${String(n).padStart(2, '0')}`);
                }
            }
            return options;
        }

        function populateLineSelect(nha) {
            lineSelect.innerHTML = '<option value="">-- Chọn Line --</option>';
            const codes = nha === 'B' ? buildLineOptionsB() : buildLineOptionsE();
            codes.forEach(function (code) {
                const opt = document.createElement('option');
                opt.value = code;
                opt.textContent = code;
                lineSelect.appendChild(opt);
            });
        }

        function formatOutTo(outTo) {
            if (!outTo) return '';
            if (/^[ABCE]$/.test(outTo)) return `Nhà ${outTo}`;
            const nha = outTo.charAt(0);
            const line = outTo.slice(2);
            return `Nhà ${nha} — Line ${line}`;
        }

        nhaButtons.forEach(function (btn) {
            btn.addEventListener('click', function () {
                selectedNha = btn.dataset.nha;
                nhaButtons.forEach(function (b) { b.classList.toggle('active', b === btn); });
                const needsLine = NHA_REQUIRE_LINE.includes(selectedNha);
                lineSelect.classList.toggle('d-none', !needsLine);
                if (needsLine) populateLineSelect(selectedNha);
                lineSelect.value = '';
                updateConfirmButton();
            });
        });
        lineSelect.addEventListener('change', updateConfirmButton);

        function renderHeader(rows) {
            if (!rows || rows.length === 0) {
                headerSection.style.display = 'none';
                headerCards.innerHTML = '';
                return;
            }

            headerCards.innerHTML = rows.map(row => {
                let stateBadge = '<span class="badge bg-gradient-secondary">CHƯA OUT</span>';
                if (row.IS_OUT === 'Y') {
                    stateBadge = '<span class="badge bg-gradient-success">✅ ĐÃ OUT XONG</span>';
                } else if (row.PROCESS_OUT === 'Y') {
                    stateBadge = '<span class="badge bg-gradient-warning">🔶 ĐANG OUT DỞ</span>';
                }
                return `
                <div class="col-12 col-md-6 mb-2">
                    <div class="card header-info-card border-radius-lg h-100">
                        <div class="card-body p-3">
                            <div class="d-flex align-items-center justify-content-between mb-2">
                                <strong class="text-dark text-md">${row.I_CARD_NO || ''}</strong>
                                ${stateBadge}
                            </div>
                            <div class="text-xs text-secondary">
                                PO: <strong class="text-dark">${row.I_PO_NO || ''}</strong> ·
                                Style: <strong class="text-dark">${row.C_STYLE || ''}</strong> ·
                                Size: <strong class="text-dark">${row.C_SIZE || ''}</strong>
                            </div>
                            <div class="text-xs text-secondary">
                                Ngày vào: ${row.IN_DATE ? new Date(row.IN_DATE).toLocaleString('vi-VN') : ''}
                            </div>
                        </div>
                    </div>
                </div>`;
            }).join('');
            headerSection.style.display = 'block';
            destinationSection.style.display = 'block';
        }

        function renderDetail(rows) {
            currentDetailRows = rows || [];

            if (!rows || rows.length === 0) {
                detailTable.style.display = 'none';
                detailEmpty.style.display = 'block';
                detailEmpty.innerHTML = `
                    🚫
                    <p class="mb-0">Basket này chưa có dữ liệu part.</p>`;
                return;
            }

            detailBody.innerHTML = rows.map(row => {
                const isDone = row.IS_DONE === 'Y';
                const isOut = row.IS_OUT === 'Y';
                let rowClass = '';
                let statusHtml = '';
                let checkboxHtml = '';

                if (!isDone) {
                    rowClass = 'row-locked';
                    statusHtml = '<span class="text-secondary">🔒 Chưa nhận đủ — chưa Out được</span>';
                    checkboxHtml = `<input type="checkbox" class="row-check" disabled /> <span class="check-label">Chưa Out được</span>`;
                } else if (isOut) {
                    rowClass = 'row-out';
                    statusHtml = `<span class="text-success font-weight-bold">✅ Đã Out → ${formatOutTo(row.OUT_TO)}</span>`;
                    checkboxHtml = `<input type="checkbox" class="row-check" checked disabled /> <span class="check-label">Đã Out rồi</span>`;
                } else {
                    statusHtml = '<span class="text-secondary">⬜ Sẵn sàng Out</span>';
                    checkboxHtml = `<input type="checkbox" class="row-check" data-parts="${row.I_PARTS_NO || ''}" /> <span class="check-label">Chọn part này để Out</span>`;
                }

                return `
                    <tr class="${rowClass}">
                        <td class="col-check" data-label="Chọn">${checkboxHtml}</td>
                        <td class="col-part-name" data-label="Part">
                            <strong>${row.I_PARTS_NO || ''}</strong> — ${row.N_PARTS_NO || ''}
                        </td>
                        <td class="col-meta" data-label="PO / Style / Size">${row.I_PO_NO || ''} · ${row.C_STYLE || ''} · Size ${row.C_SIZE || ''}</td>
                        <td data-label="Cần">${row.C_QTY || 0}</td>
                        <td data-label="Đã nhận">${row.QTY_RECEIVE || 0}</td>
                        <td data-label="Trạng thái Out">${statusHtml}</td>
                    </tr>`;
            }).join('');

            detailEmpty.style.display = 'none';
            detailTable.style.display = 'table';
            updateConfirmButton();
        }

        detailBody.addEventListener('change', function (e) {
            if (e.target.classList.contains('row-check')) updateConfirmButton();
        });

        function getCheckedPartsNo() {
            return Array.from(detailBody.querySelectorAll('.row-check:not(:disabled):checked'))
                .map(el => el.dataset.parts);
        }

        function updateConfirmButton() {
            const checkedCount = getCheckedPartsNo().length;
            const destinationReady = selectedNha && (!NHA_REQUIRE_LINE.includes(selectedNha) || lineSelect.value);
            btnConfirmOut.disabled = isSaving || checkedCount === 0 || !destinationReady;
            btnConfirmOut.textContent = checkedCount > 0
                ? `🚪 Xác nhận Out (${checkedCount} part)`
                : '🚪 Xác nhận Out';
        }

        btnConfirmOut.addEventListener('click', async function () {
            const partsNo = getCheckedPartsNo();
            if (partsNo.length === 0 || !selectedNha) return;
            const needsLine = NHA_REQUIRE_LINE.includes(selectedNha);
            if (needsLine && !lineSelect.value) return;
            const outTo = needsLine ? `${selectedNha}-${lineSelect.value}` : selectedNha;

            const ok = await showConfirm(`Xác nhận Out ${partsNo.length} part sau đây tới ${formatOutTo(outTo)}?\n${partsNo.join(', ')}`);
            if (!ok) return;

            btnConfirmOut.disabled = true;
            showSaveOverlay();
            try {
                const query = new URLSearchParams();
                query.set('basketId', currentBasketId);
                query.set('outTo', outTo);
                partsNo.forEach(p => query.append('partsNo', p));

                const res = await fetch('/Production2/MarkBasketDetailOut?' + query.toString(), {
                    method: 'POST',
                    headers: { 'RequestVerificationToken': getAntiForgeryToken() }
                });
                const data = await res.json();
                if (res.ok && data.success) {
                    showStatus(`✓ Đã Out ${partsNo.length} part tới ${formatOutTo(outTo)}.`, false);
                    window.PdaHelper.feedback(true, `Đã Out ${partsNo.length} part.`);
                    selectedNha = null;
                    nhaButtons.forEach(b => b.classList.remove('active'));
                    lineSelect.classList.add('d-none');
                    lineSelect.value = '';
                    await findBasket(currentBasketId, true);
                } else {
                    const msg = data.message || 'Không thể Out.';
                    showStatus(msg, true);
                    window.PdaHelper.feedback(false, msg);
                }
            } catch (err) {
                console.error(err);
                showStatus('Lỗi kết nối máy chủ!', true);
                window.PdaHelper.feedback(false, 'Lỗi kết nối máy chủ!');
            } finally {
                hideSaveOverlay();
                updateConfirmButton();
            }
        });

        async function findBasket(basketIdOverride, silent) {
            let basketId = basketIdOverride;

            if (!basketId) {
                const cardNo = cardNoInput.value.trim().toUpperCase();
                if (!cardNo) {
                    showStatus('Quét hoặc nhập mã PCard.', true);
                    return;
                }

                showStatus('Đang tra basket theo PCard...', false);
                try {
                    const res = await fetch('/Production2/GetBasketIdByCard?cardNo=' + encodeURIComponent(cardNo));
                    const data = await res.json();
                    if (res.ok && data.success) {
                        basketId = data.basketId;
                    } else {
                        showStatus(data.message || 'Không tìm thấy basket cho PCard này.', true);
                        window.PdaHelper.feedback(false, data.message || 'Không tìm thấy basket cho PCard này.', { flashEl: scanReaderEl });
                        return;
                    }
                } catch (err) {
                    console.error(err);
                    showStatus('Lỗi kết nối máy chủ!', true);
                    return;
                }
            }

            currentBasketId = basketId;
            if (!silent) showStatus('Đang tải...', false);

            try {
                const [headerRes, detailRes] = await Promise.all([
                    fetch('/Production2/GetBasketHeader?basketId=' + basketId),
                    fetch('/Production2/GetBasketDetail?basketId=' + basketId)
                ]);
                const headerData = await headerRes.json();
                const detailData = await detailRes.json();

                if (headerRes.ok && headerData.success) {
                    renderHeader(headerData.data);
                }
                if (detailRes.ok && detailData.success) {
                    renderDetail(detailData.data);
                    if (!silent) {
                        showStatus(`Basket ${basketId} — ${detailData.data.length} part.`, false);
                        window.PdaHelper.feedback(true, `Tìm thấy basket ${basketId}.`, { flashEl: scanReaderEl });
                    }
                } else {
                    showStatus(detailData.message || 'Không tải được dữ liệu.', true);
                }
            } catch (err) {
                console.error(err);
                showStatus('Lỗi kết nối máy chủ!', true);
            }
        }

        btnFind.addEventListener('click', function () { findBasket(); });
        cardNoInput.addEventListener('keydown', function (e) { if (e.key === 'Enter') { e.preventDefault(); findBasket(); } });

        // Súng bắn mã vạch PDA — quét mã PCard là tự tra basket luôn, không cần bấm Tìm.
        if (window.PdaHelper && window.PdaHelper.initScanner) {
            window.PdaHelper.initScanner('cardNoInput', function (code) {
                cardNoInput.value = code;
                findBasket();
            }, { autoFocus: false, clearOnScan: true });
        }

        // Camera quét mã vạch/QR — giống hệt cơ chế đã dùng ở Set In / Kiểm tra Pending.
        let html5QrCode = null;
        let isCameraRunning = false;

        async function stopCamera() {
            if (html5QrCode && isCameraRunning) {
                try { await html5QrCode.stop(); } catch (e) { console.warn('Stop camera error:', e); }
                isCameraRunning = false;
            }
            scanSection.classList.add('d-none');
            idleSection.classList.remove('d-none');
        }

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
                        cardNoInput.value = decodedText.trim().toUpperCase();
                        findBasket();
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
    });
})();
