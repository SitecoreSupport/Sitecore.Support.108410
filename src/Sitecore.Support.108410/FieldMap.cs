namespace Sitecore.Support.ContentSearch
{
    using Sitecore.ContentSearch;
    using Sitecore.Data.Fields;
    using Sitecore.Diagnostics;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.CompilerServices;

    public class FieldMap : Sitecore.ContentSearch.FieldMap, IFieldMap, IFieldMapReaders
    {
        protected readonly FieldInfo fieldNameMapInfo = typeof(Sitecore.ContentSearch.FieldMap).GetField("fieldNameMap", BindingFlags.NonPublic | BindingFlags.Instance);
        protected readonly FieldInfo fieldTypeNameMapInfo = typeof(Sitecore.ContentSearch.FieldMap).GetField("fieldTypeNameMap", BindingFlags.NonPublic | BindingFlags.Instance);

        public virtual AbstractSearchFieldConfiguration GetFieldConfiguration(Type returnType)
        {

            Assert.ArgumentNotNull(returnType, "returnType");
            var source = (from arg in (Dictionary<string, AbstractSearchFieldConfiguration>)this.fieldTypeNameMapInfo.GetValue(this)
                          select new
                          {
                              Type = FieldTypeManager.GetFieldType(arg.Key)?.Type ?? Type.GetType(arg.Key, false, true),
                              Value = arg.Value
                          } into arg
                          where arg.Type != null
                          select arg).ToList();

            Type lookupType = returnType;
            while (lookupType != null && lookupType != typeof(object))
            {
                var result = source.FirstOrDefault(arg => arg.Type == lookupType);

                if (result != null)
                    return result.Value;

                lookupType = lookupType.BaseType;
            }
           

            {
                var result = source.FirstOrDefault(arg => arg.Type.IsAssignableFrom(lookupType));

                if (result != null)
                    return result.Value;
            }

            return null;
        }


        public virtual AbstractSearchFieldConfiguration GetFieldConfiguration(IIndexableDataField field, Func<AbstractSearchFieldConfiguration, bool> fieldVisitorFunc)
        {
            AbstractSearchFieldConfiguration fieldConfiguration;
            Dictionary<string, AbstractSearchFieldConfiguration> dictionary = (Dictionary<string, AbstractSearchFieldConfiguration>)this.fieldTypeNameMapInfo.GetValue(this);
            Dictionary<string, AbstractSearchFieldConfiguration> dictionary2 = (Dictionary<string, AbstractSearchFieldConfiguration>)this.fieldNameMapInfo.GetValue(this);
            Assert.ArgumentNotNull(field, "field");
            if ((!string.IsNullOrEmpty(field.Name) && dictionary2.TryGetValue(field.Name.ToLower(), out fieldConfiguration)) && fieldVisitorFunc(fieldConfiguration))
            {
                return fieldConfiguration;
            }
            if (dictionary.TryGetValue(field.TypeKey, out fieldConfiguration) && fieldVisitorFunc(fieldConfiguration))
            {
                return fieldConfiguration;
            }
            Type returnType = Type.GetType(field.TypeKey, false, true);
            if (returnType != null)
            {
                fieldConfiguration = this.GetFieldConfiguration(returnType);
                if ((fieldConfiguration != null) && fieldVisitorFunc(fieldConfiguration))
                {
                    return fieldConfiguration;
                }
            }
            if (field.FieldType != null)
            {
                fieldConfiguration = this.GetFieldConfiguration(field.FieldType);
                if ((fieldConfiguration != null) && fieldVisitorFunc(fieldConfiguration))
                {
                    return fieldConfiguration;
                }
            }
            return null;
        }

        AbstractSearchFieldConfiguration  IFieldMap.GetFieldConfiguration(IIndexableDataField field) =>
            this.GetFieldConfiguration(field, f => true);


}
}
