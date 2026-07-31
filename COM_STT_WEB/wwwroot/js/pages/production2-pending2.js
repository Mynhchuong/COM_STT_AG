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

        let currentBasketId = null;

        function getAntiForgeryToken() {
            const el = document.querySelector('input[name="__RequestVerificationToken"]');
            return el ? el.value : '';
        }

        function showStatus(text, isError) {
            basketStatus.textContent = text;
            basketStatus.className = 'text-sm ms-2 ' + (isError ? 'text-danger font-weight-bold' : 'text-secondary');
        }

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
                            <button type="button" class="qty-btn ${hasValueClass}"
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
            // Khoá nút ngay khi bấm — tránh bấm nhanh 2 lần khiến server cộng dồn 2 lần trước khi
            // lần đầu kịp lưu (VD: đủ 24 -> bấm nhanh 2 cái -> cộng thành 48, sai số liệu thật).
            btn.disabled = true;
            try {
                const res = await fetch(`/Production2/MarkBasketDetailDone?basketId=${currentBasketId}&partsNo=${encodeURIComponent(partsNo)}`, {
                    method: 'POST',
                    headers: { 'RequestVerificationToken': getAntiForgeryToken() }
                });
                const data = await res.json();
                if (res.ok && data.success) {
                    showStatus(`✓ Đã nhận đủ ${partsNo}.`, false);
                    await findBasket(currentBasketId, true);
                } else {
                    showStatus(data.message || 'Không thể cập nhật.', true);
                    btn.disabled = false;
                }
            } catch (err) {
                console.error(err);
                showStatus('Lỗi kết nối máy chủ!', true);
                btn.disabled = false;
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
            if (!numpadContext || numpadSave.disabled) return;
            const qty = parseInt(numpadDisplay.textContent, 10) || 0;
            const partsNo = numpadContext.partsNo; // lưu lại trước — closeNumpad() sẽ set numpadContext = null

            numpadSave.disabled = true;
            try {
                const res = await fetch(`/Production2/UpdateBasketDetailQty?basketId=${currentBasketId}&partsNo=${encodeURIComponent(partsNo)}&qty=${qty}`, {
                    method: 'POST',
                    headers: { 'RequestVerificationToken': getAntiForgeryToken() }
                });
                const data = await res.json();
                if (res.ok && data.success) {
                    closeNumpad();
                    showStatus(`✓ Đã cập nhật ${partsNo} = ${qty}.`, false);
                    await findBasket(currentBasketId, true);
                } else {
                    numpadError.textContent = data.message || 'Không thể cập nhật.';
                }
            } catch (err) {
                console.error(err);
                numpadError.textContent = 'Lỗi kết nối máy chủ!';
            } finally {
                numpadSave.disabled = false;
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
