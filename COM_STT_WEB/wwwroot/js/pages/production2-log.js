(function () {
    document.addEventListener('DOMContentLoaded', function () {
        const logListContainer = document.getElementById('logListContainer');
        const logCount = document.getElementById('logCount');

        const logDateInput     = document.getElementById('logDateInput');
        const logActionFilter  = document.getElementById('logActionFilter');
        const logFilterInput   = document.getElementById('logFilterInput');
        const btnScanFilter    = document.getElementById('btnScanFilter');
        const btnClearFilter   = document.getElementById('btnClearFilter');
        const btnCancelFilterScan = document.getElementById('btnCancelFilterScan');
        const filterScanArea   = document.getElementById('filterScanArea');
        const filterStatus     = document.getElementById('filterStatus');

        const paginationBar    = document.getElementById('paginationBar');
        const pageIndicator    = document.getElementById('pageIndicator');
        const btnPrevPage      = document.getElementById('btnPrevPage');
        const btnNextPage      = document.getElementById('btnNextPage');

        const PAGE_SIZE = 10;
        let allItems = [];
        let filteredItems = [];
        let currentPage = 1;
        let html5QrCode = null;
        let scannerRunning = false;

        function getAntiForgeryToken() {
            const el = document.querySelector('input[name="__RequestVerificationToken"]');
            return el ? el.value : '';
        }

        function renderEmpty(message) {
            logListContainer.innerHTML = `
                <div class="text-center py-5 text-secondary text-sm">
                    📥
                    <p class="mb-0">${message}</p>
                </div>`;
            paginationBar.classList.add('d-none');
        }

        function renderPage() {
            const total = filteredItems.length;
            logCount.textContent = total + ' dòng' + (allItems.length !== total ? ` (lọc từ ${allItems.length})` : '');

            if (total === 0) {
                renderEmpty(allItems.length === 0 ? 'Chưa có dòng nào được ghi trong ngày này.' : 'Không có dòng nào khớp bộ lọc.');
                return;
            }

            const totalPages = Math.max(1, Math.ceil(total / PAGE_SIZE));
            if (currentPage > totalPages) currentPage = totalPages;

            const start = (currentPage - 1) * PAGE_SIZE;
            const pageItems = filteredItems.slice(start, start + PAGE_SIZE);

            logListContainer.innerHTML = pageItems.map(function (item) {
                const isIn = (item.C_ACTION || '').toUpperCase() === 'INPUT';
                const badgeClass = isIn ? 'badge-action-in' : 'badge-action-out';
                const actionLabel = isIn ? 'SET IN' : 'SET OUT';

                return `
                    <div class="log-item" id="log-${item.D_GATHER}" data-dgather="${item.D_GATHER}">
                        <div>
                            <div class="d-flex align-items-center gap-2 mb-1 flex-wrap">
                                <span class="badge text-white ${badgeClass}">${actionLabel}</span>
                                <strong class="text-dark text-sm">${item.C_STYLE || ''}</strong>
                                <span class="text-xs text-secondary">Size ${item.C_SIZE || ''}</span>
                            </div>
                            <div class="text-xs text-secondary">
                                PO: ${item.C_PO_NUM || ''} · Order: ${item.C_ORD_NO || ''} · Part: ${item.C_KEYINPART || ''} · Vị trí: ${item.C_KEYINLOC || ''}
                            </div>
                            <div class="text-xs text-secondary">
                                SL: <strong class="text-primary">${item.Q_QTY} pcs</strong> · NV: ${item.C_WORKER || ''} (${item.I_IP_NO || ''}) · ${item.D_GATHER}
                            </div>
                        </div>
                        <div>
                            <button type="button" class="btn btn-link text-danger p-2 mb-0 btn-delete-log" data-dgather="${item.D_GATHER}" aria-label="Xoá">
                                🗑️
                            </button>
                        </div>
                    </div>`;
            }).join('');

            logListContainer.querySelectorAll('.btn-delete-log').forEach(function (btn) {
                btn.addEventListener('click', function () {
                    deleteLogItem(btn.dataset.dgather);
                });
            });

            paginationBar.classList.toggle('d-none', totalPages <= 1);
            pageIndicator.textContent = `Trang ${currentPage}/${totalPages}`;
            btnPrevPage.disabled = currentPage <= 1;
            btnNextPage.disabled = currentPage >= totalPages;
        }

        function applyFilter() {
            const term = logFilterInput.value.trim().toLowerCase();
            const action = logActionFilter.value; // '' | 'INPUT' | 'OUTPUT'
            btnClearFilter.classList.toggle('d-none', !term);

            filteredItems = allItems.filter(function (item) {
                if (action && (item.C_ACTION || '').toUpperCase() !== action) return false;
                if (!term) return true;
                return [item.C_STYLE, item.C_ORD_NO, item.C_PO_NUM, item.C_KEYINPART, item.C_KEYINLOC, item.C_WORKER]
                    .some(function (f) { return (f || '').toString().toLowerCase().includes(term); });
            });

            currentPage = 1;
            renderPage();
        }

        // yyyy-MM-dd (input date) -> yyyyMMdd (API)
        function toApiDate(inputDateValue) {
            return (inputDateValue || '').replace(/-/g, '');
        }

        async function loadLog() {
            renderEmpty('Đang tải...');
            try {
                const date = toApiDate(logDateInput.value);
                const res = await fetch('/Production2/GetTodayLog?date=' + encodeURIComponent(date));
                const data = await res.json();
                if (res.ok && data.success) {
                    allItems = data.data || [];
                    applyFilter();
                } else {
                    allItems = [];
                    renderEmpty(data.message || 'Không tải được dữ liệu.');
                }
            } catch (err) {
                console.error(err);
                allItems = [];
                renderEmpty('Lỗi kết nối máy chủ!');
            }
        }

        async function deleteLogItem(dGather) {
            if (!confirm('Xác nhận xoá dòng log này (D_GATHER: ' + dGather + ')?')) return;

            try {
                const res = await fetch('/Production2/DeleteLog?dGather=' + encodeURIComponent(dGather), {
                    method: 'DELETE',
                    headers: { 'RequestVerificationToken': getAntiForgeryToken() }
                });

                if (res.ok) {
                    allItems = allItems.filter(function (i) { return i.D_GATHER !== dGather; });
                    applyFilter();
                } else {
                    alert('Không thể xoá dòng này, vui lòng thử lại.');
                }
            } catch (err) {
                console.error(err);
                alert('Lỗi kết nối khi xoá!');
            }
        }

        // --- Lọc theo ngày (gọi lại server) và theo IN/OUT + gõ tay (lọc ngay trên dữ liệu đã tải) ---
        logDateInput.addEventListener('change', loadLog);
        logActionFilter.addEventListener('change', applyFilter);
        logFilterInput.addEventListener('input', applyFilter);
        btnClearFilter.addEventListener('click', function () {
            logFilterInput.value = '';
            applyFilter();
        });

        // --- Phân trang ---
        btnPrevPage.addEventListener('click', function () {
            if (currentPage > 1) { currentPage--; renderPage(); }
        });
        btnNextPage.addEventListener('click', function () {
            currentPage++; renderPage();
        });

        // --- Lọc bằng quét PCard: quét xong tra cứu kế hoạch, lọc theo Style + Order của PCard đó ---
        function stopFilterScanner() {
            filterScanArea.classList.add('d-none');
            if (html5QrCode && scannerRunning) {
                scannerRunning = false;
                html5QrCode.stop().then(function () { html5QrCode.clear(); }).catch(function () {});
            }
        }

        async function onFilterScanSuccess(decodedText) {
            const cardNo = (decodedText || '').trim().toUpperCase();
            if (!cardNo) return;
            stopFilterScanner();

            filterStatus.textContent = `Đang tra cứu PCard ${cardNo}...`;
            filterStatus.className = 'text-xs text-secondary mt-2';

            try {
                const res = await fetch('/Production2/GetPcardInfo?cardNo=' + encodeURIComponent(cardNo));
                const data = await res.json();

                if (res.ok && data.success && data.data) {
                    const ordNo = data.data.I_PO_NO || data.data.iPoNo || '';
                    logFilterInput.value = ordNo || cardNo;
                    applyFilter();
                    filterStatus.textContent = `Đã lọc theo Order ${ordNo} (từ PCard ${cardNo}).`;
                    filterStatus.className = 'text-xs text-success font-weight-bold mt-2';
                } else {
                    logFilterInput.value = cardNo;
                    applyFilter();
                    filterStatus.textContent = `Không tìm thấy kế hoạch cho PCard ${cardNo} — lọc trực tiếp theo mã đã quét.`;
                    filterStatus.className = 'text-xs text-danger mt-2';
                }
            } catch (err) {
                console.error(err);
                filterStatus.textContent = 'Lỗi kết nối khi tra cứu PCard!';
                filterStatus.className = 'text-xs text-danger mt-2';
            }
        }

        function onFilterScanFailure() {
            // bỏ qua khung hình không đọc được mã
        }

        btnScanFilter.addEventListener('click', function () {
            filterScanArea.classList.remove('d-none');
            html5QrCode = new Html5Qrcode('filterScanReader');
            html5QrCode.start(
                { facingMode: 'environment' },
                { fps: 10, qrbox: { width: 240, height: 240 } },
                onFilterScanSuccess,
                onFilterScanFailure
            ).then(function () {
                scannerRunning = true;
            }).catch(function (err) {
                const detail = (err && err.message) ? err.message : String(err);
                filterStatus.textContent = 'Không mở được camera: ' + detail;
                filterStatus.className = 'text-xs text-danger mt-2';
                filterScanArea.classList.add('d-none');
            });
        });

        btnCancelFilterScan.addEventListener('click', stopFilterScanner);
        window.addEventListener('beforeunload', stopFilterScanner);

        // Mặc định ngày hôm nay (yyyy-MM-dd theo giờ máy khách)
        (function initDefaultDate() {
            const now = new Date();
            const yyyy = now.getFullYear();
            const mm = String(now.getMonth() + 1).padStart(2, '0');
            const dd = String(now.getDate()).padStart(2, '0');
            logDateInput.value = `${yyyy}-${mm}-${dd}`;
        })();

        loadLog();
    });
})();
