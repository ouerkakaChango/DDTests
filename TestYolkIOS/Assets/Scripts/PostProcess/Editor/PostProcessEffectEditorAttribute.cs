using System;

namespace CenturyGame.PostProcessEditor
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class PostProcessEffectEditorAttribute : Attribute
    {
        public readonly Type effectType;

        public PostProcessEffectEditorAttribute(Type type)
        {
            this.effectType = type;
        }
    }
}
