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
                Icon = "home",
                Children = new List<SideMenuItem>
                {
                    new SideMenuItem { Title = "Tổng quan", Url = "~/Home/Index",       Icon = "dashboard" },
                    new SideMenuItem { Title = "Test scan hàng loạt", Url = "~/Scan/Index", Icon = "qr_code_scanner" },
                    new SideMenuItem { Title = "Sản xuất", Url = "~/Production2/Index", Icon = "precision_manufacturing" },
                    new SideMenuItem { Title = "QC", Url = "~/Qc/Index", Icon = "verified" },
                    new SideMenuItem { Title = "Báo cáo", Url = "~/Report/Index", Icon = "bar_chart" },
                    new SideMenuItem { Title = "Đổi mật khẩu", Url = "~/Account/ChangePassword", Icon = "lock_reset" },
                }
            },
        };
    }
}
