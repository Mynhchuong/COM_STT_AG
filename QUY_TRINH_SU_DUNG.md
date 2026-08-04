# QUY TRÌNH SỬ DỤNG HỆ THỐNG COM STT — SẢN XUẤT (SET IN / SET OUT)

Tài liệu hướng dẫn sử dụng cho nhân viên vận hành. Áp dụng cho công đoạn Cắt lần 2 (CUT_2).

---

## 1. Tổng quan quy trình

```
SET IN (quét thẻ vào)  →  CHỜ SET IN (nhận đủ từng Part)  →  SET OUT (quét thẻ ra, chọn Line)
```

Mỗi lượt Set In tạo ra **1 basket** (giỏ hàng) gồm 1 hoặc 2 thẻ PCard. Basket phải được **nhận đủ tất cả các Part** ở bước "Chờ Set In" thì mới được phép Set Out.

---

## 2. Đăng nhập

- Vào trang đăng nhập, nhập **Mã nhân viên** và **Mật khẩu**.
- Nếu dùng chung máy nhiều người thì **không tick "Ghi nhớ đăng nhập"** để tránh người sau vào nhầm tài khoản.

---

## 3. SET IN — Quét thẻ vào chuyền

**Vào trang:** Trang chủ → Sản xuất → **SET IN**

**Các bước:**
1. Bấm **"BẮT ĐẦU QUÉT CAMERA"** để quét bằng camera điện thoại/máy tính bảng, hoặc dùng **súng bắn mã vạch PDA** / gõ tay vào ô nhập.
2. Mỗi lượt Set In chỉ quét được **tối đa 2 thẻ**, tổng số lượng không quá **24 pcs**. Quét vượt sẽ bị chặn và báo rõ.
3. Nếu quét 2 thẻ, **2 thẻ bắt buộc cùng 1 Order (PO)** — khác PO sẽ bị chặn khi Lưu.
4. Kiểm tra lại danh sách thẻ đã quét bên phải màn hình, bấm nút thùng rác nếu quét nhầm cần xoá.
5. Bấm **"LƯU DỮ LIỆU SET IN"**.
6. Hệ thống tự tạo basket, hệ thống **tự động chuyển sang trang "Chờ Set In"** để xem cần chuẩn bị những Part nào.

> ⚠️ Nếu quét lại đúng (các) thẻ đã Set In trước đó, hệ thống sẽ báo lỗi trùng (thẻ đã có basket) — đây là hành vi đúng, không phải lỗi hệ thống.

---

## 4. CHỜ SET IN — Nhận hàng theo từng Part

**Vào trang:** tự động sau khi Set In xong, hoặc vào tay: Trang chủ → Sản xuất → nhập **Basket ID** hoặc quét **mã PCard** rồi bấm **Tìm**.

**Màn hình hiển thị:**
- Thẻ thông tin PCard: Style, Size, PO, số lượng kế hoạch.
- Bảng danh sách từng **Part** cần chuẩn bị, kèm cột **Cần / Đã nhận / Còn lại / Trạng thái**.

**Cách nhận hàng cho từng Part (cột "Đã nhận"):**
- Ô đang **trống (dấu "-")** → bấm **1 lần** = nhận đủ ngay theo đúng số lượng cần.
- Ô **đã có số** rồi → bấm lại để mở **bàn phím số**, nhập đúng số lượng thực tế nhận được (không được nhập vượt quá số lượng cần).

> ✅ Phải nhận đủ **TẤT CẢ các Part** trong basket thì thẻ mới được phép Set Out.

---

## 5. KIỂM TRA PENDING — Tra nhanh 1 thẻ bất kỳ

**Vào trang:** Trang chủ → Sản xuất → **KIỂM TRA PENDING**

Dùng khi cần kiểm tra nhanh 1 PCard xem basket của nó còn thiếu Part nào chưa nhận đủ, **không cần nhớ Basket ID**. Quét hoặc nhập mã PCard, hệ thống tự mở đúng trang Chờ Set In tương ứng.

---

## 6. SET OUT — Quét thẻ ra chuyền

