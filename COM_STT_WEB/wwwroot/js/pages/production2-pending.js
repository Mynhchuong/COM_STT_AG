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

        async function findOrder() {
            const ordNo = ordNoInput.value.trim();
            if (!ordNo) {
                showStatus('Vui lòng nhập số Order.', true);
                return;
            }

            showStatus('Đang tìm...', false);
            pivotTable.style.display = 'none';
            pivotEmpty.style.display = 'block';
            pivotEmpty.innerHTML = `
                <div class="spinner-border text-warning" role="status"></div>
                <p class="mt-2 mb-0">Đang tải dữ liệu...</p>`;

            try {
                const res = await fetch('/Production2/GetPartYieldStatus?ordNo=' + encodeURIComponent(ordNo));
                const data = await res.json();

                if (res.ok && data.success && data.data && data.data.length > 0) {
                    buildTable(data.data);
                    pivotEmpty.style.display = 'none';
                    pivotTable.style.display = 'table';
                    showStatus(`Order ${ordNo} — ${data.data.length} dòng.`, false);
                    btnComplete.disabled = false;
                } else {
                    pivotTable.style.display = 'none';
                    pivotEmpty.style.display = 'block';
                    pivotEmpty.innerHTML = `
                        <i class="material-symbols-rounded text-secondary text-4xl mb-2">search_off</i>
                        <p class="mb-0">Chưa có dữ liệu Set In cho Order ${ordNo}.</p>`;
                    showStatus(data.message || 'Không có dữ liệu.', true);
                    btnComplete.disabled = true;
                }
            } catch (err) {
                console.error(err);
                pivotTable.style.display = 'none';
                pivotEmpty.style.display = 'block';
                pivotEmpty.innerHTML = `<p class="text-danger mb-0">Lỗi kết nối máy chủ!</p>`;
                showStatus('Lỗi kết nối máy chủ!', true);
                btnComplete.disabled = true;
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
