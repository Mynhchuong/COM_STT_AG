/* =============================================================
   global-function.js
   THƯ VIỆN HELPER TỐI ƯU CHO MOBILE & MÁY QUÉT MÃ VẠCH ANDROID PDA
   Dùng chung cho toàn bộ dự án (Scan PCard, Đăng ký sản xuất, Andon, Kho, v.v.)
   ============================================================= */

/**
 * PdaHelper — Đối tượng Helper toàn cục cung cấp tất cả tiện ích cho Android PDA & Mobile
 */
window.PdaHelper = (function () {
    let audioCtx = null;
    let toastTimer = null;

    return {
        /**
         * 1. Phát tiếng BEEP phản hồi qua Web Audio API (0ms độ trễ)
         * @param {boolean} success - true = BEEP cao trong vắt, false = TÍT TÍT trầm báo lỗi
         */
        beep: function (success) {
            try {
                if (!audioCtx) {
                    const AudioContext = window.AudioContext || window.webkitAudioContext;
                    if (AudioContext) audioCtx = new AudioContext();
                }
                if (audioCtx && audioCtx.state === 'suspended') {
                    audioCtx.resume();
                }
                if (!audioCtx) return;

                const osc = audioCtx.createOscillator();
                const gain = audioCtx.createGain();
                osc.connect(gain);
                gain.connect(audioCtx.destination);

                if (success) {
                    // Tiếng BÉEP thành công (1400Hz cao trong vắt, 0.12 giây)
                    osc.type = 'sine';
                    osc.frequency.setValueAtTime(1400, audioCtx.currentTime);
                    gain.gain.setValueAtTime(0.35, audioCtx.currentTime);
                    gain.gain.exponentialRampToValueAtTime(0.001, audioCtx.currentTime + 0.12);
                    osc.start(audioCtx.currentTime);
                    osc.stop(audioCtx.currentTime + 0.12);
                } else {
                    // Tiếng TÍT TÍT cảnh báo lỗi (350Hz -> 180Hz âm trầm báo lỗi)
                    osc.type = 'sawtooth';
                    osc.frequency.setValueAtTime(350, audioCtx.currentTime);
                    osc.frequency.linearRampToValueAtTime(180, audioCtx.currentTime + 0.25);
                    gain.gain.setValueAtTime(0.4, audioCtx.currentTime);
                    gain.gain.exponentialRampToValueAtTime(0.001, audioCtx.currentTime + 0.25);
                    osc.start(audioCtx.currentTime);
                    osc.stop(audioCtx.currentTime + 0.25);
                }
            } catch (e) {
                console.warn('Web Audio API not supported:', e);
            }
        },

        /**
         * 2. Rung Haptic phản hồi trên thiết bị di động
         * @param {boolean} success - true = 1 nhịp rung mạnh (120ms), false = 2 nhịp ngắn ([100, 60, 100])
         */
        vibrate: function (success) {
            if (navigator.vibrate) {
                if (success) {
                    navigator.vibrate(120);
                } else {
                    navigator.vibrate([100, 60, 100]);
                }
            }
        },

        /**
         * 3. Chớp hiệu ứng màu sắc trên một phần tử DOM
         * @param {HTMLElement|string} elOrId - Đơn vị DOM hoặc ID phần tử
         * @param {boolean} success - true = xanh lá, false = đỏ
         */
        flash: function (elOrId, success) {
            const el = typeof elOrId === 'string' ? document.getElementById(elOrId) : elOrId;
            if (!el) return;

            const flashClass = success ? 'flash-success' : 'flash-error';
            el.classList.add(flashClass);
            setTimeout(function () {
                el.classList.remove(flashClass);
            }, 450);
        },

        /**
         * 4. Hiển thị Toast thông báo thả từ đỉnh màn hình xuống
         * @param {boolean} success - true = Xanh, false = Đỏ
         * @param {string} title - Tiêu đề thông báo
         * @param {string} message - Nội dung chi tiết
         */
        toast: function (success, title, message) {
            let toastEl = document.getElementById('pdaGlobalToast');
            if (!toastEl) {
                toastEl = document.createElement('div');
                toastEl.id = 'pdaGlobalToast';
                toastEl.style.cssText = `
                    position: fixed; top: 16px; left: 50%; transform: translateX(-50%);
                    z-index: 99999; width: 92%; max-width: 460px; border-radius: 16px;
                    font-size: 1rem; font-weight: 600; padding: 16px 20px; color: #fff;
                    display: none; box-shadow: 0 12px 36px rgba(0,0,0,0.25);
                `;
                document.body.appendChild(toastEl);
            }

            toastEl.style.background = success 
                ? 'linear-gradient(195deg, #2dce89, #1aaf6c)' 
                : 'linear-gradient(195deg, #f5365c, #c0392b)';

            const icon = success ? 'check_circle' : 'error';
            toastEl.innerHTML = `
                <div style="display:flex; align-items:center; gap:12px;">
                    <span class="material-symbols-rounded" style="font-size:28px;">${icon}</span>
                    <div style="flex:1;">
                        <div style="font-size:1rem; font-weight:800;">${title}</div>
                        <div style="font-size:0.85rem; opacity:0.9;">${message}</div>
                    </div>
                </div>
            `;

            toastEl.style.display = 'block';
            toastEl.style.animation = 'toastSlideDown 0.25s ease forwards';

            if (toastTimer) clearTimeout(toastTimer);
            toastTimer = setTimeout(function () {
                toastEl.style.display = 'none';
            }, 3500);
        },

        /**
         * 5. Phản hồi toàn diện (BEEP + Rung + Flash + Toast)
         * @param {boolean} success - Trạng thái thành công hay thất bại
         * @param {string} message - Thông điệp hiển thị
         * @param {Object} [options] - Cấu hình thêm (flashEl, statusEl)
         */
        feedback: function (success, message, options = {}) {
            this.beep(success);
            this.vibrate(success);

            if (options.flashEl) {
                this.flash(options.flashEl, success);
            }

            if (options.statusEl) {
                const el = typeof options.statusEl === 'string' ? document.getElementById(options.statusEl) : options.statusEl;
                if (el) {
                    el.textContent = message;
                    el.className = 'text-sm font-weight-bolder mt-2 d-block py-2 px-3 border-radius-md ' +
                        (success ? 'bg-gradient-success text-white' : 'bg-gradient-danger text-white');
                }
            } else {
                this.toast(success, success ? 'Thành công ✓' : 'Cảnh báo ✕', message);
            }
        },

        /**
         * 6. Khởi tạo ô nhận mã vạch chuyên dụng cho máy đọc mã vạch Android PDA
         * Tự động: Chuyển chữ HOA + Giữ Focus liên tục khi bóp cò súng quét + Xử lý Enter
         * @param {string} inputId - ID của ô input
         * @param {Function} onScanCallback - Hàm nhận giá trị mã vạch sau khi quét
         * @param {Object} [options] - { autoFocus: true, clearOnScan: true }
         */
        initScanner: function (inputId, onScanCallback, options = {}) {
            const input = document.getElementById(inputId);
            if (!input) return;

            const opts = Object.assign({ autoFocus: true, clearOnScan: true }, options);

            // Chuyển tự động chữ HOA khi gõ/quét
            input.addEventListener('input', function () {
                input.value = input.value.toUpperCase();
            });

            // Xử lý sự kiện bấm phím Enter từ súng bắn vạch
            input.addEventListener('keydown', function (e) {
                if (e.key === 'Enter') {
                    e.preventDefault();
                    const code = input.value.trim().toUpperCase();
                    if (code && typeof onScanCallback === 'function') {
                        onScanCallback(code);
                        if (opts.clearOnScan) {
                            input.value = '';
                        }
                    }
                }
            });

            // Tự động giữ Focus cho súng bắn vạch khi chạm ngoài màn hình
            if (opts.autoFocus) {
                setTimeout(() => input.focus(), 300);

                document.addEventListener('click', function (e) {
                    const target = e.target;
                    if (
                        target.tagName !== 'BUTTON' &&
                        target.tagName !== 'A' &&
                        target.tagName !== 'INPUT' &&
                        target.tagName !== 'SELECT' &&
                        !target.closest('button') &&
                        !target.closest('a')
                    ) {
                        input.focus();
                    }
                });
            }
        }
    };
})();

