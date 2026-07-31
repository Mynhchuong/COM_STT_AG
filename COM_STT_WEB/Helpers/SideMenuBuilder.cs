using COM_STT_WEB.Models;

namespace COM_STT_WEB.Helpers;

// Xây danh sách menu cho sidenav. Mỗi nhóm là 1 SideMenuItem có Children;
// VisibleWhen để trống nghĩa là luôn hiện. Khi có hệ thống đăng nhập/phân quyền,
// truyền thêm user vào Build() và gán VisibleWhen theo role như HR_web đang làm.
public static class SideMenuBuilder
{
    public static List<SideMenuItem> Build()
    {
        return new List<SideMenuItem>
        {
            new SideMenuItem
            {
                Id = "Home",
                Title = "Trang chủ",
                Icon = "🏠",
                Children = new List<SideMenuItem>
                {
                    new SideMenuItem { Title = "Tổng quan", Url = "~/Home/Index",       Icon = "📈" },
                    new SideMenuItem { Title = "Test scan hàng loạt", Url = "~/Scan/Index", Icon = "📷" },
                    new SideMenuItem { Title = "Sản xuất", Url = "~/Production2/Index", Icon = "🏭" },
                    // QC tạm ẩn khỏi menu theo yêu cầu — action/view vẫn còn nguyên.
                    new SideMenuItem { Title = "Báo cáo", Url = "~/Report/Index", Icon = "📊" },
                    new SideMenuItem { Title = "Đổi mật khẩu", Url = "~/Account/ChangePassword", Icon = "🔒" },
                }
            },
        };
    }
}
