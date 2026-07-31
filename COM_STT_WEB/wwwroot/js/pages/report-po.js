(function () {
    document.addEventListener('DOMContentLoaded', function () {
        const poInput      = document.getElementById('poInput');
        const btnFind       = document.getElementById('btnFindReport');
        const reportStatus  = document.getElementById('reportStatus');

        const pivotTable  = document.getElementById('pivotTable');
        const pivotHeader = document.getElementById('pivotHeaderRow');
        const pivotBody   = document.getElementById('pivotBody');
        const pivotEmpty  = document.getElementById('pivotEmpty');

        const summaryRow    = document.getElementById('summaryRow');
        const sumPoEl        = document.getElementById('sumPo');
        const sumPlanEl       = document.getElementById('sumPlan');
        const sumScanEl       = document.getElementById('sumScan');

        const ALL_SIZES = [];
        for (let i = 1; i <= 22; i++) {
            const n = String(i).padStart(2, '0');
            ALL_SIZES.push(n + 'M', n + 'T');
        }

        // Ngưỡng phân loại mức thiếu hụt: <=30% kế hoạch của size đó = thiếu ít, còn lại = thiếu nhiều
        const SHORTAGE_MINOR_THRESHOLD = 0.3;

        const ROW_LABEL = { 1: 'TỔNG KẾ HOẠCH', 2: 'TỔNG ĐÃ QUÉT', 3: 'ĐÃ NHẬN', 4: 'CÒN THIẾU' };
        const ROW_CLASS = { 1: 'row-plan', 2: 'row-scan', 3: 'row-received', 4: 'row-balance' };

        function fmt(n) {
            return (n || 0).toLocaleString('vi-VN');
        }

        function showStatus(text, isError) {
            reportStatus.textContent = text;
            reportStatus.className = 'text-sm ms-2 ' + (isError ? 'text-danger font-weight-bold' : 'text-secondary');
        }

        function buildSummary(rows, po) {
            const planRow = rows.find(r => r.RW === 1);
            const scanRow = rows.find(r => r.RW === 2);

            const sumSizes = (row) => ALL_SIZES.reduce((s, size) => s + (row?.SIZES?.[size] || 0), 0);

            sumPoEl.textContent = po;
            sumPlanEl.textContent = fmt(sumSizes(planRow));
            sumScanEl.textContent = fmt(sumSizes(scanRow));
            summaryRow.style.display = 'block';
        }

        function buildTable(rows) {
            const planRow = rows.find(r => r.RW === 1);
            const planBySize = {};
            ALL_SIZES.forEach(size => { planBySize[size] = planRow?.SIZES?.[size] || 0; });

            // Chỉ hiện cột size nào có TTL PLAN khác 0 — ẩn hết cột toàn 0
            const visibleSizes = ALL_SIZES.filter(size => planBySize[size] !== 0);

            let headerHtml = `
                <th class="meta-col col-part-no">Part No</th>
                <th class="meta-col col-part-name">Tên Part</th>
                <th class="meta-col col-line-type">Loại</th>
                <th class="total-col col-total">TỔNG</th>`;
            visibleSizes.forEach(size => { headerHtml += `<th>${size}</th>`; });
            pivotHeader.innerHTML = headerHtml;

            let bodyHtml = '';
            let lastPart = null;
            rows.forEach(row => {
                const rw = row.RW;
                const isPartRow = rw === 3 || rw === 4;
                const partKey = isPartRow ? row.I_PARTS_NO : ('__RW' + rw);
                const isNewGroup = partKey !== lastPart;
                lastPart = partKey;

                let total = 0;
                let sizeCellsHtml = '';
                visibleSizes.forEach(size => {
                    const val = row.SIZES?.[size] || 0;
                    total += val;

                    let cellClass = val === 0 ? 'size-col-zero' : '';
                    if (rw === 3) {
                        cellClass = val === 0 ? '' : 'nonzero';
                    } else if (rw === 4) {
                        if (val === 0) {
                            cellClass = 'zero';
                        } else {
                            const plan = planBySize[size] || 0;
                            const ratio = plan > 0 ? val / plan : 1;
                            cellClass = ratio <= SHORTAGE_MINOR_THRESHOLD ? 'short-minor' : 'short-major';
                        }
                    }
                    sizeCellsHtml += `<td class="${cellClass}">${val !== 0 ? val : '-'}</td>`;
                });

                const partNo = isPartRow ? (row.I_PARTS_NO || '') : '';
                const partName = isPartRow ? (row.N_PARTS_NO || '') : '';

                bodyHtml += `
                    <tr class="${ROW_CLASS[rw] || ''}" style="${isNewGroup && isPartRow ? 'border-top:3px solid #344767;' : ''}">
                        <td class="meta-col col-part-no">${isNewGroup ? partNo : ''}</td>
                        <td class="meta-col col-part-name" title="${partName}">${isNewGroup ? partName : ''}</td>
                        <td class="meta-col col-line-type">${ROW_LABEL[rw] || row.LINE_TYPE}</td>
                        <td class="total-col col-total ${rw === 4 ? (total === 0 ? 'zero' : 'nonzero') : ''}">${total}</td>
                        ${sizeCellsHtml}
                    </tr>`;
            });
            pivotBody.innerHTML = bodyHtml;
        }

        async function findReport() {
            const po = poInput.value.trim();
            if (!po) {
                showStatus('Vui lòng nhập số PO.', true);
                return;
            }

            showStatus('Đang tìm...', false);
            summaryRow.style.display = 'none';
            pivotTable.style.display = 'none';
            pivotEmpty.style.display = 'block';
            pivotEmpty.innerHTML = `
                <div class="spinner-border text-info" role="status"></div>
                <p class="mt-2 mb-0">Đang tải dữ liệu...</p>`;

            try {
                const res = await fetch('/Report/GetCompSttPoReport?po=' + encodeURIComponent(po));
                const data = await res.json();

                if (res.ok && data.success && data.data && data.data.length > 0) {
                    buildSummary(data.data, po);
                    buildTable(data.data);
                    pivotEmpty.style.display = 'none';
                    pivotTable.style.display = 'table';
                    showStatus(`PO ${po} — ${data.data.length} dòng.`, false);
                } else {
                    pivotTable.style.display = 'none';
                    pivotEmpty.style.display = 'block';
                    pivotEmpty.innerHTML = `
                        <span style="font-size:2.5rem;">🚫</span>
                        <p class="mb-0">Không có dữ liệu cho PO này.</p>`;
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
        poInput.addEventListener('keydown', function (e) {
            if (e.key === 'Enter') { e.preventDefault(); findReport(); }
        });
    });
})();
