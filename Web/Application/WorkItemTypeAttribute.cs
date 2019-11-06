using System;

namespace Web.Application
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class WorkItemTypeAttribute : Attribute
    {
        public WorkItemTypeAttribute(string type)
        {
            Type = type;
        }

        public string Type { get; }
    }


}