**Vào trang:** Trang chủ → Sản xuất → **SET OUT**

**Các bước:**
1. **Bắt buộc chọn Plant (B hoặc E) và Line (01–30) trước** — chưa chọn sẽ không Lưu được.
2. Quét/nhập mã PCard như Set In (Set Out **không giới hạn** số thẻ/số lượng mỗi lượt).
3. Bấm **"LƯU DỮ LIỆU SET OUT"**.

**Điều kiện Set Out:**
- Thẻ phải đã **nhận đủ tất cả Part** ở bước Chờ Set In — chưa đủ thì bị chặn, báo rõ còn thiếu bao nhiêu (VD: "chưa đủ SET (8/12)").
- Mỗi thẻ **chỉ Out được đúng 1 lần** — cố Out lại lần 2 sẽ bị chặn, báo **"đã Out rồi"**.
- Nếu quét nhiều thẻ cùng lúc mà có thẻ không đạt điều kiện, hệ thống vẫn **lưu các thẻ hợp lệ**, chỉ báo lỗi riêng cho (các) thẻ không đạt.

---

## 7. XEM LOG — Xem lại lịch sử

**Vào trang:** Trang chủ → Sản xuất → **XEM LOG**

- Chọn **Ngày** cần xem (mặc định hôm nay).
- Lọc theo **Loại**: Tất cả / Chỉ SET IN / Chỉ SET OUT.
- Gõ từ khoá để lọc nhanh theo Style/Order/PO/Nhân viên...
- Có phân trang khi nhiều dòng.
- Nút xoá (🗑️) chỉ dùng để xoá dòng **quét nhầm/test**, không dùng cho dữ liệu sản xuất thật.

---

## 8. XEM BÁO CÁO

**Vào trang:** Trang chủ → **Báo cáo**

- **Báo cáo tổng**: chọn khoảng ngày, có thể lọc thêm theo PO/Part.
- **Báo cáo theo PO**: nhập số PO để xem chi tiết riêng đơn hàng đó.

---

## 9. ĐỔI MẬT KHẨU

Trang chủ → **Đổi mật khẩu** → nhập mật khẩu cũ và mật khẩu mới.

---

## 10. Các thông báo hay gặp và cách xử lý

| Thông báo | Ý nghĩa | Cách xử lý |
|---|---|---|
| "Đã đủ tối đa 2 thẻ trong 1 lượt Set In" | Đang cố quét thẻ thứ 3 | Bấm Lưu hoặc Xoá danh sách để bắt đầu lượt mới |
| "Vượt quá tổng số lượng tối đa 24 pcs/lượt" | Tổng số lượng 2 thẻ vượt 24 | Tách ra quét 2 lượt riêng |
| "Tất cả thẻ trong 1 lượt lưu phải cùng 1 Order (PO)" | 2 thẻ quét khác PO nhau | Kiểm tra lại thẻ, quét đúng 2 thẻ cùng PO |
| "PCard ...: chưa đủ SET (x/y) — không thể Set Out" | Chưa nhận đủ hàng ở bước Chờ Set In | Vào trang Chờ Set In (hoặc Kiểm tra Pending) nhận đủ rồi Out lại |
| "PCard ... đã Out rồi — không thể Out thêm lần nữa" | Thẻ đã được Out trước đó | Không cần thao tác gì thêm, thẻ đã hoàn tất |
| "Vui lòng chọn Plant + Line nhận hàng trước khi Lưu" | Chưa chọn Line ở Set Out | Chọn Plant + Line trước khi bấm Lưu |
| "Số lượng (...) phải từ 0 đến ..." | Nhập tay số lượng ở Chờ Set In vượt quá số cần | Nhập lại đúng số lượng thực nhận, không vượt số cần |
| "Không tìm thấy basket cho PCard ..." | PCard chưa được Set In, hoặc gõ sai mã | Kiểm tra lại mã thẻ, Set In trước nếu chưa quét |

---

*Tài liệu bàn giao dự án — Hệ thống COM STT Sản xuất (Set In/Out, Chờ Set In, Log, Báo cáo).*
