using System;

namespace SiPVLib.Tutorial
{
    /// <summary>
    /// Decorates a <see cref="SiPVLib.Tutorial.Config.TutorialNodeTargetCondition"/> subclass with a
    /// display name and hex color used by the Tutorial Graph editor port labels.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class TutorialConditionLabelAttribute : Attribute
    {
        public string Name     { get; }
        public string HexColor { get; }

        public TutorialConditionLabelAttribute(string name, string hexColor = "#FF8C00")
        {
            Name     = name;
            HexColor = hexColor;
        }
    }
}

