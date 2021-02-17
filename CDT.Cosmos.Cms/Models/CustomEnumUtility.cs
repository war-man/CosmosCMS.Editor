using System;
using System.Linq;

namespace CDT.Cosmos.Cms.Models
{
    public static class CustomEnumUtility
    {
        /// To use this extantion method, the enum need to have CustomEnumAttribute with CustomEnumAttribute(true)
        public static string TextValue(this Enum myEnum)
        {
            string value = string.Empty;
            /*Check : if the myEnum is a custom enum*/
            var customEnumAttribute = (CustomEnumAttribute)myEnum
                .GetType()
                .GetCustomAttributes(typeof(CustomEnumAttribute), false)
                .FirstOrDefault();

            if (customEnumAttribute == null)
            {
                throw new Exception("The enum doesn't contain CustomEnumAttribute");
            }
            else if (customEnumAttribute.IsCustomEnum == false)
            {
                throw new Exception("The enum is not a custom enum");
            }

            /*Get the TextValueAttribute*/
            var textValueAttribute = (TextValueAttribute)myEnum
                .GetType().GetMember(myEnum.ToString()).Single()
                .GetCustomAttributes(typeof(TextValueAttribute), false)
                .FirstOrDefault();
            value = (textValueAttribute != null) ? textValueAttribute.Value : string.Empty;
            return value;
        }

        [AttributeUsage(AttributeTargets.Enum)]
        public class CustomEnumAttribute : Attribute
        {
            public bool IsCustomEnum { get; set; }
            public CustomEnumAttribute(bool isCustomEnum) : this()
            {
                IsCustomEnum = isCustomEnum;
            }

            private CustomEnumAttribute()
            {
                IsCustomEnum = false;
            }
        }

        [AttributeUsage(AttributeTargets.Field)]
        public class TextValueAttribute : CustomEnumAttribute
        {
            public String Value { get; set; }
            public TextValueAttribute(string textValue) : this()
            {
                if (textValue == null)
                {
                    throw new NullReferenceException("Null not allowed in textValue at TextValue attribute");
                }
                Value = textValue;
            }

            private TextValueAttribute() : base(true)
            {
                Value = string.Empty;
            }
        }
    }
}