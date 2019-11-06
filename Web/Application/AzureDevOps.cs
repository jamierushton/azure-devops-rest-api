using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;

namespace Web.Application
{
    public class CreateWorkItem
    {
        public string WorkItemType { get; set; }
        public WorkItemField[] Fields { get; set; }

        public class WorkItemField
        {
            public string ReferenceName { get; set; }
            public string Value { get; set; }
        }
    }

    public class AzureDevOps : IDisposable
    {
        private readonly VssConnection _connection;
        private readonly WorkItemTrackingHttpClient _httpClient;

        public AzureDevOps(string team, string pat, string project)
        {
            Team = team;
            Project = project;
            Url = new Uri($"https://dev.azure.com/{team}");

            var credentials = new VssBasicCredential(string.Empty, pat);
            _connection = new VssConnection(Url, credentials);

            _httpClient = _connection.GetClient<WorkItemTrackingHttpClient>();
        }

        public string Team { get; }
        public string Project { get; }
        public Uri Url { get; }

        /// <summary>
        /// Returns a collection of work item types.  If <param name="workItemType">workItemType</param> is given, returns a single work item type.
        /// </summary>
        /// <param name="workItemType"></param>
        /// <returns></returns>
        public async Task<Dictionary<string, IEnumerable<string>>> GetWorkItemTypesAsync(string workItemType)
        {
            IEnumerable<WorkItemType> types;
            if (workItemType == null)
                types = await _httpClient.GetWorkItemTypesAsync(Project);
            else
                types = new[] { await _httpClient.GetWorkItemTypeAsync(Project, workItemType) };

            return types.ToDictionary(x => x.Name, x => x.Fields.Select(f => f.ReferenceName));
        }

        /// <summary>
        /// Returns a single work item.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<WorkItem> GetWorkItemAsync(int id)
        {
            return await _httpClient.GetWorkItemAsync(id, expand: WorkItemExpand.Relations);
        }

        /// <summary>
        /// Upload an attachment.
        /// </summary>
        /// <param name="workItemId"></param>
        /// <param name="fileName"></param>
        /// <param name="stream"></param>
        /// <returns></returns>
        public async Task<WorkItem> AddAttachmentAsync(int workItemId, string fileName, Stream stream)
        {
            var resourceSize = stream.Length;

            var reference = await _httpClient.CreateAttachmentAsync(stream, project: Project);

            var patchDocument = new JsonPatchDocument
            {
                new JsonPatchOperation
                {
                    Operation = Operation.Add,
                    Path = "/relations/-",
                    Value = new
                    {
                        rel = "AttachedFile",
                        url = reference.Url,
                        attributes = new
                        {
                            name = fileName,
                            resourceSize
                        }
                    }
                }
            };

            return await _httpClient.UpdateWorkItemAsync(patchDocument, workItemId);
        }

        /// <summary>
        /// Creates a single work item.
        /// </summary>
        /// <param name="workItem"></param>
        /// <returns></returns>
        public async Task<WorkItem> CreateWorkItemAsync(CreateWorkItem workItem)
        {
            var patchDocument = new JsonPatchDocument();
            patchDocument.AddRange(from field in workItem.Fields
                                   select new JsonPatchOperation
                                   {
                                       Operation = Operation.Add,
                                       Path = $"/fields/{field.ReferenceName}",
                                       Value = field.Value
                                   });

            return await _httpClient.CreateWorkItemAsync(patchDocument, Project, workItem.WorkItemType);
        }

        /// <summary>
        /// Creates a single work item using an object decorated with the <see cref="WorkItemTypeAttribute"/> and <see cref="WorkItemFieldAttribute"/> attributes.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="workItem"></param>
        /// <returns></returns>
        public async Task<WorkItem> CreateWorkItemAsync<T>(T workItem)
        {
            var type = typeof(T);
            var typeName = type.Name;

            var workItemTypeAttr = type.GetCustomAttribute<WorkItemTypeAttribute>();
            if (workItemTypeAttr == null)
                throw new Exception($"{typeName} has not been decorated with {nameof(WorkItemTypeAttribute)}.");

            var properties = type.GetProperties()
                                 .Where(prop => prop.IsDefined(typeof(WorkItemFieldAttribute), false))
                                 .ToList();

            if (!properties.Any())
                throw new Exception($"{typeName} has no properties decorated with the attribute {nameof(WorkItemFieldAttribute)}.");

            var patchDocument = new JsonPatchDocument();
            patchDocument.AddRange(from prop in properties
                                   let attr = prop.GetCustomAttribute<WorkItemFieldAttribute>()
                                   let value = prop.GetValue(workItem)
                                   where value != null
                                   select new JsonPatchOperation
                                   {
                                       Operation = Operation.Add,
                                       Path = $"/fields/{attr.Name}",
                                       Value = value
                                   });

            return await _httpClient.CreateWorkItemAsync(patchDocument, Project, workItemTypeAttr.Type);
        }

        #region IDisposable Support

        private bool _disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    if (_connection != null)
                    {
                        _connection.Disconnect();
                        _connection.Dispose();
                    }

                    _httpClient?.Dispose();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion
    }
}