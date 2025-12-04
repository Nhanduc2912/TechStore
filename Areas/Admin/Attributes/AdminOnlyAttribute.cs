using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Threading.Tasks;

namespace TechStore.Areas.Admin.Attributes
{
    /// <summary>
    /// Custom attribute để kiểm tra quyền Admin
    /// Chỉ cho phép user có VaiTro = "Admin" truy cập
    /// </summary>
    public class AdminOnlyAttribute : Attribute, IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var vaiTro = context.HttpContext.Session.GetString("VaiTro");
            
            // SỬA LỖI: Đổi MaTk thành MaKh để khớp với Controller đăng nhập
            var maKh = context.HttpContext.Session.GetString("MaKh"); 

            // Kiểm tra session: Phải có MaKh VÀ VaiTro phải là Admin
            if (string.IsNullOrEmpty(maKh) || vaiTro != "Admin")
            {
                context.Result = new RedirectToRouteResult(
                    new RouteValueDictionary
                    {
                        { "Controller", "Home" },
                        { "Action", "Index" },
                        { "area", "" } // Quay về trang chủ Khách hàng
                    }
                );
                return;
            }

            await next();
        }
    }
}