/* =============================================================
   HÀM TƯƠNG THÍCH NGƯỢC (Backward Compatibility)
   ============================================================= */
function playScanBeep(success) {
    window.PdaHelper.beep(success);
}

function initAndroidPdaScannerFocus(inputId) {
    window.PdaHelper.initScanner(inputId, null, { autoFocus: true, clearOnScan: false });
}

function initLayoutToggle(pageKey) {
    const storageKey = `hide_ui_${pageKey}`;

    const btn = document.createElement('button');
    btn.id        = 'btnToggleLayout';
    btn.className = 'layout-toggle-btn';
    btn.title     = 'Show / Hide Header & Filter';
    document.body.appendChild(btn);

    function setIcon(hidden) {
        btn.innerHTML = hidden
            ? '<span class="material-symbols-rounded" style="font-size:18px;color:#fff;">visibility</span>'
            : '<span class="material-symbols-rounded" style="font-size:18px;color:#fff;">visibility_off</span>';
    }

    const savedHidden = localStorage.getItem(storageKey) === 'true';
    if (savedHidden) {
        document.body.classList.add('hide-ui');
    }
    setIcon(savedHidden);

    btn.addEventListener('click', function () {
        document.body.classList.toggle('hide-ui');
        const isHidden = document.body.classList.contains('hide-ui');
        localStorage.setItem(storageKey, isHidden);
        setIcon(isHidden);
    });
}
