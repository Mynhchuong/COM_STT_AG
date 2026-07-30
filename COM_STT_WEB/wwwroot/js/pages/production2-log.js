(function () {
    document.addEventListener('DOMContentLoaded', function () {
        const logListContainer = document.getElementById('logListContainer');
        const logCount = document.getElementById('logCount');

        function getAntiForgeryToken() {
            const el = document.querySelector('input[name="__RequestVerificationToken"]');
            return el ? el.value : '';
        }

        function renderEmpty(message) {
            logListContainer.innerHTML = `
                <div class="text-center py-5 text-secondary text-sm">
                    <i class="material-symbols-rounded text-secondary text-4xl mb-2">inbox</i>
                    <p class="mb-0">${message}</p>
                </div>`;
        }

        function renderList(items) {
            if (!items || items.length === 0) {
                renderEmpty('Chưa có dòng nào được ghi hôm nay.');
                logCount.textContent = '0 dòng';
                return;
            }

            logCount.textContent = items.length + ' dòng';

            logListContainer.innerHTML = items.map(function (item) {
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
                                <i class="material-symbols-rounded">delete_outline</i>
                            </button>
                        </div>
                    </div>`;
            }).join('');

            logListContainer.querySelectorAll('.btn-delete-log').forEach(function (btn) {
                btn.addEventListener('click', function () {
                    deleteLogItem(btn.dataset.dgather);
                });
            });
        }

        async function loadLog() {
            renderEmpty('Đang tải...');
            try {
                const res = await fetch('/Production2/GetTodayLog');
                const data = await res.json();
                if (res.ok && data.success) {
                    renderList(data.data);
                } else {
                    renderEmpty(data.message || 'Không tải được dữ liệu.');
                }
            } catch (err) {
                console.error(err);
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
                    const el = document.getElementById('log-' + dGather);
                    if (el) el.remove();
                    const remaining = logListContainer.querySelectorAll('.log-item').length;
                    logCount.textContent = remaining + ' dòng';
                    if (remaining === 0) renderEmpty('Chưa có dòng nào được ghi hôm nay.');
                } else {
                    alert('Không thể xoá dòng này, vui lòng thử lại.');
                }
            } catch (err) {
                console.error(err);
                alert('Lỗi kết nối khi xoá!');
            }
        }

        loadLog();
    });
})();
