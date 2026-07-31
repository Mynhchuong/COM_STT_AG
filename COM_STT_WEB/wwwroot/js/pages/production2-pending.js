(function () {
    document.addEventListener('DOMContentLoaded', function () {
        const ordNoInput  = document.getElementById('ordNoInput');
        const btnFind     = document.getElementById('btnFindOrder');
        const btnComplete = document.getElementById('btnCompleteOrder');
        const pivotStatus = document.getElementById('pivotStatus');
        const pivotTable  = document.getElementById('pivotTable');
        const pivotHeader = document.getElementById('pivotHeaderRow');
        const pivotBody   = document.getElementById('pivotBody');
        const pivotEmpty  = document.getElementById('pivotEmpty');

        const numpadOverlay = document.getElementById('numpadOverlay');
        const numpadTitle   = document.getElementById('numpadTitle');
        const numpadPlan    = document.getElementById('numpadPlan');
        const numpadDisplay = document.getElementById('numpadDisplay');
        const numpadError   = document.getElementById('numpadError');
        const numpadSave    = document.getElementById('numpadSave');
        const numpadCancel  = document.getElementById('numpadCancel');
        const numpadClear   = document.getElementById('numpadClear');
        const numpadBackspace = document.getElementById('numpadBackspace');

        function getAntiForgeryToken() {
            const el = document.querySelector('input[name="__RequestVerificationToken"]');
            return el ? el.value : '';
        }

        const ALL_SIZES = [];
        for (let i = 1; i <= 22; i++) {
            const n = String(i).padStart(2, '0');
            ALL_SIZES.push(n + 'M', n + 'T');
        }

        const ROW_LABEL = { '1': 'KẾ HOẠCH', '2': 'ĐÃ QUÉT', '3': 'CÒN LẠI' };
        const ROW_CLASS = { '1': 'row-total', '2': 'row-done', '3': 'row-remain' };
        const ROW_META_CLASS = { '1': 'rowtype-total', '2': 'rowtype-done', '3': 'rowtype-remain' };

        // Kế hoạch (Q_PLAN) theo từng size — lấy từ dòng KẾ HOẠCH (ROW_TYPE=1), dùng để giới hạn
        // số lượng nhập tay ở ô ĐÃ QUÉT không được vượt quá.
        let planBySize = {};

        // Order đã bấm Hoàn tất rồi (IS_COMPLETE='Y' trong DB) → khoá không cho bấm/sửa ô ĐÃ QUÉT nữa.
        let isOrderLocked = false;

        function showStatus(text, isError) {
            pivotStatus.textContent = text;
            pivotStatus.className = 'text-sm ms-2 ' + (isError ? 'text-danger font-weight-bold' : 'text-secondary');
        }

        function buildTable(rows) {
            let headerHtml = `
                <th class="meta-col col-part-no">Part No</th>
                <th class="meta-col col-part-name">Tên Part</th>
                <th class="meta-col col-row-status">Trạng thái</th>
                <th class="total-col col-total">TỔNG</th>`;
            ALL_SIZES.forEach(size => { headerHtml += `<th>${size}</th>`; });
            pivotHeader.innerHTML = headerHtml;

            planBySize = {};
            rows.filter(r => r.ROW_TYPE === '1').forEach(r => {
                ALL_SIZES.forEach(size => { planBySize[size] = r.SIZES?.[size] || 0; });
            });

            let bodyHtml = '';
            let lastPart = null;
            rows.forEach(row => {
                const isNewPart = row.I_PARTS_NO !== lastPart;
                lastPart = row.I_PARTS_NO;

                const rowType = row.ROW_TYPE;
                let total = 0;

                let sizeCellsHtml = '';
                ALL_SIZES.forEach(size => {
                    const val = row.SIZES?.[size] || 0;
                    total += val;
                    const plan = planBySize[size] || 0;

                    if (rowType === '2') {
                        // Ô ĐÃ QUÉT: bấm để nhập tay — chưa có giá trị (0) thì bấm 1 lần là nhận đủ
                        // theo kế hoạch; đã có giá trị rồi thì bấm lại để mở bàn phím sửa số lượng.
                        // Order đã Hoàn tất rồi thì khoá hết, không cho bấm/sửa nữa.
                        const disabled = (plan === 0 || isOrderLocked) ? 'disabled' : '';
                        const hasValueClass = val > 0 ? 'has-value' : '';
                        sizeCellsHtml += `<td class="qty-cell">
                            <button type="button" class="qty-btn ${hasValueClass}" ${disabled}
                                data-part="${row.I_PARTS_NO || ''}" data-size="${size}"
                                data-qty="${val}" data-plan="${plan}">${val > 0 ? val : '-'}</button>
                        </td>`;
                        return;
                    }

                    let cellClass = val === 0 ? 'size-col-zero' : '';
                    if (rowType === '3') {
                        cellClass = val === 0 ? 'zero' : 'nonzero';
                    }
                    sizeCellsHtml += `<td class="${cellClass}">${val !== 0 ? val : '-'}</td>`;
                });

                const partName = row.N_PARTS_NO || '';
                bodyHtml += `
                    <tr class="${ROW_CLASS[rowType] || ''}" style="${isNewPart ? 'border-top:3px solid #344767;' : ''}">
                        <td class="meta-col col-part-no">${isNewPart ? (row.I_PARTS_NO || '') : ''}</td>
                        <td class="meta-col col-part-name" title="${partName}">${isNewPart ? partName : ''}</td>
                        <td class="meta-col col-row-status ${ROW_META_CLASS[rowType] || ''}">${ROW_LABEL[rowType] || rowType}</td>
                        <td class="total-col col-total ${rowType === '3' ? (total === 0 ? 'zero' : 'nonzero') : ''}">${total}</td>
                        ${sizeCellsHtml}
                    </tr>`;
            });
            pivotBody.innerHTML = bodyHtml;
        }

        // ĐÃ QUÉT = KẾ HOẠCH và CÒN LẠI = 0 ở MỌI size/part (chỉ tính size có kế hoạch > 0).
        function isAllRemainZero(rows) {
            return rows
                .filter(r => r.ROW_TYPE === '3')
                .every(r => ALL_SIZES.every(size => (planBySize[size] || 0) === 0 || (r.SIZES?.[size] || 0) === 0));
        }

        // Chỉ hiện nút Hoàn tất khi đủ điều kiện (đã quét hết); nếu đã hoàn tất rồi thì hiện
        // trạng thái tĩnh, không cho bấm lại; chưa đủ điều kiện thì ẩn hẳn nút đi.
        function updateCompleteButtonState(isLocked, allDone) {
            if (isLocked) {
                btnComplete.classList.remove('d-none');
                btnComplete.disabled = true;
                btnComplete.innerHTML = '<i class="material-symbols-rounded">verified</i>Đã hoàn tất';
            } else if (allDone) {
                btnComplete.classList.remove('d-none');
                btnComplete.disabled = false;
                btnComplete.innerHTML = '<i class="material-symbols-rounded">check_circle</i>Hoàn tất';
            } else {
                btnComplete.classList.add('d-none');
            }
        }

        // ============================================================
        // Bấm ô ĐÃ QUÉT: chưa có giá trị (0) → nhận đủ theo kế hoạch ngay.
        // Đã có giá trị rồi → mở bàn phím số để sửa số lượng thực nhận.
        // ============================================================
        let numpadContext = null; // { part, size, plan }

        pivotBody.addEventListener('click', function (e) {
            const btn = e.target.closest('.qty-btn');
            if (!btn || btn.disabled) return;

            const part = btn.dataset.part;
            const size = btn.dataset.size;
            const qty = parseInt(btn.dataset.qty, 10) || 0;
            const plan = parseInt(btn.dataset.plan, 10) || 0;

            if (qty === 0) {
                markPartDone(part, size, btn);
            } else {
                openNumpad(part, size, qty, plan);
            }
        });

        async function markPartDone(part, size, btn) {
            const ordNo = ordNoInput.value.trim();
            btn.disabled = true;
            try {
                const res = await fetch(`/Production2/MarkPartDone?ordNo=${encodeURIComponent(ordNo)}&size=${encodeURIComponent(size)}&partsNo=${encodeURIComponent(part)}`, {
                    method: 'POST',
                    headers: { 'RequestVerificationToken': getAntiForgeryToken() }
                });
                const data = await res.json();
                if (res.ok && data.success) {
                    showStatus(`✓ Đã nhận đủ ${part} / ${size}.`, false);
                    await findOrder(true);
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

        function openNumpad(part, size, currentQty, plan) {
            numpadContext = { part, size, plan };
            numpadTitle.textContent = `${part} — Size ${size}`;
            numpadPlan.textContent = `Kế hoạch: ${plan}`;
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
                const next = current + btn.dataset.digit;
                numpadDisplay.textContent = String(parseInt(next, 10));
                numpadError.textContent = '';
            });
        });

        numpadClear.addEventListener('click', function () {
            numpadDisplay.textContent = '0';
            numpadError.textContent = '';
        });

        numpadBackspace.addEventListener('click', function () {
            const current = numpadDisplay.textContent;
            const next = current.slice(0, -1);
            numpadDisplay.textContent = next === '' ? '0' : String(parseInt(next, 10));
            numpadError.textContent = '';
        });

        numpadCancel.addEventListener('click', closeNumpad);

        numpadSave.addEventListener('click', async function () {
            if (!numpadContext) return;
            const qty = parseInt(numpadDisplay.textContent, 10) || 0;
            if (qty > numpadContext.plan) {
                numpadError.textContent = `Không được vượt kế hoạch (${numpadContext.plan})`;
                return;
            }

            const ordNo = ordNoInput.value.trim();
            const { part, size } = numpadContext;
            numpadSave.disabled = true;
            try {
                const res = await fetch(`/Production2/UpdatePartQty?ordNo=${encodeURIComponent(ordNo)}&size=${encodeURIComponent(size)}&partsNo=${encodeURIComponent(part)}&qty=${qty}`, {
                    method: 'POST',
                    headers: { 'RequestVerificationToken': getAntiForgeryToken() }
                });
                const data = await res.json();
                if (res.ok && data.success) {
                    closeNumpad();
                    showStatus(`✓ Đã cập nhật ${part} / ${size} = ${qty}.`, false);
                    await findOrder(true);
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

        async function findOrder(silent) {
            const ordNo = ordNoInput.value.trim();
            if (!ordNo) {
                showStatus('Vui lòng nhập số Order.', true);
                return;
            }

            // silent = true: chỉ vừa cập nhật 1 ô ĐÃ QUÉT xong, tải lại dữ liệu để đồng bộ số liệu
            // nhưng KHÔNG hiện spinner/reset bảng — tránh nhấp nháy khó chịu mỗi lần bấm 1 ô.
            if (!silent) {
                showStatus('Đang tìm...', false);
                pivotTable.style.display = 'none';
                pivotEmpty.style.display = 'block';
                pivotEmpty.innerHTML = `
                    <div class="spinner-border text-warning" role="status"></div>
                    <p class="mt-2 mb-0">Đang tải dữ liệu...</p>`;
            }

            try {
                const [res, completeRes] = await Promise.all([
                    fetch('/Production2/GetPartYieldStatus?ordNo=' + encodeURIComponent(ordNo)),
                    fetch('/Production2/GetOrderCompleteStatus?ordNo=' + encodeURIComponent(ordNo))
                ]);
                const data = await res.json();
                const completeData = await completeRes.json();
                isOrderLocked = completeRes.ok && completeData.success && completeData.isComplete === true;

                if (res.ok && data.success && data.data && data.data.length > 0) {
                    buildTable(data.data);
                    pivotEmpty.style.display = 'none';
                    pivotTable.style.display = 'table';
                    if (!silent) {
                        showStatus(`Order ${ordNo} — ${data.data.length} dòng.`, false);
                    }
                    updateCompleteButtonState(isOrderLocked, isAllRemainZero(data.data));
                } else {
                    pivotTable.style.display = 'none';
                    pivotEmpty.style.display = 'block';
                    pivotEmpty.innerHTML = `
                        <i class="material-symbols-rounded text-secondary text-4xl mb-2">search_off</i>
                        <p class="mb-0">Chưa có dữ liệu Set In cho Order ${ordNo}.</p>`;
                    showStatus(data.message || 'Không có dữ liệu.', true);
                    btnComplete.classList.add('d-none');
                }
            } catch (err) {
                console.error(err);
                if (!silent) {
                    pivotTable.style.display = 'none';
                    pivotEmpty.style.display = 'block';
                    pivotEmpty.innerHTML = `<p class="text-danger mb-0">Lỗi kết nối máy chủ!</p>`;
                }
                showStatus('Lỗi kết nối máy chủ!', true);
                btnComplete.classList.add('d-none');
            }
        }

        async function completeOrder() {
            const ordNo = ordNoInput.value.trim();
            if (!ordNo) return;
            if (!confirm(`Xác nhận đánh dấu HOÀN TẤT Set In cho Order ${ordNo}?`)) return;

            btnComplete.disabled = true;
            try {
                const res = await fetch('/Production2/CompleteOrder?ordNo=' + encodeURIComponent(ordNo), {
                    method: 'POST',
                    headers: { 'RequestVerificationToken': getAntiForgeryToken() }
                });
                const data = await res.json();

                if (res.ok && data.success) {
                    showStatus(`✓ Đã đánh dấu hoàn tất Order ${ordNo}.`, false);
                    isOrderLocked = true;
                    await findOrder(true);
                } else {
                    showStatus(data.message || 'Không thể đánh dấu hoàn tất.', true);
                    btnComplete.disabled = false;
                }
            } catch (err) {
                console.error(err);
                showStatus('Lỗi kết nối máy chủ!', true);
                btnComplete.disabled = false;
            }
        }

        btnComplete.addEventListener('click', completeOrder);

        btnFind.addEventListener('click', findOrder);
        ordNoInput.addEventListener('keydown', function (e) {
            if (e.key === 'Enter') {
                e.preventDefault();
                findOrder();
            }
        });

        // Nếu đến từ trang Set In (sau khi lưu xong) đã có sẵn Order — tự tìm luôn
        if (window.pendingConfig && window.pendingConfig.initialOrdNo) {
            findOrder();
        } else {
            ordNoInput.focus();
        }
    });
})();
