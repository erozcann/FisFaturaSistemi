using FisFaturaAPI.Models;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FisFaturaAPI.Services
{
    public interface IOcrService
    {
        Task<OcrResult> ProcessInvoiceAsync(IFormFile file);
        Task<OcrResult> ProcessReceiptAsync(IFormFile file);
    }
} 