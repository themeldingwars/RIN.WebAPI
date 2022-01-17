using System.Text;
using Microsoft.AspNetCore.Mvc;

namespace RIN.WebAPI.Controllers
{
    public class TmwController : Controller
    {
        protected string GetHeadersDev()
        {
            var sb = new StringBuilder();
            foreach (var kvp in Request.Headers) {
                sb.AppendLine($"{kvp.Key}: {kvp.Value}");
            }

            return sb.ToString();
        }
    }
}