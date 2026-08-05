(function () {
    document.addEventListener('DOMContentLoaded', function () {
        const basketIdInput = document.getElementById('basketIdInput');
        const cardNoInput   = document.getElementById('cardNoInput');
        const btnFind       = document.getElementById('btnFindBasket');
        const basketStatus  = document.getElementById('basketStatus');

        const headerSection = document.getElementById('headerSection');
        const headerCards    = document.getElementById('headerCards');

        const detailTable = document.getElementById('basketDetailTable');
        const detailBody  = document.getElementById('detailBody');
        const detailEmpty = document.getElementById('detailEmpty');

        const numpadOverlay = document.getElementById('numpadOverlay');
        const numpadTitle   = document.getElementById('numpadTitle');
        const numpadDisplay = document.getElementById('numpadDisplay');
        const numpadError   = document.getElementById('numpadError');
        const numpadSave    = document.getElementById('numpadSave');
        const numpadCancel  = document.getElementById('numpadCancel');
        const numpadClear   = document.getElementById('numpadClear');
        const numpadBackspace = document.getElementById('numpadBackspace');

        const saveLoadingOverlay = document.getElementById('saveLoadingOverlay');
        const basketOutBanner = document.getElementById('basketOutBanner');
        const btnMarkAllDone = document.getElementById('btnMarkAllDone');

        const confirmOverlay = document.getElementById('confirmOverlay');
        const confirmMessage = document.getElementById('confirmMessage');
        const confirmOkBtn   = document.getElementById('confirmOk');
        const confirmCancelBtn = document.getElementById('confirmCancel');

        let currentBasketId = null;
        // true nếu BẤT KỲ thẻ nào trong basket (1-2 thẻ) đã Set Out — khoá không cho sửa nữa,
        // khớp đúng logic chặn phía server (IsBasketOutAsync trong CompSttSetService).
        let basketIsOut = false;
        // Danh sách part của lần tải gần nhất — dùng cho nút "Nhận đủ tất cả".
        let currentDetailRows = [];

        // Khoá toàn trang trong lúc đang lưu — không chỉ khoá riêng nút vừa bấm. Lý do: nếu chỉ khoá
        // 1 nút, bấm thêm ô khác trong lúc ô đầu đang lưu vẫn được, và khi ô đầu lưu xong nó render lại
        // TOÀN BỘ bảng (renderDetail ghi đè innerHTML) — nút của ô thứ 2 sẽ bị "hồi sinh" (mất trạng thái
        // disabled) ngay giữa lúc yêu cầu lưu của nó vẫn còn đang chạy, dẫn tới bấm trùng/lưu 2 lần.
        let isSaving = false;

        function showSaveOverlay() {
            isSaving = true;
            saveLoadingOverlay.classList.add('show');
        }

        function hideSaveOverlay() {
            isSaving = false;
            saveLoadingOverlay.classList.remove('show');
        }

        function getAntiForgeryToken() {
            const el = document.querySelector('input[name="__RequestVerificationToken"]');
            return el ? el.value : '';
        }

        function showStatus(text, isError) {
            basketStatus.textContent = text;
            basketStatus.className = 'text-sm ms-2 ' + (isError ? 'text-danger font-weight-bold' : 'text-secondary');
        }

        // Popup xác nhận riêng của app — KHÔNG dùng confirm() gốc trình duyệt. Trả về Promise<boolean>
        // để dùng được kiểu "await showConfirm(...)" giống hệt cách gọi confirm() cũ.
        let confirmResolver = null;
        function showConfirm(message) {
            confirmMessage.textContent = message;
            confirmOverlay.classList.add('show');
            return new Promise(function (resolve) {
                confirmResolver = resolve;
            });
        }
        function closeConfirm(result) {
            confirmOverlay.classList.remove('show');
            if (confirmResolver) {
                confirmResolver(result);
                confirmResolver = null;
            }
        }
        confirmOkBtn.addEventListener('click', function () { closeConfirm(true); });
        confirmCancelBtn.addEventListener('click', function () { closeConfirm(false); });

        function renderHeader(rows) {
            if (!rows || rows.length === 0) {
                headerSection.style.display = 'none';
                headerCards.innerHTML = '';
                return;
            }

            headerCards.innerHTML = rows.map(row => `
                <div class="col-12 col-md-6 mb-2">
                    <div class="card border-radius-lg h-100">
                        <div class="card-body p-3">
                            <div class="d-flex align-items-center justify-content-between mb-2">
                                <strong class="text-dark text-md">${row.I_CARD_NO || ''}</strong>
                                <span class="badge bg-gradient-warning">${row.IS_IN === 'Y' ? 'ĐÃ VÀO' : 'CHƯA VÀO'}</span>
                            </div>
                            <div class="text-xs text-secondary">
                                PO: <strong class="text-dark">${row.I_PO_NO || ''}</strong> ·
                                Style: <strong class="text-dark">${row.C_STYLE || ''}</strong> ·
                                Size: <strong class="text-dark">${row.C_SIZE || ''}</strong>
                            </div>
                            <div class="text-xs text-secondary">
                                Kế hoạch/thẻ: <strong class="text-primary">${row.Q_PLAN || 0}</strong> ·
                                Số lượng thẻ: <strong class="text-primary">${row.C_QTY || 0}</strong> pcs
                            </div>
                            <div class="text-xs text-secondary">
                                Ngày vào: ${row.IN_DATE ? new Date(row.IN_DATE).toLocaleString('vi-VN') : ''}
                            </div>
                        </div>
                    </div>
                </div>
            `).join('');
            headerSection.style.display = 'block';
        }

        function renderDetail(rows) {
            currentDetailRows = rows || [];
            updateMarkAllDoneButton();

            if (!rows || rows.length === 0) {
                detailTable.style.display = 'none';
                detailEmpty.style.display = 'block';
                detailEmpty.innerHTML = `
                    🚫
                    <p class="mb-0">Basket này chưa có dữ liệu part.</p>`;
                return;
            }

            detailBody.innerHTML = rows.map(row => {
                const remain = (row.C_QTY || 0) - (row.QTY_RECEIVE || 0);
                const isDone = row.IS_DONE === 'Y';
                const qty = row.QTY_RECEIVE || 0;
                const hasValueClass = qty > 0 ? 'has-value' : '';
                const disabledAttr = basketIsOut ? 'disabled' : '';

                return `
                    <tr class="${isDone ? 'row-done' : ''}">
                        <td class="col-part-name">
                            <strong>${row.I_PARTS_NO || ''}</strong> — ${row.N_PARTS_NO || ''}
                        </td>
                        <td>${row.I_PO_NO || ''}</td>
                        <td>${row.C_STYLE || ''}</td>
                        <td>${row.C_SIZE || ''}</td>
                        <td>${row.C_QTY || 0}</td>
                        <td>
                            <button type="button" class="qty-btn ${hasValueClass}" ${disabledAttr}
                                data-parts="${row.I_PARTS_NO || ''}" data-qty="${qty}" data-cqty="${row.C_QTY || 0}">
                                ${qty > 0 ? qty : '-'}
                            </button>
                        </td>
                        <td>${remain}</td>
                        <td>${isDone ? '<span class="text-success font-weight-bold">Xong</span>' : '<span class="text-secondary">Chưa</span>'}</td>
                    </tr>`;
            }).join('');

            detailEmpty.style.display = 'none';
            detailTable.style.display = 'table';
        }

        // Nút "Nhận đủ tất cả": chỉ bật khi có ít nhất 1 part chưa nhận đủ VÀ basket chưa Out.
        function updateMarkAllDoneButton() {
            const hasRemaining = currentDetailRows.some(row => (row.C_QTY || 0) - (row.QTY_RECEIVE || 0) > 0);
            btnMarkAllDone.disabled = basketIsOut || !hasRemaining || isSaving;
        }

        // Nhận đủ TẤT CẢ part còn thiếu trong 1 lần bấm — đỡ phải bấm từng ô một khi công nhân
        // đã kiểm đủ hết hàng thật rồi. Gọi tuần tự (không song song) cho an toàn, xong mới tải lại.
        async function markAllDone() {
            const remainingParts = currentDetailRows.filter(row => (row.C_QTY || 0) - (row.QTY_RECEIVE || 0) > 0);
            if (remainingParts.length === 0) return;
            const ok = await showConfirm(`Xác nhận nhận đủ TẤT CẢ ${remainingParts.length} part còn thiếu trong basket này?`);
            if (!ok) return;

            btnMarkAllDone.disabled = true;
            showSaveOverlay();
            const errors = [];
            try {
                for (const row of remainingParts) {
                    const partsNo = row.I_PARTS_NO;
                    try {
                        const res = await fetch(`/Production2/MarkBasketDetailDone?basketId=${currentBasketId}&partsNo=${encodeURIComponent(partsNo)}`, {
                            method: 'POST',
                            headers: { 'RequestVerificationToken': getAntiForgeryToken() }
                        });
                        const data = await res.json();
                        if (!(res.ok && data.success)) {
                            errors.push(`${partsNo}: ${data.message || 'lỗi'}`);
                        }
                    } catch (err) {
                        console.error(err);
                        errors.push(`${partsNo}: lỗi kết nối`);
                    }
                }

                await findBasket(currentBasketId, true);

                if (errors.length === 0) {
                    showStatus(`✓ Đã nhận đủ tất cả ${remainingParts.length} part.`, false);
                    window.PdaHelper.feedback(true, 'Đã nhận đủ tất cả part.');
                } else {
                    const msg = `Xong ${remainingParts.length - errors.length}/${remainingParts.length} part — lỗi: ${errors.join('; ')}`;
                    showStatus(msg, true);
                    window.PdaHelper.feedback(false, msg);
                }
            } finally {
                hideSaveOverlay();
                updateMarkAllDoneButton();
            }
        }

        btnMarkAllDone.addEventListener('click', markAllDone);

        async function findBasket(basketIdOverride, silent) {
            let basketId = basketIdOverride;

            if (!basketId) {
                const rawId = basketIdInput.value.trim();
                const cardNo = cardNoInput.value.trim().toUpperCase();

                if (rawId) {
                    basketId = parseInt(rawId, 10);
                } else if (cardNo) {
                    showStatus('Đang tra basket theo PCard...', false);
                    try {
                        const res = await fetch('/Production2/GetBasketIdByCard?cardNo=' + encodeURIComponent(cardNo));
                        const data = await res.json();
                        if (res.ok && data.success) {
                            basketId = data.basketId;
                            basketIdInput.value = basketId;
                        } else {
                            showStatus(data.message || 'Không tìm thấy basket cho PCard này.', true);
                            return;
                        }
                    } catch (err) {
                        console.error(err);
                        showStatus('Lỗi kết nối máy chủ!', true);
                        return;
                    }
                } else {
                    showStatus('Nhập Basket ID hoặc mã PCard.', true);
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
                    basketIsOut = (headerData.data || []).some(h => h.IS_OUT === 'Y');
                    basketOutBanner.classList.toggle('show', basketIsOut);
                    renderHeader(headerData.data);
                }
                if (detailRes.ok && detailData.success) {
                    renderDetail(detailData.data);
                    if (!silent) showStatus(`Basket ${basketId} — ${detailData.data.length} part.`, false);
                } else {
                    showStatus(detailData.message || 'Không tải được dữ liệu.', true);
                }
            } catch (err) {
                console.error(err);
                showStatus('Lỗi kết nối máy chủ!', true);
            }
        }

        // Bấm ô Đã nhận: chưa có giá trị (0) -> nhận đủ; đã có giá trị -> mở bàn phím sửa.
        let numpadContext = null;

        detailBody.addEventListener('click', function (e) {
            if (isSaving) return; // trang đang khoá lúc lưu — bỏ qua mọi bấm khác

            const btn = e.target.closest('.qty-btn');
            if (!btn) return;

            const partsNo = btn.dataset.parts;
            const qty = parseInt(btn.dataset.qty, 10) || 0;
            const cQty = parseInt(btn.dataset.cqty, 10) || 0;

            if (btn.disabled) return;

            if (qty === 0) {
                markDone(partsNo, btn);
            } else {
                openNumpad(partsNo, qty, cQty);
            }
        });

        async function markDone(partsNo, btn) {
            // Khoá cả nút lẫn toàn trang ngay khi bấm — tránh bấm nhanh 2 lần khiến server cộng dồn
            // 2 lần trước khi lần đầu kịp lưu, và tránh bấm sang ô KHÁC trong lúc ô này đang lưu
            // (nếu không khoá cả trang, khi lưu xong findBasket() sẽ vẽ lại toàn bộ bảng và có thể
            // "hồi sinh" nút của ô khác giữa chừng khi nó cũng đang có yêu cầu lưu chạy dở).
            btn.disabled = true;
            showSaveOverlay();
            try {
                const res = await fetch(`/Production2/MarkBasketDetailDone?basketId=${currentBasketId}&partsNo=${encodeURIComponent(partsNo)}`, {
                    method: 'POST',
                    headers: { 'RequestVerificationToken': getAntiForgeryToken() }
                });
                const data = await res.json();
                if (res.ok && data.success) {
                    showStatus(`✓ Đã nhận đủ ${partsNo}.`, false);
                    window.PdaHelper.feedback(true, `Đã nhận đủ part ${partsNo}.`);
                    await findBasket(currentBasketId, true);
                } else {
                    const msg = data.message || 'Không thể cập nhật.';
                    showStatus(msg, true);
                    window.PdaHelper.feedback(false, msg);
                    btn.disabled = false;
                }
            } catch (err) {
                console.error(err);
                showStatus('Lỗi kết nối máy chủ!', true);
                window.PdaHelper.feedback(false, 'Lỗi kết nối máy chủ!');
                btn.disabled = false;
            } finally {
                hideSaveOverlay();
            }
        }

        function openNumpad(partsNo, currentQty, cQty) {
            numpadContext = { partsNo, cQty };
            numpadTitle.textContent = `${partsNo} (cần ${cQty})`;
            numpadDisplay.textContent = String(currentQty);
            numpadError.textContent = '';
            numpadOverlay.classList.add('show');
        }

        function closeNumpad() {
            numpadOverlay.classList.remove('show');
            numpadContext = null;
        }

        numpadOverlay.querySelectorAll('.numpad-grid button[data-digit]').forEach(btn => {
            btn.addEventListener('click', function () {
                const current = numpadDisplay.textContent === '0' ? '' : numpadDisplay.textContent;
                numpadDisplay.textContent = String(parseInt(current + btn.dataset.digit, 10));
                numpadError.textContent = '';
            });
        });

        numpadClear.addEventListener('click', function () {
            numpadDisplay.textContent = '0';
            numpadError.textContent = '';
        });

        numpadBackspace.addEventListener('click', function () {
            const next = numpadDisplay.textContent.slice(0, -1);
            numpadDisplay.textContent = next === '' ? '0' : String(parseInt(next, 10));
            numpadError.textContent = '';
        });

        numpadCancel.addEventListener('click', closeNumpad);

        numpadSave.addEventListener('click', async function () {
            if (!numpadContext || numpadSave.disabled || isSaving) return;
            const qty = parseInt(numpadDisplay.textContent, 10) || 0;
            const partsNo = numpadContext.partsNo; // lưu lại trước — closeNumpad() sẽ set numpadContext = null

            numpadSave.disabled = true;
            showSaveOverlay();
            try {
                const res = await fetch(`/Production2/UpdateBasketDetailQty?basketId=${currentBasketId}&partsNo=${encodeURIComponent(partsNo)}&qty=${qty}`, {
                    method: 'POST',
                    headers: { 'RequestVerificationToken': getAntiForgeryToken() }
                });
                const data = await res.json();
                if (res.ok && data.success) {
                    closeNumpad();
                    showStatus(`✓ Đã cập nhật ${partsNo} = ${qty}.`, false);
                    window.PdaHelper.feedback(true, `Đã cập nhật ${partsNo} = ${qty}.`);
                    await findBasket(currentBasketId, true);
                } else {
                    const msg = data.message || 'Không thể cập nhật.';
                    numpadError.textContent = msg;
                    window.PdaHelper.feedback(false, msg);
                }
            } catch (err) {
                console.error(err);
                numpadError.textContent = 'Lỗi kết nối máy chủ!';
                window.PdaHelper.feedback(false, 'Lỗi kết nối máy chủ!');
            } finally {
                numpadSave.disabled = false;
                hideSaveOverlay();
            }
        });

        btnFind.addEventListener('click', function () { findBasket(); });
        basketIdInput.addEventListener('keydown', function (e) { if (e.key === 'Enter') { e.preventDefault(); findBasket(); } });
        cardNoInput.addEventListener('keydown', function (e) { if (e.key === 'Enter') { e.preventDefault(); findBasket(); } });

        if (window.pending2Config && window.pending2Config.initialBasketId) {
            findBasket(window.pending2Config.initialBasketId);
        }
    });
})();
