using ASC.Business.Interfaces;
using ASC.Model.Models;
using ASC.Web.Areas.Configuration.Models;
using ASC.Web.Controllers;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using System.ComponentModel;
using ASC.Utilities;

namespace ASC.Web.Areas.Configuration.Controllers
{
    [Area("Configuration")]
    [Authorize(Roles = "Admin")]
    public class MasterDataController : BaseController
    {
        private readonly IMasterDataOperations _masterData;
        private readonly IMapper _mapper;

        public MasterDataController(IMasterDataOperations masterData, IMapper mapper)
        {
            _masterData = masterData;
            _mapper = mapper;
        }

        // ================= MASTER KEYS =================
        [HttpGet]
        public async Task<IActionResult> MasterKeys()
        {
            var masterKeys = await _masterData.GetAllMasterKeysAsync();
            var masterKeysViewModel = _mapper.Map<List<MasterDataKey>, List<MasterDataKeyViewModel>>(masterKeys);
            // Hold all Master Keys in session
            HttpContext.Session.SetSession("MasterKeys", masterKeysViewModel);
            return View(new MasterKeysViewModel
            {
                MasterKeys = masterKeysViewModel == null ? null : masterKeysViewModel.ToList(),
                IsEdit = false
            });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MasterKeys(MasterKeysViewModel masterKeys)
        {
            Console.WriteLine("===== POST MASTER KEYS START =====");

            if (masterKeys.MasterKeyInContext == null)
            {
                masterKeys.MasterKeyInContext = new MasterDataKeyViewModel();
            }

            ModelState.Remove("MasterKeys");
            ModelState.Remove("MasterKeyInContext.RowKey");
            ModelState.Remove("MasterKeyInContext.PartitionKey");
            ModelState.Remove("MasterKeyInContext.CreatedBy");
            ModelState.Remove("MasterKeyInContext.UpdatedBy");
            ModelState.Remove("MasterKeyInContext.CreatedDate");
            ModelState.Remove("MasterKeyInContext.UpdatedDate");
            ModelState.Remove("MasterKeyInContext.IsDeleted");

            if (string.IsNullOrWhiteSpace(masterKeys.MasterKeyInContext.Name))
            {
                ModelState.AddModelError("MasterKeyInContext.Name", "Name is required.");
            }

            if (!ModelState.IsValid)
            {
                Console.WriteLine("===== MODEL STATE INVALID =====");

                foreach (var error in ModelState)
                {
                    foreach (var subError in error.Value.Errors)
                    {
                        Console.WriteLine(error.Key + " : " + subError.ErrorMessage);
                    }
                }

                var allMasterKeys = await _masterData.GetAllMasterKeysAsync();
                masterKeys.MasterKeys = _mapper.Map<List<MasterDataKeyViewModel>>(allMasterKeys);

                return View(masterKeys);
            }

            var masterKey = _mapper.Map<MasterDataKey>(masterKeys.MasterKeyInContext);

            var currentUser = HttpContext.User.GetCurrentUserDetails();

            var currentUserName = currentUser != null && !string.IsNullOrWhiteSpace(currentUser.Name)
                ? currentUser.Name
                : "System";

            if (masterKeys.IsEdit)
            {
                Console.WriteLine("===== UPDATE MASTER KEY =====");

                if (string.IsNullOrWhiteSpace(masterKey.RowKey))
                {
                    masterKey.RowKey = Guid.NewGuid().ToString();
                }

                if (string.IsNullOrWhiteSpace(masterKey.PartitionKey))
                {
                    masterKey.PartitionKey = masterKey.Name;
                }

                masterKey.UpdatedBy = currentUserName;
                masterKey.UpdatedDate = DateTime.UtcNow;

                Console.WriteLine("UPDATE DATA:");
                Console.WriteLine("PartitionKey = " + masterKey.PartitionKey);
                Console.WriteLine("RowKey = " + masterKey.RowKey);
                Console.WriteLine("Name = " + masterKey.Name);
                Console.WriteLine("IsActive = " + masterKey.IsActive);
                Console.WriteLine("UpdatedBy = " + masterKey.UpdatedBy);

                await _masterData.UpdateMasterKeyAsync(masterKey.PartitionKey, masterKey);
            }
            else
            {
                Console.WriteLine("===== INSERT MASTER KEY =====");

                if (string.IsNullOrWhiteSpace(masterKey.RowKey))
                {
                    masterKey.RowKey = Guid.NewGuid().ToString();
                }

                if (string.IsNullOrWhiteSpace(masterKey.PartitionKey))
                {
                    masterKey.PartitionKey = masterKey.Name;
                }

                masterKey.CreatedBy = currentUserName;
                masterKey.UpdatedBy = currentUserName;

                masterKey.CreatedDate = DateTime.UtcNow;
                masterKey.UpdatedDate = DateTime.UtcNow;

                masterKey.IsDeleted = false;

                Console.WriteLine("INSERT DATA:");
                Console.WriteLine("PartitionKey = " + masterKey.PartitionKey);
                Console.WriteLine("RowKey = " + masterKey.RowKey);
                Console.WriteLine("Name = " + masterKey.Name);
                Console.WriteLine("IsActive = " + masterKey.IsActive);
                Console.WriteLine("CreatedBy = " + masterKey.CreatedBy);
                Console.WriteLine("UpdatedBy = " + masterKey.UpdatedBy);
                Console.WriteLine("CreatedDate = " + masterKey.CreatedDate);
                Console.WriteLine("UpdatedDate = " + masterKey.UpdatedDate);

                await _masterData.InsertMasterKeyAsync(masterKey);
            }

            Console.WriteLine("===== POST MASTER KEYS END =====");

            return RedirectToAction(nameof(MasterKeys));
        }

        // ================= MASTER VALUES =================
        [HttpGet]
        public async Task<IActionResult> MasterValues()
        {
            var masterKeys = await _masterData.GetAllMasterKeysAsync();

            ViewBag.MasterKeys = masterKeys
                .Where(x => x.IsDeleted == false)
                .ToList();

            return View(new MasterValuesViewModel
            {
                MasterValues = new List<MasterDataValueViewModel>(),
                IsEdit = false
            });
        }

        [HttpGet]
        public async Task<IActionResult> MasterValuesByKey(string key)
        {
            Console.WriteLine("===== GET MASTER VALUES BY KEY =====");
            Console.WriteLine("key = " + key);

            if (string.IsNullOrWhiteSpace(key))
            {
                return Json(new
                {
                    data = new List<MasterDataValue>()
                });
            }

            var values = await _masterData.GetAllMasterValuesByKeyAsync(key);

            return Json(new
            {
                data = values
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MasterValues(bool isEdit, MasterDataValueViewModel masterValue)
        {
            Console.WriteLine("===== POST MASTER VALUES START =====");
            Console.WriteLine("isEdit = " + isEdit);
            Console.WriteLine("PartitionKey = " + masterValue?.PartitionKey);
            Console.WriteLine("RowKey = " + masterValue?.RowKey);
            Console.WriteLine("Name = " + masterValue?.Name);
            Console.WriteLine("IsActive = " + masterValue?.IsActive);

            if (masterValue == null)
            {
                return Json("Error: masterValue is null");
            }

            ModelState.Remove("masterValue.RowKey");
            ModelState.Remove("masterValue.CreatedBy");
            ModelState.Remove("masterValue.UpdatedBy");
            ModelState.Remove("masterValue.CreatedDate");
            ModelState.Remove("masterValue.UpdatedDate");
            ModelState.Remove("masterValue.IsDeleted");

            if (string.IsNullOrWhiteSpace(masterValue.PartitionKey))
            {
                ModelState.AddModelError("masterValue.PartitionKey", "Partition Key is required.");
            }

            if (string.IsNullOrWhiteSpace(masterValue.Name))
            {
                ModelState.AddModelError("masterValue.Name", "Name is required.");
            }

            if (!ModelState.IsValid)
            {
                Console.WriteLine("===== MASTER VALUE MODEL STATE INVALID =====");

                foreach (var error in ModelState)
                {
                    foreach (var subError in error.Value.Errors)
                    {
                        Console.WriteLine(error.Key + " : " + subError.ErrorMessage);
                    }
                }

                return Json("Error");
            }

            var masterDataValue = _mapper.Map<MasterDataValueViewModel, MasterDataValue>(masterValue);

            var currentUser = HttpContext.User.GetCurrentUserDetails();

            var currentUserName = currentUser != null && !string.IsNullOrWhiteSpace(currentUser.Name)
                ? currentUser.Name
                : "System";

            if (isEdit)
            {
                Console.WriteLine("===== UPDATE MASTER VALUE =====");

                if (string.IsNullOrWhiteSpace(masterDataValue.RowKey))
                {
                    return Json("Error: RowKey is required for update.");
                }

                masterDataValue.UpdatedBy = currentUserName;
                masterDataValue.UpdatedDate = DateTime.UtcNow;

                await _masterData.UpdateMasterValueAsync(
                    masterDataValue.PartitionKey,
                    masterDataValue.RowKey,
                    masterDataValue
                );
            }
            else
            {
                Console.WriteLine("===== INSERT MASTER VALUE =====");

                if (string.IsNullOrWhiteSpace(masterDataValue.RowKey))
                {
                    masterDataValue.RowKey = Guid.NewGuid().ToString();
                }

                masterDataValue.CreatedBy = currentUserName;
                masterDataValue.UpdatedBy = currentUserName;

                masterDataValue.CreatedDate = DateTime.UtcNow;
                masterDataValue.UpdatedDate = DateTime.UtcNow;

                masterDataValue.IsDeleted = false;

                Console.WriteLine("INSERT MASTER VALUE:");
                Console.WriteLine("PartitionKey = " + masterDataValue.PartitionKey);
                Console.WriteLine("RowKey = " + masterDataValue.RowKey);
                Console.WriteLine("Name = " + masterDataValue.Name);
                Console.WriteLine("IsActive = " + masterDataValue.IsActive);
                Console.WriteLine("CreatedBy = " + masterDataValue.CreatedBy);
                Console.WriteLine("UpdatedBy = " + masterDataValue.UpdatedBy);

                await _masterData.InsertMasterValueAsync(masterDataValue);
            }

            Console.WriteLine("===== POST MASTER VALUES END =====");

            return Json(true);
        }

        // ================= EXCEL =================

        //private async Task<List<MasterDataValue>> ParseMasterDataExcel(IFormFile excelFile)
        //{
        //    var masterValueList = new List<MasterDataValue>();
        //    using (var memoryStream = new MemoryStream())
        //    {
        //        // Get MemoryStream from Excel file
        //        await excelFile.CopyToAsync(memoryStream);
        //        // Create a ExcelPackage object from MemoryStream
        //        using (ExcelPackage package = new ExcelPackage(memoryStream))
        //        {
        //            // Get the first Excel sheet from the Workbook
        //            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        //            ExcelWorksheet worksheet = package.Workbook.Worksheets[0];
        //            int rowCount = worksheet.Dimension.Rows;
        //            // Iterate all the rows and create the list of MasterDataValue
        //            // Ignore first row as it is header
        //            for (int row = 2; row <= rowCount; row++)
        //            {
        //                var masterDataValue = new MasterDataValue();
        //                masterDataValue.RowKey = Guid.NewGuid().ToString();
        //                masterDataValue.PartitionKey = worksheet.Cells[row, 1].Value.ToString();
        //                masterDataValue.Name = worksheet.Cells[row, 2].Value.ToString();
        //                masterDataValue.IsActive = Boolean.Parse(worksheet.Cells[row, 3].Value.ToString());
        //                masterValueList.Add(masterDataValue);
        //            }
        //        }
        //    }
        //    return masterValueList;
        //}
        private async Task<List<MasterDataValue>> ParseMasterDataExcel(IFormFile excelFile)
        {
            var masterValueList = new List<MasterDataValue>();

            using (var memoryStream = new MemoryStream())
            {
                await excelFile.CopyToAsync(memoryStream);

                // ✅ Đặt license ở đây (trước khi tạo package)
                ExcelPackage.License.SetNonCommercialPersonal("Tin Ni");

                using (ExcelPackage package = new ExcelPackage(memoryStream))
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets[0];
                    int rowCount = worksheet.Dimension.Rows;

                    for (int row = 2; row <= rowCount; row++)
                    {
                        var masterDataValue = new MasterDataValue();

                        masterDataValue.RowKey = Guid.NewGuid().ToString();
                        masterDataValue.PartitionKey = worksheet.Cells[row, 1].Value?.ToString();
                        masterDataValue.Name = worksheet.Cells[row, 2].Value?.ToString();

                        // tránh lỗi null
                        masterDataValue.IsActive = Boolean.Parse(worksheet.Cells[row, 3].Value?.ToString() ?? "false");

                        masterValueList.Add(masterDataValue);
                    }
                }
            }

            return masterValueList;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadExcel()
        {
            var files = Request.Form.Files;

            if (!files.Any())
            {
                return Json(new { Error = true, Text = "Upload a file" });
            }

            var excelFile = files.First();

            if (excelFile.Length <= 0)
            {
                return Json(new { Error = true, Text = "Upload a file" });
            }

            var masterData = await ParseMasterDataExcel(excelFile);
            var result = await _masterData.UploadBulkMasterData(masterData);

            return Json(new { Success = result });
        }
    }
}