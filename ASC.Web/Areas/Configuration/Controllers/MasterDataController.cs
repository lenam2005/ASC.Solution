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
    var masterKeysViewModel = _mapper.Map<List<MasterDataKeyViewModel>>(masterKeys);

    HttpContext.Session.SetSession("MasterKeys", masterKeysViewModel);

    return View(new MasterKeysViewModel
    {
        MasterKeys = masterKeysViewModel?.ToList(),
        MasterKeyInContext = new MasterDataKeyViewModel(),
        IsEdit = false
    });
}
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> MasterKeys(MasterKeysViewModel masterKeys)
        //{
        //    masterKeys.MasterKeys =
        //        HttpContext.Session.GetSession<List<MasterDataKeyViewModel>>("MasterKeys");

        //    if (!ModelState.IsValid)
        //    {
        //        return View(masterKeys);
        //    }

        //    var masterKey = _mapper.Map<MasterDataKey>(masterKeys.MasterKeyInContext);

        //    if (masterKeys.IsEdit)
        //    {
        //        // Update
        //        await _masterData.UpdateMasterKeyAsync(
        //            masterKeys.MasterKeyInContext.PartitionKey,
        //            masterKey);
        //    }
        //    else
        //    {
        //        // Insert
        //        masterKey.RowKey = Guid.NewGuid().ToString();
        //        masterKey.PartitionKey = masterKey.Name;

        //        await _masterData.InsertMasterKeyAsync(masterKey);
        //    }

        //    return RedirectToAction("MasterKeys");
        //}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MasterKeys(MasterKeysViewModel masterKeys)
        {
            var postedName = masterKeys?.MasterKeyInContext?.Name;
            var postedIsActive = masterKeys?.MasterKeyInContext?.IsActive;
            var isEdit = masterKeys?.IsEdit;

            if (string.IsNullOrWhiteSpace(postedName))
            {
                return Content("Name rong hoac khong bind duoc");
            }

            var masterKey = _mapper.Map<MasterDataKey>(masterKeys.MasterKeyInContext);

            if (masterKeys.IsEdit)
            {
                await _masterData.UpdateMasterKeyAsync(
                    masterKeys.MasterKeyInContext.PartitionKey,
                    masterKey);
            }
            else
            {
                masterKey.RowKey = Guid.NewGuid().ToString();
                masterKey.PartitionKey = masterKey.Name;

                await _masterData.InsertMasterKeyAsync(masterKey);
            }

            return Content($"Da nhan du lieu: Name = {postedName}, IsActive = {postedIsActive}, IsEdit = {isEdit}");
        }

        // ================= MASTER VALUES =================

        [HttpGet]
        public async Task<IActionResult> MasterValues()
        {
            ViewBag.MasterKeys = await _masterData.GetAllMasterKeysAsync();

            return View(new MasterValuesViewModel
            {
                MasterValues = new List<MasterDataValueViewModel>(),
                IsEdit = false
            });
        }

        [HttpGet]
        public async Task<IActionResult> MasterValuesByKey(string key)
        {
            return Json(new
            {
                data = await _masterData.GetAllMasterValuesByKeyAsync(key)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MasterValues(bool isEdit, MasterDataValueViewModel masterValue)
        {
            if (!ModelState.IsValid)
            {
                return Json("Error");
            }

            var masterDataValue =
                _mapper.Map<MasterDataValue>(masterValue);

            if (isEdit)
            {
                // Update
                await _masterData.UpdateMasterValueAsync(
                    masterDataValue.PartitionKey,
                    masterDataValue.RowKey,
                    masterDataValue);
            }
            else
            {
                // Insert
                masterDataValue.RowKey = Guid.NewGuid().ToString();
                masterDataValue.CreatedBy = HttpContext.User.GetCurrentUserDetails().Name;

                await _masterData.InsertMasterValueAsync(masterDataValue);
            }

            return Json(true);
        }

        // ================= EXCEL =================

        private async Task<List<MasterDataValue>> ParseMasterDataExcel(IFormFile excelFile)
        {
            var masterValueList = new List<MasterDataValue>();

            using (var memoryStream = new MemoryStream())
            {
                await excelFile.CopyToAsync(memoryStream);

                using (ExcelPackage package = new ExcelPackage(memoryStream))
                {
                    //ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                    OfficeOpenXml.ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
                    ExcelWorksheet worksheet = package.Workbook.Worksheets[0];
                    int rowCount = worksheet.Dimension.Rows;

                    for (int row = 2; row <= rowCount; row++)
                    {
                        var masterDataValue = new MasterDataValue();

                        masterDataValue.RowKey = Guid.NewGuid().ToString();
                        masterDataValue.PartitionKey = worksheet.Cells[row, 1].Value.ToString();
                        masterDataValue.Name = worksheet.Cells[row, 2].Value.ToString();
                        masterDataValue.IsActive =
                            Boolean.Parse(worksheet.Cells[row, 3].Value.ToString());

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