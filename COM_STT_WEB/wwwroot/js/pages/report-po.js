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

        const cardGroupsRow = document.getElementById('cardGroupsRow');
        const cntOut          = document.getElementById('cntOut');
        const cntPending       = document.getElementById('cntPending');
        const cntNotStarted    = document.getElementById('cntNotStarted');

        const CARDS_PAGE_SIZE = 10;

        // Mỗi cột (Đã Out / Đang chờ nhận / Chưa nhận hàng) tự quản lý trang riêng — PO nhiều
        // thẻ vẫn xem được gọn, không phải cuộn dài trong 1 khung nhỏ.
        function createCardPager(listElId, pagerElId, countElId) {
            const listEl = document.getElementById(listElId);
            const pagerEl = document.getElementById(pagerElId);
            const countEl = document.getElementById(countElId);
            const prevBtn = pagerEl.querySelector('.pager-prev');
            const nextBtn = pagerEl.querySelector('.pager-next');
            const labelEl = pagerEl.querySelector('.pager-label');

            let items = [];
            let page = 1;

            function render() {
                countEl.textContent = items.length;
                if (items.length === 0) {
                    listEl.innerHTML = '<div class="card-group-empty">Chưa có thẻ nào</div>';
                    pagerEl.style.display = 'none';
                    return;
                }

                const totalPages = Math.max(1, Math.ceil(items.length / CARDS_PAGE_SIZE));
                if (page > totalPages) page = totalPages;
                const start = (page - 1) * CARDS_PAGE_SIZE;
                const pageItems = items.slice(start, start + CARDS_PAGE_SIZE);

                listEl.innerHTML = pageItems.map(cardItemHtml).join('');
                pagerEl.style.display = totalPages > 1 ? 'flex' : 'none';
                labelEl.textContent = `Trang ${page}/${totalPages}`;
                prevBtn.disabled = page <= 1;
                nextBtn.disabled = page >= totalPages;
            }

            prevBtn.addEventListener('click', function () { if (page > 1) { page--; render(); } });
            nextBtn.addEventListener('click', function () {
                const totalPages = Math.max(1, Math.ceil(items.length / CARDS_PAGE_SIZE));
                if (page < totalPages) { page++; render(); }
            });

            return {
                setItems: function (newItems) { items = newItems; page = 1; render(); }
            };
        }

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

        function cardItemHtml(row) {
            const lineOutHtml = row.IS_OUT === 'Y' && row.LINEOUT
                ? `<div class="card-item-lineout">🚚 Line: <strong>${row.LINEOUT}</strong></div>`
                : '';
            return `
                <a href="/Production2/Pending2?basketId=${row.BASKET_ID}" class="card-item">
                    <div class="card-item-code">${row.I_CARD_NO || ''}</div>
                    <div class="card-item-meta">${row.C_STYLE || ''} · Size ${row.C_SIZE || ''}</div>
                    <div class="card-item-progress">${row.SET_QTY || 0}/${row.C_QTY || 0} pcs</div>
                    ${lineOutHtml}
                </a>`;
        }

        const pagerOut        = createCardPager('listOut', 'pagerOut', 'cntOut');
        const pagerPending     = createCardPager('listPending', 'pagerPending', 'cntPending');
        const pagerNotStarted  = createCardPager('listNotStarted', 'pagerNotStarted', 'cntNotStarted');

        function renderCardGroups(rows) {
            if (!rows || rows.length === 0) {
                cardGroupsRow.style.display = 'none';
                return;
            }

            const outCards = rows.filter(r => r.IS_OUT === 'Y');
            const pendingCards = rows.filter(r => r.IS_OUT !== 'Y' && (r.SET_QTY || 0) > 0);
            const notStartedCards = rows.filter(r => r.IS_OUT !== 'Y' && (r.SET_QTY || 0) === 0);

            pagerOut.setItems(outCards);
            pagerPending.setItems(pendingCards);
            pagerNotStarted.setItems(notStartedCards);

            cardGroupsRow.style.display = 'block';
        }

        async function findReport() {
            const po = poInput.value.trim();
            if (!po) {
                showStatus('Vui lòng nhập số PO.', true);
                return;
            }

            showStatus('Đang tìm...', false);
            summaryRow.style.display = 'none';
            cardGroupsRow.style.display = 'none';
            pivotTable.style.display = 'none';
            pivotEmpty.style.display = 'block';
            pivotEmpty.innerHTML = `
                <div class="spinner-border text-info" role="status"></div>
                <p class="mt-2 mb-0">Đang tải dữ liệu...</p>`;

            try {
                const [res, cardsRes] = await Promise.all([
                    fetch('/Report/GetCompSttPoReport?po=' + encodeURIComponent(po)),
                    fetch('/Report/GetCardsByPo?po=' + encodeURIComponent(po))
                ]);
                const data = await res.json();
                const cardsData = await cardsRes.json();

                if (cardsRes.ok && cardsData.success) {
                    renderCardGroups(cardsData.data);
                }

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

        // Nếu đến từ trang Báo cáo tổng (bấm vào cột PO) đã có sẵn PO — tự tìm luôn.
        if (window.byPoConfig && window.byPoConfig.initialPo) {
            findReport();
        }
    });
})();
