(function () {
    document.addEventListener('DOMContentLoaded', function () {
        const ordNoInput   = document.getElementById('ordNoInput');
        const btnFind      = document.getElementById('btnFindOrder');
        const pivotStatus  = document.getElementById('pivotStatus');
        const pivotTable   = document.getElementById('pivotTable');
        const pivotHeader  = document.getElementById('pivotHeaderRow');
        const pivotBody    = document.getElementById('pivotBody');
        const pivotEmpty   = document.getElementById('pivotEmpty');

        // Toàn bộ 44 mã size theo đúng thứ tự 01M..22T (khớp view V_KEYIN_YIELD_SIZE_PIVOT)
        const ALL_SIZES = [];
        for (let i = 1; i <= 22; i++) {
            const n = String(i).padStart(2, '0');
            ALL_SIZES.push(n + 'M', n + 'T');
        }

        const ACTION_LABEL = { INPUT: 'INPUT', OUTPUT: 'OUTPUT', BALANCE: 'BALANCE' };
        const ACTION_CLASS = { INPUT: 'row-input', OUTPUT: 'row-output', BALANCE: 'row-balance' };

        function showStatus(text, isError) {
            pivotStatus.textContent = text;
            pivotStatus.className = 'text-sm ms-2 ' + (isError ? 'text-danger font-weight-bold' : 'text-secondary');
        }

        function buildTable(rows) {
            // Hiện đủ toàn bộ 44 size (01M..22T) — kể cả cột toàn số 0, để thấy rõ size nào chưa có dữ liệu
            const usedSizes = ALL_SIZES;

            // Header — Style/Width/Loại/TỔNG đều dính bên trái (width cố định để tính đúng vị trí "left")
            let headerHtml = `
                <th class="meta-col" style="left:0; width:90px; min-width:90px;">Style</th>
                <th class="meta-col" style="left:90px; width:60px; min-width:60px;">Width</th>
                <th class="meta-col" style="left:150px; width:90px; min-width:90px;">Loại</th>
                <th class="total-col" style="left:240px; width:80px; min-width:80px;">TỔNG</th>`;
            usedSizes.forEach(size => { headerHtml += `<th>${size}</th>`; });
            pivotHeader.innerHTML = headerHtml;

            // Body — nhóm 3 dòng INPUT/OUTPUT/BALANCE liền nhau cho từng Style/Width (API đã ORDER BY sẵn)
            let bodyHtml = '';
            let lastGroupKey = null;
            rows.forEach(row => {
                const groupKey = row.C_STYLE + '|' + row.C_WIDTH;
                const isNewGroup = groupKey !== lastGroupKey;
                lastGroupKey = groupKey;

                const rowClass = ACTION_CLASS[row.C_ACTION] || '';
                const actionClass = 'action-' + (row.C_ACTION || '').toLowerCase();
                let total = 0;

                let sizeCellsHtml = '';
                usedSizes.forEach(size => {
                    const val = row.SIZES?.[size] || 0;
                    total += val;
                    let cellClass = val === 0 ? 'size-col-zero' : '';
                    if (row.C_ACTION === 'BALANCE' && val !== 0) {
                        cellClass = val < 0 ? 'neg' : 'pos-zero';
                    }
                    sizeCellsHtml += `<td class="${cellClass}">${val !== 0 ? val : '-'}</td>`;
                });

                bodyHtml += `
                    <tr class="${rowClass}" style="${isNewGroup ? 'border-top:3px solid #344767;' : ''}">
                        <td class="meta-col" style="left:0;">${isNewGroup ? (row.C_STYLE || '') : ''}</td>
                        <td class="meta-col" style="left:90px;">${isNewGroup ? (row.C_WIDTH || '') : ''}</td>
                        <td class="meta-col ${actionClass}" style="left:150px;">${ACTION_LABEL[row.C_ACTION] || row.C_ACTION || ''}</td>
                        <td class="total-col ${row.C_ACTION === 'BALANCE' && total < 0 ? 'neg' : ''}" style="left:240px;">${total}</td>
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
                <div class="spinner-border text-danger" role="status"></div>
                <p class="mt-2 mb-0">Đang tải dữ liệu...</p>`;

            try {
                const res = await fetch('/Production2/GetSizePivot?ordNo=' + encodeURIComponent(ordNo));
                const data = await res.json();

                if (res.ok && data.success && data.data && data.data.length > 0) {
                    buildTable(data.data);
                    pivotEmpty.style.display = 'none';
                    pivotTable.style.display = 'table';
                    showStatus(`Tìm thấy ${data.data.length} dòng cho Order ${ordNo}.`, false);
                } else {
                    pivotTable.style.display = 'none';
                    pivotEmpty.style.display = 'block';
                    pivotEmpty.innerHTML = `
                        🚫
                        <p class="mb-0">Không tìm thấy dữ liệu cho Order ${ordNo}.</p>`;
                    showStatus(data.message || 'Không có dữ liệu.', true);
                }
            } catch (err) {
                console.error(err);
                pivotTable.style.display = 'none';
                pivotEmpty.style.display = 'block';
                pivotEmpty.innerHTML = `<p class="text-danger mb-0">Lỗi kết nối máy chủ!</p>`;
                showStatus('Lỗi kết nối máy chủ!', true);
            }
        }

        btnFind.addEventListener('click', findOrder);
        ordNoInput.addEventListener('keydown', function (e) {
            if (e.key === 'Enter') {
                e.preventDefault();
                findOrder();
            }
        });
        ordNoInput.focus();
    });
})();
