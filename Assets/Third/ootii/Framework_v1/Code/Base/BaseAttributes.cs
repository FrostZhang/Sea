using System;
using com.ootii.Helpers;

namespace com.ootii.Base
{
    /// <summary>
    /// Defines the tooltip value for properties
    /// </summary>
    public class BaseNameAttribute : Attribute
    {
        /// <summary>
        /// Default tooltip value
        /// </summary>
        protected string mValue;
        public string Value
        {
            get { return mValue; }
        }

        /// <summary>
        /// Constructor for the attribute
        /// </summary>
        /// <param name="rValue">Value that is the tooltip</param>
        public BaseNameAttribute(string rValue)
        {
            mValue = rValue;
        }

        /// <summary>
        /// Attempts to find a fiendly name. If found, returns it.
        /// </summary>
        /// <param name="rType">Type whose friendly name we want</param>
        /// <returns>String that is the friendly name for class name</returns>
        public static string GetName(Type rType)
        {
            string lTypeName = rType.Name;

            BaseNameAttribute lAttribute = ReflectionHelper.GetAttribute<BaseNameAttribute>(rType);
            if (lAttribute != null) { lTypeName = lAttribute.Value; }

            return lTypeName;
        }
    }

    /// <summary>
    /// Defines the tooltip value for properties
    /// </summary>
    public class BaseDescriptionAttribute : Attribute
    {
        /// <summary>
        /// Default tooltip value
        /// </summary>
        protected string mValue;
        public string Value
        {
            get { return mValue; }
        }

        /// <summary>
        /// Constructor for the attribute
        /// </summary>
        /// <param name="rValue">Value that is the tooltip</param>
        public BaseDescriptionAttribute(string rValue)
        {
            this.mValue = rValue;
        }

        /// <summary>
        /// Attempts to find description.
        /// </summary>
        /// <param name="rType">Type whose description we want</param>
        /// <returns>String that is the description</returns>
        public static string GetDescription(Type rType)
        {
            string lDescription = "";

            BaseDescriptionAttribute lAttribute = ReflectionHelper.GetAttribute<BaseDescriptionAttribute>(rType);
            if (lAttribute != null) { lDescription = lAttribute.Value; }

            return lDescription;
        }
    }
}
