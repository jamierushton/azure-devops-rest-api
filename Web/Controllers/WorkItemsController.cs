using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Web.Application;

namespace Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WorkItemsController : ControllerBase
    {
        private readonly AzureDevOps _azureDevOps;

        public WorkItemsController(AzureDevOps azureDevOps)
        {
            _azureDevOps = azureDevOps;
        }

        [HttpGet("types/{workItemType?}")]
        public async Task<IActionResult> Get(string workItemType = null)
        {
            var resp = await _azureDevOps.GetWorkItemTypesAsync(workItemType);
            return Ok(resp);
        }

        [HttpGet("{workItemId}")]
        public async Task<IActionResult> Get(int workItemId)
        {
            var resp = await _azureDevOps.GetWorkItemAsync(workItemId);
            return Ok(resp);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] CreateWorkItem req)
        {
            var resp = await _azureDevOps.CreateWorkItemAsync(req);
            return Ok(resp);
        }

        [HttpPost("{workItemId}/attachments/{fileName}")]
        public async Task<IActionResult> UploadAttachment(int workItemId, string fileName, [FromBody] string base64)
        {
            if (!Path.HasExtension(fileName))
                throw new ArgumentException("FileName does not have an extension", nameof(fileName));

            var byteArray = Convert.FromBase64String(base64);
            
            using (Stream stream = new MemoryStream(byteArray))
            {
                var resp = await _azureDevOps.AddAttachmentAsync(workItemId, fileName, stream);
                return Ok(resp);
            }
        }
    }
}