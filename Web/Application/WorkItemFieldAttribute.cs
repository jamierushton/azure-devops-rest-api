using System;

namespace Web.Application
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class WorkItemFieldAttribute : Attribute
    {
        public WorkItemFieldAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }
}