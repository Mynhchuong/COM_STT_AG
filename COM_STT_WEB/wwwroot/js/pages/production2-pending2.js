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
        const numpadFull    = document.getElementById('numpadFull');
        const numpadFullQty = document.getElementById('numpadFullQty');

        const saveLoadingOverlay = document.getElementById('saveLoadingOverlay');
        const basketOutBanner = document.getElementById('basketOutBanner');
        const basketProcessOutBanner = document.getElementById('basketProcessOutBanner');
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

            headerCards.innerHTML = rows.map(row => {
                // Trạng thái Out ở mức cả thẻ — để công nhân quét lại 1 cái biết ngay thẻ này
                // đang ở đâu: chưa Out / đang Out dở (1 phần part đã Out) / đã Out xong hết.
                let outBadge = '';
                if (row.IS_OUT === 'Y') {
                    outBadge = '<span class="badge bg-gradient-success">✅ ĐÃ OUT XONG</span>';
                } else if (row.PROCESS_OUT === 'Y') {
                    outBadge = '<span class="badge bg-gradient-warning">🔶 ĐANG OUT DỞ</span>';
                }

                return `
                <div class="col-12 col-md-6 mb-2">
                    <div class="card header-info-card border-radius-lg h-100">
                        <div class="card-body p-3">
                            <div class="d-flex align-items-center justify-content-between mb-2 flex-wrap gap-1">
                                <strong class="text-dark text-md">${row.I_CARD_NO || ''}</strong>
                                <div class="d-flex align-items-center gap-1">
                                    <span class="badge bg-gradient-warning">${row.IS_IN === 'Y' ? 'ĐÃ VÀO' : 'CHƯA VÀO'}</span>
                                    ${outBadge}
                                </div>
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
                </div>`;
            }).join('');
            headerSection.style.display = 'block';
        }

        // Đồng bộ đúng định dạng OUT_TO với production2-setout.js: "A"/"C" (chỉ tên nhà) hoặc
        // "B-5-38"/"E-9-07" (nhà B/E kèm Line — ghép chữ nhà phía trước để không lẫn Line trùng
        // số giữa 2 nhà).
        function formatOutTo(outTo) {
            if (!outTo) return '';
            if (/^[ABCE]$/.test(outTo)) return `Nhà ${outTo}`;
            const nha = outTo.charAt(0);
            const line = outTo.slice(2);
            return `Nhà ${nha} — Line ${line}`;
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

            // Part chưa nhận/đang nhận dở hiện lên trước, part đã nhận đủ đẩy xuống dưới —
            // để công nhân thấy ngay việc còn phải làm, đỡ phải dò cả danh sách.
            const sortedRows = rows.slice().sort((a, b) => {
                const aDone = a.IS_DONE === 'Y' ? 1 : 0;
                const bDone = b.IS_DONE === 'Y' ? 1 : 0;
                return aDone - bDone;
            });

            detailBody.innerHTML = sortedRows.map(row => {
                const remain = (row.C_QTY || 0) - (row.QTY_RECEIVE || 0);
                const isDone = row.IS_DONE === 'Y';
                const qty = row.QTY_RECEIVE || 0;
                // 1 part đã Out (IS_OUT='Y') thì khoá riêng part đó luôn, kể cả khi các part khác
                // trong cùng basket vẫn đang được nhận hàng dở (Out theo từng nhà, không phải cả
                // basket cùng lúc) — khớp đúng luật chặn phía server (IsPartLockedAsync).
                const partOut = row.IS_OUT === 'Y';
                // Đã nhận ĐỦ (xanh) khác màu với đang nhận DỞ (cam) — nhìn màu biết ngay,
                // không lẫn "có số" với "xong" như trước.
                const rowStateClass = partOut ? 'row-out-locked' : (isDone ? 'row-done' : (qty > 0 ? 'row-partial' : ''));
                const btnStateClass = isDone ? 'has-value' : (qty > 0 ? 'partial' : '');
                const disabledAttr = (basketIsOut || partOut) ? 'disabled' : '';
                const statusHtml = partOut
                    ? `<span class="font-weight-bold" style="color:#1a73e8;">🚚 Đã Out${row.OUT_TO ? ' → ' + formatOutTo(row.OUT_TO) : ''}</span>`
                    : (isDone
                        ? '<span class="text-success font-weight-bold">✅ Đã đủ</span>'
                        : (qty > 0
                            ? '<span class="font-weight-bold" style="color:#fb6340;">🔶 Còn thiếu</span>'
                            : '<span class="text-secondary">⬜ Chưa nhận</span>'));

                return `
                    <tr class="${rowStateClass}">
                        <td class="col-part-name">
                            <strong>${row.I_PARTS_NO || ''}</strong> — ${row.N_PARTS_NO || ''}
                        </td>
                        <td class="col-meta" data-label="PO / Style / Size">${row.I_PO_NO || ''} · ${row.C_STYLE || ''} · Size ${row.C_SIZE || ''}</td>
                        <td data-label="Cần">${row.C_QTY || 0}</td>
                        <td class="col-qty" data-label="Đã nhận">
                            <button type="button" class="qty-btn ${btnStateClass}" ${disabledAttr}
                                data-parts="${row.I_PARTS_NO || ''}" data-qty="${qty}" data-cqty="${row.C_QTY || 0}">
                                ${qty > 0 ? qty : '-'}
                            </button>
                        </td>
                        <td data-label="Còn lại">${remain}</td>
                        <td data-label="Trạng thái">${statusHtml}</td>
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
                    const basketProcessOut = (headerData.data || []).some(h => h.PROCESS_OUT === 'Y');
                    basketOutBanner.classList.toggle('show', basketIsOut);
                    basketProcessOutBanner.classList.toggle('show', !basketIsOut && basketProcessOut);
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

        // Bấm ô Đã nhận: LUÔN mở bàn phím (dù đang là 0 hay đã có số) — tránh tự động đánh dấu
        // "nhận đủ" ngay khi vừa bấm 1 cái, vì nếu công nhân chỉ nhận được 1 phần rồi bị gọi đi
        // việc khác, hệ thống sẽ ghi NHẦM là đã nhận đủ mà không ai biết. Trong bàn phím có sẵn
        // nút "Nhận đủ" để bấm nhanh cho trường hợp phổ biến (nhận đúng đủ số cần).
        let numpadContext = null;

        detailBody.addEventListener('click', function (e) {
            if (isSaving) return; // trang đang khoá lúc lưu — bỏ qua mọi bấm khác

            const btn = e.target.closest('.qty-btn');
            if (!btn) return;
            if (btn.disabled) return;

            const partsNo = btn.dataset.parts;
            const qty = parseInt(btn.dataset.qty, 10) || 0;
            const cQty = parseInt(btn.dataset.cqty, 10) || 0;

            openNumpad(partsNo, qty, cQty);
        });

        function openNumpad(partsNo, currentQty, cQty) {
            numpadContext = { partsNo, cQty };
            numpadTitle.textContent = `${partsNo} (cần ${cQty})`;
            numpadDisplay.textContent = String(currentQty);
            numpadFullQty.textContent = String(cQty);
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
                let next = parseInt(current + btn.dataset.digit, 10);
                // Không cho nhập vượt quá số cần — kẹp lại ngay tại chỗ thay vì để công nhân bấm
                // Lưu rồi mới thấy lỗi (dễ bỏ qua thông báo lỗi nếu ít đọc chữ).
                if (numpadContext && next > numpadContext.cQty) {
                    next = numpadContext.cQty;
                    numpadError.textContent = `Không vượt quá ${numpadContext.cQty}.`;
                } else {
                    numpadError.textContent = '';
                }
                numpadDisplay.textContent = String(next);
            });
        });

        numpadFull.addEventListener('click', function () {
            if (!numpadContext) return;
            numpadDisplay.textContent = String(numpadContext.cQty);
            numpadError.textContent = '';
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
