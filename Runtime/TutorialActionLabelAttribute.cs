using System;

namespace SiPVLib.Tutorial
{
    /// <summary>
    /// Decorates a <see cref="SiPVLib.Tutorial.Config.TutorialAction"/> subclass with a
    /// display name and hex color used by the Tutorial Graph editor.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class TutorialActionLabelAttribute : Attribute
    {
        public string Name     { get; }
        public string HexColor { get; }

        public TutorialActionLabelAttribute(string name, string hexColor = "#6A9153")
        {
            Name     = name;
            HexColor = hexColor;
        }
    }
}

