(function () {
    document.addEventListener('DOMContentLoaded', function () {
        const fromDateInput = document.getElementById('fromDateInput');
        const toDateInput   = document.getElementById('toDateInput');
        const poInput       = document.getElementById('poInput');
        const partInput     = document.getElementById('partInput');
        const btnFind       = document.getElementById('btnFindReport');
        const reportStatus  = document.getElementById('reportStatus');

        const pivotTable  = document.getElementById('pivotTable');
        const pivotHeader = document.getElementById('pivotHeaderRow');
        const pivotBody   = document.getElementById('pivotBody');
        const pivotEmpty  = document.getElementById('pivotEmpty');

        const ALL_SIZES = [];
        for (let i = 1; i <= 22; i++) {
            const n = String(i).padStart(2, '0');
            ALL_SIZES.push(n + 'M', n + 'T');
        }

        const LINE_LABEL = { 1: 'KẾ HOẠCH', 2: 'ĐÃ SET', 3: 'CÒN LẠI' };
        const ROW_CLASS = { 1: 'row-qplan', 2: 'row-setqty', 3: 'row-balance' };

        function showStatus(text, isError) {
            reportStatus.textContent = text;
            reportStatus.className = 'text-sm ms-2 ' + (isError ? 'text-danger font-weight-bold' : 'text-secondary');
        }

        function toApiDate(inputDateValue) {
            return (inputDateValue || '').replace(/-/g, '');
        }

        function buildTable(rows) {
            let headerHtml = `
                <th class="meta-col col-po">PO</th>
                <th class="meta-col col-style">Style</th>
                <th class="meta-col col-part-no">Part No</th>
                <th class="meta-col col-part-name">Tên Part</th>
                <th class="meta-col col-line-type">Loại</th>
                <th class="total-col col-total">TỔNG</th>`;
            ALL_SIZES.forEach(size => { headerHtml += `<th>${size}</th>`; });
            pivotHeader.innerHTML = headerHtml;

            let bodyHtml = '';
            let lastKey = null;
            rows.forEach(row => {
                const key = row.I_PO_NO + '|' + row.I_PARTS_NO;
                const isNewGroup = key !== lastKey;
                lastKey = key;

                const lineNo = row.LINE_NO;
                let total = 0;

                let sizeCellsHtml = '';
                ALL_SIZES.forEach(size => {
                    const val = row.SIZES?.[size] || 0;
                    total += val;
                    let cellClass = val === 0 ? 'size-col-zero' : '';
                    if (lineNo === 3) {
                        cellClass = val === 0 ? 'zero' : 'nonzero';
                    }
                    sizeCellsHtml += `<td class="${cellClass}">${val !== 0 ? val : '-'}</td>`;
                });

                bodyHtml += `
                    <tr class="${ROW_CLASS[lineNo] || ''}" style="${isNewGroup ? 'border-top:3px solid #344767;' : ''}">
                        <td class="meta-col col-po">${isNewGroup && row.I_PO_NO ? `<a href="/Report/ByPo?po=${encodeURIComponent(row.I_PO_NO)}" class="po-link">${row.I_PO_NO}</a>` : ''}</td>
                        <td class="meta-col col-style">${isNewGroup ? (row.C_STYLE || '') : ''}</td>
                        <td class="meta-col col-part-no">${isNewGroup ? (row.I_PARTS_NO || '') : ''}</td>
                        <td class="meta-col col-part-name" title="${row.N_PARTS_NO || ''}">${isNewGroup ? (row.N_PARTS_NO || '') : ''}</td>
                        <td class="meta-col col-line-type">${LINE_LABEL[lineNo] || row.LINE_TYPE}</td>
                        <td class="total-col col-total ${lineNo === 3 ? (total === 0 ? 'zero' : 'nonzero') : ''}">${total}</td>
                        ${sizeCellsHtml}
                    </tr>`;
            });
            pivotBody.innerHTML = bodyHtml;
        }

        async function findReport() {
            const from = toApiDate(fromDateInput.value);
            const to = toApiDate(toDateInput.value);
            if (!from || !to) {
                showStatus('Vui lòng chọn Từ ngày và Đến ngày.', true);
                return;
            }

            showStatus('Đang tìm...', false);
            pivotTable.style.display = 'none';
            pivotEmpty.style.display = 'block';
            pivotEmpty.innerHTML = `
                <div class="spinner-border text-info" role="status"></div>
                <p class="mt-2 mb-0">Đang tải dữ liệu...</p>`;

            try {
                const params = new URLSearchParams({ fromDate: from, toDate: to });
                if (poInput.value.trim()) params.set('po', poInput.value.trim());
                if (partInput.value.trim()) params.set('part', partInput.value.trim());

                const res = await fetch('/Report/GetCompSttSetReport?' + params.toString());
                const data = await res.json();

                if (res.ok && data.success && data.data && data.data.length > 0) {
                    buildTable(data.data);
                    pivotEmpty.style.display = 'none';
                    pivotTable.style.display = 'table';
                    showStatus(`${data.data.length / 3} PO/Part — ${data.data.length} dòng.`, false);
                } else {
                    pivotTable.style.display = 'none';
                    pivotEmpty.style.display = 'block';
                    pivotEmpty.innerHTML = `
                        🚫
                        <p class="mb-0">Không có dữ liệu khớp bộ lọc.</p>`;
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

        btnFind.addEventListener('click', findReport);
        [poInput, partInput].forEach(el => {
            el.addEventListener('keydown', function (e) {
                if (e.key === 'Enter') { e.preventDefault(); findReport(); }
            });
        });

        // Mặc định: hôm nay
        (function initDefaultDates() {
            const now = new Date();
            const yyyy = now.getFullYear();
            const mm = String(now.getMonth() + 1).padStart(2, '0');
            const dd = String(now.getDate()).padStart(2, '0');
            const today = `${yyyy}-${mm}-${dd}`;
            fromDateInput.value = today;
            toDateInput.value = today;
        })();
    });
})();
