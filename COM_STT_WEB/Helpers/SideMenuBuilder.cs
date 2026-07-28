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
                    new SideMenuItem { Title = "Tổng quan", Url = "~/Home/Index",   Icon = "dashboard" },
                    new SideMenuItem { Title = "Chính sách", Url = "~/Home/Privacy", Icon = "policy"    },
                }
            },
        };
    }
}
