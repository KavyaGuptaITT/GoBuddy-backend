using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading.Tasks;

namespace GoBuddy.Application.Helpers
{
    public static class FileHelper
    {
        public static async Task<byte[]> ConvertToBytes(IFormFile? file)
        {
            if (file == null || file.Length == 0)
                return Array.Empty<byte>();

            MemoryStream memoryStream = new MemoryStream();
            using (memoryStream)
            {
                await file.CopyToAsync(memoryStream);
                return memoryStream.ToArray();
            }
        }
    }
}